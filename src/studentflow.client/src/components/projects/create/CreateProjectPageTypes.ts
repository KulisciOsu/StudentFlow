/// <summary>
/// Uchovává hodnoty jednotlivých polí formuláře pro založení projektu.
/// </summary>
export type FormValues = {
  projectName: string
  description: string
  deadline: string
  teamName: string
}

/// <summary>
/// Uchovává validační chybové zprávy jednotlivých polí formuláře.
/// </summary>
export type FormErrors = Partial<Record<keyof FormValues, string>>

/// <summary>
/// Definuje vlastnosti komponenty CreateProjectPage.
/// Obsahuje funkci pro návrat na Kanban nástěnku.
/// </summary>
export type CreateProjectPageProps = {
  onBack: () => void
}