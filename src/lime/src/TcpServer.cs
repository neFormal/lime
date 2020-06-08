using System;
using System.Net;
using Akka;
using Akka.Actor;
using Akka.Event;
using Akka.IO;
using lime.user;

namespace lime
{
    class TcpServer : UntypedActor
    {
        public ILoggingAdapter Log { get; } = Context.GetLogger();

        private string host;
        private int port;

        public TcpServer(string host, int port)
        {
            this.host = host;
            this.port = port;
        }

        protected override void PreStart()
        {
            Log.Info($"tcp server is starting at: {host}:{port}");
            Context.System.Tcp().Tell(new Tcp.Bind(Self, new IPEndPoint(IPAddress.Parse(host), port)));
        }

        protected override void PreRestart(Exception reason, object message)
        {
            listener?.Tell(Tcp.Unbind.Instance);
        }

        private IActorRef listener;
        protected override void OnReceive(object msg) => msg.Match()
            .With<Tcp.CommandFailed>(msg => throw new Exception(msg.ToString()))
            .With<Tcp.Bound>(bound =>
            {
                Log.Info($"bound at: {bound}");
                listener = Sender;
            })
            .With<Tcp.Connected>(c => {
                Log.Info("connected");
                var connection = Context.ActorOf(Props.Create(() => new TcpConnection(Sender)));
                Sender.Tell(new Tcp.Register(connection));
            })
            .With<Terminated>(msg =>
            {
                listener?.Tell(Tcp.Unbind.Instance);
                Context.Stop(Self);
            })
            .Default(Unhandled);
    }
}
