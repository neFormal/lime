using System;
using FluentMigrator.Runner.VersionTableInfo;

namespace Migrations
{
    [VersionTableMetaData]
    public class VersionTable : IVersionTableMetaData
    {
        public virtual string ColumnName
        {
            get { return "version"; }
        }

        public virtual string SchemaName
        {
            get { return "public"; }
        }

        public virtual string TableName
        {
            get { return "versions"; }
        }

        public virtual string UniqueIndexName
        {
            get { return "uc_version"; }
        }

        public virtual string AppliedOnColumnName
        {
            get { return "created_at"; }
        }

        public virtual string DescriptionColumnName
        {
            get { return "desc"; }
        }
        
        public virtual bool OwnsSchema { get => false; }

        [Obsolete("Use dependency injection to get the IRunnerContext")]
        public object ApplicationContext { get; set; }
    }
}
