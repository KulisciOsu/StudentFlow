/// <summary>
/// Vlastnosti formuláře „Přidat člena“.
/// </summary>
export interface AddMemberFormProps {
  /// <summary>
  /// Projekt, do kterého se člen přidává.
  /// </summary>
  projectId: number

  /// <summary>
  /// Zavolá se, když se členové na serveru mohli změnit; stránka pak seznam znovu načte.
  /// </summary>
  onChanged: () => void
}
