using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using Bakalari.Logika;
using Bakalari.Okna;
using Bakalari.Tridy;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace Bakalari;

/// <summary>
/// Hlavní okno aplikace. Zobrazuje seznam tříd a studentů,
/// zprostředkovává přechod do ostatních oken a udržuje stavový řádek.
/// </summary>
public partial class MainWindow : Window
{
    private Skola _skola;

    /// <summary>Kolekce skupin (tříd) aktuálně zobrazených v seznamu – mění se při každém filtru nebo hledání.</summary>
    private ObservableCollection<ClassGroup> _zobrazeneTridy = new();

    /// <summary>Student vybraný kliknutím v seznamu. Null, pokud není nic vybráno.</summary>
    private Student? _vybranyStudent;

    public MainWindow()
    {
        InitializeComponent();

        _skola = SpravceDat.Nacist();

        // Napojíme zobrazovanou kolekci na ItemsControl v XAML.
        ClassGroupControl.ItemsSource = _zobrazeneTridy;

        NastavitSledovaniZmen();
        ObnovFiltrTrid();

        // Při každé změně filtru nebo hledaného textu přestavíme seznam.
        SortComboBox.SelectionChanged += (s, e) => AktualizujSeznam();
        ClassFilterComboBox.SelectionChanged += (s, e) => AktualizujSeznam();
        SearchTextBox.TextChanged += (s, e) => AktualizujSeznam();

        PridatStudentaButton.Click += async (sender, e) =>
        {
            VycistiStav();
            var okno = new PridaniStudentaOkno(_skola);
            await okno.ShowDialog(this);
            if (okno.PocetPridanych > 0)
            {
                // Aktualizujeme filtr, pokud přibyla nová třída (z TXT importu).
                ObnovFiltrTrid();
                SpravceDat.Ulozit(_skola);
                ZobrazStav($"Přidáno {okno.PocetPridanych} studentů.");
            }
        };

        PridatZnamkuButton.Click += async (sender, e) =>
        {
            VycistiStav();
            if (_vybranyStudent == null)
            {
                ZobrazStav("Nejprve vyberte studenta ze seznamu.", jeChyba: true);
                return;
            }

            // Zachytíme referenci před otevřením dialogu – dialog může _vybranyStudent vynulovat.
            var student = _vybranyStudent;
            var tridaStudenta = _skola.Tridy.FirstOrDefault(t => t.Nazev == student.Trida);
            var predmety = tridaStudenta?.Predmety ?? new ObservableCollection<string>();

            var okno = new PridaniZnamekOkno(student, predmety);
            await okno.ShowDialog(this);
            SpravceDat.Ulozit(_skola);
            ZobrazStav($"Změny u studenta {student.Jmeno} {student.Prijmeni} uloženy.");
        };

        DetailButton.Click += async (sender, e) =>
        {
            VycistiStav();
            if (_vybranyStudent == null)
            {
                ZobrazStav("Nejprve vyberte studenta ze seznamu.", jeChyba: true);
                return;
            }
            await OtevritDetailStudenta(_vybranyStudent);
        };

        ExportButton.Click += async (sender, e) =>
        {
            VycistiStav();
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Exportovat seznam studentů",
                SuggestedFileName = "studenti.csv",
                FileTypeChoices = new[] { new FilePickerFileType("CSV soubor") { Patterns = new[] { "*.csv" } } }
            });

