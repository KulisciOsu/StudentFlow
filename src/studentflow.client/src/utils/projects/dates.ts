/// <summary>
/// Vrátí datum ve formátu rrrr-mm-dd v místním čase.
/// </summary>
export function toIsoDate(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${date.getFullYear()}-${month}-${day}`
}
