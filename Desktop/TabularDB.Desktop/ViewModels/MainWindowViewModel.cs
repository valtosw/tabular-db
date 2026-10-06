using System.Collections.ObjectModel;
using System.Data;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using TabularDB.Core.Domain;
using TabularDB.Desktop.Commands;
using TabularDB.Desktop.Constants;
using TabularDB.Desktop.Services;

namespace TabularDB.Desktop.ViewModels
{
    public sealed class MainWindowViewModel : ViewModelBase
    {
        private readonly DatabaseApiClient _databaseApiClient;
        private readonly IDatabaseFileDialogService _fileDialogService;
        private TabularDatabase? _selectedDatabase;
        private TabularTable? _selectedTable;
        private ColumnModel? _selectedColumn;
        private ColumnDefinitionInputViewModel? _selectedTableColumn;
        private DataRowView? _selectedRow;
        private DataTable _rowsTable = new("Rows");
        private string _databaseName = string.Empty;
        private string _tableName = string.Empty;
        private string _columnName = string.Empty;
        private string _statusMessage = "Connecting to the local TabularDB server…";
        private Guid? _editingRowId;
        private string _rowIdentifierColumnName = "__TabularDbInternalRowId";

        public MainWindowViewModel(DatabaseApiClient databaseApiClient, IDatabaseFileDialogService fileDialogService)
        {
            _databaseApiClient = databaseApiClient;
            _fileDialogService = fileDialogService;
            Databases = [];
            Tables = [];
            RowInputs = [];
            NewTableColumns = [];
            ColumnTypeOptions =
            [
                new DataTypeOption(TabularDataType.Integer, "Integer"),
                new DataTypeOption(TabularDataType.Real, "Real"),
                new DataTypeOption(TabularDataType.Character, "Char"),
                new DataTypeOption(TabularDataType.String, "String"),
                new DataTypeOption(TabularDataType.Money, "Money ($)"),
                new DataTypeOption(TabularDataType.MoneyInterval, "Money interval ($Invl)")
            ];

            RefreshDatabasesCommand = new AsyncRelayCommand(RefreshDatabasesAsync);
            CreateDatabaseCommand = new AsyncRelayCommand(CreateDatabaseAsync);
            DeleteDatabaseCommand = new AsyncRelayCommand(DeleteDatabaseAsync);
            CreateTableCommand = new AsyncRelayCommand(CreateTableAsync);
            DeleteTableCommand = new AsyncRelayCommand(DeleteTableAsync);
            SaveDatabaseCommand = new AsyncRelayCommand(SaveDatabaseAsync);
            LoadDatabaseCommand = new AsyncRelayCommand(LoadDatabaseAsync);
            SaveRowCommand = new AsyncRelayCommand(SaveRowAsync);
            NewRowCommand = new RelayCommand(StartNewRow);
            DeleteRowCommand = new AsyncRelayCommand(DeleteRowAsync);
            RenameColumnCommand = new AsyncRelayCommand(RenameColumnAsync);
            AddTableColumnCommand = new RelayCommand(AddTableColumn);
            RemoveTableColumnCommand = new RelayCommand(RemoveSelectedTableColumn);
            AddTableColumn();
            _ = RefreshDatabasesAsync();
        }

        public static string Title => "TabularDB";

        public ObservableCollection<TabularDatabase> Databases { get; }

        public ObservableCollection<TabularTable> Tables { get; }

        public ObservableCollection<RowCellInputViewModel> RowInputs { get; }

        public ObservableCollection<ColumnDefinitionInputViewModel> NewTableColumns { get; }

        public IReadOnlyList<DataTypeOption> ColumnTypeOptions { get; }

        public ICommand RefreshDatabasesCommand { get; }

        public ICommand CreateDatabaseCommand { get; }

        public ICommand DeleteDatabaseCommand { get; }

        public ICommand CreateTableCommand { get; }

        public ICommand DeleteTableCommand { get; }

        public ICommand SaveDatabaseCommand { get; }

        public ICommand LoadDatabaseCommand { get; }

        public ICommand SaveRowCommand { get; }

        public ICommand NewRowCommand { get; }

        public ICommand DeleteRowCommand { get; }

        public ICommand RenameColumnCommand { get; }

        public ICommand AddTableColumnCommand { get; }

        public ICommand RemoveTableColumnCommand { get; }

        public bool HasDatabases => Databases.Count > 0;

        public Visibility DatabaseEmptyStateVisibility => HasDatabases ? Visibility.Collapsed : Visibility.Visible;

        public static string DatabaseEmptyStateMessage => "No databases yet. Enter a name above to create one.";

