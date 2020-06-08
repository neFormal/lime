using System;
using System.Collections.Generic;
using System.IO;
using Akka.Event;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace lime.config
{
    using data.game;
    public class Config
    {
        public static ILoggingAdapter Log { get; private set; }

        // TODO: make configs immutable
        public static data.System System { get; set; }
        public static data.game.Game Game { get; set; }

        public static void Load(string filename, ILoggingAdapter log)
        {
            Log = log;
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            System = deserializer.Deserialize<data.System>(File.ReadAllText(filename));
            Game = LoadGame(System.GameConfigsPath);
        }

        public static void Update()
        {
            Game = LoadGame(System.GameConfigsPath);
        }

        private static Game LoadGame(string path)
        {
            string basePath = Path.GetFullPath(path);
            
            Game game = new Game();

            foreach (var property in game.GetType().GetProperties())
            {
                var attributes = property.GetCustomAttributes(typeof(PathAttribute), false);
                if (attributes.Length == 0)
                {
                    continue;
                }

                var pathAttribute = attributes[0] as PathAttribute;
                Log.Info($"load config: {Path.GetFullPath(pathAttribute.Path, basePath)}");

                if (pathAttribute.SingleFile)
                {
                    var dummyType = typeof(Dummy<>).MakeGenericType(property.PropertyType);
                    var deserializer = new DeserializerBuilder()
                        .WithTagMapping("tag:unity3d.com,2011:114", dummyType)
                        .IgnoreUnmatchedProperties()
                        .Build();

                    var filename = Path.GetFullPath(pathAttribute.Path, basePath);

                    // workaround for temp edit files like '.#filename'
                    string text = "";
                    try { text = File.ReadAllText(filename);}
                    catch (FileNotFoundException) { continue; }

                    var dummy = deserializer.Deserialize(text, dummyType);
                    var monobeh = dummyType.GetProperty("MonoBehaviour").GetValue(dummy);
                    if (monobeh == null)
                    {
                        Log.Warning($"cant load {filename}");
                        continue;
                    }
                    property.SetValue(game, monobeh);
                }
                else
                {
                    var type = property.PropertyType.GenericTypeArguments[1];
                    var dummyType = typeof(Dummy<>).MakeGenericType(type);
                    var deserializer = new DeserializerBuilder()
                        .WithTagMapping("tag:unity3d.com,2011:114", dummyType)
                        .IgnoreUnmatchedProperties()
                        .Build();

                    var dictType = typeof(Dictionary<,>).MakeGenericType(typeof(string), type);
                    var dictInstance = Activator.CreateInstance(dictType);
                    foreach (var filename in Directory.GetFiles(Path.GetFullPath(pathAttribute.Path, basePath), "*.asset"))
                    {
                        var relativePath = filename.Split(separator: basePath, 2)[1];

                        // workaround for temp edit files like '.#filename'
                        string text = "";
                        try { text = File.ReadAllText(filename);}
                        catch (FileNotFoundException) { continue; }

                        var dummy = deserializer.Deserialize(text, dummyType);

                        var monobeh = dummyType.GetProperty("MonoBehaviour").GetValue(dummy);
                        if (monobeh == null)
                        {
                            Log.Warning($"cant load {filename}");
                            continue;
                        }

                        dictType.GetMethod("Add").Invoke(dictInstance, new []{relativePath, monobeh});
                    }
                    property.SetValue(game, dictInstance);
                }
            }

            return game;
        }

        class Dummy<T>
        {
            public T MonoBehaviour { get; set; }
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class PathAttribute : Attribute
    {
        public string Path { get; private set; }
        public bool SingleFile { get; private set; }
        public PathAttribute(string path, bool singleFile=false)
        {
            Path = path;
            SingleFile = singleFile;
        }
    }
}
