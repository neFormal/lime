namespace lime.user.models
{
    using Dapper.Contrib.Extensions;
    
    [Table("auth_device")]
    class AuthDevice
    {
        [ExplicitKey]
        public string id { get; set; }
        public long? user_id { get; set; }
    }
}
