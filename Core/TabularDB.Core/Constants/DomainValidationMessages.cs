using System.Globalization;

namespace TabularDB.Core.Constants
{
    public static class DomainValidationMessages
    {
        public const int MaximumNameLength = 80;
        public const string MissingValue = "A value is required.";
        public const string UnsupportedDataType = "Column '{0}' has an unsupported data type.";
        public const string IntegerRequired = "Column '{0}' requires a 64-bit integer.";
        public const string RealRequired = "Column '{0}' requires a finite real number.";
        public const string CharacterRequired = "Column '{0}' requires exactly one character.";
        public const string MoneyRequired = "Column '{0}' requires a monetary value with at most two decimal places and absolute value no greater than {1}.";
        public const string MoneyIntervalRequired = "Column '{0}' requires an interval in the form minimum..maximum.";
        public const string MoneyIntervalOrder = "Column '{0}' interval minimum cannot exceed its maximum.";
        public const string EmptyName = "{0} name cannot be empty.";
        public const string NameTooLong = "{0} name cannot exceed {1} characters.";
        public const string DuplicateNames = "{0} names must be unique.";
        public const string DuplicateNamedItem = "A {0} named '{1}' already exists.";
        public const string TableRequiresColumn = "A table must contain at least one column.";
        public const string TableRequiresSchemaAndRows = "A table must include a schema and row collection.";
        public const string DatabaseRequiresTables = "A database must include a table collection.";
        public const string CatalogRequiresDatabases = "A catalog must include a database collection.";
        public const string EmptyTableEntry = "A database cannot contain an empty table entry.";
        public const string EmptyDatabaseEntry = "A catalog cannot contain an empty database entry.";
        public const string EmptySchemaEntry = "A table cannot contain an empty column or row entry.";
        public const string MissingTableIdentifier = "Table identifier must be present.";
        public const string DuplicateTableIdentifiers = "Table identifiers must be present and unique.";
        public const string DuplicateDatabaseIdentifiers = "Database identifiers must be present and unique.";
        public const string DuplicateRowIdentifiers = "Row identifiers must be present and unique.";
        public const string InvalidSavedRowSchema = "A saved row does not match its table schema.";
        public const string EmptyPrimaryKeyValue = "Primary key columns cannot contain empty values.";
        public const string DuplicatePrimaryKey = "A row with the same primary key already exists.";
        public const string RowValuesRequired = "Row values are required.";
        public const string ColumnIndexOutOfRange = "Column index {0} is outside the schema.";
        public const string RowValueCountMismatch = "Expected {0} values but received {1}.";
        public const string RowNotFound = "Row '{0}' was not found.";
        public const string TableNotFound = "Table '{0}' was not found.";
        public const string DatabaseNotFound = "Database '{0}' was not found.";
        public const string TableInputRequiresColumn = "A table must include at least one column.";
        public const string DuplicateColumnName = "A column named '{0}' already exists.";
        public const string DatabaseIdentifierAlreadyExists = "A database with this identifier already exists.";
        public const string InvalidDatabaseImport = "The selected file does not contain a database.";

        public static string Format(string message, params object?[] arguments)
        {
            return string.Format(CultureInfo.InvariantCulture, message, arguments);
        }
    }
}