            if (file != null)
            {
                await using var stream = await file.OpenWriteAsync();
                await using var writer = new StreamWriter(stream, Encoding.UTF8);
                await writer.WriteAsync(ExportLogika.GenerujCsv(_skola));
                ZobrazStav($"Export dokončen: {file.Name}");
            }
        };

        SettingsButton.Click += async (sender, e) =>
        {
            VycistiStav();
            var okno = new NastaveniSkolyOkno(_skola);
            await okno.ShowDialog(this);
            // Po zavření nastavení obnovíme filtr a seznam (mohly přibýt/ubýt třídy nebo studenti).
            ObnovFiltrTrid();
            AktualizujSeznam();
            SpravceDat.Ulozit(_skola);
        };

        SortComboBox.SelectedIndex = 0;
        AktualizujSeznam();
    }

    /// <summary>
    /// Přihlásí odběr změn kolekcí – přidání studenta nebo změna známky okamžitě aktualizuje seznam.
    /// Díky tomu jsou hlavní okno a dialogy živě synchronizované bez nutnosti ručního obnovení.
    /// </summary>
    private void NastavitSledovaniZmen()
    {
        // Přihlásíme sledování změn pro studenty načtené ze souboru.
        foreach (var student in _skola.Studenti)
            PrihlasitStudenta(student);

        // Když přibyde nový student, přihlásíme sledování i jeho změn.
        _skola.Studenti.CollectionChanged += (s, e) =>
        {
            if (e.NewItems != null)
                foreach (Student student in e.NewItems)
                    PrihlasitStudenta(student);
            AktualizujSeznam();
        };
    }

    /// <summary>
    /// Přihlásí odběr změn jednoho studenta:
    /// přidání/odebrání známky a změna třídy okamžitě aktualizují seznam v hlavním okně.
    /// </summary>
    private void PrihlasitStudenta(Student student)
    {
        student.Znamky.CollectionChanged += (_, _) => AktualizujSeznam();
        student.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Student.Trida))
                AktualizujSeznam();
        };
    }

    /// <summary>
    /// Reaguje na kliknutí studenta v libovolném ListBoxu.
    /// Zapamatuje vybraného studenta a zruší výběr ve všech ostatních ListBoxech,
    /// aby nemohlo být vybráno více studentů najednou (každá třída má vlastní ListBox).
    /// </summary>
    /// <summary>
    /// Otevře kartu studenta – sdílená logika pro tlačítko i dvojklik.
    /// </summary>
    private async Task OtevritDetailStudenta(Student student)
    {
        var okno = new PodrobnostiStudentaOkno(student, _skola.Tridy);
        await okno.ShowDialog(this);

        if (okno.IsDeleted)
        {
            ZobrazStav($"Student {student.Jmeno} {student.Prijmeni} byl smazán.");
            _skola.Studenti.Remove(student);
        }
        else
        {
            ZobrazStav("Změny uloženy.");
        }

        AktualizujSeznam();
        SpravceDat.Ulozit(_skola);
    }

    /// <summary>
    /// Dvojklik na studenta v seznamu otevře jeho kartu.
    /// </summary>
    private async void StudentListBox_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_vybranyStudent == null) return;
        VycistiStav();
        await OtevritDetailStudenta(_vybranyStudent);
    }

    private void StudentListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox lb && lb.SelectedItem is Student student)
        {
            _vybranyStudent = student;

            foreach (var child in this.GetVisualDescendants().OfType<ListBox>())
            {
                if (child != lb)
                    child.SelectedItem = null;
            }
        }
    }

    /// <summary>
    /// Aktualizuje položky v ComboBoxu pro filtrování tříd.
    /// Zachová předchozí výběr, pokud daná třída stále existuje.
    /// </summary>
    private void ObnovFiltrTrid()
    {
        var vybrana = ClassFilterComboBox.SelectedItem as string;
        var tridyList = new List<string> { "Vše" };
        tridyList.AddRange(_skola.Tridy.Select(t => t.Nazev));
        ClassFilterComboBox.ItemsSource = tridyList;
        ClassFilterComboBox.SelectedItem = vybrana != null && tridyList.Contains(vybrana) ? vybrana : "Vše";
        if (ClassFilterComboBox.SelectedItem == null)
            ClassFilterComboBox.SelectedIndex = 0;
    }

    /// <summary>
    /// Zobrazí zprávu ve stavovém řádku.
    /// </summary>
    /// <param name="jeChyba">True = červené pozadí (chyba), false = zelené pozadí (úspěch).</param>
    private void ZobrazStav(string zprava, bool jeChyba = false)
    {
        StatusText.Text = zprava;
        StatusBar.Background = new SolidColorBrush(jeChyba ? Color.Parse("#FFEBEE") : Color.Parse("#E8F5E9"));
        StatusText.Foreground = new SolidColorBrush(jeChyba ? Color.Parse("#C62828") : Color.Parse("#1B5E20"));
    }

    /// <summary>
    /// Vymaže zprávu ze stavového řádku a obnoví průhledné pozadí.
    /// Volá se na začátku každého tlačítkového handleru, aby předchozí zpráva nezmátla uživatele.
    /// </summary>
    private void VycistiStav()
    {
        StatusText.Text = string.Empty;
        StatusBar.Background = new SolidColorBrush(Colors.Transparent);
    }

    /// <summary>
    /// Přestaví kolekci _zobrazeneTridy podle aktuálního filtru, řazení a hledaného textu.
    /// Volá se při každé změně ovládacích prvků i při změnách v modelu (přidání studenta, změna známky).
    /// Přestavba zruší vizuální výběr v ListBoxech, proto vynulujeme i _vybranyStudent.
    /// </summary>
    private void AktualizujSeznam()
    {
        if (SortComboBox?.SelectedItem is not ComboBoxItem selectedSortItem ||
            ClassFilterComboBox?.SelectedItem is not string vybranaTridaStr)
            return;

        var razeni = selectedSortItem.Content?.ToString() ?? "Příjmení";
        var hledanyText = SearchTextBox?.Text ?? string.Empty;

        // Přestavba seznamu ztratí vizuální výběr, proto vynulujeme i interní referenci.
        _vybranyStudent = null;
        _zobrazeneTridy.Clear();
        foreach (var group in SpravaStudentu.ZiskejSkupiny(_skola, vybranaTridaStr, razeni, hledanyText))
            _zobrazeneTridy.Add(group);
    }
}
