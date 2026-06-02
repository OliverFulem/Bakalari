using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Bakalari.Tridy;

/// <summary>
/// Pomocná třída pro zobrazení jedné třídy v hlavním okně.
/// Seskupuje studenty stejné třídy dohromady a počítá průměr třídy.
/// Používá se jako datový zdroj pro ItemsControl (seznam tříd s rozbalovacím panelem).
/// </summary>
public class ClassGroup
{
    /// <summary>Název třídy, např. "2.A".</summary>
    public string ClassName { get; }

    /// <summary>Studenti patřící do této třídy v aktuálně zobrazené skupině.</summary>
    public ObservableCollection<Student> Students { get; }

    /// <summary>Počet studentů v této skupině – zobrazuje se v záhlaví třídy.</summary>
    public int StudentCount => Students.Count;

    /// <summary>
    /// Průměr třídy vypočítaný jako průměr váženых průměrů jednotlivých studentů.
    /// Studenti bez známek do průměru nevstupují.
    /// Tím má každý student stejnou váhu bez ohledu na počet jeho známek.
    /// </summary>
    public double ClassAverage
    {
        get
        {
            var sZnamkami = Students.Where(s => s.Znamky.Any()).ToList();
            return sZnamkami.Count == 0 ? 0 : sZnamkami.Average(s => s.CelkovyPrumer);
        }
    }

    /// <summary>
    /// Vytvoří skupinu pro danou třídu se zadanými studenty.
    /// </summary>
    public ClassGroup(string className, IEnumerable<Student> students)
    {
        ClassName = className;
        Students = new ObservableCollection<Student>(students);
    }
}
