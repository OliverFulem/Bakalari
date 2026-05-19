using System.Collections.Generic;
using Avalonia.Controls;
using Bakalari.Tridy;

namespace Bakalari;

public partial class MainWindow : Window
{
    private List<Student> studenti = new();

    public MainWindow()
    {
        InitializeComponent();

        studenti.Add(new Student
        {
            Jmeno = "Jan",
            Prijmeni = "Novák",
            Znamky = new List<float> { 1, 2, 1 }
        });

        studenti.Add(new Student
        {
            Jmeno = "Petr",
            Prijmeni = "Svoboda",
            Znamky = new List<float> { 2, 3, 2 }
        });
        
        studenti.Add(new Student
        {
            Jmeno = "Adam",
            Prijmeni = "Témen ",
            Znamky = new List<float> { 4,5, 4, 5 }
        });
        

        StudentListBox.ItemsSource = studenti;
    }
}