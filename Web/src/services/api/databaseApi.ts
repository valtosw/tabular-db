import type {
  CreateDatabaseRequest,
  CreateTableRequest,
  Database,
  RenameColumnRequest,
  ReorderColumnRequest,
  Row,
  RowValuesRequest,
  Table,
} from '../../types/database';
import { apiRoutes } from './routes';
import { request } from './httpClient';

export const databaseApi = {
  getDatabases: () => request<Database[]>(apiRoutes.databases),
  getDatabase: (databaseId: string) => request<Database>(apiRoutes.database(databaseId)),
  createDatabase: (body: CreateDatabaseRequest) =>
    request<Database>(apiRoutes.databases, { method: 'POST', body: JSON.stringify(body) }),
  deleteDatabase: (databaseId: string) =>
    request<void>(apiRoutes.database(databaseId), { method: 'DELETE' }),
  createTable: (databaseId: string, body: CreateTableRequest) =>
    request<Table>(apiRoutes.tables(databaseId), { method: 'POST', body: JSON.stringify(body) }),
  deleteTable: (databaseId: string, tableId: string) =>
    request<void>(apiRoutes.table(databaseId, tableId), { method: 'DELETE' }),
  createRow: (databaseId: string, tableId: string, body: RowValuesRequest) =>
    request<Row>(apiRoutes.rows(databaseId, tableId), { method: 'POST', body: JSON.stringify(body) }),
  updateRow: (databaseId: string, tableId: string, rowId: string, body: RowValuesRequest) =>
    request<Row>(apiRoutes.row(databaseId, tableId, rowId), { method: 'PUT', body: JSON.stringify(body) }),
  deleteRow: (databaseId: string, tableId: string, rowId: string) =>
    request<void>(apiRoutes.row(databaseId, tableId, rowId), { method: 'DELETE' }),
  renameColumn: (databaseId: string, tableId: string, columnIndex: number, body: RenameColumnRequest) =>
    request<void>(apiRoutes.renameColumn(databaseId, tableId, columnIndex), { method: 'PATCH', body: JSON.stringify(body) }),
  reorderColumns: (databaseId: string, tableId: string, body: ReorderColumnRequest) =>
    request<void>(apiRoutes.reorderColumns(databaseId, tableId), { method: 'PATCH', body: JSON.stringify(body) }),
  exportDatabase: (databaseId: string) => request<Database>(apiRoutes.exportDatabase(databaseId)),
  importDatabase: (database: Database) =>
    request<Database>(apiRoutes.importDatabase, { method: 'POST', body: JSON.stringify(database) }),
};