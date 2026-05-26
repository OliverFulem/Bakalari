using Avalonia.Controls;
using Bakalari.Tridy;

namespace Bakalari.Okna;

public partial class PridaniZnamekOkno : Window
{
    private Student _student;

    public PridaniZnamekOkno(Student student)
    {
        InitializeComponent();

        _student = student;
        StudentJmenoText.Text = $"{student.Jmeno} {student.Prijmeni}";

        SaveButton.Click += (sender, e) =>
        {
            if (!float.TryParse(ZnamkaTextBox.Text, out float znamka) || 
                znamka < 1 || znamka > 5)
                return;

            _student.Znamky.Add(znamka);
            Close();
        };
    }
}