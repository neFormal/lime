namespace lime.user
{
    using System;
    using System.Security.Cryptography;
    using System.Text;
    using Akka.Actor;
    using Akka.Cluster;
    using Akka.DistributedData;
    using Dapper;
    using Dapper.Contrib.Extensions;
    using protobuf.user;
    using models;

    partial class TcpConnection
    {
        public void Process(Auth request)
        {
            User user = null;

            Db.WithTransaction(GetShardId(request.Id), conn =>
            {
                var auth = conn.QueryFirstOrDefault<AuthDevice>(@"select * from auth_device where id=@Id", new { Id = request.Id});
                if (auth == null)
                {
                    auth = new AuthDevice { id = request.Id };
                    conn.Insert(auth);
                }

                if (auth.user_id != null)
                {
                    user = conn.QueryFirstOrDefault<User>(@"select * from users where id = @Id", new { Id = auth.user_id});
                }

                if (user == null)
                {
                    user = new User();
                    user.created_at = DateTime.UtcNow;
                    user.login_at = DateTime.UtcNow;
                    long userId = conn.Insert(user);
                    
                    auth.user_id = userId;
                    user.name = $"User#{userId}";
                    conn.Update(auth);
                }
                else
                {
                    user.login_at = DateTime.UtcNow;
                    conn.Update(user);
                }
            });

            this.state.User = user;
            this.state.Auth = true;

            RegisterUserId();

            Write(new Response
            {
                Type = MsgType.Auth,
                Auth = new Auth.Types.Response
                {
                    Status = Auth.Types.Response.Types.Status.Success,
                    Profile = BuildProfile(user)
                }
            });
        }

        private Profile BuildProfile(User user)
        {
            var profile = new Profile
            {
                Id = user.id,
                Name = user.name,
                LevelData = new Profile.Types.LevelData { Exp = user.exp, Level = user.level}
            };

            profile.Items.Add(new protobuf.user.Item {Type = "money", Count = 100 });

            return profile;
        }

        private async void RegisterUserId()
        {
            var userId = state.User.id;

            var cluster = Cluster.Get(Context.System);
            var replicator = DistributedData.Get(Context.System).Replicator;

            var key = new LWWDictionaryKey<long, IActorRef>("users");

            var dict = LWWDictionary<long, IActorRef>.Empty;
            dict = dict.SetItem(cluster, userId, Self);

            var writeConsistency = new WriteAll(TimeSpan.FromSeconds(5));
            var result = await replicator.Ask<IUpdateResponse>(
                Akka.DistributedData.Dsl.Update(key, dict, writeConsistency, old => old.Merge(dict))
            );

            if (result.IsSuccessful)
            {
                var data = result.Key;
                Log.Info($"write success: {data}");
            }
            else
            {
                Log.Info("write fail");
            }
        }

        private async void UnregisterUserId()
        {
            var userId = state.User.id;

            var cluster = Cluster.Get(Context.System);
            var replicator = DistributedData.Get(Context.System).Replicator;

            var key = new LWWDictionaryKey<long, IActorRef>("users");
            var writeConsistency = new WriteAll(TimeSpan.FromSeconds(5));

            var result = await replicator.Ask<IDeleteResponse>(Akka.DistributedData.Dsl.Delete(key, writeConsistency));
            if (result.IsSuccessful)
            {
                Log.Info($"delete: {result.AlreadyDeleted}");
            }
            else
            {
                Log.Info("delete fail");
            }
        }

        private Db.Shard GetShardId(string id)
        {
            var bytes = Encoding.UTF8.GetBytes(id);
            var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(id));

            int shardId = BitConverter.ToInt32(hash, 0) % ((int)Db.Shard.users_shards_count);
            return (Db.Shard)shardId;
        }
    }
}
