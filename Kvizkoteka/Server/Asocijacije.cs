using System.Linq;
using System.Text;
using System;

public class Asocijacije
{
    public string[][] Kolone { get; set; }
    public bool[][] OtvorenaPolja { get; set; }
    public string KonacnoResenje { get; set; }

    public Asocijacije()
    {
        UcitajAsocijacije();
    }

    public void UcitajAsocijacije()
    {
        Kolone = new string[4][]
        {
            new string[] {"TCP", "PSI", "3.Godina", "Protokol", "MREZE"},
            new string[] {"DELTA", "Reka", "Planina", "Obala", "GEOGRAFIJA"},
            new string[] {"Voće", "Jabuka", "Kruška", "Šljiva", "HRANA"},
            new string[] {"Nekretnina", "Kuća", "Stan", "Vikendica", "DOM"}
        };

        KonacnoResenje = "PRIMER";

        OtvorenaPolja = new bool[4][];
        for (int i = 0; i < 4; i++)
            OtvorenaPolja[i] = new bool[5];
    }

    public string OtvoriPolje(string unos)
    {
        int poeni = 0;
        string poruka = "";

        if (unos.Contains(':'))
        {
            string[] delovi = unos.Split(':');
            string oznaka = delovi[0];
            string odgovor = delovi[1];

            if (oznaka == "K")
            {
                if (KonacnoResenje.Equals(odgovor, StringComparison.OrdinalIgnoreCase))
                {
                    for (int i = 0; i < 4; i++)
                    {
                        for (int j = 0; j < 5; j++)
                        {
                            OtvorenaPolja[i][j] = true;
                        }
                    }
                    poeni += 10;
                    poruka = "Čestitamo! Tačno konačno rešenje! Dobijate 10 poena.\n";
                }
                else
                {
                    poruka = "Netačno konačno rešenje.\n";
                }
            }
            else if ("ABCD".Contains(oznaka) && odgovor.Equals(Kolone[oznaka[0] - 'A'][4], StringComparison.OrdinalIgnoreCase))
            {
                int kolona = oznaka[0] - 'A';
                int neotvorenih = OtvorenaPolja[kolona].Count(o => !o);

                for (int i = 0; i < 5; i++)
                {
                    OtvorenaPolja[kolona][i] = true;
                }

                poeni += neotvorenih + 2;
                poruka = $"Tačno rešenje kolone! Dobijate {neotvorenih + 2} poena.\n";
            }
            else
            {
                poruka = "Netačan odgovor za kolonu.\n";
            }
        }
        else
        {
            if (unos.Length != 2 || !"ABCD".Contains(unos[0]) || !char.IsDigit(unos[1]))
            {
                return "Nevalidna oznaka polja.\n";
            }

            int kolona = unos[0] - 'A';
            int red = unos[1] - '1';

            if (kolona < 0 || kolona > 3 || red < 0 || red > 4)
            {
                return "Oznaka polja nije u opsegu.\n";
            }

            if (OtvorenaPolja[kolona][red])
            {
                return "Polje je već otvoreno.\n";
            }

            OtvorenaPolja[kolona][red] = true;
            poruka = $"Otvoreno polje {unos}.\n";
        }

        poruka += PrikaziAsocijaciju();
        return poruka;
    }

    public string PrikaziAsocijaciju()
    {
        var sb = new StringBuilder();

        for (int k = 0; k < 4; k++)
        {
            for (int r = 0; r < 4; r++)
            {
                sb.Append($"{(char)('A' + k)}{r + 1}: {(OtvorenaPolja[k][r] ? Kolone[k][r] : "???")}\n");
            }

            sb.Append($"{(char)('A' + k)}: {(OtvorenaPolja[k][4] ? Kolone[k][4] : "???")}\n");
        }

        if (OtvorenaPolja.All(k => k.All(p => p)))
            sb.Append($"Konacno resenje: {KonacnoResenje}\n");
        else
            sb.Append("Konacno resenje: ???\n");

        return sb.ToString();
    }

    public bool DaLiJeKraj()
    {
        return OtvorenaPolja.All(k => k.All(p => p));
    }
}
