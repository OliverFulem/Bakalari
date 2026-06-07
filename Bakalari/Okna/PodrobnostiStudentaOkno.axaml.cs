using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Bakalari.Tridy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bakalari.Okna;

/// <summary>
/// Karta studenta – zobrazuje průměry, historii známek a umožňuje přidat nebo upravit známku.
/// Výběr řádku v historii přepne formulář do režimu editace.
/// </summary>
public partial class PodrobnostiStudentaOkno : Window
{
    private Student? _student;
    private List<TridaInfo> _tridy = new();

    /// <summary>Známka právě editovaná ve formuláři. Null = formulář je v režimu přidání.</summary>
    private Znamka? _editovanaZnamka;

    /// <summary>
    /// True, pokud uživatel klikl na "Smazat studenta".
    /// Hlavní okno tuto vlastnost zkontroluje po zavření dialogu.
    /// </summary>
    public bool IsDeleted { get; private set; }

    public PodrobnostiStudentaOkno()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Otevře kartu studenta a naplní všechny zobrazované hodnoty.
    /// </summary>
    public PodrobnostiStudentaOkno(Student student, IEnumerable<TridaInfo> dostupneTridy)
    {
        InitializeComponent();

        _student = student;
        _tridy = dostupneTridy.ToList();

        JmenoText.Text = $"{student.Jmeno} {student.Prijmeni}";
        PrumerText.Text = $"{student.CelkovyPrumer:0.00}";

        SubjectAveragesListBox.ItemsSource = student.PrumeryPoPredmetech;
        ZnamkyListBox.ItemsSource = student.Znamky;

        SubjectAveragesListBox.SelectionChanged += (s, e) => AktualizujFiltrZnamek();

        // Výběr řádku z historie přepne formulář do editačního režimu.
        ZnamkyListBox.SelectionChanged += (s, e) =>
        {
            if (ZnamkyListBox.SelectedItem is Znamka znamka)
                ZacitEditaci(znamka);
            else
                UkoncitEditaci();
        };

        // --- Přiřazení do třídy ---
        ClassComboBox.ItemsSource = _tridy.Select(t => t.Nazev).ToList();
        ClassComboBox.SelectedItem = student.Trida;

        ClassComboBox.SelectionChanged += (s, e) =>
        {
            ZmenaTridy.IsEnabled =
                ClassComboBox.SelectedItem is string nova && nova != student.Trida;
        };

        ZmenaTridy.Click += async (s, e) =>
        {
            if (ClassComboBox.SelectedItem is not string novaTrida) return;

            // Zjistíme, které předměty nová třída nemá, a které known by se tím ztratily.
            var predmetyNoveTridy = _tridy
                .FirstOrDefault(t => t.Nazev == novaTrida)?.Predmety.ToHashSet()
                ?? new HashSet<string>();

            var znamkyKeSmazani = student.Znamky
                .Where(z => !predmetyNoveTridy.Contains(z.Predmet.Nazev))
                .ToList();

            if (znamkyKeSmazani.Count > 0)
            {
                // Sestavíme přehled: "Matematika (3 known), ICT (2 known)"
                var prehled = znamkyKeSmazani
                    .GroupBy(z => z.Predmet.Nazev)
                    .Select(g => $"{g.Key} ({g.Count()} {(g.Count() == 1 ? "známka" : g.Count() < 5 ? "známky" : "known")})")
                    .ToList();

                var zprava =
                    $"Třída \"{novaTrida}\" nemá tyto předměty, ze kterých má student/ka hodnocení:\n" +
                    $"  • {string.Join("\n  • ", prehled)}\n\n" +
                    $"Přeřazením bude nevratně smazáno {znamkyKeSmazani.Count} " +
                    $"{(znamkyKeSmazani.Count == 1 ? "known" : "known")}. Chcete pokračovat?";

                var potvrzeno = await ZobrazPotvrzeni(zprava);
                if (!potvrzeno)
                {
                    ClassComboBox.SelectedItem = student.Trida;
                    return;
                }

                foreach (var z in znamkyKeSmazani)
                    student.Znamky.Remove(z);
            }

            student.Trida = novaTrida;
            ZmenaTridy.IsEnabled = false;
            NaplnitPredmety(student.Trida);
            AktualizujDetail();
        };

        // --- Formulář pro přidání / úpravu ---
        NaplnitPredmety(student.Trida);
        NovyDatumPicker.SelectedDate = DateTimeOffset.Now;
        NovaVahaComboBox.ItemsSource = Enumerable.Range(1, 10).ToList();
        NovaVahaComboBox.SelectedIndex = 0;

        PridatZnamkuButton.Click += (s, e) => UlozitZnamku();
        ZrusitEditaciButton.Click += (s, e) => ZnamkyListBox.SelectedItem = null;

        NovaZnamkaTextBox.KeyDown += (s, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter)
                UlozitZnamku();
        };

