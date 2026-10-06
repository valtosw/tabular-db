using TabularDB.Core.Domain;

namespace TabularDB.Desktop.ViewModels
{
    public sealed class ColumnDefinitionInputViewModel(DataTypeOption selectedType) : ViewModelBase
    {
        private string _name = string.Empty;
        private DataTypeOption _selectedType = selectedType;
        private bool _isPrimaryKey;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public DataTypeOption SelectedType
        {
            get => _selectedType;
            set => SetProperty(ref _selectedType, value);
        }

        public bool IsPrimaryKey
        {
            get => _isPrimaryKey;
            set => SetProperty(ref _isPrimaryKey, value);
        }
    }

    public sealed record DataTypeOption(TabularDataType Value, string DisplayName);
}
