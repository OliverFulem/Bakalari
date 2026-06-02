using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Bakalari.Tridy;

/// <summary>
/// Kořenový datový model celé aplikace.
/// Obsahuje seznam všech studentů a seznam tříd s jejich předměty.
/// Tato třída se serializuje do JSON a ukládá na disk.
/// </summary>
public class Skola
{
    /// <summary>Všichni studenti evidovaní v systému.</summary>
    public ObservableCollection<Student> Studenti { get; set; }

    /// <summary>Třídy školy, každá obsahuje název a seznam předmětů.</summary>
    public ObservableCollection<TridaInfo> Tridy { get; set; }

    /// <summary>Vytvoří prázdnou školu s několika ukázkovými třídami.</summary>
    public Skola()
    {
        Studenti = new ObservableCollection<Student>();
        Tridy = new ObservableCollection<TridaInfo>
        {
            new TridaInfo("1.A"),
            new TridaInfo("1.B"),
            new TridaInfo("2.A")
        };
    }

    /// <summary>Konstruktor používaný při načítání ze souboru JSON.</summary>
    [JsonConstructor]
    public Skola(ObservableCollection<Student> studenti, ObservableCollection<TridaInfo> tridy)
    {
        Studenti = studenti ?? new ObservableCollection<Student>();
        Tridy = tridy ?? new ObservableCollection<TridaInfo>();
    }
}

/// <summary>
/// Informace o jedné třídě – její název a seznam předmětů, které se v ní vyučují.
/// </summary>
public class TridaInfo
{
    /// <summary>Označení třídy, např. "3.B".</summary>
    public string Nazev { get; set; }

    /// <summary>Předměty vyučované v této třídě. Nová třída dostane základní předměty automaticky.</summary>
    public ObservableCollection<string> Predmety { get; set; }

    [JsonConstructor]
    public TridaInfo(string nazev, ObservableCollection<string>? predmety = null)
    {
        Nazev = nazev;
        // Pokud třída nemá uložené předměty, nastaví se výchozí sada.
        Predmety = predmety ?? new ObservableCollection<string> { "Matematika", "ICT", "Český jazyk" };
    }
}
