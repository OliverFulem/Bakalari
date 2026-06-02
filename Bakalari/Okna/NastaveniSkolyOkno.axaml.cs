using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Bakalari.Tridy;
using System.Linq;

namespace Bakalari.Okna;

/// <summary>
/// Dialog pro správu tříd a předmětů školy a pro zálohu nebo obnovu dat.
/// Změny se projevují přímo v modelu _skola – není třeba je potvrzovat tlačítkem.
/// </summary>
public partial class NastaveniSkolyOkno : Window
{
    private Skola? _skola;

    /// <summary>Aktuálně vybraná třída v seznamu tříd.</summary>
    private TridaInfo? _vybranaTrida;

    public NastaveniSkolyOkno()
    {
        InitializeComponent();
    }

    public NastaveniSkolyOkno(Skola skola)
    {
        InitializeComponent();
        _skola = skola;

        // Napojíme seznam tříd přímo na kolekci – nové třídy se zobrazí okamžitě.
        ClassesListBox.ItemsSource = _skola.Tridy;

        ClassesListBox.SelectionChanged += (s, e) =>
        {
            if (ClassesListBox.SelectedItem is TridaInfo trida)
            {
                _vybranaTrida = trida;
                // Zobrazíme předměty vybrané třídy a povolíme jejich editaci.
                SubjectsListBox.ItemsSource = trida.Predmety;
                NewSubjectTextBox.IsEnabled = true;
                AddSubjectButton.IsEnabled = true;
                DeleteSubjectButton.IsEnabled = true;
            }
            else
            {
                _vybranaTrida = null;
                SubjectsListBox.ItemsSource = null;
                NewSubjectTextBox.IsEnabled = false;
                AddSubjectButton.IsEnabled = false;
                DeleteSubjectButton.IsEnabled = false;
            }
        };

        AddClassButton.Click += (s, e) =>
        {
            // Přidáme třídu jen pokud má název a ještě neexistuje.
            if (_skola != null && !string.IsNullOrWhiteSpace(NewClassTextBox.Text) &&
                !_skola.Tridy.Any(t => t.Nazev == NewClassTextBox.Text))
            {
                _skola.Tridy.Add(new TridaInfo(NewClassTextBox.Text.Trim()));
                NewClassTextBox.Text = string.Empty;
            }
        };

        DeleteClassButton.Click += (s, e) =>
        {
            if (_skola != null && ClassesListBox.SelectedItem is TridaInfo trida)
                _skola.Tridy.Remove(trida);
        };

        AddSubjectButton.Click += (s, e) =>
        {
            // Předmět přidáme jen pokud není prázdný a ve třídě ještě není.
            if (_vybranaTrida != null && !string.IsNullOrWhiteSpace(NewSubjectTextBox.Text) &&
                !_vybranaTrida.Predmety.Contains(NewSubjectTextBox.Text))
            {
                _vybranaTrida.Predmety.Add(NewSubjectTextBox.Text.Trim());
                NewSubjectTextBox.Text = string.Empty;
            }
        };

        DeleteSubjectButton.Click += (s, e) =>
        {
            if (_vybranaTrida != null && SubjectsListBox.SelectedItem is string predmet)
                _vybranaTrida.Predmety.Remove(predmet);
        };

        ExportDataButton.Click += async (s, e) =>
        {
            if (_skola == null) return;

            // Otevřeme dialog pro uložení souboru.
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Exportovat data školy",
                SuggestedFileName = "bakalari_data.json",
                FileTypeChoices = new[] { new FilePickerFileType("JSON soubor") { Patterns = new[] { "*.json" } } }
            });

            if (file == null) return;

            await using var stream = await file.OpenWriteAsync();
            await SpravceDat.ExportujDataAsync(_skola, stream);
            DataStatusText.Text = $"Data exportována do {file.Name}.";
        };

        ImportDataButton.Click += async (s, e) =>
        {
            if (_skola == null) return;

            // Otevřeme dialog pro výběr souboru.
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Importovat data školy",
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType("JSON soubor") { Patterns = new[] { "*.json" } } }
            });

            if (files.Count == 0) return;

            await using var stream = await files[0].OpenReadAsync();
            var noveData = await SpravceDat.ImportujDataAsync(stream);

            if (noveData == null)
            {
                DataStatusText.Text = "Chyba: soubor nelze načíst.";
                return;
            }

            // Nahradíme stávající data novými – vyčistíme kolekce a naplníme je znovu.
            // Tím zůstanou zachovány reference na kolekce, které mají přihlášené odběratele změn.
            _skola.Studenti.Clear();
            _skola.Tridy.Clear();
            foreach (var t in noveData.Tridy) _skola.Tridy.Add(t);
            foreach (var s2 in noveData.Studenti) _skola.Studenti.Add(s2);

            SpravceDat.Ulozit(_skola);
            DataStatusText.Text = $"Importováno {noveData.Studenti.Count} studentů a {noveData.Tridy.Count} tříd.";
        };

        CloseButton.Click += (s, e) => Close();

        // Na začátku jsou ovládací prvky předmětů zakázány, dokud uživatel nevybere třídu.
        NewSubjectTextBox.IsEnabled = false;
        AddSubjectButton.IsEnabled = false;
        DeleteSubjectButton.IsEnabled = false;
    }
}
