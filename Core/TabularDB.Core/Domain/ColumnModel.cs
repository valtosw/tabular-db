namespace TabularDB.Core.Domain
{
    public sealed class ColumnModel(string name, TabularDataType dataType, bool isPrimaryKey = false)
    {
        public ColumnModel()
            : this(string.Empty, TabularDataType.Integer)
        {
        }

        public string Name { get; set; } = name;

        public TabularDataType DataType { get; set; } = dataType;

        public bool IsPrimaryKey { get; set; } = isPrimaryKey;
    }
}
