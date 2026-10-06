using TabularDB.Core.Domain;

namespace TabularDB.Desktop.ViewModels
{
    public sealed class RowCellInputViewModel(string name, TabularDataType dataType, string value = "", bool isPrimaryKey = false) : ViewModelBase
    {
        private string _value = value;

        public string Name { get; } = name;

        public string DisplayName { get; } = isPrimaryKey ? $"{name} (primary key)" : name;

        public string DataType { get; } = GetDataTypeDisplayName(dataType);

        public string InputHint => dataType == TabularDataType.MoneyInterval
            ? "Enter minimum..maximum, for example 5.00..8.25"
            : $"Enter a {DataType} value";

        public string Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        private static string GetDataTypeDisplayName(TabularDataType dataType)
        {
            return dataType switch
            {
                TabularDataType.Integer => "Integer",
                TabularDataType.Real => "Real",
                TabularDataType.Character => "Char",
                TabularDataType.String => "String",
                TabularDataType.Money => "Money ($)",
                TabularDataType.MoneyInterval => "Money interval ($Invl)",
                _ => dataType.ToString()
            };
        }
    }
}
