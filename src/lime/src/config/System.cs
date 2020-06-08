namespace lime.config.data
{
    using global::System.Collections.Generic;

    public class System
    {
        public class TcpServer
        {
            public string Ip { get; set; }
            public int Port { get; set; }
        }

        public class DbShard
        {
            public string Host { get; set; }
            public string User { get; set; }
            public string Password { get; set; }
            public string Database { get; set; }
            public int MinPoolSize { get; set; }
            public int MaxPoolSize { get; set; }
            public int ConnectionIdleLifetime { get; set; }
        }

        public TcpServer Server { get; set; }
        public Dictionary<string, DbShard> Shards { get; set; }
        public string GameConfigsPath { get; set; }
    }
}
