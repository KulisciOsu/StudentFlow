import type { ReactNode } from 'react'

/// <summary>
/// Vstupní parametry karty přihlašovacích obrazovek.
/// </summary>
export interface AuthCardProps {
  /// <summary>
  /// Identifikátor nadpisu.
  /// </summary>
  titleId: string

  /// <summary>
  /// Nadpis obrazovky.
  /// </summary>
  title: string

  /// <summary>
  /// Krátký popis pod nadpisem.
  /// </summary>
  lead: string

  /// <summary>
  /// Obsah karty.
  /// </summary>
  children: ReactNode
}
