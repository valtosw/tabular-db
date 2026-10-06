export const DATA_TYPES = [
  'Integer',
  'Real',
  'Character',
  'String',
  'Money',
  'MoneyInterval',
] as const;

export type DataType = (typeof DATA_TYPES)[number];

export interface Column {
  name: string;
  dataType: DataType;
  isPrimaryKey: boolean;
}

export interface Row {
  id: string;
  values: string[];
}

export interface Table {
  id: string;
  name: string;
  columns: Column[];
  rows: Row[];
}

export interface Database {
  id: string;
  name: string;
  tables: Table[];
}

export interface CreateDatabaseRequest {
  name: string;
}

export interface CreateTableRequest {
  name: string;
  columns: Column[];
}

export interface RowValuesRequest {
  values: string[];
}

export interface RenameColumnRequest {
  name: string;
}

export interface ReorderColumnRequest {
  fromIndex: number;
  toIndex: number;
}