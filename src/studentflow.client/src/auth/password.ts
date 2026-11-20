/** Shodná pravidla jako RegistrationPasswordRules na serveru; mezery se nikdy automaticky neodstraňují. */
export function registrationPasswordError(password: string): string | undefined {
  const length = Array.from(password).length
  if (length < 8 || length > 15) return 'Heslo musí mít 8 až 15 znaků.'
  if (/[\p{White_Space}\uFEFF]/u.test(password)) return 'Heslo zadejte bez mezer, tabulátorů a odřádkování.'
  if (!/\p{Lu}/u.test(password)) return 'Přidejte do hesla alespoň jedno velké písmeno.'
  if (!/\p{Nd}/u.test(password)) return 'Přidejte do hesla alespoň jednu číslici.'
  if (!/[\p{P}\p{S}]/u.test(password)) return 'Přidejte do hesla alespoň jeden speciální znak, například !, ? nebo #.'
  return undefined
}
