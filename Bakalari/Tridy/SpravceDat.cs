using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Bakalari.Tridy;

/// <summary>
/// Stará se o ukládání a načítání dat aplikace.
/// Data jsou uložena ve formátu JSON v systémové složce AppData uživatele.
/// Všechny metody jsou statické – není třeba vytvářet instanci třídy.
/// </summary>
public static class SpravceDat
{
    // Cesta ke složce a souboru s daty – např. C:\Users\jmeno\AppData\Roaming\Bakalari\data.json
    private static readonly string FolderPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Bakalari"
    );

    private static readonly string FilePath = Path.Combine(FolderPath, "data.json");

    // Nastavení JSON serializéru: hezké odsazení a ignorování cyklických referencí.
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    /// <summary>
    /// Uloží celý model školy do JSON souboru na disk.
    /// Složka se vytvoří automaticky, pokud ještě neexistuje.
    /// </summary>
    public static void Ulozit(Skola skola)
    {
        try
        {
            if (!Directory.Exists(FolderPath))
                Directory.CreateDirectory(FolderPath);

            string json = JsonSerializer.Serialize(skola, Options);
            File.WriteAllText(FilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba při ukládání dat: {ex.Message}");
        }
    }

    /// <summary>
    /// Načte data ze souboru JSON. Pokud soubor neexistuje nebo je poškozený,
    /// vrátí prázdnou školu s výchozím nastavením.
    /// </summary>
    public static Skola Nacist()
    {
        try
        {
            if (!File.Exists(FilePath))
                return new Skola();

            string json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<Skola>(json, Options) ?? new Skola();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba při načítání dat: {ex.Message}");
            return new Skola();
        }
    }

    /// <summary>
    /// Exportuje data školy do libovolného streamu (např. souboru zvoleného uživatelem).
    /// Používá se při záloze dat přes dialog "Exportovat data".
    /// </summary>
    public static async Task ExportujDataAsync(Skola skola, Stream stream)
    {
        string json = JsonSerializer.Serialize(skola, Options);
        await using var writer = new StreamWriter(stream, Encoding.UTF8);
        await writer.WriteAsync(json);
    }

    /// <summary>
    /// Načte data ze streamu (např. souboru zvoleného uživatelem) a vrátí objekt Skola.
    /// Vrátí null, pokud soubor není platný JSON nebo má nesprávný formát.
    /// Používá se při obnově dat přes dialog "Importovat data".
    /// </summary>
    public static async Task<Skola?> ImportujDataAsync(Stream stream)
    {
        try
        {
            using var reader = new StreamReader(stream, Encoding.UTF8);
            string json = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<Skola>(json, Options);
        }
        catch
        {
            return null;
        }
    }
}
