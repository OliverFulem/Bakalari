using System.Text.Json.Serialization;

namespace Bakalari.Tridy;

/// <summary>
/// Představuje jeden vyučovací předmět (např. Matematika, ICT).
/// </summary>
public class Predmet
{
    /// <summary>Název předmětu zobrazovaný v UI.</summary>
    public string Nazev { get; set; }

    [JsonConstructor]
    public Predmet(string nazev)
    {
        Nazev = nazev;
    }

    // ToString() se použije automaticky v ComboBoxu místo výchozího výpisu typu.
    public override string ToString() => Nazev;
}
