using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Npgsql;
using Akka.Event;

namespace lime
{
    public sealed class Db
    {
        public enum Shard : int
        {
            user_1 = 0,
            user_2 = 1,
            users_shards_count = 2,
            admin
        }

        public static ILoggingAdapter Log { get; private set; }
        private static Dictionary<Shard, string> shardsConnectionStrings = new Dictionary<Shard, string>();

        public static void WithConnection(Shard shard, Action<IDbConnection> action)
        {
            if (!shardsConnectionStrings.ContainsKey(shard))
                throw new ArgumentException($"shard ${shard} hasnt been initialized");

            using (var connection = new NpgsqlConnection(shardsConnectionStrings[shard]))
            {
                connection.Open();
                action(connection);
            }
        }

        public static void WithTransaction(Shard shard, Action<IDbConnection> action)
        {
            if (!shardsConnectionStrings.ContainsKey(shard))
                throw new ArgumentException($"shard ${shard} hasnt been initialized");

            using var connection = new NpgsqlConnection(shardsConnectionStrings[shard]);
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    action(connection);
                    transaction.Commit();
                }
                catch (Exception e)
                {
                    Log.Error($"transaction error: {e.GetType()}");
                    Log.Error(e.Message);

                    try
                    {
                        transaction.Rollback();
                    }
                    catch (Exception rollbackError)
                    {
                        Log.Error($"rollbackError: {rollbackError.GetType()}");
                        Log.Error(rollbackError.Message);
                    }
                }
            }
        }

        public static void Init(ILoggingAdapter log)
        {
            Log = log;
            foreach (Shard shardValue in Enum.GetValues(typeof(Shard)))
            {
                var shardName = Enum.GetName(typeof(Shard), shardValue);

                config.data.System.DbShard shard;
                if (config.Config.System.Shards.TryGetValue(shardName, out shard))
                {
                    NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder();
                    builder.Host = shard.Host;
                    builder.Username = shard.User;
                    builder.Password = shard.Password;
                    builder.Database = shard.Database;
                    builder.MinPoolSize = shard.MinPoolSize;
                    builder.MaxPoolSize = shard.MaxPoolSize;
                    builder.ConnectionIdleLifetime = shard.ConnectionIdleLifetime;

                    shardsConnectionStrings.Add(shardValue, builder.ToString());
                }
                else
                {
                    Log.Warning($"shard not found in config: {shardName}");
                }
            }

            Task.Run(async () =>
            {
                foreach (var cs in shardsConnectionStrings.Values)
                {
                    await using var conn = new NpgsqlConnection(cs);
                    await conn.OpenAsync();
                }
            })
            .Wait();

            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true; // WTF
        }

        public static void Clear()
        {
            shardsConnectionStrings.Clear();
            NpgsqlConnection.ClearAllPools();
        }
    }
}
