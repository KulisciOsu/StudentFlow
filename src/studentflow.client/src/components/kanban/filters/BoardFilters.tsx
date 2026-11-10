import type { TaskPriority } from '../../../models/tasks/TaskTypes'
import { priorityLabels } from '../../../utils/tasks/taskDisplay'
import type { BoardFiltersProps } from './BoardFiltersTypes'
import './BoardFilters.scss'

/// <summary>
/// Zobrazuje hledání a filtry úkolů.
/// </summary>
export function BoardFilters({ filters, assignees, showOnlyMine, onChange }: BoardFiltersProps) {
  return (
    <div className="board-filters">
      <input
        type="search"
        placeholder="Hledat úkol"
        aria-label="Hledat úkol"
        value={filters.query}
        onChange={(event) => onChange({ ...filters, query: event.target.value })}
      />
      <select
        aria-label="Řešitel"
        value={filters.assignee}
        onChange={(event) => onChange({ ...filters, assignee: event.target.value })}
      >
        <option value="">Řešitel: všichni</option>
        {assignees.map((assignee) => (
          <option key={assignee} value={assignee}>{assignee}</option>
        ))}
      </select>
      <select
        aria-label="Priorita"
        value={filters.priority ?? ''}
        onChange={(event) => onChange({
          ...filters,
          priority: (event.target.value || null) as TaskPriority | null,
        })}
      >
        <option value="">Priorita: vše</option>
        {(Object.keys(priorityLabels) as TaskPriority[]).map((priority) => (
          <option key={priority} value={priority}>{priorityLabels[priority]}</option>
        ))}
      </select>
      {showOnlyMine && (
        <label className="board-filters-mine">
          <input
            type="checkbox"
            checked={filters.onlyMine}
            onChange={(event) => onChange({ ...filters, onlyMine: event.target.checked })}
          />
          Jen moje úkoly
        </label>
      )}
    </div>
  )
}
