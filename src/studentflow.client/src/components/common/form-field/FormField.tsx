import type { FormFieldProps } from './FormFieldTypes'
import './FormField.scss'

/// <summary>
/// Zobrazuje popisek, textové pole a chybovou hlášku formuláře.
/// </summary>
export function FormField({
  id, label, value, onChange, required, error, multiline, rows = 4,
  type = 'text', placeholder, maxLength, min, autoComplete,
}: FormFieldProps) {
  const errorId = `${id}-error`
  const commonProps = {
    id,
    value,
    placeholder,
    maxLength,
    required,
    'aria-invalid': error ? true : undefined,
    'aria-describedby': error ? errorId : undefined,
  }

  return (
    <div className={`form-field${error ? ' form-field--invalid' : ''}`}>
      <label htmlFor={id}>
        {label}
        {required && <span aria-hidden="true"> *</span>}
      </label>
      {multiline ? (
        <textarea {...commonProps} rows={rows} onChange={(event) => onChange(event.target.value)} />
      ) : (
        <input {...commonProps} type={type} min={min} autoComplete={autoComplete} onChange={(event) => onChange(event.target.value)} />
      )}
      {error && <p id={errorId} className="form-field__error" role="alert">{error}</p>}
    </div>
  )
}
