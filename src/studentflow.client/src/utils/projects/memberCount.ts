/// <summary>
/// Vrátí počet členů se správným tvarem slova: 1 člen, 2–4 členové, jinak členů.
/// </summary>
export function formatMemberCount(count: number): string {
  if (count === 1) return '1 člen'
  if (count >= 2 && count <= 4) return `${count} členové`
  return `${count} členů`
}
