using Avalonia.Controls;
using Bakalari.Tridy;

namespace Bakalari.Okna;

public partial class PridaniStudentaOkno : Window
{
    public Student? NovyStudent { get; private set; }

    public PridaniStudentaOkno()
    {
        InitializeComponent();

        SaveButton.Click += (sender, e) =>
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text) || 
                string.IsNullOrWhiteSpace(SurnameTextBox.Text))
                return;

            NovyStudent = new Student
            {
                Jmeno = NameTextBox.Text,
                Prijmeni = SurnameTextBox.Text
            };

            Close();
        };
    }
}