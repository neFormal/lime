namespace lime.user.models
{
    using Dapper.Contrib.Extensions;

    [Table("items")]
    class Item
    {
        [Key]
        public long Id { get; set; }
        public long UserId { get; set; }
        public string Type { get; set; }
        public int Count { get; set; }
    }
}
