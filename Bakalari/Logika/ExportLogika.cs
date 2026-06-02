using System.Globalization;
using System.Linq;
using System.Text;
using Bakalari.Tridy;

namespace Bakalari.Logika;

/// <summary>
/// Obsahuje logiku pro export dat do souboru CSV.
/// CSV (Comma-Separated Values) je textový formát, který lze otevřít v Excelu nebo LibreOffice Calc.
/// </summary>
public static class ExportLogika
{
    /// <summary>
    /// Vygeneruje obsah CSV souboru se všemi studenty a jejich známkami.
    /// Každá řádka odpovídá jedné známce – student s deseti známkami bude mít deset řádků.
    /// Student bez známek se objeví jako jeden řádek s prázdnými sloupci pro známku.
    /// </summary>
    public static string GenerujCsv(Skola skola)
    {
        var sb = new StringBuilder();

        // Záhlaví CSV souboru – popisuje sloupce.
        sb.AppendLine("Třída,Příjmení,Jméno,Předmět,Datum,Poznámka,Hodnota,Váha");

        foreach (var student in skola.Studenti.OrderBy(s => s.Trida).ThenBy(s => s.Prijmeni).ThenBy(s => s.Jmeno))
        {
            if (student.Znamky.Count == 0)
            {
                // Student bez známek – zapíšeme ho s prázdnými sloupci pro známku.
                sb.AppendLine($"{student.Trida},{student.Prijmeni},{student.Jmeno},,,,, ");
                continue;
            }

            foreach (var z in student.Znamky)
            {
                var datum = z.DatumText;
                // Čárky v poznámce nahradíme středníkem, aby nenarušily CSV formát.
                var poznamka = z.Poznamka.Replace(",", ";");
                // InvariantCulture zajistí desetinnou tečku místo čárky (Excel to lépe rozpozná).
                var hodnota = z.Hodnota.ToString(CultureInfo.InvariantCulture);
                sb.AppendLine($"{student.Trida},{student.Prijmeni},{student.Jmeno},{z.Predmet.Nazev},{datum},{poznamka},{hodnota},{z.Vaha}");
            }
        }

        return sb.ToString();
    }
}