        // --- Ostatní ---
        DeleteStudentButton.Click += (s, e) => { IsDeleted = true; Close(); };
        WarningBorder.IsVisible = student.JeProspechOhrozen;
        CloseButton.Click += (sender, e) => Close();
    }

    /// <summary>
    /// Přepne formulář do režimu editace – naplní pole hodnotami vybrané známky.
    /// </summary>
    private void ZacitEditaci(Znamka znamka)
    {
        _editovanaZnamka = znamka;

        // Naplníme formulář hodnotami vybrané známky.
        var predmety = NovyPredmetComboBox.ItemsSource as IEnumerable<Predmet>;
        NovyPredmetComboBox.SelectedItem = predmety?.FirstOrDefault(p => p.Nazev == znamka.Predmet.Nazev);

        NovyDatumPicker.SelectedDate = znamka.Datum.HasValue
            ? new DateTimeOffset(znamka.Datum.Value.ToDateTime(TimeOnly.MinValue), DateTimeOffset.Now.Offset)
            : DateTimeOffset.Now;

        NovaZnamkaTextBox.Text = znamka.Hodnota.ToString();
        NovaVahaComboBox.SelectedItem = znamka.Vaha;
        NovaPoznamkaTextBox.Text = znamka.Poznamka;

        // Přepneme vizuál formuláře do editačního režimu.
        FormNadpis.Text = "Upravit vybranou známku:";
        PridatZnamkuButton.Content = "Uložit změny";
        ZrusitEditaciButton.IsVisible = true;
        NovaPridatValidation.IsVisible = false;
    }

    /// <summary>
    /// Přepne formulář zpět do režimu přidávání nové známky.
    /// </summary>
    private void UkoncitEditaci()
    {
        _editovanaZnamka = null;

        NovaZnamkaTextBox.Text = string.Empty;
        NovaPoznamkaTextBox.Text = string.Empty;
        NovaVahaComboBox.SelectedIndex = 0;
        NovyDatumPicker.SelectedDate = DateTimeOffset.Now;

        FormNadpis.Text = "Přidat novou známku:";
        PridatZnamkuButton.Content = "Přidat známku";
        ZrusitEditaciButton.IsVisible = false;
        NovaPridatValidation.IsVisible = false;
    }

    /// <summary>
    /// Zobrazí modální potvrzovací dialog s tlačítky Zrušit / Potvrdit.
    /// Vrátí true, pokud uživatel klikl na Potvrdit.
    /// </summary>
    private async Task<bool> ZobrazPotvrzeni(string zprava)
    {
        var potvrzeno = false;

        var btnZrusit = new Button
        {
            Content = "Zrušit",
            Width = 100,
            Background = new SolidColorBrush(Color.Parse("#888")),
            Foreground = Brushes.White,
            Padding = new Thickness(10, 5),
            CornerRadius = new CornerRadius(4)
        };
        var btnPotvrdit = new Button
        {
            Content = "Ano, přeřadit a smazat",
            Background = new SolidColorBrush(Color.Parse("#C62828")),
            Foreground = Brushes.White,
            Padding = new Thickness(10, 5),
            CornerRadius = new CornerRadius(4)
        };

        var okno = new Window
        {
            Title = "Upozornění – rozdílné předměty",
            Width = 420,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://Bakalari/icon-letter-b-100.png"))),
            Content = new StackPanel
            {
                Margin = new Thickness(24),
                Spacing = 20,
                Children =
                {
                    new TextBlock
                    {
                        Text = zprava,
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 13
                    },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 10,
                        Children = { btnZrusit, btnPotvrdit }
                    }
                }
            }
        };

        btnZrusit.Click += (_, _) => okno.Close();
        btnPotvrdit.Click += (_, _) => { potvrzeno = true; okno.Close(); };

        await okno.ShowDialog(this);
        return potvrzeno;
    }

    /// <summary>
    /// Naplní ComboBox předmětů podle aktuální třídy studenta.
    /// </summary>
    private void NaplnitPredmety(string nazevTridy)
    {
        var tridaInfo = _tridy.FirstOrDefault(t => t.Nazev == nazevTridy);
        var predmety = tridaInfo?.Predmety.Select(p => new Predmet(p)).ToList() ?? new List<Predmet>();
        NovyPredmetComboBox.ItemsSource = predmety;
        NovyPredmetComboBox.SelectedIndex = predmety.Count > 0 ? 0 : -1;
    }

    /// <summary>
    /// Uloží formulář – buď přidá novou známku, nebo upraví editovanou.
    /// </summary>
    private void UlozitZnamku()
    {
        if (_student == null ||
            NovyPredmetComboBox.SelectedItem is not Predmet predmet ||
            !float.TryParse(NovaZnamkaTextBox.Text, out float hodnota) ||
            hodnota < 1 || hodnota > 5)
        {
            NovaPridatValidation.IsVisible = true;
            return;
        }

        var datum = NovyDatumPicker.SelectedDate.HasValue
            ? DateOnly.FromDateTime(NovyDatumPicker.SelectedDate.Value.DateTime)
            : DateOnly.FromDateTime(DateTime.Today);
        var poznamka = NovaPoznamkaTextBox.Text?.Trim() ?? string.Empty;
        var vaha = NovaVahaComboBox.SelectedItem is int v ? v : 1;

        NovaPridatValidation.IsVisible = false;

        if (_editovanaZnamka != null)
        {
            // Aktualizujeme existující známku (vlastnosti implementují INotifyPropertyChanged).
            _editovanaZnamka.Hodnota = hodnota;
            _editovanaZnamka.Predmet = predmet;
            _editovanaZnamka.Datum = datum;
            _editovanaZnamka.Poznamka = poznamka;
            _editovanaZnamka.Vaha = vaha;
            // Zrušení výběru přepne formulář zpět do režimu přidávání.
            ZnamkyListBox.SelectedItem = null;
        }
        else
        {
            // Přidáme novou známku.
            _student.Znamky.Add(new Znamka(hodnota, predmet, datum, poznamka, vaha));
            NovaZnamkaTextBox.Text = string.Empty;
            NovaPoznamkaTextBox.Text = string.Empty;
            NovaVahaComboBox.SelectedIndex = 0;
            NovyDatumPicker.SelectedDate = DateTimeOffset.Now;
            NovaZnamkaTextBox.Focus();
        }

        AktualizujDetail();
    }

    /// <summary>
    /// Aktualizuje průměr, varování a seznam průměrů po změně v kolekci známek.
    /// </summary>
    private void AktualizujDetail()
    {
        if (_student == null) return;
        PrumerText.Text = $"{_student.CelkovyPrumer:0.00}";
        WarningBorder.IsVisible = _student.JeProspechOhrozen;
        SubjectAveragesListBox.ItemsSource = null;
        SubjectAveragesListBox.ItemsSource = _student.PrumeryPoPredmetech;
    }

    /// <summary>
    /// Filtruje historii podle vybraných předmětů v SubjectAveragesListBox.
    /// </summary>
    private void AktualizujFiltrZnamek()
    {
        if (_student == null) return;

        var vybranePredmety = SubjectAveragesListBox.SelectedItems?
            .OfType<SubjectAverage>()
            .Select(sa => sa.Predmet)
            .ToHashSet() ?? new HashSet<string>();

        if (vybranePredmety.Count == 0)
        {
            ZnamkyListBox.ItemsSource = _student.Znamky;
            ZnamkyNadpis.Text = "Historie všech známek:";
        }
        else
        {
            ZnamkyListBox.ItemsSource = _student.Znamky
                .Where(z => vybranePredmety.Contains(z.Predmet.Nazev))
                .ToList();
            ZnamkyNadpis.Text = $"Filtr: {string.Join(", ", vybranePredmety)}";
        }
    }

    /// <summary>
    /// Smaže vybranou známku z kolekce.
    /// </summary>
    public void DeleteMark_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button { Tag: Znamka znamka } && _student != null)
        {
            _student.Znamky.Remove(znamka);
            AktualizujDetail();
        }
    }
}
