import { useId, type InputHTMLAttributes, type ReactNode } from 'react'
import { panel } from './ui'

interface TextFieldProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'className'> {
  label: string
  helper?: string
  error?: string
  optional?: boolean
}

/** DESIGN.md §3 Form fields: label above, 44 px input, helper/error caption linked by aria-describedby. */
export function TextField({ label, helper, error, optional, id, ...input }: TextFieldProps) {
  const autoId = useId()
  const inputId = id ?? autoId
  const helperId = `${inputId}-help`
  const errorId = `${inputId}-error`
  const describedBy = [helper && helperId, error && errorId].filter(Boolean).join(' ') || undefined

  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={inputId} className="font-semibold">
        {label}
        {optional && <span className="font-normal text-ow-muted"> (optional)</span>}
      </label>
      <input
        id={inputId}
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy}
        className={`h-11 rounded-control border bg-ow-bg px-3 text-ow-text placeholder:text-ow-muted ${error ? 'border-ow-danger' : 'border-ow-border'}`}
        {...input}
      />
      {helper && (
        <p id={helperId} className="text-caption text-ow-muted">
          {helper}
        </p>
      )}
      {error && (
        <p id={errorId} className="text-caption text-ow-danger">
          {error}
        </p>
      )}
    </div>
  )
}

/** DESIGN.md §3 Checkbox card. */
export function CheckboxCard({
  title,
  description,
  ...input
}: { title: string; description?: string } & Omit<InputHTMLAttributes<HTMLInputElement>, 'type' | 'className'>) {
  return (
    <label className="flex cursor-pointer items-start gap-3 rounded-control border border-ow-border bg-ow-bg px-3.5 py-3">
      <input type="checkbox" className="mt-0.5 size-5 accent-ow-accent" {...input} />
      <span>
        <span className="block font-medium">{title}</span>
        {description && <span className="block text-body-sm text-ow-muted">{description}</span>}
      </span>
    </label>
  )
}

/** Form-level error (not tied to one field). Announced by screen readers. */
export function FormError({ message }: { message?: string }) {
  if (!message) return null
  return (
    <p role="alert" className="text-body-sm text-ow-danger">
      {message}
    </p>
  )
}

/** DESIGN.md §3 Status / confirmation: cyan-bordered panel, h3 + muted body + optional action. */
export function StatusPanel({ title, children, action }: { title: string; children?: ReactNode; action?: ReactNode }) {
  return (
    <div role="status" className={panel.status}>
      <h3 className="text-h3">{title}</h3>
      {children && <div className="text-ow-muted">{children}</div>}
      {action && <div className="pt-2">{action}</div>}
    </div>
  )
}
