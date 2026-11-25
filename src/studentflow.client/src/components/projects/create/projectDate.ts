/// <summary>
/// Vrátí dnešní datum v pásmu Europe/Prague bez závislosti na pásmu prohlížeče.
/// </summary>
const pragueDateFormatter = new Intl.DateTimeFormat('en-GB', {
  timeZone: 'Europe/Prague',
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
})

export function todayPragueDate(instant: Date = new Date()): string {
  const parts = pragueDateFormatter.formatToParts(instant)
  const year = parts.find(part => part.type === 'year')?.value
  const month = parts.find(part => part.type === 'month')?.value
  const day = parts.find(part => part.type === 'day')?.value

  if (!year || !month || !day) throw new Error('Nepodařilo se určit dnešní datum.')
  return `${year}-${month}-${day}`
}
