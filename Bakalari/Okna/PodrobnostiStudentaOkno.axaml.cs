using Avalonia.Controls;
using Bakalari.Tridy;
using System.Collections.Generic;
using System.Linq;

namespace Bakalari.Okna;

/// <summary>
/// Dialog zobrazující podrobný přehled studenta:
/// celkový průměr, průměry podle předmětů a historii všech známek.
/// Umožňuje také přeřazení studenta do jiné třídy nebo jeho smazání.
/// </summary>
public partial class PodrobnostiStudentaOkno : Window
{
    private Student? _student;

    /// <summary>
    /// True, pokud uživatel klikl na "Smazat studenta".
    /// Hlavní okno tuto vlastnost zkontroluje po zavření dialogu a studenta odstraní.
    /// </summary>
    public bool IsDeleted { get; private set; }

    public PodrobnostiStudentaOkno()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Otevře detail studenta a naplní všechny zobrazované hodnoty.
    /// </summary>
    /// <param name="student">Student, jehož detail se zobrazuje.</param>
    /// <param name="dostupneTridy">Seznam tříd pro přeřazení studenta.</param>
    public PodrobnostiStudentaOkno(Student student, IEnumerable<string> dostupneTridy)
    {
        InitializeComponent();

        _student = student;

        JmenoText.Text = $"{student.Jmeno} {student.Prijmeni}";
        PrumerText.Text = $"{student.CelkovyPrumer:0.00}";

        // Napojíme seznam průměrů na ListBox předmětů – kliknutím lze filtrovat historii.
        SubjectAveragesListBox.ItemsSource = student.PrumeryPoPredmetech;

        // Napojíme seznam známek přímo na model studenta.
        ZnamkyListBox.ItemsSource = student.Znamky;

        // Při každé změně výběru předmětů přefiltrujeme historii známek.
        SubjectAveragesListBox.SelectionChanged += (s, e) => AktualizujFiltrZnamek();

        // Naplníme seznam tříd a předvybereme aktuální třídu studenta.
        ClassComboBox.ItemsSource = dostupneTridy;
        ClassComboBox.SelectedItem = student.Trida;

        // Při změně výběru třídy okamžitě aktualizujeme model.
        ClassComboBox.SelectionChanged += (s, e) =>
        {
            if (ClassComboBox.SelectedItem is string novaTrida)
                student.Trida = novaTrida;
        };

        DeleteStudentButton.Click += (s, e) =>
        {
            // Nastavíme příznak – skutečné odebrání provede hlavní okno po zavření dialogu.
            IsDeleted = true;
            Close();
        };

        // Zobrazíme varování, pokud je prospěch studenta ohrožen.
        WarningBorder.IsVisible = student.JeProspechOhrozen;

        CloseButton.Click += (sender, e) => Close();
        
        // this.KeyDown += (sender, e) =>
        // {
        //     if (e.Key == Avalonia.Input.Key.Enter)
        //     {
        //         e.Handled = true;
        //         ();
        //     }
        // };
        
        this.KeyDown += (sender, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Escape)
            {
                e.Handled = true;
                Close();
            }
        };
    }

    /// <summary>
    /// Filtruje seznam známek podle aktuálně vybraných předmětů v SubjectAveragesListBox.
    /// Pokud není vybrán žádný předmět, zobrazí se všechny známky.
    /// Nadpis nad historií se dynamicky aktualizuje.
    /// </summary>
    private void AktualizujFiltrZnamek()
    {
        if (_student == null) return;

        // Zjistíme názvy všech vybraných předmětů.
        var vybranePredmety = SubjectAveragesListBox.SelectedItems?
            .OfType<SubjectAverage>()
            .Select(sa => sa.Predmet)
            .ToHashSet() ?? new HashSet<string>();

        if (vybranePredmety.Count == 0)
        {
            // Žádný filtr – zobrazíme celou historii.
            ZnamkyListBox.ItemsSource = _student.Znamky;
            ZnamkyNadpis.Text = "Historie všech známek:";
        }
        else
        {
            // Zobrazíme jen známky z vybraných předmětů.
            ZnamkyListBox.ItemsSource = _student.Znamky
                .Where(z => vybranePredmety.Contains(z.Predmet.Nazev))
                .ToList();
            ZnamkyNadpis.Text = $"Filtr: {string.Join(", ", vybranePredmety)}";
        }
    }

    /// <summary>
    /// Smaže vybranou známku a aktualizuje zobrazené průměry.
    /// Průměry se musí obnovit ručně, protože SubjectAveragesListBox není přímo navázán na změny kolekcí.
    /// </summary>
    public void DeleteMark_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button { Tag: Znamka znamka } && _student != null)
        {
            _student.Znamky.Remove(znamka);

            // Ručně aktualizujeme průměry a varování po smazání.
            PrumerText.Text = $"{_student.CelkovyPrumer:0.00}";
            WarningBorder.IsVisible = _student.JeProspechOhrozen;

            // Obnovíme seznam průměrů – výběr se tím resetuje a filtr se automaticky zruší.
            SubjectAveragesListBox.ItemsSource = null;
            SubjectAveragesListBox.ItemsSource = _student.PrumeryPoPredmetech;
        }
    }
}
