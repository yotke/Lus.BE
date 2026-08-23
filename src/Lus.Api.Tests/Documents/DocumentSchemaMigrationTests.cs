using System.Reflection;
using Lus.Application.Documents.Entities;
using Lus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace Lus.Api.Tests.Documents;

/// <summary>
/// Guards the document-builder persistence layer against the two ways it silently rots:
/// an entity/configuration change that never reaches a migration, and a relationship
/// (the carry-in chain especially) quietly losing its shape.
///
/// These build the EF model offline via the design-time factory — no database required, so they
/// run in CI exactly as they run locally.
/// </summary>
public class DocumentSchemaMigrationTests
{
    private static ApplicationContext Context() =>
        new ApplicationContextDesignTimeFactory().CreateDbContext([]);

    /// <summary>
    /// The design-time model — NOT <c>context.Model</c>. The read-optimized runtime model drops
    /// configuration the differ needs (it throws
    /// "The requested configuration is not stored in the read-optimized model" on Collation), so
    /// the diff must run against the design-time model.
    ///
    /// Resolved by reflection because <c>IDesignTimeModel</c> does not bind by name from this test
    /// project's transitive EF reference. Reflection here is a build-time inconvenience, not a
    /// design choice: the service is resolved from EF's own container exactly as
    /// <c>GetService&lt;IDesignTimeModel&gt;()</c> would.
    /// </summary>
    private static IModel DesignTimeModel(ApplicationContext context)
    {
        // Scan loaded assemblies rather than guessing which EF assembly declares it — the
        // interface has moved between EFCore and EFCore.Relational across versions.
        var contract = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) == true)
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t is not null)!; }
            })
            .FirstOrDefault(t => t!.IsInterface && t.Name == "IDesignTimeModel");

        Assert.True(contract is not null,
            "IDesignTimeModel not found in any loaded Microsoft.EntityFrameworkCore* assembly");

        var accessor = ((IInfrastructure<IServiceProvider>)context).Instance.GetService(contract!);
        Assert.True(accessor is not null, "EF did not provide an IDesignTimeModel service");

        var model = contract!.GetProperty("Model")!.GetValue(accessor) as IModel;
        Assert.True(model is not null, "IDesignTimeModel.Model was null");
        return model!;
    }

    [Fact]
    public void Model_has_no_pending_changes_beyond_the_last_migration()
    {
        // THE DRIFT GUARD. Fails the moment someone edits an entity or an EntityTypeConfiguration
        // without running `dotnet ef migrations add`. Without it, the model and the database
        // diverge silently until a deploy fails.
        using var context = Context();

        var differ = context.GetService<IMigrationsModelDiffer>();
        var snapshot = context.GetService<IMigrationsAssembly>().ModelSnapshot;
        Assert.NotNull(snapshot);

        var snapshotModel = context.GetService<IModelRuntimeInitializer>()
            .Initialize(snapshot!.Model, designTime: true, validationLogger: null);

        var differences = differ.GetDifferences(
            snapshotModel.GetRelationalModel(),
            DesignTimeModel(context).GetRelationalModel());

        Assert.True(
            differences.Count == 0,
            "The EF model has changes not captured in a migration. Run:\n" +
            "  dotnet ef migrations add <Name> --project Lus.Infrastructure/Lus.Infrastructure.csproj " +
            "--startup-project Lus.Infrastructure/Lus.Infrastructure.csproj\n" +
            $"Pending operations: {differences.Count}");
    }

    [Theory]
    [InlineData(typeof(DocumentSeries))]
    [InlineData(typeof(DocumentTemplate))]
    [InlineData(typeof(DocumentInstance))]
    [InlineData(typeof(DocumentDay))]
    [InlineData(typeof(DocumentRow))]
    [InlineData(typeof(DocumentBuildSessionRow))]
    [InlineData(typeof(RateCard))]
    public void Every_document_entity_is_mapped(Type entityType)
    {
        using var context = Context();

        Assert.NotNull(context.Model.FindEntityType(entityType));
    }

    [Fact]
    public void Carry_in_is_a_self_reference_on_DocumentInstance()
    {
        // C7/C6: the balance chain is an ENTITY relationship, not a cell address. If this ever
        // becomes a plain int with no FK, the chain can point at a deleted instance and the
        // exemplar's hand-typed-cross-sheet-reference defect comes straight back.
        using var context = Context();

        var instance = context.Model.FindEntityType(typeof(DocumentInstance));
        Assert.NotNull(instance);

        var carryIn = instance!.GetForeignKeys()
            .SingleOrDefault(fk => fk.Properties.Any(p => p.Name == nameof(DocumentInstance.CarryInFromInstanceId)));

        Assert.True(carryIn is not null,
            "CarryInFromInstanceId must be a real foreign key, not a loose int");
        Assert.Equal(typeof(DocumentInstance), carryIn!.PrincipalEntityType.ClrType);
    }

    [Fact]
    public void Document_rows_hang_off_days_which_hang_off_instances()
    {
        // The two-level data band (spec §3.5): archetype 2 groups time segments into days;
        // archetype 1 is the degenerate one-segment-per-day case. Both need this shape.
        using var context = Context();

        var row = context.Model.FindEntityType(typeof(DocumentRow));
        var day = context.Model.FindEntityType(typeof(DocumentDay));
        Assert.NotNull(row);
        Assert.NotNull(day);

        Assert.Contains(row!.GetForeignKeys(),
            fk => fk.PrincipalEntityType.ClrType == typeof(DocumentDay));
        Assert.Contains(day!.GetForeignKeys(),
            fk => fk.PrincipalEntityType.ClrType == typeof(DocumentInstance));
    }

    [Fact]
    public void Build_sessions_are_indexed_by_user()
    {
        // The session store reads by user on every turn; an unindexed scan there is a per-keystroke
        // cost once the table grows.
        using var context = Context();

        var session = context.Model.FindEntityType(typeof(DocumentBuildSessionRow));
        Assert.NotNull(session);

        Assert.Contains(session!.GetIndexes(),
            ix => ix.Properties.Any(p => p.Name == nameof(DocumentBuildSessionRow.UserId)));
    }
}
