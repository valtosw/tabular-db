using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Win32;
using TabularDB.Core.Constants;
using TabularDB.Core.Domain;

namespace TabularDB.Desktop.Services
{
    public sealed class DatabaseFileDialogService : IDatabaseFileDialogService
    {
        private static readonly JsonSerializerOptions _serializerOptions = CreateSerializerOptions();

        public async Task<bool> SaveAsync(TabularDatabase database)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Save database",
                FileName = $"{database.Name}.json",
                DefaultExt = ".json",
                Filter = "TabularDB database (*.json)|*.json|All files (*.*)|*.*",
                AddExtension = true,
                OverwritePrompt = true
            };

            if (dialog.ShowDialog() != true)
            {
                return false;
            }

            var json = JsonSerializer.Serialize(database, _serializerOptions);
            await File.WriteAllTextAsync(dialog.FileName, json);

            return true;
        }

        public async Task<TabularDatabase?> LoadAsync()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Load database",
                DefaultExt = ".json",
                Filter = "TabularDB database (*.json)|*.json|All files (*.*)|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() != true)
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(dialog.FileName);
            var database = JsonSerializer.Deserialize<TabularDatabase>(json, _serializerOptions)
                ?? throw new InvalidDataException(DomainValidationMessages.InvalidDatabaseImport);

            return DatabaseCatalog.Restore([database]).Databases[0];
        }

        private static JsonSerializerOptions CreateSerializerOptions()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };
            options.Converters.Add(new JsonStringEnumConverter());

            return options;
        }
    }
}