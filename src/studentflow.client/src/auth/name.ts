/** Samostatné jméno a příjmení: 1–50 Unicode znaků, jen písmena a související diakritika. */
export function registrationNameError(value: string, label: string): string | undefined {
  const length = Array.from(value).length
  if (length === 0) return `Vyplňte prosím pole „${label}“.`
  if (length > 50) return `${label} může mít nejvýše 50 znaků.`
  if (/[\p{White_Space}\uFEFF]/u.test(value)) return `${label} zadejte bez mezer, tabulátorů a odřádkování.`
  if (!/^\p{L}[\p{L}\p{Mn}\p{Mc}]*$/u.test(value)) return `${label} může obsahovat pouze písmena a diakritiku.`
  return undefined
}
