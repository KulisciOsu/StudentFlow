/// <summary>
/// Vstupní parametry textového pole formuláře.
/// </summary>
export interface FormFieldProps {
  /// <summary>
  /// Identifikátor pole.
  /// </summary>
  id: string

  /// <summary>
  /// Popisek pole.
  /// </summary>
  label: string

  /// <summary>
  /// Hodnota pole.
  /// </summary>
  value: string

  /// <summary>
  /// Zavolá se při změně hodnoty.
  /// </summary>
  onChange: (value: string) => void

  /// <summary>
  /// Určuje, zda je pole povinné.
  /// </summary>
  required?: boolean

  /// <summary>
  /// Chybová hláška pole.
  /// </summary>
  error?: string

  /// <summary>
  /// Zobrazí víceřádkové pole.
  /// </summary>
  multiline?: boolean

  /// <summary>
  /// Počet řádků víceřádkového pole.
  /// </summary>
  rows?: number

  /// <summary>
  /// Typ jednořádkového pole.
  /// </summary>
  type?: 'text' | 'date' | 'email' | 'password'

  /// <summary>
  /// Nápověda uvnitř prázdného pole.
  /// </summary>
  placeholder?: string

  /// <summary>
  /// Nejvyšší počet znaků.
  /// </summary>
  maxLength?: number

  /// <summary>
  /// Nejnižší povolená hodnota (u data).
  /// </summary>
  min?: string

  /// <summary>
  /// Nápověda prohlížeči pro automatické vyplnění.
  /// </summary>
  autoComplete?: string
}
