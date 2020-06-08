namespace Specs
{
    using System;
    using Akka.Actor;
    using Akka.TestKit.Xunit2;
    using Xbehave;
    using lime;
    using lime.config;
    using lime.user;
    using protobuf.user;

    public class AuthFeature : TestKit
    {
        public AuthFeature() : base(Helper.GetConfig())
        {
            Db.Clear(); // kludge
            Config.Load("config.yml", Log);
            Db.Init(Log);
        }

        new void Dispose()
        {
            base.Dispose();
            Db.Clear();
        }

        [Scenario]
        public void FirstLogin(IActorRef user, Request request, Response response)
        {
            "user".x(() => user = this.Sys.ActorOf(Props.Create(() => new TcpConnection(this.TestActor))) );
            "with first login by id".x(() =>
            {
                request = new Request() {Type = MsgType.Auth, Auth = new Auth() {Id = "u1"}};
                response = new Response()
                {
                    Type = MsgType.Auth,
                    Auth = new Auth.Types.Response()
                    {
                        Status = Auth.Types.Response.Types.Status.Success
                    }
                };

                user.Tell(this.MakeReceive(request));
            });
            "should get success response with profile".x(() =>
            {
                var answer = this.ExpectResponse(TimeSpan.FromSeconds(1));
                Xunit.Assert.Equal(MsgType.Auth, answer.Type);
            });
        }

        [Scenario]
        public void LoginWithPlatform()
        {
            "when login with google".pending();
            "when login with apple".pending();
        }
    }
}
