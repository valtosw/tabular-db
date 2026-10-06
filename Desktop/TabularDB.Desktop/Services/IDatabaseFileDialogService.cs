using TabularDB.Core.Domain;

namespace TabularDB.Desktop.Services
{
    public interface IDatabaseFileDialogService
    {
        Task<bool> SaveAsync(TabularDatabase database);
        Task<TabularDatabase?> LoadAsync();
    }
}
