import './MobileBottomNav.scss'

/// <summary>
/// Položky spodní navigace a jejich ikony.
/// </summary>
const items = [
  { label: 'Nástěnka', icon: 'M4 5h5v14H4zM13 5h7v7h-7z', active: true },
  { label: 'Plán', icon: 'M6 21V4h11l-2 4 2 4H6', active: false },
  { label: 'Statistiky', icon: 'M4 20h16M7 17v-5M12 17V7M17 17v-8', active: false },
  { label: 'Soubory', icon: 'M7 3h7l5 5v13H7zM14 3v5h5', active: false },
]

/// <summary>
/// Zobrazuje spodní navigaci a tlačítko pro nový úkol na mobilu. Tlačítka zatím nemají funkci.
/// </summary>
export function MobileBottomNav() {
  return (
    <>
      <button type="button" className="mobile-fab" aria-label="Nový úkol">
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" aria-hidden="true">
          <path d="M12 5v14M5 12h14" />
        </svg>
      </button>
      <nav className="mobile-bottom-nav" aria-label="Hlavní navigace">
        {items.map((item) => (
          <button
            key={item.label}
            type="button"
            className={item.active ? 'mobile-bottom-nav-item mobile-bottom-nav-item--active' : 'mobile-bottom-nav-item'}
            aria-current={item.active ? 'page' : undefined}
          >
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinejoin="round" strokeLinecap="round" aria-hidden="true">
              <path d={item.icon} />
            </svg>
            {item.label}
          </button>
        ))}
      </nav>
    </>
  )
}
