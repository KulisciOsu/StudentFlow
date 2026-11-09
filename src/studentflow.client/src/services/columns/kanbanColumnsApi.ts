import type { Column } from '../../models/columns/ColumnTypes'

/**
 * Ověří data sloupce.
 */
function isColumn(value: unknown): value is Column {
  return typeof value === 'object'
    && value !== null
    && 'id' in value
    && typeof value.id === 'number'
    && Number.isInteger(value.id)
    && 'title' in value
    && typeof value.title === 'string'
    && 'isDone' in value
    && typeof value.isDone === 'boolean'
}

/**
 * Načte sloupce nástěnky podle ID projektu.
 */
export async function getColumns(
  projectId: number,
  signal?: AbortSignal,
): Promise<Column[]> {
  const response = await fetch(`/api/projects/${projectId}/columns`, {
    signal,
    credentials: 'same-origin',
  })

  if (!response.ok) {
    throw new Error('Nepodařilo se načíst sloupce.')
  }

  const columns: unknown = await response.json()

  if (!Array.isArray(columns) || !columns.every(isColumn)) {
    throw new Error('Neplatná odpověď API.')
  }

  return columns
}