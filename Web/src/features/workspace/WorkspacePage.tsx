import { useRef, useState, type ChangeEvent } from 'react';
import { FileDown, FileUp, Plus, RefreshCw, Trash2 } from 'lucide-react';
import type { Database, Row, Table } from '../../types/database';
import { CreateTableForm } from '../tables/CreateTableForm';
import { TableView } from '../tables/TableView';

interface WorkspacePageProps {
  databases: Database[];
  database: Database | null;
  table: Table | null;
  isLoading: boolean;
  isWorking: boolean;
  isApiConnected: boolean;
  onRefresh: () => void;
  onCreateDatabase: (name: string) => Promise<boolean>;
  onDeleteDatabase: (database: Database) => void;
  onSelectDatabase: (database: Database) => void;
  onImport: (file: File) => void;
  onExport: (database: Database) => void;
  onCreateTable: (schema: { name: string; columns: import('../../types/database').Column[] }) => Promise<boolean>;
  onDeleteTable: (table: Table) => void;
  onSelectTable: (tableId: string) => void;
  onSaveRow: (values: string[], row?: Row) => Promise<boolean>;
  onDeleteRow: (row: Row) => void;
  onRenameColumn: (index: number, name: string) => void;
  onReorderColumns: (fromIndex: number, toIndex: number) => void;
}

export function WorkspacePage(props: WorkspacePageProps) {
  const {
    databases, database, table, isLoading, isWorking, isApiConnected,
    onRefresh, onCreateDatabase, onDeleteDatabase, onSelectDatabase, onImport,
    onExport, onCreateTable, onDeleteTable, onSelectTable,
    onSaveRow, onDeleteRow, onRenameColumn, onReorderColumns,
  } = props;
  const [databaseName, setDatabaseName] = useState('');
  const importInput = useRef<HTMLInputElement>(null);

  const handleImport = (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) onImport(file);
    event.target.value = '';
  };

  const handleCreateDatabase = async () => {
    if (await onCreateDatabase(databaseName.trim())) setDatabaseName('');
  };

  return (
    <main className="desktop-app">
      <header className="database-toolbar">
        <a className="app-title" href="#workspace">TabularDB</a>
        <label className="visually-hidden" htmlFor="database-select">Selected database</label>
        <select id="database-select" className="database-select" value={database?.id ?? ''} onChange={(event) => {
          const selected = databases.find((item) => item.id === event.target.value);
          if (selected) onSelectDatabase(selected);
        }} disabled={databases.length === 0 || isWorking}>
          {databases.length === 0 ? <option value="">No databases yet</option> : databases.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
        </select>
        <input className="toolbar-database-name" aria-label="New database name" value={databaseName} onChange={(event) => setDatabaseName(event.target.value)} onKeyDown={(event) => { if (event.key === 'Enter') void handleCreateDatabase(); }} placeholder="New database name" maxLength={80} disabled={isWorking} />
        <button className="toolbar-button" type="button" disabled={!databaseName.trim() || isWorking} onClick={() => void handleCreateDatabase()}><Plus size={15} /> Create database</button>
        <div className="toolbar-spacer" />
        <button className="toolbar-button" type="button" disabled={!database || isWorking} onClick={() => database && onDeleteDatabase(database)}><Trash2 size={15} /> Delete database</button>
        <button className="toolbar-button" type="button" disabled={!database || isWorking} onClick={() => database && onExport(database)}><FileDown size={15} /> Save database</button>
        <input ref={importInput} className="visually-hidden" type="file" accept="application/json,.json" onChange={handleImport} />
        <button className="toolbar-button" type="button" disabled={isWorking} onClick={() => importInput.current?.click()}><FileUp size={15} /> Load database</button>
        <button className="toolbar-button" type="button" disabled={isWorking} onClick={onRefresh}><RefreshCw size={15} /> Refresh</button>
      </header>

      {isLoading && !database ? <div className="workspace-loading">Connecting to the TabularDB server…</div> : (
        <div className="workspace-grid">
          <aside className="schema-pane">
            <section className="table-list-section" aria-labelledby="tables-heading">
              <div className="section-heading"><h1 id="tables-heading">Tables</h1><span>{database?.tables.length ?? 0}</span></div>
              {database?.tables.length ? (
                <ul className="table-list">{database.tables.map((item) => (
                  <li key={item.id}><button type="button" className={`table-list-item ${table?.id === item.id ? 'selected' : ''}`} disabled={isWorking} onClick={() => onSelectTable(item.id)}><span>{item.name}</span><small>{item.columns.length} cols · {item.rows.length} rows</small></button></li>
                ))}</ul>
              ) : <p className="pane-empty">{database ? 'This database has no tables yet.' : 'Create or select a database to view its tables.'}</p>}
              <button className="button button-quiet delete-table-button" type="button" disabled={!table || isWorking} onClick={() => table && onDeleteTable(table)}><Trash2 size={15} /> Delete selected table</button>
            </section>
            <div className="pane-divider" />
            <CreateTableForm isWorking={isWorking} enabled={Boolean(database)} onCreate={onCreateTable} />
          </aside>

          <section className="data-pane" aria-label="Selected table data">
            {table ? (
              <TableView
                table={table}
                isWorking={isWorking}
                onSaveRow={onSaveRow}
                onDeleteRow={onDeleteRow}
                onRenameColumn={onRenameColumn}
                onReorderColumns={onReorderColumns}
              />
            ) : (
              <div className="table-empty-workspace">
                <h2>{database ? 'No table selected' : 'Select or create a database'}</h2>
                <p>{database ? 'Choose a table from the list or create one using the schema form.' : 'Use the controls above to create a database or load one from a JSON file.'}</p>
              </div>
            )}
          </section>
        </div>
      )}

      <footer className="status-bar"><span className={`status-indicator ${isApiConnected ? 'connected' : ''}`} />{isApiConnected ? 'Connected to TabularDB API' : 'API unavailable'}<span className="status-detail">{isWorking ? 'Working…' : database ? `${database.name}${table ? ` · ${table.name}` : ''}` : 'Ready'}</span></footer>
    </main>
  );
}
