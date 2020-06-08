using System;
using Akka;
using Akka.Actor;
using Akka.Event;

namespace lime
{
    using config;

    public class CrashSupervisorStrategy : SupervisorStrategyConfigurator
    {
        public static readonly OneForOneStrategy CrashStrategy = new OneForOneStrategy(ex => Directive.Escalate);
        public override SupervisorStrategy Create()
        {
            return CrashStrategy;
        }
    }

    class RootSup : UntypedActor
    {
        public ILoggingAdapter Log { get; } = Context.GetLogger();

        protected override void OnReceive(object message) => message.Match()
            .Default(Unhandled);

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(
                maxNrOfRetries: 3,
                withinTimeRange: TimeSpan.FromMinutes(1),
                localOnlyDecider: ex => Directive.Restart
            );
        }

        protected override void PreStart()
        {
            var listen = Config.System.Server;
            Context.ActorOf(Props.Create<TcpServer>(() => new TcpServer(listen.Ip, listen.Port)), "tcp_server");
            Context.ActorOf(Props.Create<ConfigWatcher>(() => new ConfigWatcher(Config.System.GameConfigsPath)), "config_watcher");
        }
    }
}
