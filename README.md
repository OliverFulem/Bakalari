# Bakaláři – Evidence studentů

Desktopová aplikace pro správu studentů a jejich klasifikace. Umožňuje vést přehled tříd, předmětů a hodnocení, počítat vážené průměry a exportovat výsledky. Napsána v C# (.NET 9) s frameworkem [Avalonia UI](https://avaloniaui.net/) – běží na Windows.

## Funkce

- Správa tříd a jejich předmětů
- Přidávání studentů ručně nebo hromadným importem ze souboru `.txt`
- Zadávání známek s hodnotou, váhou, datem a poznámkou (téma zkoušení)
- Vážený průměr za každý předmět i celkově, upozornění při ohroženém prospěchu
- Filtrování a vyhledávání studentů v hlavním okně
- Export klasifikace do CSV, záloha a obnova dat ve formátu JSON
- Podpora světlého i tmavého motivu (sleduje nastavení systému)

## Datový model

```mermaid
classDiagram
    class Skola {
        +List~Student~ Studenti
        +List~TridaInfo~ Tridy
    }
    class TridaInfo {
        +string Nazev
        +List~string~ Predmety
    }
    class Student {
        +string Jmeno
        +string Prijmeni
        +string Trida
        +List~Znamka~ Znamky
        +double CelkovyPrumer
    }
    class Znamka {
        +float Hodnota
        +int Vaha
        +DateOnly Datum
        +string Poznamka
        +Predmet Predmet
    }
    class Predmet {
        +string Nazev
    }

    Skola "1" --> "*" TridaInfo : spravuje
    Skola "1" --> "*" Student : eviduje
    Student "1" --> "*" Znamka : má
    Znamka "*" --> "1" Predmet : z předmětu
```

## Sestavení a spuštění

Vyžaduje [.NET 9 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet run --project Bakalari/Bakalari.csproj
```

Vydání pro Windows (samostatný `.exe` bez nutnosti instalace .NET) se sestavuje automaticky při každém pushnutí do větve `main` přes GitHub Actions a je dostupné v záložce **Releases**.
