using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
using YamlDotNet.Serialization;

namespace lime
{
    using config;

    public class Program
    {
        private static ActorSystem actorSystem;

        public static ILoggingAdapter Log { get => actorSystem.Log; }

        static void Main(string[] args)
        {
            string akkaConfigFilename = "root.config";
            if (args.Length > 0)
                akkaConfigFilename = args[0];
            InitAkka(akkaConfigFilename);

            // TODO: read config path from args
            Config.Load("config.yml", Log);
            Db.Init(Log);

            actorSystem.ActorOf(Props.Create<RootSup>(), "root_sup");

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                // print tree
                var serializer = new SerializerBuilder().Build();
                var ex = (ExtendedActorSystem)actorSystem;
                // var system = GatherTree(ex.SystemGuardian);
                // Log.Info(serializer.Serialize(system));
                var user = GatherTree(ex.Guardian);
                Log.Info(serializer.Serialize(user));

                CoordinatedShutdown.Get(actorSystem).Run(CoordinatedShutdown.ClrExitReason.Instance);
                eventArgs.Cancel = true;
            };

            actorSystem.WhenTerminated.Wait();
        }

        private static void InitAkka(string configFilename)
        {
            string configText = File.ReadAllText(configFilename, Encoding.UTF8);
            var config = ConfigurationFactory.ParseString(configText);
            actorSystem = ActorSystem.Create("akka", config);
        }

        private static Tuple<string, object> GatherTree(IActorRef anchor)
        {
            Log.Info($"node: {anchor.Path}");
            Tuple<string, object> result = Tuple.Create(anchor.Path.ToString(),
                                                        new LinkedList<Tuple<string, object>>() as object);

            if (anchor is ActorRefWithCell refWithCell)
            {
                foreach (var c in refWithCell.Children)
                {
                    (result.Item2 as LinkedList<Tuple<string, object>>).AddLast(GatherTree(c));
                }
            }

            return result;
        }
    }
}
