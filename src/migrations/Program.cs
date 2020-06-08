using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;

namespace Migrations
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = CreateServices();

            using (var scope = serviceProvider.CreateScope())
            {
                var runner = serviceProvider.GetRequiredService<IMigrationRunner>();

                ParseArgs(args,
                    upAction: (_) => runner.MigrateUp(),
                    downAction: count => runner.Rollback(count)
                );
            }
        }

        private static IServiceProvider CreateServices()
        {
            // TODO: use config instead of this
            var connectionString = "Host=localhost;Username=user;Password=password;Database=lime";

            return new ServiceCollection()
                // Add common FluentMigrator services
                .AddFluentMigratorCore()
                .ConfigureRunner(rb => rb
                    .AddPostgres()
                    .WithGlobalConnectionString(connectionString)
                    .WithVersionTable(new VersionTable())
                    .ScanIn(typeof(Program).Assembly).For.Migrations())
                // Enable logging to console in the FluentMigrator way
                .AddLogging(lb => lb.AddFluentMigratorConsole())
                .BuildServiceProvider(false);
        }

        private static void ParseArgs(string[] args, Action<int?> upAction = null, Action<int> downAction = null)
        {
            RootCommand root = new RootCommand
            {
                Name = "migration",
                Description = "migration tool"
            };

            //
            var up = new Command("up")
            {
                new Argument
                {
                    Name = "count",
                    ArgumentType = typeof(int),
                    Arity = ArgumentArity.ZeroOrOne
                }
            };
            up.Handler = CommandHandler.Create<int?>(upAction);
            root.AddCommand(up);

            //
            var down = new Command("down")
            {
                new Argument
                {
                    Name = "count",
                    ArgumentType = typeof(int),
                    Arity = ArgumentArity.ExactlyOne
                }
            };
            down.Handler = CommandHandler.Create<int>(downAction);
            root.AddCommand(down);

            root.Invoke(args);
        }
    }
}
