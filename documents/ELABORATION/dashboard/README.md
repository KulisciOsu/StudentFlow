# KAN-80 – Wireframe úvodního dashboardu

## Účel

Úvodní dashboard poskytuje přihlášenému uživateli přehled jeho projektů, úkolů a důležitých termínů.

Rozložení je společné pro studenty i vyučující. Zobrazená data a dostupné akce závisí na přihlášeném uživateli a jeho oprávněních.

Návrh vychází z projektového dokumentu `Wireframe.pdf` (Wireframe 03) a zachovává jednotný černobílý vizuální styl.

## Responzivní varianty

- **Desktop:** `dashboard-wireframe.svg`
- **Tablet:** `dashboard-tablet.svg`
- **Mobil:** `dashboard-mobile.svg`

Na desktopu jsou projektové karty vedle sebe. Tablet používá dvousloupcové rozložení. Na mobilu jsou projektové karty pod sebou a tabulku úkolů nahrazují samostatné karty.

## Obsah dashboardu

### Hlavička

- Logo StudentFlow – odkaz na úvodní dashboard.
- Zvonek – přístup k oznámením.
- Uživatelský profil – iniciály uživatele, nastavení a odhlášení.

### Moje projekty

- Seznam projektů přihlášeného uživatele.
- Název projektu a tým.
- Role uživatele v projektu.
- Průběh dokončení úkolů.
- Počet otevřených úkolů uživatele.
- Nejbližší projektový milník.
- Členové týmu zobrazení pomocí iniciál.
- Tlačítko **Založit projekt**.
- Karta pro založení nového projektu.

### Moje úkoly napříč projekty

- Přepínač **Otevřené / Hotové**.
- Název úkolu.
- Příslušný projekt.
- Termín dokončení.
- Priorita.
- Stav úkolu.
- Zvýraznění úkolů po termínu.

Na mobilních zařízeních jsou úkoly zobrazeny jako samostatné karty.

## Navigace a plánované interakce

- Kliknutí na logo StudentFlow → úvodní dashboard.
- Kliknutí na projekt → detail projektu.
- Kliknutí na úkol → detail úkolu.
- Založit projekt → formulář založení projektu (samostatný epik).
- Zvonek → oznámení.
- Uživatelský profil → nastavení a odhlášení.
- Otevřené / Hotové → přepínání seznamu úkolů.

Tyto interakce jsou zatím pouze návrhem. SVG wireframy nejsou funkční aplikací.

## Prázdný stav

Pokud uživatel nemá žádné projekty nebo úkoly, zobrazí se odpovídající informační zpráva.

Možnost založení projektu bude záviset na oprávněních uživatele.

## Poznámky

- Wireframy používají smyšlená ukázková data.
- Červená barva slouží pouze ke zvýraznění prošlých termínů.
- Skutečná data bude poskytovat backend API.
- Finální funkcionalita bude implementována v navazujících úkolech.
- Wireframy slouží jako podklad pro implementaci frontendové části dashboardu.