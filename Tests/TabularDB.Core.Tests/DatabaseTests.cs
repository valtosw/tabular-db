using TabularDB.Core.Domain;
using TabularDB.Core.Validation;

namespace TabularDB.Core.Tests
{
    public sealed class DatabaseTests
    {
        [Fact]
        public void ReorderColumn_PreservesEachRowsValues()
        {
            var database = new TabularDatabase("Sample");
            var table = database.CreateTable("Products", [
                new ColumnModel("Name", TabularDataType.String),
            new ColumnModel("Price", TabularDataType.Money),
            new ColumnModel("Quantity", TabularDataType.Integer)
            ]);
            var row = table.AddRow(["Notebook", "4.50", "3"]);

            table.ReorderColumn(2, 0);

            Assert.Equal(["Quantity", "Name", "Price"], table.Columns.Select(column => column.Name));
            Assert.Equal(["3", "Notebook", "4.50"], row.Values);
        }

        [Fact]
        public void RenameColumn_RejectsDuplicateNames()
        {
            var table = new TabularDatabase("Sample").CreateTable("Products", [
                new ColumnModel("Name", TabularDataType.String),
            new ColumnModel("Price", TabularDataType.Money)
            ]);

            Assert.Throws<DomainValidationException>(() => table.RenameColumn(1, "name"));
        }

        [Fact]
        public void ValidateMoney_AcceptsMaximumAndRejectsValuesAboveIt()
        {
            Assert.Equal("10000000000000.00", ValueValidator.Validate("10000000000000.00", TabularDataType.Money, "Amount"));
            Assert.Throws<DomainValidationException>(() => ValueValidator.Validate("10000000000000.01", TabularDataType.Money, "Amount"));
        }

        [Fact]
        public void ValidateMoneyInterval_RequiresOrderedValidMoneyBounds()
        {
            Assert.Equal("5.00..8.25", ValueValidator.Validate("5..8.25", TabularDataType.MoneyInterval, "Range"));
            Assert.Throws<DomainValidationException>(() => ValueValidator.Validate("8..5", TabularDataType.MoneyInterval, "Range"));
        }
    }
}
