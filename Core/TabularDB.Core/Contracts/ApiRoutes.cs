namespace TabularDB.Core.Contracts
{
    public static class ApiRoutes
    {
        public const string DatabaseCollection = "/api/databases";
        public const string CollectionRoot = "/";
        public const string DatabaseRoute = "/{databaseId:guid}";
        public const string TableCollectionRoute = "/{databaseId:guid}/tables";
        public const string TableRoute = "/{databaseId:guid}/tables/{tableId:guid}";
        public const string RowCollectionRoute = "/{databaseId:guid}/tables/{tableId:guid}/rows";
        public const string RowRoute = "/{databaseId:guid}/tables/{tableId:guid}/rows/{rowId:guid}";
        public const string ColumnNameRoute = "/{databaseId:guid}/tables/{tableId:guid}/columns/{columnIndex:int}/name";
        public const string ColumnOrderRoute = "/{databaseId:guid}/tables/{tableId:guid}/columns/reorder";
        public const string ExportRoute = "/{databaseId:guid}/export";
        public const string ImportRoute = "/import";
        public const string DatabaseImportRoute = "/api/databases/import";

        public static string Database(Guid databaseId) => $"{DatabaseCollection}/{databaseId:D}";

        public static string DatabaseCollectionPath => DatabaseCollection.TrimStart('/');

        public static string Tables(Guid databaseId) => $"{Database(databaseId)}/tables";

        public static string Table(Guid databaseId, Guid tableId) => $"{Tables(databaseId)}/{tableId:D}";

        public static string Rows(Guid databaseId, Guid tableId) => $"{Table(databaseId, tableId)}/rows";

        public static string Row(Guid databaseId, Guid tableId, Guid rowId) => $"{Rows(databaseId, tableId)}/{rowId:D}";

        public static string ColumnName(Guid databaseId, Guid tableId, int columnIndex) => $"{Table(databaseId, tableId)}/columns/{columnIndex}/name";

        public static string ColumnOrder(Guid databaseId, Guid tableId) => $"{Table(databaseId, tableId)}/columns/reorder";

        public static string Export(Guid databaseId) => $"{Database(databaseId)}/export";

        public static string DatabaseImportPath => DatabaseImportRoute.TrimStart('/');
    }

    public sealed record ApiErrorResponse(string Error);
}
