using Avalonia.Controls;
using Bakalari.Tridy;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bakalari.Okna;

/// <summary>
/// Dialog pro zadávání a správu známek vybraného studenta.
/// Okno zůstává otevřené – uživatel může přidat více známek najednou.
/// </summary>
public partial class PridaniZnamekOkno : Window
{
    private Student? _student;

    public PridaniZnamekOkno()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Otevře dialog se seznamem stávajících známek a formulářem pro přidání nové.
    /// </summary>
    /// <param name="student">Student, jehož známky se upravují.</param>
    /// <param name="dostupnePredmety">Seznam předmětů dostupných v dané třídě.</param>
    public PridaniZnamekOkno(Student student, IEnumerable<string> dostupnePredmety)
    {
        InitializeComponent();

        _student = student;

        // Zobrazíme jméno a třídu studenta v záhlaví okna.
        StudentJmenoText.Text = $"{student.Jmeno} {student.Prijmeni}";
        StudentTridaText.Text = student.Trida;

        // Napojíme seznam známek přímo na kolekci studenta – změny se projeví okamžitě.
        ZnamkyListBox.ItemsSource = _student.Znamky;

        // Předměty přebalíme do objektů Predmet, aby ComboBox zobrazoval název.
        PredmetComboBox.ItemsSource = dostupnePredmety.Select(p => new Predmet(p)).ToList();
        PredmetComboBox.SelectedIndex = 0;

        DatumPicker.SelectedDate = DateTimeOffset.Now;

        // Váhu naplníme čísly 1–10.
        VahaPicker.ItemsSource = Enumerable.Range(1, 10).ToList();
        VahaPicker.SelectedIndex = 0;

        SaveButton.Click += (sender, e) => UlozitZnamku();
        CloseButton.Click += (sender, e) => Close();

        // Enter v poli pro hodnotu funguje jako kliknutí na Přidat.
        ZnamkaTextBox.KeyDown += (sender, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter)
                UlozitZnamku();
        };
    }

    /// <summary>
    /// Ověří vstupní data a přidá novou známku studentovi.
    /// Po úspěšném uložení se formulář vyresetuje a kurzor se vrátí do pole Hodnota.
    /// </summary>
    private void UlozitZnamku()
    {
        // Ověříme, že je vybrán předmět a zadána platná hodnota (číslo 1–5).
        if (_student == null ||
            PredmetComboBox.SelectedItem is not Predmet vybranyPredmet ||
            !float.TryParse(ZnamkaTextBox.Text, out float hodnota) ||
            hodnota < 1 || hodnota > 5)
        {
            ValidationErrorText.IsVisible = true;
            return;
        }

        // Načteme datum z pickeru, případně použijeme dnešní datum jako zálohu.
        var datum = DatumPicker.SelectedDate.HasValue
            ? DateOnly.FromDateTime(DatumPicker.SelectedDate.Value.DateTime)
            : DateOnly.FromDateTime(DateTime.Today);

        var poznamka = PoznamkaTextBox.Text?.Trim() ?? string.Empty;
        var vaha = VahaPicker.SelectedItem is int v ? v : 1;

        ValidationErrorText.IsVisible = false;
        _student.Znamky.Add(new Znamka(hodnota, vybranyPredmet, datum, poznamka, vaha));

        // Vyresetujeme formulář pro zadání další známky.
        ZnamkaTextBox.Text = string.Empty;
        PoznamkaTextBox.Text = string.Empty;
        VahaPicker.SelectedIndex = 0;
        DatumPicker.SelectedDate = DateTimeOffset.Now;
        ZnamkaTextBox.Focus();
    }

    /// <summary>
    /// Smaže vybranou známku ze seznamu.
    /// Tlačítko Smazat v každém řádku seznamu má v Tag nastavenou příslušnou Znamku.
    /// </summary>
    public void DeleteMark_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button { Tag: Znamka znamka } && _student != null)
        {
            _student.Znamky.Remove(znamka);
        }
    }
}
