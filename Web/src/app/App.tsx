import { ErrorDialog, NoticeBanner } from '../components/FeedbackDialogs';
import { WorkspacePage } from '../features/workspace/WorkspacePage';
import { useWorkspace } from '../features/workspace/useWorkspace';
import type { Database, Row, Table } from '../types/database';

export function App() {
  const workspace = useWorkspace();

  const confirmDeleteDatabase = (database: Database) => {
    if (window.confirm(`Delete “${database.name}” and all of its tables? This cannot be undone.`)) void workspace.deleteDatabase(database);
  };

  const confirmDeleteTable = (table: Table) => {
    if (window.confirm(`Delete the table “${table.name}” and all of its rows? This cannot be undone.`)) void workspace.deleteTable(table);
  };

  const confirmDeleteRow = (row: Row) => {
    if (window.confirm('Delete the selected row? This cannot be undone.')) void workspace.deleteRow(row);
  };

  return (
    <>
      <WorkspacePage
        databases={workspace.databases}
        database={workspace.selectedDatabase}
        table={workspace.selectedTable}
        isLoading={workspace.isLoading}
        isWorking={workspace.isWorking}
        isApiConnected={workspace.isApiConnected}
        onRefresh={() => void workspace.refreshDatabases()}
        onCreateDatabase={workspace.createDatabase}
        onDeleteDatabase={confirmDeleteDatabase}
        onSelectDatabase={workspace.selectDatabase}
        onImport={(file) => void workspace.importDatabase(file)}
        onExport={(database) => void workspace.exportDatabase(database)}
        onCreateTable={workspace.createTable}
        onDeleteTable={confirmDeleteTable}
        onSelectTable={workspace.selectTable}
        onSaveRow={workspace.saveRow}
        onDeleteRow={confirmDeleteRow}
        onRenameColumn={(index, name) => void workspace.renameColumn(index, name)}
        onReorderColumns={(fromIndex, toIndex) => void workspace.reorderColumns(fromIndex, toIndex)}
      />
      {workspace.noticeMessage && <NoticeBanner message={workspace.noticeMessage} onClose={workspace.dismissNotice} />}
      {workspace.errorMessage && <ErrorDialog message={workspace.errorMessage} onClose={workspace.dismissError} />}
    </>
  );
}
