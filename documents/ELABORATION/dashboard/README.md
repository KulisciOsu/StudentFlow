# KAN-80 – Wireframe úvodního dashboardu

## 1. Účel

Úvodní dashboard aplikace StudentFlow poskytuje přihlášenému uživateli přehled jeho projektů, úkolů a důležitých termínů.

Dashboard je společný pro studenty i vyučující. Zobrazovaná data a dostupné funkce se budou řídit rolí a oprávněními přihlášeného uživatele.

Návrh vychází z projektového dokumentu `documents/ELABORATION/Wireframe.pdf`, konkrétně z části **Wireframe 03**.

## 2. Responzivní varianty

| Varianta | Soubor | Rozložení |
|---|---|---|
| Desktop | `dashboard-wireframe.svg` | Tři projektové karty vedle sebe, tabulka úkolů |
| Tablet | `dashboard-tablet.svg` | Dvě projektové karty vedle sebe, kompaktní tabulka úkolů |
| Mobil | `dashboard-mobile.svg` | Projektové a úkolové karty pod sebou |

Všechny varianty používají jednotný černobílý vizuální styl.

## 3. Obsah dashboardu

### 3.1 Hlavička

Hlavička obsahuje:

- **Logo StudentFlow** – v budoucí aplikaci bude sloužit jako odkaz na úvodní dashboard.
- **Zvonek** – přístup k oznámením, včetně ukázkového počtu nepřečtených oznámení.
- **Uživatelský profil** – identifikace přihlášeného uživatele a budoucí přístup k nastavení a odhlášení.

Na mobilní variantě je profil zjednodušen na kulatý avatar s iniciálami.

### 3.2 Moje projekty

Sekce zobrazuje projekty, ve kterých je přihlášený uživatel členem nebo vedoucím týmu.

Každá projektová karta obsahuje:

- Název projektu.
- Název týmu a počet členů.
- Roli uživatele v projektu.
- Počet dokončených úkolů a ukazatel průběhu.
- Počet otevřených úkolů přihlášeného uživatele.
- Nejbližší projektový milník a jeho termín.
- Iniciály členů týmu v kulatých avatarech.
- Viditelné tlačítko **Zobrazit projekt →**.

Ukázkové projekty:

1. **Rezervační systém knihovny** – Tým Sova, role Vedoucí, dokončeny 3 z 10 úkolů.
2. **Analýza dat: semestrální práce** – Tým Delta, role Člen, dokončeno 5 z 8 úkolů.

Sekce dále obsahuje tlačítko **Založit projekt** a samostatnou kartu **Založit nový projekt**.

### 3.3 Moje úkoly napříč projekty

Sekce poskytuje přehled úkolů přihlášeného uživatele bez ohledu na projekt.

Obsahuje přepínač:

- **Otevřené** – dosud nedokončené úkoly.
- **Hotové** – dokončené úkoly.

Každý úkol zobrazuje:

- Název úkolu.
- Příslušný projekt.
- Termín dokončení.
- Prioritu.
- Aktuální stav.

Desktopová a tabletová varianta zobrazují úkoly v tabulce. Mobilní varianta používá samostatné karty kvůli lepší čitelnosti.

Wireframy obsahují tři ukázkové úkoly:

| Úkol | Projekt | Termín | Priorita | Stav |
|---|---|---|---|---|
| Čištění datasetu | Analýza dat | 24. 10. – po termínu | Vysoká | Rozpracováno |
| Wireframy obrazovek | Rezervační systém knihovny | 30. 10. | Střední | Rozpracováno |
| Regresní model | Analýza dat | 6. 11. | Střední | K řešení |

Úkoly po termínu jsou barevně zvýrazněny.

### 3.4 Připomenutí termínů

Dashboard obsahuje samostatný panel **Připomenutí termínu**, který upozorňuje uživatele na blížící se termíny.

Ukázkový reminder:

- **Úkol:** Regresní model
- **Projekt:** Analýza dat
- **Termín:** 6. 11.

Panel je zobrazen ve všech třech responzivních variantách.

V budoucí implementaci budou připomenutí vycházet ze skutečných termínů uživatelových úkolů.

## 4. Navigace a plánované interakce

| Prvek | Plánovaná funkce |
|---|---|
| Logo StudentFlow | Přechod na úvodní dashboard |
| Zvonek | Zobrazení oznámení |
| Uživatelský profil | Nastavení účtu a odhlášení |
| Založit projekt | Přechod k založení projektu |
| Projektová karta | Přechod na detail projektu |
| Zobrazit projekt → | Přechod na detail vybraného projektu |
| Otevřené / Hotové | Přepínání mezi skupinami úkolů |
| Úkol | Přechod na detail úkolu |

### Placeholder odkazy

Projektové karty obsahují zástupné SVG odkazy:

- `#detail-projektu-rezervacni-system`
- `#detail-projektu-analyza-dat`

Tyto odkazy představují plánovanou navigaci. **Nevedou zatím na skutečné stránky projektu.**

Viditelná tlačítka **Zobrazit projekt →** slouží k názornému zobrazení budoucího ovládání.

Funkční navigace bude realizována při implementaci frontendu.

## 5. Prázdné stavy

Při budoucí implementaci je nutné počítat se situacemi, kdy:

- Uživatel nemá žádný přiřazený projekt.
- Uživatel nemá žádné otevřené úkoly.
- Uživatel nemá žádné blížící se termíny.
- Uživatel nemá žádná nová oznámení.

V těchto případech se zobrazí odpovídající informační zpráva.

Možnost založení projektu bude záviset na oprávněních uživatele.

Prázdné stavy nejsou v aktuálních ukázkových SVG samostatně vykresleny.

## 6. Technické poznámky

- Wireframy jsou vytvořeny ve formátu SVG.
- Obsahují pouze smyšlená ukázková data.
- Nejedná se o funkční frontend aplikace.
- Placeholder odkazy nejsou napojeny na skutečné stránky.
- Přepínač Otevřené / Hotové je pouze vizuální návrh.
- Zvonek, uživatelský profil a založení projektu představují plánované funkce.
- Černobílý vzhled slouží jako sjednocený styl wireframů, nikoli jako závazné finální grafické řešení.
- Červená barva označuje prošlé termíny.
- Skutečná data dashboardu bude poskytovat backend API.

## 7. Navazující úkoly

| Jira | Popis |
|---|---|
| KAN-80 | Návrh wireframů dashboardu |
| KAN-81 | Implementace frontendové části dashboardu |
| KAN-82 | Implementace backendové části dashboardu |
| KAN-83 | Databázová část dashboardu |
| KAN-84 | Testování dashboardu |

## 8. Stav návrhu

Wireframy zahrnují desktopovou, tabletovou a mobilní variantu.

Návrh obsahuje všechny hlavní prvky dashboardu podle Wireframe 03, ukázkové projekty, úkoly, připomenutí termínů a placeholder odkazy na detaily projektů.

Další implementace bude vycházet ze schválených wireframů a navazujících požadavků projektu StudentFlow.