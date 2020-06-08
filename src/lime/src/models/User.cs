namespace lime.user.models
{
    using System;
    using System.Collections.Generic;
    using Dapper.Contrib.Extensions;

    [Table("users")]
    class User
    {
        [Key]
        public long id { get; set; }
        public string name { get; set; }
        public DateTime created_at { get; set; }
        public DateTime login_at { get; set; }
        public int exp { get; set; }
        public int level { get; set; }

        public User()
        {
            name = "user";
            exp = 0;
            level = 1;
        }

        [Write(false)]
        [Computed]
        public Dictionary<string, Item> Items { get; set; }
    }
}
