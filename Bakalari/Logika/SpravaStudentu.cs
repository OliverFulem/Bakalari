using System.Collections.Generic;
using System.Linq;
using Bakalari.Tridy;

namespace Bakalari.Logika;

/// <summary>
/// Obsahuje logiku pro filtrování, řazení a vyhledávání studentů.
/// Oddělená od UI – hlavní okno zavolá metodu a dostane připravená data pro zobrazení.
/// </summary>
public static class SpravaStudentu
{
    /// <summary>
    /// Vrátí seznam skupin (tříd) studentů podle zadaných kritérií.
    /// Metoda se volá vždy, když uživatel změní filtr, řazení nebo hledaný text.
    /// </summary>
    /// <param name="skola">Zdrojová data se všemi studenty.</param>
    /// <param name="vybranaTrída">Název vybrané třídy nebo "Vše" pro zobrazení všech.</param>
    /// <param name="razeni">Způsob řazení – "Příjmení" nebo cokoliv jiného (= průměr).</param>
    /// <param name="hledanyText">Text pro vyhledávání podle jména nebo příjmení. Prázdný = bez filtru.</param>
    public static IEnumerable<ClassGroup> ZiskejSkupiny(
        Skola skola,
        string vybranaTrída,
        string razeni,
        string hledanyText)
    {
        var studenti = skola.Studenti.AsEnumerable();

        // Filtrujeme podle hledaného textu – porovnáváme malá písmena, aby hledání nerozlišovalo velikost.
        if (!string.IsNullOrWhiteSpace(hledanyText))
        {
            var text = hledanyText.Trim().ToLower();
            studenti = studenti.Where(s =>
                s.Jmeno.ToLower().Contains(text) ||
                s.Prijmeni.ToLower().Contains(text));
        }

        // Seskupíme studenty podle třídy a případně omezíme na vybranou třídu.
        var groups = studenti
            .GroupBy(s => s.Trida)
            .Where(g => vybranaTrída == "Vše" || g.Key == vybranaTrída);

        var result = new List<ClassGroup>();
        foreach (var group in groups)
        {
            // Seřadíme studenty uvnitř každé třídy.
            IEnumerable<Student> sorted = group;
            if (razeni == "Příjmení")
                sorted = sorted.OrderBy(s => s.Prijmeni).ThenBy(s => s.Jmeno);
            else
                sorted = sorted.OrderBy(s => s.CelkovyPrumer);

            result.Add(new ClassGroup(group.Key, sorted));
        }

        // Prázdné třídy zobrazujeme jen bez aktivního hledání – při hledání by matly.
        if (string.IsNullOrWhiteSpace(hledanyText))
        {
            if (vybranaTrída == "Vše")
            {
                // Přidáme třídy, ve kterých aktuálně není žádný student.
                foreach (var trida in skola.Tridy)
                {
                    if (!result.Any(g => g.ClassName == trida.Nazev))
                        result.Add(new ClassGroup(trida.Nazev, Enumerable.Empty<Student>()));
                }
            }
            else if (!result.Any())
            {
                // Vybraná třída neobsahuje žádného studenta – zobrazíme ji prázdnou.
                result.Add(new ClassGroup(vybranaTrída, Enumerable.Empty<Student>()));
            }
        }

        // Třídy seřadíme abecedně podle názvu.
        return result.OrderBy(g => g.ClassName);
    }
}