        public bool HasTables => Tables.Count > 0;

        public Visibility TableEmptyStateVisibility => HasTables ? Visibility.Collapsed : Visibility.Visible;

        public string TableEmptyStateMessage => SelectedDatabase is null
            ? "Create or select a database to view its tables."
            : "This database has no tables yet.";

        public bool HasColumns => SelectedTable?.Columns.Count > 0;

        public Visibility ColumnEmptyStateVisibility => HasColumns ? Visibility.Collapsed : Visibility.Visible;

        public string ColumnEmptyStateMessage => SelectedTable is null
            ? "Select a table to manage its schema."
            : "This table has no columns.";

        public bool HasSelectedDatabase => SelectedDatabase is not null;

        public bool HasSelectedTable => SelectedTable is not null;

        public bool HasSelectedRow => SelectedRow is not null;

        public TabularDatabase? SelectedDatabase
        {
            get => _selectedDatabase;
            set
            {
                if (!SetProperty(ref _selectedDatabase, value))
                {
                    return;
                }

                OnPropertyChanged(nameof(HasSelectedDatabase));
                OnPropertyChanged(nameof(TableEmptyStateMessage));
                Tables.Clear();
                UpdateTableEmptyState();
                SelectedTable = null;
                _ = RefreshTablesAsync(value?.Id);
            }
        }

        public TabularTable? SelectedTable
        {
            get => _selectedTable;
            set
            {
                if (!SetProperty(ref _selectedTable, value))
                {
                    return;
                }

                OnPropertyChanged(nameof(HasSelectedTable));
                UpdateColumnEmptyState();
                SelectedColumn = null;
                SelectedRow = null;
                ConfigureRowsTable();
                ConfigureRowInputs();
            }
        }

        public ColumnModel? SelectedColumn
        {
            get => _selectedColumn;
            set => SetProperty(ref _selectedColumn, value);
        }

        public ColumnDefinitionInputViewModel? SelectedTableColumn
        {
            get => _selectedTableColumn;
            set => SetProperty(ref _selectedTableColumn, value);
        }

        public DataRowView? SelectedRow
        {
            get => _selectedRow;
            set
            {
                if (!SetProperty(ref _selectedRow, value))
                {
                    return;
                }

                _editingRowId = value?.Row[_rowIdentifierColumnName] is Guid rowId ? rowId : null;
                OnPropertyChanged(nameof(HasSelectedRow));
                ConfigureRowInputs(value);
            }
        }

        public DataTable RowsTable
        {
            get => _rowsTable;
            private set => SetProperty(ref _rowsTable, value);
        }

        public string DatabaseName
        {
            get => _databaseName;
            set => SetProperty(ref _databaseName, value);
        }

        public string TableName
        {
            get => _tableName;
            set => SetProperty(ref _tableName, value);
        }

