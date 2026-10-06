import { useEffect, useMemo, useRef, useState, type FormEvent } from 'react';
import { GripVertical, Plus, Save, Search, Trash2 } from 'lucide-react';
import type { Row, Table } from '../../types/database';

interface TableViewProps {
  table: Table;
  isWorking: boolean;
  onSaveRow: (values: string[], row?: Row) => Promise<boolean>;
  onDeleteRow: (row: Row) => void;
  onRenameColumn: (index: number, name: string) => void;
  onReorderColumns: (fromIndex: number, toIndex: number) => void;
}

function dataTypeLabel(dataType: string): string {
  if (dataType === 'Money') return 'Money ($)';
  if (dataType === 'MoneyInterval') return 'Money interval ($Invl)';
  if (dataType === 'Character') return 'Char';
  return dataType;
}

function inputHint(dataType: string): string {
  if (dataType === 'MoneyInterval') return 'Minimum..maximum, for example 5.00..8.25';
  if (dataType === 'Money') return 'For example 12.50';
  return `Enter a ${dataType} value`;
}

export function TableView({ table, isWorking, onSaveRow, onDeleteRow, onRenameColumn, onReorderColumns }: TableViewProps) {
  const [selectedRowId, setSelectedRowId] = useState<string | null>(null);
  const [editingRow, setEditingRow] = useState<Row | null>(null);
  const [values, setValues] = useState<string[]>([]);
  const [searchText, setSearchText] = useState('');
  const [selectedColumnIndex, setSelectedColumnIndex] = useState(0);
  const [columnName, setColumnName] = useState('');
  const draggedColumnIndex = useRef<number | null>(null);

  const visibleRows = useMemo(() => {
    const normalizedSearch = searchText.trim().toLocaleLowerCase();
    return normalizedSearch
      ? table.rows.filter((row) => row.values.some((value) => value.toLocaleLowerCase().includes(normalizedSearch)))
      : table.rows;
  }, [searchText, table.rows]);
  const selectedRow = table.rows.find((row) => row.id === selectedRowId) ?? null;

  useEffect(() => {
    setSelectedRowId(null);
    setEditingRow(null);
    setValues(table.columns.map(() => ''));
    setSearchText('');
    setSelectedColumnIndex(0);
    setColumnName(table.columns[0]?.name ?? '');
  }, [table.id]);

  useEffect(() => {
    const selectedColumn = table.columns[selectedColumnIndex];
    setColumnName((current) => selectedColumn && (selectedColumn.name === current || !current) ? selectedColumn.name : current);
  }, [selectedColumnIndex, table.columns]);

  const startNewRow = () => {
    setSelectedRowId(null);
    setEditingRow(null);
    setValues(table.columns.map(() => ''));
  };

  const startEditingRow = (row: Row) => {
    setSelectedRowId(row.id);
    setEditingRow(row);
    setValues([...row.values]);
  };

  const saveRow = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (await onSaveRow(values, editingRow ?? undefined)) {
      setEditingRow(null);
      setValues(table.columns.map(() => ''));
      setSelectedRowId(null);
    }
  };

  const handleDropColumn = (targetIndex: number) => {
    const sourceIndex = draggedColumnIndex.current;
    draggedColumnIndex.current = null;
    if (sourceIndex !== null && sourceIndex !== targetIndex) onReorderColumns(sourceIndex, targetIndex);
  };

  return (
    <div className="table-workspace">
      <div className="table-workspace-heading">
        <div><div className="section-kicker">TABLE DATA</div><h1>{table.name}</h1><p>{table.rows.length} {table.rows.length === 1 ? 'row' : 'rows'} · {table.columns.length} {table.columns.length === 1 ? 'column' : 'columns'}</p></div>
        <div className="table-heading-actions"><button className="button button-primary" type="button" disabled={isWorking} onClick={startNewRow}><Plus size={15} /> New row</button><button className="button button-quiet" type="button" disabled={!selectedRow || isWorking} onClick={() => selectedRow && onDeleteRow(selectedRow)}><Trash2 size={15} /> Delete selected row</button></div>
      </div>

      <div className="table-grid-area">
        <div className="grid-toolbar"><label className="search-control"><Search size={15} /><input aria-label="Search table rows" value={searchText} onChange={(event) => setSearchText(event.target.value)} placeholder="Search rows" /></label><span>{visibleRows.length} of {table.rows.length} rows</span></div>
        <div className="table-scroll-area">
          <table className="data-table">
            <thead><tr><th className="row-number-heading">#</th>{table.columns.map((column, index) => <th key={`${column.name}-${index}`} draggable={!isWorking} onDragStart={(event) => { draggedColumnIndex.current = index; event.dataTransfer.effectAllowed = 'move'; event.dataTransfer.setData('text/plain', String(index)); }} onDragOver={(event) => event.preventDefault()} onDrop={(event) => { event.preventDefault(); handleDropColumn(index); }} onDragEnd={() => { draggedColumnIndex.current = null; }} title="Drag to reorder this column"><span className="table-column-heading"><GripVertical size={14} className="column-grip" />{column.name}{column.isPrimaryKey && <span className="key-badge">PK</span>}<small>{dataTypeLabel(column.dataType)}</small></span></th>)}</tr></thead>
            <tbody>{visibleRows.map((row) => <tr key={row.id} className={selectedRowId === row.id ? 'selected-row' : ''} tabIndex={0} onClick={() => startEditingRow(row)} onDoubleClick={() => startEditingRow(row)} onKeyDown={(event) => { if (event.key === 'Enter') startEditingRow(row); }}><td className="row-number-cell">{String(table.rows.indexOf(row) + 1).padStart(2, '0')}</td>{table.columns.map((column, index) => <td key={`${column.name}-${index}`} title={row.values[index]}>{row.values[index] || <span className="empty-cell">—</span>}</td>)}</tr>)}</tbody>
          </table>
          {visibleRows.length === 0 && <div className="grid-empty-state">{table.rows.length === 0 ? 'This table has no rows yet.' : 'No rows match your search.'}</div>}
        </div>
      </div>

      <form className="row-editor" onSubmit={(event) => void saveRow(event)}>
        <div className="editor-heading"><div><h2>Row values</h2><p>{editingRow ? 'Edit the selected row.' : 'Enter values for a new row.'} Money intervals use minimum..maximum, for example 5.00..8.25.</p></div><div className="editor-actions"><button className="button button-quiet" type="button" disabled={isWorking} onClick={startNewRow}>New row</button><button className="button button-primary" type="submit" disabled={isWorking || table.columns.length === 0}><Save size={15} /> Save row</button></div></div>
        {table.columns.length === 0 ? <p className="pane-empty">This table has no columns.</p> : <div className="row-fields">{table.columns.map((column, index) => <label className="row-field" key={`${column.name}-${index}`}><span>{column.name}<small>{dataTypeLabel(column.dataType)}{column.isPrimaryKey ? ' · Primary key' : ''}</small></span><input value={values[index] ?? ''} onChange={(event) => setValues((current) => current.map((value, valueIndex) => valueIndex === index ? event.target.value : value))} placeholder={inputHint(column.dataType)} disabled={isWorking} /></label>)}</div>}
      </form>

      <section className="column-editor">
        <div className="column-editor-heading"><h2>Column schema</h2><span>Drag a column header above to change its order.</span></div>
        {table.columns.length === 0 ? <p className="pane-empty">This table has no columns.</p> : <div className="column-editor-controls"><label className="visually-hidden" htmlFor="column-select">Column</label><select id="column-select" className="text-input column-select" value={selectedColumnIndex} onChange={(event) => { const index = Number(event.target.value); setSelectedColumnIndex(index); setColumnName(table.columns[index]?.name ?? ''); }} disabled={isWorking}>{table.columns.map((column, index) => <option value={index} key={`${column.name}-${index}`}>{column.name}</option>)}</select><label className="visually-hidden" htmlFor="column-new-name">New column name</label><input id="column-new-name" className="text-input column-name-input" value={columnName} onChange={(event) => setColumnName(event.target.value)} placeholder="New column name" disabled={isWorking} /><button className="button button-quiet" type="button" disabled={isWorking || !columnName.trim() || !table.columns[selectedColumnIndex] || columnName.trim() === table.columns[selectedColumnIndex].name} onClick={() => { onRenameColumn(selectedColumnIndex, columnName.trim()); setColumnName(columnName.trim()); }}>Rename</button></div>}
        <p className="money-note">Money supports up to two decimal places. Intervals use minimum..maximum.</p>
      </section>
    </div>
  );
}
