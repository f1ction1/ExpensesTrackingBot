using ExpenseBot.Api.Application.Abstractions;
using ExpenseBot.Api.Infrastructure.Persistence;
using ExpenseBot.Api.Infrastructure.Telegram;
using HealthChecks = Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Npgsql;
using Telegram.Bot;
using Telegram.Bot.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.ConfigureTelegramBotMvc();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMediatR(configuration =>
    configuration.RegisterServicesFromAssemblyContaining<Program>());

builder.Services
    .AddOptions<TelegramOptions>()
    .Bind(builder.Configuration.GetSection(TelegramOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.Token), "Telegram token is required.")
    .Validate(
        options => builder.Environment.IsDevelopment() || !string.IsNullOrWhiteSpace(options.WebhookSecret),
        "Telegram webhook secret is required outside Development.")
    .ValidateOnStart();

builder.Services
    .AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName));

var connectionString = builder.Configuration.GetConnectionString("Postgres");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'Postgres' is missing.");
}

builder.Services.AddSingleton(_ => new NpgsqlDataSourceBuilder(connectionString).Build());

builder.Services.AddPooledDbContextFactory<ExpenseBotDbContext>((serviceProvider, options) =>
    options.UseNpgsql(
        serviceProvider.GetRequiredService<NpgsqlDataSource>(),
        npgsql => npgsql.EnableRetryOnFailure()));

builder.Services.AddSingleton<ITelegramContextService, TelegramContextService>();
builder.Services.AddSingleton<ITelegramUpdateGate, TelegramUpdateGate>();
builder.Services.AddSingleton<ITelegramInteractionService, TelegramInteractionService>();
builder.Services.AddHostedService<ProcessedTelegramUpdateCleanupService>();
builder.Services.AddSingleton<ITelegramBotClient>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<IOptions<TelegramOptions>>().Value;
    return new TelegramBotClient(options.Token);
});

builder.Services
    .AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("postgres", tags: ["ready"]);

var app = builder.Build();

app.UseExceptionHandler();

if (args.Any(argument => string.Equals(argument, "--migrate-only", StringComparison.OrdinalIgnoreCase)))
{
    await using (var migrationContext = await app.Services
                     .GetRequiredService<IDbContextFactory<ExpenseBotDbContext>>()
                     .CreateDbContextAsync())
    {
        await migrationContext.Database.MigrateAsync();
    }

    await app.DisposeAsync();
    return;
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var databaseOptions = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;

if (databaseOptions.ApplyMigrationsOnStartup)
{
    await using var dbContext = await app.Services
        .GetRequiredService<IDbContextFactory<ExpenseBotDbContext>>()
        .CreateDbContextAsync();
    await dbContext.Database.MigrateAsync();
}

app.MapHealthChecks("/health/live", new HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthChecks.HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});

app.MapControllers();
app.Run();

public partial class Program;
