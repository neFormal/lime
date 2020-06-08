namespace Specs
{
    using System;
    using System.IO;
    using System.Text;
    using Akka.Configuration;
    using Akka.IO;
    using Akka.TestKit;
    using Xbehave;
    using Xbehave.Sdk;
    using Google.Protobuf;
    using protobuf.user;
    using ByteString = Akka.IO.ByteString;

    public static class Helper
    {
        public static Response ExpectResponse(this TestKitBase testKit, TimeSpan? timeout = null)
        {
            var msg = testKit.ExpectMsg<Tcp.Write>(TimeSpan.FromSeconds(1));
            MemoryStream dataStream = new MemoryStream(msg.Data.ToArray(), 2, msg.Data.Count - 2);
            var result = Response.Parser.ParseFrom(dataStream);
            return result;
        }

        public static Tcp.Received MakeReceive(this TestKitBase testKit, Request request)
        {
            int calcSize = request.CalculateSize();
            var stream = new MemoryStream(2 + calcSize);
            stream.Write(BitConverter.GetBytes((ushort)calcSize), 0, 2);
            request.WriteTo(stream);
            return new Tcp.Received(ByteString.FromBytes(stream.ToArray()));
        }

        public static Config GetConfig()
        {
            string configText = File.ReadAllText("akka.config", Encoding.UTF8);
            return ConfigurationFactory.ParseString(configText);
        }

        public static IStepBuilder pending(this string text)
        {
            return text.x(() => {}).Skip("pending");
        }
    }
}
