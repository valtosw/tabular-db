const databaseCollection = '/api/databases';

export const apiRoutes = {
  databases: databaseCollection,
  database: (databaseId: string) => `${databaseCollection}/${databaseId}`,
  tables: (databaseId: string) => `${databaseCollection}/${databaseId}/tables`,
  table: (databaseId: string, tableId: string) => `${databaseCollection}/${databaseId}/tables/${tableId}`,
  rows: (databaseId: string, tableId: string) => `${databaseCollection}/${databaseId}/tables/${tableId}/rows`,
  row: (databaseId: string, tableId: string, rowId: string) =>
    `${databaseCollection}/${databaseId}/tables/${tableId}/rows/${rowId}`,
  renameColumn: (databaseId: string, tableId: string, columnIndex: number) =>
    `${databaseCollection}/${databaseId}/tables/${tableId}/columns/${columnIndex}/name`,
  reorderColumns: (databaseId: string, tableId: string) =>
    `${databaseCollection}/${databaseId}/tables/${tableId}/columns/reorder`,
  exportDatabase: (databaseId: string) => `${databaseCollection}/${databaseId}/export`,
  importDatabase: `${databaseCollection}/import`,
} as const;