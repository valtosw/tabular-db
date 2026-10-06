using TabularDB.Core.Constants;
using TabularDB.Core.Validation;

namespace TabularDB.Core.Domain
{
    public sealed class TabularTable(string name, IEnumerable<ColumnModel> columns)
    {
        public TabularTable()
            : this(string.Empty, [])
        {
            Id = Guid.Empty;
        }

        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = name;

        public List<ColumnModel> Columns { get; set; } = [.. columns];

        public List<RowModel> Rows { get; set; } = [];

        public RowModel AddRow(IEnumerable<string> values)
        {
            var validatedValues = ValidateValues(values);
            EnsureUniquePrimaryKey(validatedValues);
            var row = new RowModel(validatedValues);
            Rows.Add(row);
            return row;
        }

        public RowModel UpdateRow(Guid rowId, IEnumerable<string> values)
        {
            var row = FindRow(rowId);
            var validatedValues = ValidateValues(values);
            EnsureUniquePrimaryKey(validatedValues, rowId);
            row.Values = validatedValues;
            return row;
        }

        public void DeleteRow(Guid rowId)
        {
            Rows.Remove(FindRow(rowId));
        }

        public void RenameColumn(int index, string name)
        {
            EnsureColumnIndex(index);
            var validatedName = NameRules.Validate(name, "Column");
            var hasDuplicateName = Columns.Where((_, columnIndex) => columnIndex != index)
                .Any(column => string.Equals(column.Name, validatedName, StringComparison.OrdinalIgnoreCase));
            DomainValidation.Ensure(!hasDuplicateName,
                DomainValidationMessages.Format(DomainValidationMessages.DuplicateColumnName, validatedName));
            Columns[index].Name = validatedName;
        }

        public void ReorderColumn(int fromIndex, int toIndex)
        {
            EnsureColumnIndex(fromIndex);
            EnsureColumnIndex(toIndex);

            if (fromIndex == toIndex)
            {
                return;
            }

            var column = Columns[fromIndex];
            Columns.RemoveAt(fromIndex);
            Columns.Insert(toIndex, column);

            foreach (var row in Rows)
            {
                var value = row.Values[fromIndex];
                row.Values.RemoveAt(fromIndex);
                row.Values.Insert(toIndex, value);
            }
        }

        public void ValidateSchema()
        {
            Name = NameRules.Validate(Name, "Table");
            var columns = DomainValidation.Require(Columns, DomainValidationMessages.TableRequiresSchemaAndRows);
            var rows = DomainValidation.Require(Rows, DomainValidationMessages.TableRequiresSchemaAndRows);
            DomainValidation.Ensure(Id != Guid.Empty, DomainValidationMessages.MissingTableIdentifier);
            DomainValidation.Ensure(columns.Count > 0, DomainValidationMessages.TableRequiresColumn);
            DomainValidation.Ensure(columns.All(column => column is not null), DomainValidationMessages.EmptySchemaEntry);

            foreach (var column in columns)
            {
                column.Name = NameRules.Validate(column.Name, "Column");
                DomainValidation.Ensure(Enum.IsDefined(column.DataType),
                    DomainValidationMessages.Format(DomainValidationMessages.UnsupportedDataType, column.Name));
            }

            NameRules.EnsureUnique(columns.Select(column => column.Name), "Column");
            DomainValidation.Ensure(rows.All(row => row is not null && row.Values is not null), DomainValidationMessages.EmptySchemaEntry);

            var rowIdentifiersAreUnique = rows.All(row => row.Id != Guid.Empty)
                && rows.Select(row => row.Id).Distinct().Count() == rows.Count;
            DomainValidation.Ensure(rowIdentifiersAreUnique, DomainValidationMessages.DuplicateRowIdentifiers);

            foreach (var row in rows)
            {
                DomainValidation.Ensure(row.Values.Count == columns.Count, DomainValidationMessages.InvalidSavedRowSchema);

                for (var index = 0; index < row.Values.Count; index++)
                {
                    row.Values[index] = ValueValidator.Validate(row.Values[index], columns[index].DataType, columns[index].Name);
                }

                EnsureUniquePrimaryKey(row.Values, row.Id);
            }
        }

        public static TabularTable Restore(Guid id, string name, IEnumerable<ColumnModel> columns, IEnumerable<RowModel> rows)
        {
            List<ColumnModel> columnEntries = [.. DomainValidation.Require(columns, DomainValidationMessages.TableRequiresSchemaAndRows)];
            List<RowModel> rowEntries = [.. DomainValidation.Require(rows, DomainValidationMessages.TableRequiresSchemaAndRows)];
            DomainValidation.Ensure(columnEntries.All(column => column is not null)
                && rowEntries.All(row => row is not null && row.Values is not null), DomainValidationMessages.EmptySchemaEntry);

            var table = new TabularTable(name, columnEntries.Select(column => new ColumnModel(column.Name, column.DataType, column.IsPrimaryKey)))
            {
                Id = id,
                Rows = [.. rowEntries.Select(row => new RowModel(row.Values) { Id = row.Id })]
            };
            table.ValidateSchema();
            return table;
        }

        private List<string> ValidateValues(IEnumerable<string> values)
        {
            List<string> providedValues = [.. DomainValidation.Require(values, DomainValidationMessages.RowValuesRequired)];
            DomainValidation.Ensure(providedValues.Count == Columns.Count,
                DomainValidationMessages.Format(DomainValidationMessages.RowValueCountMismatch, Columns.Count, providedValues.Count));

            for (var index = 0; index < providedValues.Count; index++)
            {
                providedValues[index] = ValueValidator.Validate(providedValues[index], Columns[index].DataType, Columns[index].Name);
            }

            return providedValues;
        }

        private void EnsureUniquePrimaryKey(IReadOnlyList<string> values, Guid? excludedRowId = null)
        {
            var keyIndexes = Columns
                .Select((column, index) => (column, index))
                .Where(entry => entry.column.IsPrimaryKey)
                .Select(entry => entry.index)
                .ToArray();
            if (keyIndexes.Length == 0)
            {
                return;
            }

            DomainValidation.Ensure(keyIndexes.All(index => !string.IsNullOrWhiteSpace(values[index])),
                DomainValidationMessages.EmptyPrimaryKeyValue);
            var hasDuplicateKey = Rows
                .Where(row => row.Id != excludedRowId)
                .Any(row => keyIndexes.All(index => string.Equals(row.Values[index], values[index], StringComparison.Ordinal)));
            DomainValidation.Ensure(!hasDuplicateKey, DomainValidationMessages.DuplicatePrimaryKey);
        }

        private RowModel FindRow(Guid rowId)
        {
            return Rows.FirstOrDefault(row => row.Id == rowId)
                ?? throw new DomainValidationException(DomainValidationMessages.Format(DomainValidationMessages.RowNotFound, rowId));
        }

        private void EnsureColumnIndex(int index)
        {
            DomainValidation.Ensure(index >= 0 && index < Columns.Count,
                DomainValidationMessages.Format(DomainValidationMessages.ColumnIndexOutOfRange, index));
        }
    }
}
