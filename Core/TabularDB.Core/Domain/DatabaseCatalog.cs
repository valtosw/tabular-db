using TabularDB.Core.Constants;
using TabularDB.Core.Validation;

namespace TabularDB.Core.Domain
{
    public sealed class DatabaseCatalog
    {
        public DatabaseCatalog()
        {
            Databases = [];
        }

        public List<TabularDatabase> Databases { get; set; }

        public TabularDatabase CreateDatabase(string name)
        {
            var validatedName = NameRules.Validate(name, "Database");
            var hasDuplicateName = Databases.Any(database => string.Equals(database.Name, validatedName, StringComparison.OrdinalIgnoreCase));

            DomainValidation.Ensure(!hasDuplicateName, DomainValidationMessages.Format(DomainValidationMessages.DuplicateNamedItem, "database", validatedName));

            var database = new TabularDatabase(validatedName);
            Databases.Add(database);

            return database;
        }

        public void DeleteDatabase(Guid databaseId)
        {
            Databases.Remove(FindDatabase(databaseId));
        }

        public TabularDatabase FindDatabase(Guid databaseId)
        {
            return Databases.FirstOrDefault(database => database.Id == databaseId)
                ?? throw new DomainValidationException(DomainValidationMessages.Format(DomainValidationMessages.DatabaseNotFound, databaseId));
        }

        public static DatabaseCatalog Restore(IEnumerable<TabularDatabase> databases)
        {
            List<TabularDatabase> databaseEntries = [.. DomainValidation.Require(databases, DomainValidationMessages.CatalogRequiresDatabases)];
            DomainValidation.Ensure(databaseEntries.All(database => database is not null), DomainValidationMessages.EmptyDatabaseEntry);

            List<TabularDatabase> restoredDatabases =
            [
                .. databaseEntries.Select(database => TabularDatabase.Restore(database.Id, database.Name, database.Tables))
            ];

            NameRules.EnsureUnique(restoredDatabases.Select(database => database.Name), "Database");

            var identifiersAreUnique = restoredDatabases.All(database => database.Id != Guid.Empty)
                && restoredDatabases.Select(database => database.Id).Distinct().Count() == restoredDatabases.Count;

            DomainValidation.Ensure(identifiersAreUnique, DomainValidationMessages.DuplicateDatabaseIdentifiers);

            return new DatabaseCatalog { Databases = restoredDatabases };
        }
    }
}
