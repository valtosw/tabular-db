import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { databaseApi } from '../../services/api/databaseApi';
import { ApiError } from '../../services/api/httpClient';
import type { Column, Database, Row, Table } from '../../types/database';

interface TableSchemaInput {
  name: string;
  columns: Column[];
}

function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : 'Something went wrong. Please try again.';
}

export function useWorkspace() {
  const [databases, setDatabases] = useState<Database[]>([]);
  const [selectedDatabaseId, setSelectedDatabaseId] = useState<string | null>(null);
  const [selectedTableId, setSelectedTableId] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isWorking, setIsWorking] = useState(false);
  const [isApiConnected, setIsApiConnected] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [noticeMessage, setNoticeMessage] = useState<string | null>(null);
  const selectedDatabaseIdRef = useRef<string | null>(null);
  const selectedTableIdRef = useRef<string | null>(null);

  const selectedDatabase = useMemo(
    () => databases.find((database) => database.id === selectedDatabaseId) ?? null,
    [databases, selectedDatabaseId],
  );
  const selectedTable = useMemo(
    () => selectedDatabase?.tables.find((table) => table.id === selectedTableId) ?? null,
    [selectedDatabase, selectedTableId],
  );

  const chooseDatabase = useCallback((databaseId: string | null) => {
    selectedDatabaseIdRef.current = databaseId;
    setSelectedDatabaseId(databaseId);
  }, []);

  const chooseTable = useCallback((tableId: string | null) => {
    selectedTableIdRef.current = tableId;
    setSelectedTableId(tableId);
  }, []);

  const replaceDatabase = useCallback((updatedDatabase: Database) => {
    setDatabases((currentDatabases) =>
      currentDatabases.map((database) => database.id === updatedDatabase.id ? updatedDatabase : database),
    );

    if (selectedDatabaseIdRef.current === updatedDatabase.id) {
      const nextTableId = updatedDatabase.tables.some((table) => table.id === selectedTableIdRef.current)
        ? selectedTableIdRef.current
        : updatedDatabase.tables[0]?.id ?? null;
      chooseTable(nextTableId);
    }
  }, [chooseTable]);

  const refreshDatabase = useCallback(async (databaseId: string) => {
    replaceDatabase(await databaseApi.getDatabase(databaseId));
  }, [replaceDatabase]);

  const refreshDatabases = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(null);
    try {
      const refreshedDatabases = await databaseApi.getDatabases();
      setIsApiConnected(true);
      setDatabases(refreshedDatabases);
      const nextDatabaseId = refreshedDatabases.some((database) => database.id === selectedDatabaseIdRef.current)
        ? selectedDatabaseIdRef.current
        : refreshedDatabases[0]?.id ?? null;
      chooseDatabase(nextDatabaseId);
      const nextDatabase = refreshedDatabases.find((database) => database.id === nextDatabaseId);
      const nextTableId = nextDatabase?.tables.some((table) => table.id === selectedTableIdRef.current)
        ? selectedTableIdRef.current
        : nextDatabase?.tables[0]?.id ?? null;
      chooseTable(nextTableId);
    } catch (error) {
      setIsApiConnected(error instanceof ApiError && error.statusCode !== undefined);
      setErrorMessage(getErrorMessage(error));
    } finally {
      setIsLoading(false);
    }
  }, [chooseDatabase, chooseTable]);

  useEffect(() => {
    void refreshDatabases();
  }, [refreshDatabases]);

  const runAction = useCallback(async (action: () => Promise<void>): Promise<boolean> => {
    setIsWorking(true);
    setErrorMessage(null);
    setNoticeMessage(null);
    try {
      await action();
      setIsApiConnected(true);
      return true;
    } catch (error) {
      if (error instanceof ApiError && error.statusCode === undefined) setIsApiConnected(false);
      setErrorMessage(getErrorMessage(error));
      return false;
    } finally {
      setIsWorking(false);
    }
  }, []);

  const createDatabase = useCallback((name: string) => runAction(async () => {
    const createdDatabase = await databaseApi.createDatabase({ name });
    const refreshedDatabases = await databaseApi.getDatabases();
    setDatabases(refreshedDatabases);
    chooseDatabase(createdDatabase.id);
    chooseTable(createdDatabase.tables[0]?.id ?? null);
    setNoticeMessage(`Database “${createdDatabase.name}” created.`);
  }), [chooseDatabase, chooseTable, runAction]);

  const deleteDatabase = useCallback((database: Database) => runAction(async () => {
    await databaseApi.deleteDatabase(database.id);
    const refreshedDatabases = await databaseApi.getDatabases();
    setDatabases(refreshedDatabases);
    if (selectedDatabaseIdRef.current === database.id) {
      const nextDatabase = refreshedDatabases[0] ?? null;
      chooseDatabase(nextDatabase?.id ?? null);
      chooseTable(nextDatabase?.tables[0]?.id ?? null);
    }
    setNoticeMessage(`Database “${database.name}” deleted.`);
  }), [chooseDatabase, chooseTable, runAction]);

  const createTable = useCallback((schema: TableSchemaInput) => {
    if (!selectedDatabase) return Promise.resolve(false);
    return runAction(async () => {
      const createdTable = await databaseApi.createTable(selectedDatabase.id, schema);
      await refreshDatabase(selectedDatabase.id);
      chooseTable(createdTable.id);
      setNoticeMessage(`Table “${createdTable.name}” created.`);
    });
  }, [chooseTable, refreshDatabase, runAction, selectedDatabase]);

  const deleteTable = useCallback((table: Table) => {
    if (!selectedDatabase) return Promise.resolve(false);
    return runAction(async () => {
      await databaseApi.deleteTable(selectedDatabase.id, table.id);
      await refreshDatabase(selectedDatabase.id);
      setNoticeMessage(`Table “${table.name}” deleted.`);
    });
  }, [refreshDatabase, runAction, selectedDatabase]);

  const saveRow = useCallback((values: string[], row?: Row) => {
    if (!selectedDatabase || !selectedTable) return Promise.resolve(false);
    return runAction(async () => {
      const request = { values };
      if (row) {
        await databaseApi.updateRow(selectedDatabase.id, selectedTable.id, row.id, request);
      } else {
        await databaseApi.createRow(selectedDatabase.id, selectedTable.id, request);
      }
      await refreshDatabase(selectedDatabase.id);
      setNoticeMessage(row ? 'Row updated.' : 'Row added.');
    });
  }, [refreshDatabase, runAction, selectedDatabase, selectedTable]);

  const deleteRow = useCallback((row: Row) => {
    if (!selectedDatabase || !selectedTable) return Promise.resolve(false);
    return runAction(async () => {
      await databaseApi.deleteRow(selectedDatabase.id, selectedTable.id, row.id);
      await refreshDatabase(selectedDatabase.id);
      setNoticeMessage('Row deleted.');
    });
  }, [refreshDatabase, runAction, selectedDatabase, selectedTable]);

  const renameColumn = useCallback((columnIndex: number, name: string) => {
    if (!selectedDatabase || !selectedTable) return Promise.resolve(false);
    return runAction(async () => {
      await databaseApi.renameColumn(selectedDatabase.id, selectedTable.id, columnIndex, { name });
      await refreshDatabase(selectedDatabase.id);
      setNoticeMessage('Column name updated.');
    });
  }, [refreshDatabase, runAction, selectedDatabase, selectedTable]);

  const reorderColumns = useCallback((fromIndex: number, toIndex: number) => {
    if (!selectedDatabase || !selectedTable || fromIndex === toIndex) return Promise.resolve(false);
    return runAction(async () => {
      await databaseApi.reorderColumns(selectedDatabase.id, selectedTable.id, { fromIndex, toIndex });
      await refreshDatabase(selectedDatabase.id);
      setNoticeMessage('Column order updated.');
    });
  }, [refreshDatabase, runAction, selectedDatabase, selectedTable]);

  const importDatabase = useCallback((file: File) => runAction(async () => {
    const importedDatabase = JSON.parse(await file.text()) as Database;
    const savedDatabase = await databaseApi.importDatabase(importedDatabase);
    const refreshedDatabases = await databaseApi.getDatabases();
    setDatabases(refreshedDatabases);
    chooseDatabase(savedDatabase.id);
    chooseTable(savedDatabase.tables[0]?.id ?? null);
    setNoticeMessage(`Database “${savedDatabase.name}” imported.`);
  }), [chooseDatabase, chooseTable, runAction]);

  const exportDatabase = useCallback((database: Database) => runAction(async () => {
    const savedDatabase = await databaseApi.exportDatabase(database.id);
    const downloadUrl = URL.createObjectURL(
      new Blob([JSON.stringify(savedDatabase, null, 2)], { type: 'application/json' }),
    );
    const downloadLink = document.createElement('a');
    downloadLink.href = downloadUrl;
    downloadLink.download = `${database.name.replace(/[^a-z0-9-_]+/gi, '-')}.json`;
    downloadLink.click();
    window.setTimeout(() => URL.revokeObjectURL(downloadUrl), 1_000);
    setNoticeMessage(`Database “${database.name}” exported.`);
  }), [runAction]);

  const selectDatabase = useCallback((database: Database) => {
    chooseDatabase(database.id);
    chooseTable(database.tables[0]?.id ?? null);
    setErrorMessage(null);
    setNoticeMessage(null);
  }, [chooseDatabase, chooseTable]);

  const selectTable = useCallback((tableId: string) => {
    chooseTable(tableId);
    setNoticeMessage(null);
  }, [chooseTable]);

  const dismissError = useCallback(() => setErrorMessage(null), []);
  const dismissNotice = useCallback(() => setNoticeMessage(null), []);

  return {
    databases,
    selectedDatabase,
    selectedTable,
    isLoading,
    isWorking,
    isApiConnected,
    errorMessage,
    noticeMessage,
    refreshDatabases,
    createDatabase,
    deleteDatabase,
    createTable,
    deleteTable,
    saveRow,
    deleteRow,
    renameColumn,
    reorderColumns,
    importDatabase,
    exportDatabase,
    selectDatabase,
    selectTable,
    dismissError,
    dismissNotice,
  };
}
