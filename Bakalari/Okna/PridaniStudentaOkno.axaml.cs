using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Bakalari.Tridy;
using System.IO;
using System.Linq;

namespace Bakalari.Okna;

/// <summary>
/// Dialog pro přidávání nových studentů.
/// Okno zůstává otevřené – uživatel může přidat více studentů najednou
/// nebo importovat celý seznam z textového souboru.
/// </summary>
public partial class PridaniStudentaOkno : Window
{
    private Skola? _skola;

    /// <summary>Počet studentů přidaných v tomto sezení (od otevření okna).</summary>
    public int PocetPridanych { get; private set; }

    public PridaniStudentaOkno()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Otevře dialog s vyplněným seznamem dostupných tříd.
    /// </summary>
    /// <param name="skola">Model školy, do kterého se noví studenti přidávají přímo.</param>
    public PridaniStudentaOkno(Skola skola)
    {
        InitializeComponent();

        _skola = skola;
        // Naplníme seznam tříd z modelu školy.
        ClassComboBox.ItemsSource = skola.Tridy.Select(t => t.Nazev).ToList();
        ClassComboBox.SelectedIndex = 0;

        AddButton.Click += (sender, e) => PridatStudenta();
        CloseButton.Click += (sender, e) => Close();

        
        NameTextBox.KeyDown += (sender, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter)
            {
                e.Handled = true;
                PridatStudenta();
            }
        };
        SurnameTextBox.KeyDown += (sender, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter)
            {
                e.Handled = true;
                PridatStudenta();
            }
        };
        this.KeyDown += (sender, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Escape)
            {
                e.Handled = true;
                Close();
            }
        };

        ImportButton.Click += async (sender, e) =>
        {
            if (ClassComboBox.SelectedItem is not string vybranaTrida) return;

            // Otevřeme dialog pro výběr souboru .txt.
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Importovat studenty ze souboru",
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType("Textový soubor") { Patterns = new[] { "*.txt" } } }
            });

            if (files.Count == 0) return;

            await using var stream = await files[0].OpenReadAsync();
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();

            int count = 0;
            // Každý řádek souboru je ve tvaru "Jméno Příjmení".
            foreach (var line in content.Split('\n'))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                // Rozdělíme na jméno (před první mezerou) a příjmení (zbytek).
                var spaceIndex = trimmed.IndexOf(' ');
                if (spaceIndex < 0) continue;

                var jmeno = trimmed[..spaceIndex].Trim();
                var prijmeni = trimmed[(spaceIndex + 1)..].Trim();
                if (string.IsNullOrWhiteSpace(jmeno) || string.IsNullOrWhiteSpace(prijmeni)) continue;

                _skola!.Studenti.Add(new Student { Jmeno = jmeno, Prijmeni = prijmeni, Trida = vybranaTrida });
                PocetPridanych++;
                count++;
            }

            AktualizujStatus($"Importováno {count} studentů ze souboru.");
        };
    }

    /// <summary>
    /// Přečte jméno, příjmení a třídu z formuláře, vytvoří nového studenta a přidá ho do modelu.
    /// Pokud je formulář neúplný, metoda nic neudělá.
    /// Po úspěšném přidání se pole vyčistí a kurzor se vrátí do pole Jméno.
    /// </summary>
    private void PridatStudenta()
    {
        if (_skola == null ||
            string.IsNullOrWhiteSpace(NameTextBox.Text) ||
            string.IsNullOrWhiteSpace(SurnameTextBox.Text) ||
            ClassComboBox.SelectedItem is not string vybranaTrida)
            return;

        _skola.Studenti.Add(new Student
        {
            Jmeno = NameTextBox.Text.Trim(),
            Prijmeni = SurnameTextBox.Text.Trim(),
            Trida = vybranaTrida
        });

        PocetPridanych++;
        NameTextBox.Text = string.Empty;
        SurnameTextBox.Text = string.Empty;
        NameTextBox.Focus();
        AktualizujStatus($"Přidáno celkem: {PocetPridanych} studentů.");
    }

    /// <summary>Zobrazí zprávu o výsledku akce v dolní části okna.</summary>
    private void AktualizujStatus(string zprava)
    {
        StatusText.Text = zprava;
    }
}
