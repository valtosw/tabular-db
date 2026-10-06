using System.Text.Json.Serialization;
using TabularDB.Core.Constants;
using TabularDB.Core.Contracts;
using TabularDB.Core.Domain;
using TabularDB.Core.Validation;
using TabularDB.Server.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["TabularDB:Address"] ?? ServerDefaults.DefaultAddress);
builder.Services.AddSingleton<DatabaseFileStore>();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("TabularDB:AllowedOrigins").Get<string[]>() ?? [ServerDefaults.DefaultWebOrigin])
    .AllowAnyHeader()
    .AllowAnyMethod()));

var application = builder.Build();
application.UseCors();
application.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (DomainValidationException exception)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new ApiErrorResponse(exception.Message));
    }
});

var databaseRoutes = application.MapGroup(ApiRoutes.DatabaseCollection);
databaseRoutes.MapGet(ApiRoutes.CollectionRoot, (DatabaseFileStore store) => store.Read(catalog => catalog.Databases));
databaseRoutes.MapPost(ApiRoutes.CollectionRoot, (CreateDatabaseRequest request, DatabaseFileStore store) =>
{
    var database = store.Update(catalog => catalog.CreateDatabase(request.Name));
    return Results.Created(ApiRoutes.Database(database.Id), database);
});
databaseRoutes.MapGet(ApiRoutes.DatabaseRoute, (Guid databaseId, DatabaseFileStore store) =>
    store.Read(catalog => catalog.FindDatabase(databaseId)));
databaseRoutes.MapDelete(ApiRoutes.DatabaseRoute, (Guid databaseId, DatabaseFileStore store) =>
{
    store.Update(catalog =>
    {
        catalog.DeleteDatabase(databaseId);
        return true;
    });
    return Results.NoContent();
});
databaseRoutes.MapPost(ApiRoutes.TableCollectionRoute, (Guid databaseId, CreateTableRequest request, DatabaseFileStore store) =>
{
    var table = store.Update(catalog => catalog.FindDatabase(databaseId).CreateTable(request.Name, request.Columns));
    return Results.Created(ApiRoutes.Table(databaseId, table.Id), table);
});
databaseRoutes.MapDelete(ApiRoutes.TableRoute, (Guid databaseId, Guid tableId, DatabaseFileStore store) =>
{
    store.Update(catalog =>
    {
        catalog.FindDatabase(databaseId).DeleteTable(tableId);
        return true;
    });
    return Results.NoContent();
});
databaseRoutes.MapPost(ApiRoutes.RowCollectionRoute, (Guid databaseId, Guid tableId, RowValuesRequest request, DatabaseFileStore store) =>
{
    var row = store.Update(catalog => FindTable(catalog, databaseId, tableId).AddRow(request.Values));
    return Results.Created(ApiRoutes.Row(databaseId, tableId, row.Id), row);
});
databaseRoutes.MapPut(ApiRoutes.RowRoute, (Guid databaseId, Guid tableId, Guid rowId, RowValuesRequest request, DatabaseFileStore store) =>
    store.Update(catalog => FindTable(catalog, databaseId, tableId).UpdateRow(rowId, request.Values)));
databaseRoutes.MapDelete(ApiRoutes.RowRoute, (Guid databaseId, Guid tableId, Guid rowId, DatabaseFileStore store) =>
{
    store.Update(catalog =>
    {
        FindTable(catalog, databaseId, tableId).DeleteRow(rowId);
        return true;
    });
    return Results.NoContent();
});
databaseRoutes.MapPatch(ApiRoutes.ColumnNameRoute, (Guid databaseId, Guid tableId, int columnIndex, RenameColumnRequest request, DatabaseFileStore store) =>
{
    store.Update(catalog =>
    {
        FindTable(catalog, databaseId, tableId).RenameColumn(columnIndex, request.Name);
        return true;
    });
    return Results.NoContent();
});
databaseRoutes.MapPatch(ApiRoutes.ColumnOrderRoute, (Guid databaseId, Guid tableId, ReorderColumnRequest request, DatabaseFileStore store) =>
{
    store.Update(catalog =>
    {
        FindTable(catalog, databaseId, tableId).ReorderColumn(request.FromIndex, request.ToIndex);
        return true;
    });
    return Results.NoContent();
});
databaseRoutes.MapGet(ApiRoutes.ExportRoute, (Guid databaseId, DatabaseFileStore store) =>
    Results.Json(store.Read(catalog => catalog.FindDatabase(databaseId))));
databaseRoutes.MapPost(ApiRoutes.ImportRoute, (TabularDatabase database, DatabaseFileStore store) =>
{
    var importedDatabase = store.Update(catalog =>
    {
        var validatedDatabase = DatabaseCatalog.Restore([database]).Databases[0];
        var existingDatabaseIndex = catalog.Databases.FindIndex(existing => existing.Id == validatedDatabase.Id);
        var hasDuplicateName = catalog.Databases.Where((_, index) => index != existingDatabaseIndex)
            .Any(existing => string.Equals(existing.Name, validatedDatabase.Name, StringComparison.OrdinalIgnoreCase));
        DomainValidation.Ensure(!hasDuplicateName,
            DomainValidationMessages.Format(DomainValidationMessages.DuplicateNamedItem, "database", validatedDatabase.Name));

        if (existingDatabaseIndex < 0)
        {
            catalog.Databases.Add(validatedDatabase);
        }
        else
        {
            catalog.Databases[existingDatabaseIndex] = validatedDatabase;
        }

        return validatedDatabase;
    });
    return Results.Ok(importedDatabase);
});

application.Run();

static TabularTable FindTable(DatabaseCatalog catalog, Guid databaseId, Guid tableId)
{
    return catalog.FindDatabase(databaseId).FindTable(tableId);
}

static class ServerDefaults
{
    public const string DefaultAddress = "http://localhost:5194";
    public const string DefaultWebOrigin = "http://localhost:5173";
}
