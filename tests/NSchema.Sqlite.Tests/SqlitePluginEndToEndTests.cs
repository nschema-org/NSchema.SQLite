using NSchema.Configuration;
using NSchema.Operations.Apply;
using NSchema.Operations.Plan;
using NSchema.Sql.Model;
using NSchema.Sqlite.Tests.Fixtures;

namespace NSchema.Sqlite.Tests;

/// <summary>
/// End-to-end proof that the <see cref="SqlitePlugin"/> manifest wires a fully working provider: it runs a real
/// migration THROUGH the plugin's <c>Configure</c> (not the direct <c>UseSqliteSchema</c> API) against a real (temp
/// file) SQLite database, then re-introspects to confirm the schema was applied. In-process — no Docker.
/// </summary>
public sealed class SqlitePluginEndToEndTests : SqliteTestBase
{
    /// <summary>
    /// The plugin lets <c>NSCHEMA_SQLITE_CONNECTION_STRING</c> override the configured connection string (by design).
    /// If that variable is set in the host environment (Rider run-config, CI, a stray <c>launchctl setenv</c>), the
    /// apply would target a different database than <see cref="SqliteTestBase.Introspect"/> reads, leaving the
    /// assertion's live schema empty. Pinning it to the test's own connection string makes the test hermetic.
    /// </summary>
    private const string EnvConnectionString = "NSCHEMA_SQLITE_CONNECTION_STRING";

    [Fact]
    public async Task Apply_ThroughThePlugin_CreatesTheDesiredSchema()
    {
        // Arrange — a desired schema on disk (under SQLite's built-in `main` schema), configured ONLY via the plugin.
        var projectDirectory = Directory.CreateTempSubdirectory("nschema-sqlite-e2e-").FullName;
        var priorEnvConnectionString = Environment.GetEnvironmentVariable(EnvConnectionString);
        Environment.SetEnvironmentVariable(EnvConnectionString, ConnectionString);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(projectDirectory, "schema.sql"), """
                CREATE TABLE main.widgets (
                  id   bigint NOT NULL,
                  name text,
                  CONSTRAINT widgets_pkey PRIMARY KEY (id)
                );
                """, TestContext.Current.CancellationToken);

            var builder = NSchemaApplication.CreateBuilder();
            var configured = new SqlitePlugin().Configure(builder, new ConfigBlock("provider", "sqlite", new Dictionary<string, ConfigValue>
            {
                ["connection_string"] = ConfigValue.OfString(ConnectionString),
            }));
            configured.Succeeded.ShouldBeTrue();

            builder.AddDdlSchemas(projectDirectory);
            using var app = builder.Build();

            // Act — a real plan + apply through the plugin-wired provider.
            var planResult = await app.Operations.Plan(new PlanArguments { Schemas = ["main"], Target = PlanTarget.Live }, TestContext.Current.CancellationToken);
            planResult.IsSuccess.ShouldBeTrue();
            await app.Operations.Apply(new ApplyArguments { Sql = planResult.Value!.Sql ?? new SqlPlan([]) }, TestContext.Current.CancellationToken);

            // Assert — the table really exists, read back via a fresh introspection.
            var live = await Introspect();
            live.Schemas.ShouldHaveSingleItem().Tables.ShouldHaveSingleItem().Name.ShouldBe("widgets");
        }
        finally
        {
            Environment.SetEnvironmentVariable(EnvConnectionString, priorEnvConnectionString);
            Directory.Delete(projectDirectory, recursive: true);
        }
    }
}
