/// <summary>
/// Cíl odkazu na přehled přihlášeného uživatele.
/// </summary>
export interface HomeLink {
  /// <summary>
  /// Adresa přehledu podle typu účtu.
  /// </summary>
  path: string

  /// <summary>
  /// Text odkazu v drobečkové navigaci.
  /// </summary>
  label: string
}

/// <summary>
/// Přejde v aplikaci na zadanou adresu bez nového načtení stránky.
/// </summary>
export type NavigateTo = (path: string) => void
