using System.Text.Json;
using System.Text.Json.Serialization;
using TabularDB.Core.Contracts;
using TabularDB.Core.Domain;
using TabularDB.Core.Validation;

namespace TabularDB.Server.Persistence
{

    public sealed class DatabaseFileStore
    {
        private static readonly JsonSerializerOptions _serializerOptions = CreateSerializerOptions();
        private readonly Lock _syncRoot = new();
        private readonly string _filePath;
        private DatabaseCatalog _catalog;

        public DatabaseFileStore(IConfiguration configuration)
        {
            _filePath = configuration["TabularDB:DataFile"]
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TabularDB", "databases.json");

            _catalog = Load();
        }

        public T Read<T>(Func<DatabaseCatalog, T> operation)
        {
            lock (_syncRoot)
            {
                var snapshot = DatabaseCatalog.Restore(_catalog.Databases);
                return operation(snapshot);
            }
        }

        public T Update<T>(Func<DatabaseCatalog, T> operation)
        {
            lock (_syncRoot)
            {
                var updatedCatalog = DatabaseCatalog.Restore(_catalog.Databases);
                var result = operation(updatedCatalog);
                Save(updatedCatalog);
                _catalog = updatedCatalog;
                return CreateStableResult(result);
            }
        }

        private static T CreateStableResult<T>(T result)
        {
            return result switch
            {
                TabularDatabase database => (T)(object)DatabaseCatalog.Restore([database]).Databases[0],
                TabularTable table => (T)(object)TabularDatabase.Restore(Guid.NewGuid(), "Snapshot", [table]).Tables[0],
                RowModel row => (T)(object)new RowModel(row.Values) { Id = row.Id },
                _ => result
            };
        }

        private static JsonSerializerOptions CreateSerializerOptions()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                WriteIndented = true
            };
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }

        private DatabaseCatalog Load()
        {
            if (!File.Exists(_filePath))
            {
                return new DatabaseCatalog();
            }

            var json = File.ReadAllText(_filePath);
            var savedCatalog = JsonSerializer.Deserialize<DatabaseCatalog>(json, _serializerOptions)
                ?? throw new InvalidDataException(PersistenceMessages.InvalidDatabaseFile);
            return DatabaseCatalog.Restore(savedCatalog.Databases);
        }

        private void Save(DatabaseCatalog databaseCatalog)
        {
            var directoryPath = Path.GetDirectoryName(_filePath);

            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var temporaryFilePath = $"{_filePath}.tmp";
            var json = JsonSerializer.Serialize(databaseCatalog, _serializerOptions);
            File.WriteAllText(temporaryFilePath, json);
            File.Move(temporaryFilePath, _filePath, true);
        }
    }
}
