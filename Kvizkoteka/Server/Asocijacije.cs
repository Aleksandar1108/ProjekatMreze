using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Asocijacije
    {
        public string[][] Kolone { get; set; }
        public bool[][] OtvorenaPolja { get; set; }
        public string KonacnoResenje { get; set; }

        public Asocijacije()
        {
            UcitajAsocijacije();
        }

        // Metode
        public void UcitajAsocijacije()
        {
            // Definiše vrednosti unutar klase
            Kolone = new string[4][]
            {
            new string[] {"???", "PSI", "3.Godina", "Protokol", "???"},
            new string[] {"???", "Reka", "Planina", "Obala", "???"},
            new string[] {"???", "Jabuka", "Kruška", "Šljiva", "???"},
            new string[] {"???", "Kuća", "Stan", "Vikendica", "???"}
            };

            KonacnoResenje = "PRIMER";

            OtvorenaPolja = new bool[4][];
            for (int i = 0; i < 4; i++)
            {
                OtvorenaPolja[i] = new bool[5];
            }
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
            }

            poruka += PrikaziAsocijaciju();

            return poruka;
        }




        public string PrikaziAsocijaciju()
        {
            StringBuilder sb = new StringBuilder();

            for (int k = 0; k < 4; k++) // kolone A-D
            {
                for (int r = 0; r < 4; r++) // redovi 1-4
                {
                    if (OtvorenaPolja[k][r])
                        sb.Append($"{(char)('A' + k)}{r + 1}: {Kolone[k][r]}\n");
                    else
                        sb.Append($"{(char)('A' + k)}{r + 1}: ???\n");
                }
                // Prikaži rešenje kolone ako je otvoreno
                if (OtvorenaPolja[k][4]) // poslednji indeks za rešenje kolone
                    sb.Append($"{(char)('A' + k)}: {Kolone[k][4]}\n");
                else
                    sb.Append($"{(char)('A' + k)}: ???\n");
            }

            // Prikaži konačno rešenje ako je otvoreno (možeš imati boolean flag ili slično)
            if (OtvorenaPolja.All(k => k.All(p => p)))
                sb.Append($"Konacno resenje: {KonacnoResenje}\n");
            else
                sb.Append("Konacno resenje: ???\n");

            return sb.ToString();
        }




        /*
        for (int kolona = 0; kolona < 4; kolona++)
        {
            Console.WriteLine((char)('A' + kolona) + ":");
            for (int red = 0; red < 4; red++)
            {
                if (OtvorenaPolja[kolona][red])
                {
                    Console.WriteLine($" {red + 1}: {Kolone[kolona][red]}");
                }
                else
                {
                    Console.WriteLine($" {red + 1}: ???");
                }
            }
            Console.WriteLine($" {Kolone[kolona][4]}: {(OtvorenaPolja[kolona][4] ? Kolone[kolona][4] : "???")}");
        }
        Console.WriteLine($"Konačno rešenje: {(OtvorenaPolja.All(k => k.All(o => o)) ? KonacnoResenje : "???")}");
    }
        */


    }
}

