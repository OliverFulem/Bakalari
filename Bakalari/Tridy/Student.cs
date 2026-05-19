using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Bakalari.Tridy;

public class Student
{
    public string Jmeno { get; set; }

    public string Prijmeni { get; set; }

    public List<float> Znamky { get; set; }

    public Student()
    {
        Znamky = new List<float>();
    }

    public double VypocetPrumeru()
    {
        if (Znamky.Count == 0)
            return 0;

        return Znamky.Average();
    }

    public override string ToString()
    {
        return $"{Jmeno} {Prijmeni} | Průměr: {VypocetPrumeru():0.00}";
    }
}