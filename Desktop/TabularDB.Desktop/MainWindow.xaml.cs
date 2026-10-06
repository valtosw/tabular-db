using System.Windows;
using System.Windows.Controls;
using TabularDB.Desktop.ViewModels;

namespace TabularDB.Desktop
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnAutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs eventArguments)
        {
            if (eventArguments.PropertyName.StartsWith("__TabularDbInternalRowId", StringComparison.Ordinal))
            {
                eventArguments.Cancel = true;
            }
        }

        private void OnColumnReordered(object? sender, DataGridColumnEventArgs eventArguments)
        {
            if (DataContext is not MainWindowViewModel viewModel || viewModel.SelectedTable is null)
            {
                return;
            }

            var columnName = eventArguments.Column.Header?.ToString();
            var sourceIndex = viewModel.SelectedTable.Columns.FindIndex(column => column.Name == columnName);
            var targetIndex = eventArguments.Column.DisplayIndex;
            if (sourceIndex >= 0 && sourceIndex != targetIndex)
            {
                _ = viewModel.ReorderColumnAfterDragAsync(sourceIndex, targetIndex);
            }
        }
    }
}
