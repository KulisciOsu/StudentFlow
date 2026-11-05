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

/** Zapojení formuláře do navigace a rozvržení aplikace. */
export type CreateProjectPageProps = {
  onBack: () => void
  /** Použije sekci uvnitř aplikace a vynechá samostatnou ukázkovou hlavičku účtu. */
  embedded?: boolean
}
