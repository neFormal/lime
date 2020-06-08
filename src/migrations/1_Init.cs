using FluentMigrator;

namespace test
{
    [Migration(202001010101)]
    public class Init : Migration
    {
        public override void Up()
        {
            Create.Table("auth_device")
                .WithColumn("id").AsString(64).PrimaryKey()
                .WithColumn("user_id").AsInt64().Nullable();

            Create.Table("users")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("name").AsString(64).WithDefaultValue("user")
                .WithColumn("created_at").AsDateTime().WithDefault(SystemMethods.CurrentUTCDateTime)
                .WithColumn("login_at").AsDateTime().Nullable()
                .WithColumn("exp").AsInt32().WithDefaultValue(0)
                .WithColumn("level").AsInt32().WithDefaultValue(1);

            Create.Table("items")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("user_id").AsInt64()
                .WithColumn("type").AsFixedLengthString(96)
                .WithColumn("count").AsInt32().WithDefaultValue(0);
            
            Create.Index("uq_user_id_type")
                .OnTable("items")
                .WithOptions()
                .Unique().OnColumn("user_id")
                .Unique().OnColumn("type");
        }

        public override void Down()
        {
            Delete.Table("items");
            Delete.Table("users");
            Delete.Table("auth_device");
        }
    }
}
