using TabularDB.Core.Domain;

namespace TabularDB.Core.Contracts
{
    public sealed record CreateDatabaseRequest(string Name);

    public sealed record CreateTableRequest(string Name, List<ColumnModel> Columns);

    public sealed record RowValuesRequest(List<string> Values);

    public sealed record RenameColumnRequest(string Name);

    public sealed record ReorderColumnRequest(int FromIndex, int ToIndex);
}
