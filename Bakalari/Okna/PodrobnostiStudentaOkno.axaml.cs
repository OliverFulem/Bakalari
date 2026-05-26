using Avalonia.Controls;
using Bakalari.Tridy;

namespace Bakalari.Okna;

public partial class PodrobnostiStudentaOkno : Window
{
    public PodrobnostiStudentaOkno()
    {
        InitializeComponent();
    }

    public PodrobnostiStudentaOkno(Student student)
    {
        InitializeComponent();

        JmenoText.Text = $"Jméno: {student.Jmeno} {student.Prijmeni}";
        PrumerText.Text = $"Průměr: {student.VypocetPrumeru():0.00}";
        ZnamkyListBox.ItemsSource = student.Znamky;
    }
}