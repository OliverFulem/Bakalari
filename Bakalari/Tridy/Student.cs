using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Bakalari.Tridy;

/// <summary>
/// Datový model jednoho studenta. Uchovává osobní údaje a kolekci jeho známek.
/// Implementuje INotifyPropertyChanged – při změně jakékoliv vlastnosti se UI automaticky překreslí.
/// </summary>
public class Student : INotifyPropertyChanged
{
    private string _jmeno = string.Empty;
    private string _prijmeni = string.Empty;
    private string _trida = string.Empty;

    public string Jmeno
    {
        get => _jmeno;
        // Při změně jména upozorníme UI i na změnu FullInfo (zobrazovaný řetězec).
        set { _jmeno = value; OnPropertyChanged(); OnPropertyChanged(nameof(FullInfo)); }
    }

    public string Prijmeni
    {
        get => _prijmeni;
        set { _prijmeni = value; OnPropertyChanged(); OnPropertyChanged(nameof(FullInfo)); }
    }

    /// <summary>Označení třídy, do které student patří, např. "2.A".</summary>
    public string Trida
    {
        get => _trida;
        set { _trida = value; OnPropertyChanged(); OnPropertyChanged(nameof(FullInfo)); }
    }

    /// <summary>Seznam všech známek studenta. ObservableCollection automaticky oznamuje změny UI.</summary>
    public ObservableCollection<Znamka> Znamky { get; set; }

    /// <summary>Konstruktor pro nového studenta vytvořeného v UI.</summary>
    public Student()
    {
        _trida = string.Empty;
        Znamky = new ObservableCollection<Znamka>();
        NastavitReakciNaZmenuZnamek();
    }

    /// <summary>Konstruktor používaný při načítání ze souboru JSON.</summary>
    [JsonConstructor]
    public Student(string jmeno, string prijmeni, string trida, ObservableCollection<Znamka> znamky)
    {
        _jmeno = jmeno;
        _prijmeni = prijmeni;
        _trida = trida;
        Znamky = znamky ?? new ObservableCollection<Znamka>();
        NastavitReakciNaZmenuZnamek();
    }

    /// <summary>
    /// Přihlásí reakci na změny v kolekci Znamky.
    /// Kdykoli se přidá nebo odebere známka, přepočítají se průměry a aktualizuje se UI.
    /// </summary>
    private void NastavitReakciNaZmenuZnamek()
    {
        Znamky.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(CelkovyPrumer));
            OnPropertyChanged(nameof(FullInfo));
            OnPropertyChanged(nameof(JeProspechOhrozen));
            OnPropertyChanged(nameof(PrumeryPoPredmetech));
            
        };
    }

    /// <summary>
    /// Vážený průměr přes všechny předměty.
    /// Vzorec: součet(hodnota × váha) / součet(váhy).
    /// Vrátí 0, pokud student nemá žádné známky.
    /// </summary>
    public double CelkovyPrumer => Znamky.Count == 0
        ? 0
        : Znamky.Sum(z => z.Hodnota * z.Vaha) / (double)Znamky.Sum(z => z.Vaha);

    /// <summary>Vrátí true, pokud má student z jakéhokoliv předmětu vážený průměr horší než 4,0.</summary>
    public bool JeProspechOhrozen => PrumeryPoPredmetech.Any(p => p.JeOhrozen);

    /// <summary>
    /// Vážené průměry rozdělené podle předmětů.
    /// Výsledek se používá v okně s detailem studenta.
    /// </summary>
    public List<SubjectAverage> PrumeryPoPredmetech => Znamky
        .GroupBy(z => z.Predmet.Nazev)
        .Select(g =>
        {
            double prumer = g.Sum(z => z.Hodnota * z.Vaha) / (double)g.Sum(z => z.Vaha);
            return new SubjectAverage
            {
                Predmet = g.Key,
                Prumer = prumer,
                JeOhrozen = prumer > 4.0
            };
        })
        .ToList();

    /// <summary>Zkrácený řetězec pro zobrazení v seznamu, např. "Jan Novák | Průměr: 2,50".</summary>
    public string FullInfo => $"{Jmeno} {Prijmeni} | Průměr: {CelkovyPrumer:0.00}";

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public override string ToString() => FullInfo;
}

/// <summary>
/// Pomocná třída pro zobrazení průměru jednoho předmětu v detailu studenta.
/// </summary>
public class SubjectAverage
{
    /// <summary>Název předmětu.</summary>
    public string Predmet { get; set; } = string.Empty;

    /// <summary>Vážený průměr ze všech známek daného předmětu.</summary>
    public double Prumer { get; set; }

    /// <summary>True, pokud je průměr horší než 4,0 – signalizuje ohrožení prospěchu.</summary>
    public bool JeOhrozen { get; set; }
}
