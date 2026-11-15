/// <summary>
/// Vrátí iniciály pro avatar člena: první znak prvního a posledního slova jména, velkými písmeny.
/// </summary>
/// <remarks>Pracuje se znaky Unicode, takže nerozdělí diakritiku ani emoji; prázdné jméno vrátí otazník.</remarks>
export function memberInitials(displayName: string): string {
  const words = displayName.trim().split(/\s+/u).filter(word => word.length > 0)
  if (words.length === 0) return '?'

  const first = Array.from(words[0])[0]
  const last = words.length > 1 ? Array.from(words[words.length - 1])[0] : ''
  return (first + last).toLocaleUpperCase('cs-CZ')
}
