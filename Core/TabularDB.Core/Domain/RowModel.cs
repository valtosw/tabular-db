namespace TabularDB.Core.Domain
{
    public sealed class RowModel(IEnumerable<string> values)
    {
        public RowModel()
            : this([])
        {
        }

        public Guid Id { get; set; } = Guid.NewGuid();

        public List<string> Values { get; set; } = [.. values];
    }
}
