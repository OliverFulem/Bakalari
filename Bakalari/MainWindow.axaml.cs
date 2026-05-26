using Avalonia.Controls;
using Bakalari.Okna;
using Bakalari.Tridy;
using System.Collections.ObjectModel;

namespace Bakalari;

public partial class MainWindow : Window
{
    private ObservableCollection<Student> _studenti = new();

    public MainWindow()
    {
        InitializeComponent();

        StudentListBox.ItemsSource = _studenti;

        PridatStudentaButton.Click += async (sender, e) =>
        {
            var okno = new PridaniStudentaOkno();
            await okno.ShowDialog(this);

            if (okno.NovyStudent != null)
                _studenti.Add(okno.NovyStudent);
        };

        DetailButton.Click += async (sender, e) =>
        {
            if (StudentListBox.SelectedItem is Student vybranýStudent)
            {
                var okno = new PridaniZnamekOkno(vybranýStudent);
                await okno.ShowDialog(this);
            }
        };
    }
}