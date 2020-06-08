using System;
using System.Collections.Generic;
using System.IO;
using Akka;
using Akka.Actor;
using Akka.Event;
using Akka.IO;
using Akka.Util.Internal;
using Google.Protobuf;
using protobuf.user;
using ByteString = Akka.IO.ByteString;

namespace lime.user
{
    using lime.user.models;

    public partial class TcpConnection : UntypedActor
    {
        private struct State
        {
            public bool Auth { get; set; }
            public User User { get; internal set; }
        }

        public ILoggingAdapter Log { get; } = Context.GetLogger();

        private readonly IActorRef socket;
        private State state = new State { Auth = false };

        public TcpConnection(IActorRef socket)
        {
            this.socket = socket;
        }

        private const int sizeLength = 2;
        private const int packageSize = 4096;
        private MemoryStream buffer = new MemoryStream(packageSize * 3);
        private byte[] readBuffer = new byte[packageSize];
        protected override void OnReceive(object msg) => msg.Match()
            .With<Tcp.Connected>(c => {
                Log.Info("client connected");
            })
            .With<Tcp.Received>(m => {
                var msg = m as Tcp.Received;
                Log.Info($"msg: {msg.Data.Count}");
                
                if (msg.Data.Count >= buffer.Capacity - buffer.Length)
                    throw new Exception("out of buffer");

                msg.Data.WriteTo(buffer);

                var requests = new LinkedList<Request>();
                buffer.Seek(0, SeekOrigin.Begin);

                while (buffer.Length - buffer.Position > sizeLength)
                {
                    byte[] sizeBuffer = new byte[sizeLength];
                    buffer.Read(sizeBuffer, 0, sizeLength);
                
                    ushort size = BitConverter.ToUInt16(sizeBuffer);

                    if (size > buffer.Length - buffer.Position)
                    {
                        buffer.Seek(-sizeLength, SeekOrigin.Current);

                        Log.Info("break");
                        break;
                    }
                
                    MemoryStream dataStream = buffer.ReadWithBuffer(readBuffer, size);
            
                    try
                    {
                        var request = Request.Parser.ParseFrom(dataStream);
                        if (request == null)
                            throw new Exception("empty request");

                        requests.AddLast(request);
                    }
                    catch (InvalidProtocolBufferException e)
                    {
                        Log.Error(e.Message);
                        throw;
                    }
                }
                buffer.Compact();
                
                requests.ForEach(r => Process(r));
            })
            .With<Tcp.ConnectionClosed>(_ =>
            {
                Log.Info("close connection");
                if (state.Auth)
                    UnregisterUserId();
            })
            .Default(Unhandled);
        
        private void Process(Request request)
        {
            switch (request.Type)
            {
                case MsgType.PingPong:
                {
                    Write(new Response() {Type = MsgType.PingPong, PingPong = new Pong() { Time = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds() }});
                    break;
                }
                case MsgType.Auth:
                {
                    this.Process(request.Auth);
                    break;
                }
                case MsgType t when state.Auth == false:
                {
                    Log.Error($"not authorized message: {t}");
                    break;
                }
                default:
                    throw new Exception("unhandled request");
            }
        }

        private void Write(Response response)
        {
            int calcSize = response.CalculateSize();
            var stream = new MemoryStream(sizeLength + calcSize);
            stream.Write(BitConverter.GetBytes((ushort)calcSize), 0, sizeLength);
            response.WriteTo(stream);

            socket.Tell(Tcp.Write.Create(
                ByteString.FromBytes(stream.GetBuffer(), 0, stream.Capacity)
            ));
        }
    }

    static class StreamExtension
    {
        public static void Compact(this MemoryStream stream)
        {
            byte[] buffer = stream.GetBuffer();
            long length = stream.Length - stream.Position;
            for (int i = 0; i < length; i++)
            {
                buffer[i] = buffer[stream.Position + i];
            }

            stream.Seek(0, SeekOrigin.Begin);
            stream.SetLength(length);
        }

        public static MemoryStream ReadWithBuffer(this MemoryStream stream, byte[] buffer, int count)
        {
            // var buffer = new byte[count];
            stream.Read(buffer, 0, count);
            return new MemoryStream(buffer, 0, count, false);
        }
    }
}
