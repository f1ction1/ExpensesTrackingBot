using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ExpenseBot.Api.Infrastructure.Persistence;

public sealed class ExpenseBotDbContextFactory : IDesignTimeDbContextFactory<ExpenseBotDbContext>
{
    public ExpenseBotDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Postgres");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'Postgres' is missing.");
        }

        var options = new DbContextOptionsBuilder<ExpenseBotDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ExpenseBotDbContext(options);
    }
}
