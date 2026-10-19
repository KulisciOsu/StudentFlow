import type { Column } from '../../models/columns/ColumnTypes'

/// <summary>
/// Ověří data sloupce.
/// </summary>
function isColumn(value: unknown): value is Column {
  return typeof value === 'object'
    && value !== null
    && 'id' in value
    && typeof value.id === 'number'
    && Number.isInteger(value.id)
    && 'title' in value
    && typeof value.title === 'string'
}

/// <summary>
/// Načte sloupce nástěnky z API.
/// </summary>
export async function getColumns(signal?: AbortSignal): Promise<Column[]> {
  const response = await fetch('/api/columns', { signal })

  if (!response.ok) {
    throw new Error('Nepodařilo se načíst sloupce.')
  }

  const columns: unknown = await response.json()

  if (!Array.isArray(columns) || !columns.every(isColumn)) {
    throw new Error('Neplatná odpověď API.')
  }

  return columns
}
