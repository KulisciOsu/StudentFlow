/// <summary>
/// Pole formuláře pro založení projektu.
/// </summary>
export type CreateProjectField = 'name' | 'description' | 'deadline' | 'teamName'

/// <summary>
/// Hodnoty formuláře pro založení projektu.
/// </summary>
export type CreateProjectFormValues = Record<CreateProjectField, string>

/// <summary>
/// Chybové hlášky formuláře podle polí.
/// </summary>
export type CreateProjectErrors = Partial<Record<CreateProjectField, string>>

/// <summary>
/// Požadavek na založení projektu a týmu.
/// </summary>
export interface CreateProjectRequest {
  /// <summary>
  /// Název projektu.
  /// </summary>
  name: string

  /// <summary>
  /// Popis projektu. Bez popisu je hodnota null.
  /// </summary>
  description: string | null

  /// <summary>
  /// Plánovaný termín odevzdání ve formátu rrrr-mm-dd.
  /// </summary>
  deadline: string

  /// <summary>
  /// Název nového týmu.
  /// </summary>
  teamName: string
}

/// <summary>
/// Založený projekt.
/// </summary>
export interface CreatedProject {
  /// <summary>
  /// Identifikátor projektu.
  /// </summary>
  id: number

  /// <summary>
  /// Název projektu.
  /// </summary>
  name: string

  /// <summary>
  /// Identifikátor týmu projektu.
  /// </summary>
  teamId: number
}