        public string ColumnName
        {
            get => _columnName;
            set => SetProperty(ref _columnName, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public async Task ReorderColumnAfterDragAsync(int fromIndex, int toIndex)
        {
            if (SelectedDatabase is null || SelectedTable is null || fromIndex == toIndex)
            {
                return;
            }

            var databaseId = SelectedDatabase.Id;
            var tableId = SelectedTable.Id;
            await RunOperationAsync(async () =>
            {
                await _databaseApiClient.ReorderColumnAsync(databaseId, tableId, fromIndex, toIndex);
                await RefreshTablesAsync(databaseId, tableId);
                StatusMessage = "Column order and row values updated.";
            });
        }

        private async Task RefreshDatabasesAsync()
        {
            await RunOperationAsync(async () =>
            {
                var selectedDatabaseId = SelectedDatabase?.Id;
                var databases = await _databaseApiClient.GetDatabasesAsync();
                Databases.Clear();
                foreach (var database in databases)
                {
                    Databases.Add(database);
                }

                OnPropertyChanged(nameof(HasDatabases));
                OnPropertyChanged(nameof(DatabaseEmptyStateVisibility));
                SelectedDatabase = Databases.FirstOrDefault(database => database.Id == selectedDatabaseId)
                    ?? Databases.FirstOrDefault();
                StatusMessage = "Databases loaded.";
            });
        }

        private async Task RefreshTablesAsync(Guid? databaseId, Guid? preferredTableId = null)
        {
            if (databaseId is null || SelectedDatabase?.Id != databaseId)
            {
                return;
            }

            await RunOperationAsync(async () =>
            {
                var database = await _databaseApiClient.GetDatabaseAsync(databaseId.Value);
                if (SelectedDatabase?.Id != databaseId)
                {
                    return;
                }

                Tables.Clear();
                foreach (var table in database.Tables)
                {
                    Tables.Add(table);
                }

                UpdateTableEmptyState();
                SelectedTable = Tables.FirstOrDefault(table => table.Id == preferredTableId)
                    ?? Tables.FirstOrDefault();
                StatusMessage = $"Database '{database.Name}' loaded.";
            });
        }

        private async Task CreateDatabaseAsync()
        {
            await RunOperationAsync(async () =>
            {
                var database = await _databaseApiClient.CreateDatabaseAsync(DatabaseName);
                DatabaseName = string.Empty;
                await RefreshDatabasesAsync();
                SelectedDatabase = Databases.FirstOrDefault(item => item.Id == database.Id);
                StatusMessage = $"Database '{database.Name}' created.";
            });
        }

        private async Task DeleteDatabaseAsync()
        {
            if (SelectedDatabase is null)
            {
                ShowWarning("Select a database first.");
                return;
            }

            var databaseName = SelectedDatabase.Name;
            await RunOperationAsync(async () =>
            {
                await _databaseApiClient.DeleteDatabaseAsync(SelectedDatabase!.Id);
                await RefreshDatabasesAsync();
                StatusMessage = $"Database '{databaseName}' deleted.";
            });
        }

        private async Task CreateTableAsync()
        {
            if (SelectedDatabase is null)
            {
                ShowWarning("Create or select a database first.");
                return;
            }

            await RunOperationAsync(async () =>
            {
                ColumnModel[] columns =
                [
                    .. NewTableColumns.Select(definition => new ColumnModel(
                        definition.Name,
                        definition.SelectedType.Value,
                        definition.IsPrimaryKey))
                ];
                var table = await _databaseApiClient.CreateTableAsync(SelectedDatabase!.Id, TableName, columns);
                TableName = string.Empty;
                NewTableColumns.Clear();
                SelectedTableColumn = null;
                AddTableColumn();
                await RefreshTablesAsync(SelectedDatabase.Id, table.Id);
                StatusMessage = $"Table '{table.Name}' created.";
            });
        }

        private async Task DeleteTableAsync()
        {
            if (SelectedDatabase is null || SelectedTable is null)
            {
                ShowWarning("Select a table first.");
                return;
            }

            var databaseId = SelectedDatabase.Id;
            var tableId = SelectedTable.Id;
            var tableName = SelectedTable.Name;
            await RunOperationAsync(async () =>
            {
                await _databaseApiClient.DeleteTableAsync(databaseId, tableId);
                await RefreshTablesAsync(databaseId);
                StatusMessage = $"Table '{tableName}' deleted.";
            });
        }

        private async Task SaveDatabaseAsync()
        {
            if (SelectedDatabase is null)
            {
                ShowWarning("Select a database to save.");
                return;
            }

            var databaseId = SelectedDatabase.Id;
            await RunOperationAsync(async () =>
            {
                var database = await _databaseApiClient.ExportDatabaseAsync(databaseId);
                if (await _fileDialogService.SaveAsync(database))
                {
                    StatusMessage = $"Database '{database.Name}' saved to a file.";
                }
            });
        }

        private async Task LoadDatabaseAsync()
        {
            await RunOperationAsync(async () =>
            {
                var database = await _fileDialogService.LoadAsync();
                if (database is null)
                {
                    return;
                }

                var importedDatabase = await _databaseApiClient.ImportDatabaseAsync(database);
                await RefreshDatabasesAsync();
                SelectedDatabase = Databases.FirstOrDefault(item => item.Id == importedDatabase.Id);
                StatusMessage = $"Database '{importedDatabase.Name}' loaded from a file.";
            });
        }

        private async Task SaveRowAsync()
        {
            if (SelectedDatabase is null || SelectedTable is null)
            {
                ShowWarning("Select a table before saving a row.");
                return;
            }

            var databaseId = SelectedDatabase.Id;
            var tableId = SelectedTable.Id;
            await RunOperationAsync(async () =>
            {
                string[] values = [.. RowInputs.Select(input => input.Value)];
                if (_editingRowId is Guid rowId)
                {
                    await _databaseApiClient.UpdateRowAsync(databaseId, tableId, rowId, values);
                }
                else
                {
                    await _databaseApiClient.CreateRowAsync(databaseId, tableId, values);
                }

                await RefreshTablesAsync(databaseId, tableId);
                StatusMessage = "Row saved.";
            });
        }

        private async Task DeleteRowAsync()
        {
            if (SelectedDatabase is null || SelectedTable is null || _editingRowId is not Guid rowId)
            {
                ShowWarning("Select a row to delete.");
                return;
            }

            var databaseId = SelectedDatabase.Id;
            var tableId = SelectedTable.Id;
            await RunOperationAsync(async () =>
            {
                await _databaseApiClient.DeleteRowAsync(databaseId, tableId, rowId);
                await RefreshTablesAsync(databaseId, tableId);
                StatusMessage = "Row deleted.";
            });
        }

        private async Task RenameColumnAsync()
        {
            if (SelectedDatabase is null || SelectedTable is null || SelectedColumn is null)
            {
                ShowWarning("Select a column to rename.");
                return;
            }

            var databaseId = SelectedDatabase.Id;
            var tableId = SelectedTable.Id;
            var columnIndex = SelectedTable.Columns.IndexOf(SelectedColumn);
            await RunOperationAsync(async () =>
            {
                await _databaseApiClient.RenameColumnAsync(databaseId, tableId, columnIndex, ColumnName);
                ColumnName = string.Empty;
                await RefreshTablesAsync(databaseId, tableId);
                StatusMessage = "Column renamed.";
            });
        }

        private void AddTableColumn()
        {
            var definition = new ColumnDefinitionInputViewModel(ColumnTypeOptions[0]);
            NewTableColumns.Add(definition);
            SelectedTableColumn = definition;
        }

        private void RemoveSelectedTableColumn()
        {
            if (SelectedTableColumn is null)
            {
                ShowWarning("Select a column definition to remove.");
                return;
            }

            var selectedIndex = NewTableColumns.IndexOf(SelectedTableColumn);
            NewTableColumns.Remove(SelectedTableColumn);
            SelectedTableColumn = NewTableColumns.ElementAtOrDefault(selectedIndex)
                ?? NewTableColumns.LastOrDefault();
        }

        private void StartNewRow()
        {
            SelectedRow = null;
            _editingRowId = null;
            ConfigureRowInputs();
            StatusMessage = "Enter values for the new row.";
        }

        private void ConfigureRowsTable()
        {
            var table = new DataTable("Rows");
            _rowIdentifierColumnName = "__TabularDbInternalRowId";
            while (SelectedTable?.Columns.Any(column => string.Equals(column.Name, _rowIdentifierColumnName, StringComparison.OrdinalIgnoreCase)) == true)
            {
                _rowIdentifierColumnName += "_";
            }

            table.Columns.Add(_rowIdentifierColumnName, typeof(Guid));

            if (SelectedTable is not null)
            {
                foreach (var column in SelectedTable.Columns)
                {
                    table.Columns.Add(column.Name, typeof(string));
                }

                foreach (var row in SelectedTable.Rows)
                {
                    var values = new object[row.Values.Count + 1];
                    values[0] = row.Id;

                    for (var index = 0; index < row.Values.Count; index++)
                    {
                        values[index + 1] = row.Values[index];
                    }

                    table.Rows.Add(values);
                }
            }

            RowsTable = table;
            SelectedRow = null;
        }

        private void ConfigureRowInputs(DataRowView? row = null)
        {
            RowInputs.Clear();
            if (SelectedTable is null)
            {
                return;
            }

            for (var index = 0; index < SelectedTable.Columns.Count; index++)
            {
                var column = SelectedTable.Columns[index];
                var value = row is null ? string.Empty : Convert.ToString(row[index + 1]) ?? string.Empty;
                RowInputs.Add(new RowCellInputViewModel(column.Name, column.DataType, value, column.IsPrimaryKey));
            }
        }

        private void UpdateTableEmptyState()
        {
            OnPropertyChanged(nameof(HasTables));
            OnPropertyChanged(nameof(TableEmptyStateVisibility));
            OnPropertyChanged(nameof(TableEmptyStateMessage));
        }

        private void UpdateColumnEmptyState()
        {
            OnPropertyChanged(nameof(HasColumns));
            OnPropertyChanged(nameof(ColumnEmptyStateVisibility));
            OnPropertyChanged(nameof(ColumnEmptyStateMessage));
        }

        private async Task RunOperationAsync(Func<Task> operation)
        {
            try
            {
                await operation();
            }
            catch (HttpRequestException exception)
            {
                ShowWarning(exception.StatusCode is null ? ApiClientMessages.CannotReachServer : exception.Message);
            }
            catch (Exception exception)
            {
                ShowWarning(exception.Message);
            }
        }

        private static void ShowWarning(string message)
        {
            if (Application.Current?.MainWindow is { } mainWindow)
            {
                MessageBox.Show(mainWindow, message, ApiClientMessages.WarningTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show(message, ApiClientMessages.WarningTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
