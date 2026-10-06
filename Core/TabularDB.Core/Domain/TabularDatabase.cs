using TabularDB.Core.Constants;
using TabularDB.Core.Validation;

namespace TabularDB.Core.Domain
{
    public sealed class TabularDatabase
    {
        public TabularDatabase()
        {
            Name = string.Empty;
            Tables = [];
        }

        public TabularDatabase(string name)
        {
            Id = Guid.NewGuid();
            Name = NameRules.Validate(name, "Database");
            Tables = [];
        }

        public Guid Id { get; set; }

        public string Name { get; set; }

        public List<TabularTable> Tables { get; set; }

        public TabularTable CreateTable(string name, IEnumerable<ColumnModel> columns)
        {
            var validatedName = NameRules.Validate(name, "Table");
            var hasDuplicateName = Tables.Any(table => string.Equals(table.Name, validatedName, StringComparison.OrdinalIgnoreCase));

            DomainValidation.Ensure(!hasDuplicateName, DomainValidationMessages.Format(DomainValidationMessages.DuplicateNamedItem, "table", validatedName));

            List<ColumnModel> materializedColumns = [.. DomainValidation.Require(columns, DomainValidationMessages.TableInputRequiresColumn)];
            var columnsArePresent = materializedColumns.Count > 0 && materializedColumns.All(column => column is not null);
            DomainValidation.Ensure(columnsArePresent, DomainValidationMessages.TableRequiresColumn);

            foreach (var column in materializedColumns)
            {
                column.Name = NameRules.Validate(column.Name, "Column");
                DomainValidation.Ensure(Enum.IsDefined(column.DataType), DomainValidationMessages.Format(DomainValidationMessages.UnsupportedDataType, column.Name));
            }

            NameRules.EnsureUnique(materializedColumns.Select(column => column.Name), "Column");

            var table = new TabularTable(validatedName, materializedColumns);
            Tables.Add(table);

            return table;
        }

        public void DeleteTable(Guid tableId)
        {
            Tables.Remove(FindTable(tableId));
        }

        public TabularTable FindTable(Guid tableId)
        {
            return Tables.FirstOrDefault(table => table.Id == tableId)
                ?? throw new DomainValidationException(DomainValidationMessages.Format(DomainValidationMessages.TableNotFound, tableId));
        }

        public static TabularDatabase Restore(Guid id, string name, IEnumerable<TabularTable> tables)
        {
            List<TabularTable> tableEntries = [.. DomainValidation.Require(tables, DomainValidationMessages.DatabaseRequiresTables)];
            DomainValidation.Ensure(tableEntries.All(table => table is not null), DomainValidationMessages.EmptyTableEntry);

            List<TabularTable> restoredTables =
            [
                .. tableEntries.Select(table => TabularTable.Restore(
                    table.Id,
                    table.Name,
                    table.Columns,
                    table.Rows))
            ];

            var tableIdentifiersAreUnique = restoredTables.All(table => table.Id != Guid.Empty)
                && restoredTables.Select(table => table.Id).Distinct().Count() == restoredTables.Count;

            DomainValidation.Ensure(tableIdentifiersAreUnique, DomainValidationMessages.DuplicateTableIdentifiers);
            NameRules.EnsureUnique(restoredTables.Select(table => table.Name), "Table");

            return new TabularDatabase(name)
            {
                Id = id,
                Tables = restoredTables
            };
        }
    }
}
