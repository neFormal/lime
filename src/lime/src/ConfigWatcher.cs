using System;
using System.IO;
using Akka;
using Akka.Actor;
using Akka.Event;

namespace lime
{
    using config;

    class ConfigWatcher : UntypedActor
    {
        public ILoggingAdapter Log { get; } = Context.GetLogger();

        private FileSystemWatcher watcher;
        private string path;
        private const int delaySeconds = 3;
        private ICancelable scheduledMessage;

        public ConfigWatcher(string path)
        {
            this.path = Path.GetFullPath(path);
        }

        protected override void OnReceive(object message) => message.Match()
            .With<FileSystemEventArgs>(_ => Config.Update())
            .With<FileSystemEventArgs>(_ => Config.Update())
            .Default(Unhandled);

        protected override void PreStart()
        {
            var self = Self;
            var scheduler = Context.System.Scheduler;
            watcher = new FileSystemWatcher();
            watcher.Path = path;
            watcher.Filter = "*";
            watcher.IncludeSubdirectories = true;
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;

            var handler = new FileSystemEventHandler((source, args) =>
            {
                if (scheduledMessage != null)
                    scheduledMessage.CancelIfNotNull();
                scheduledMessage = scheduler.ScheduleTellOnceCancelable(TimeSpan.FromSeconds(delaySeconds), self, args, ActorRefs.NoSender);
            });
            var renameHandler = new RenamedEventHandler((source, args) =>
            {
                if (scheduledMessage != null)
                    scheduledMessage.CancelIfNotNull();
                scheduledMessage = scheduler.ScheduleTellOnceCancelable(TimeSpan.FromSeconds(delaySeconds), self, args, ActorRefs.NoSender);
            });

            watcher.Changed += handler;
            watcher.Deleted += handler;
            watcher.Created += handler;
            watcher.Renamed += renameHandler;
            watcher.EnableRaisingEvents = true;
        }

        protected override void PreRestart(Exception reason, object message)
        {
            if (watcher != null)
                watcher.EnableRaisingEvents = false;
        }
    }
}
