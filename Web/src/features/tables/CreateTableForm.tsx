import { useState, type FormEvent } from 'react';
import { Plus, Trash2 } from 'lucide-react';
import { DATA_TYPES, type Column, type DataType } from '../../types/database';

interface ColumnDraft extends Column {
  localId: string;
}

interface CreateTableFormProps {
  isWorking: boolean;
  enabled: boolean;
  onCreate: (schema: { name: string; columns: Column[] }) => Promise<boolean>;
}

function createColumnDraft(): ColumnDraft {
  return { localId: crypto.randomUUID(), name: '', dataType: 'String', isPrimaryKey: false };
}

function dataTypeLabel(dataType: DataType): string {
  if (dataType === 'Money') return 'Money ($)';
  if (dataType === 'MoneyInterval') return 'Money interval ($Invl)';
  if (dataType === 'Character') return 'Char';
  return dataType;
}

export function CreateTableForm({ isWorking, enabled, onCreate }: CreateTableFormProps) {
  const [name, setName] = useState('');
  const [columns, setColumns] = useState<ColumnDraft[]>([createColumnDraft()]);

  const updateColumn = (localId: string, update: Partial<ColumnDraft>) => {
    setColumns((current) => current.map((column) => column.localId === localId ? { ...column, ...update } : column));
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const schemaColumns = columns.map(({ name: columnName, dataType, isPrimaryKey }) => ({
      name: columnName.trim(), dataType, isPrimaryKey,
    }));
    if (await onCreate({ name: name.trim(), columns: schemaColumns })) {
      setName('');
      setColumns([createColumnDraft()]);
    }
  };

  const schemaIsValid = columns.length > 0 && columns.every((column) => column.name.trim());

  return (
    <form className="create-table-form" onSubmit={handleSubmit}>
      <h2>Create a table</h2>
      <label className="field-label" htmlFor="new-table-name">Table name</label>
      <input id="new-table-name" className="text-input" maxLength={80} value={name} onChange={(event) => setName(event.target.value)} placeholder="For example, Products" disabled={!enabled || isWorking} />
      <div className="schema-title-row">
        <h3>Column schema</h3>
        <button className="text-action" type="button" disabled={!enabled || isWorking} onClick={() => setColumns((current) => [...current, createColumnDraft()])}><Plus size={15} /> Add column</button>
      </div>
      <p className="schema-description">Choose a type and mark columns that uniquely identify each row.</p>
      <div className="schema-list">
        {columns.map((column, index) => (
          <div className="schema-item" key={column.localId}>
            <div className="schema-item-heading"><span>Column {index + 1}</span><button className="icon-button" type="button" aria-label={`Remove column ${index + 1}`} disabled={!enabled || isWorking || columns.length === 1} onClick={() => setColumns((current) => current.filter((item) => item.localId !== column.localId))}><Trash2 size={15} /></button></div>
            <input className="text-input" aria-label={`Column ${index + 1} name`} maxLength={80} value={column.name} onChange={(event) => updateColumn(column.localId, { name: event.target.value })} placeholder="Column name" disabled={!enabled || isWorking} />
            <div className="schema-item-options">
              <select className="text-input" aria-label={`Column ${index + 1} data type`} value={column.dataType} onChange={(event) => updateColumn(column.localId, { dataType: event.target.value as DataType })} disabled={!enabled || isWorking}>{DATA_TYPES.map((dataType) => <option key={dataType} value={dataType}>{dataTypeLabel(dataType)}</option>)}</select>
              <label className="checkbox-label"><input type="checkbox" checked={column.isPrimaryKey} onChange={(event) => updateColumn(column.localId, { isPrimaryKey: event.target.checked })} disabled={!enabled || isWorking} /> Primary key</label>
            </div>
          </div>
        ))}
      </div>
      <button className="button button-primary create-table-button" type="submit" disabled={!enabled || !name.trim() || !schemaIsValid || isWorking}><Plus size={15} /> Create table</button>
      {!enabled && <p className="inline-empty">Select a database before creating a table.</p>}
    </form>
  );
}
