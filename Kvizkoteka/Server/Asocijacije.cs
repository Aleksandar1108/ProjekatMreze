using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server
{
    public class Asocijacije
    {
        public string[][] Kolone { get; set; }
        public bool[][] OtvorenaPolja { get; set; }
        public string KonacnoResenje { get; set; }
        public int UkupniBodovi { get; set; }

        public Asocijacije()
        {
            UcitajAsocijacije();
            UkupniBodovi = 0;
        }

        public void UcitajAsocijacije()
        {
            // Definiše vrednosti unutar klase
            Kolone = new string[4][]
            {
                new string[] {"LABRADOR", "NEMACKI OVCAR", "ZLATNI RETRIVER", "PUDLA", "PSI"},
                new string[] {"DUNAV", "SAVA", "MORAVA", "DRINA", "REKE"},
                new string[] {"JABUKA", "BANANA", "NARANDZA", "GROZDJE", "VOCE"},
                new string[] {"KUCA", "STAN", "VIKENDICA", "APARTMAN", "NEKRETNINE"}
            };

            KonacnoResenje = "PRIRODA";

            OtvorenaPolja = new bool[4][];
            for (int i = 0; i < 4; i++)
            {
                OtvorenaPolja[i] = new bool[5]; // 4 polja + 1 za rešenje kolone
            }
        }

        public (string poruka, int bodovi) OtvoriPolje(string unos)
        {
            string poruka = "";
            int osvojeniBodovi = 0;

            if (unos.Contains(':'))
            {
                // Rešavanje kolone ili konačnog rešenja
                string[] delovi = unos.Split(':');
                if (delovi.Length != 2)
                {
                    return ("Neispravna komanda. Format: oznaka:odgovor", 0);
                }

                string oznaka = delovi[0].Trim().ToUpper();
                string odgovor = delovi[1].Trim().ToUpper();

                if (oznaka == "K")
                {
                    // Pokušaj rešavanja konačnog rešenja
                    if (KonacnoResenje.Equals(odgovor, StringComparison.OrdinalIgnoreCase))
                    {
                        // ISPRAVLJENA LOGIKA: Izračunaj bodove za nepogodjene kolone
                        int bonusBodovi = 0;
                        StringBuilder detaljiKolona = new StringBuilder();

                        for (int k = 0; k < 4; k++)
                        {
                            // Ako kolona nije rešena (rešenje kolone nije otvoreno)
                            if (!OtvorenaPolja[k][4])
                            {
                                // ISPRAVKA: Broji samo polja A1-A4 (indeksi 0-3), NE i rešenje kolone
                                int neotvorenihPolja = 0;
                                for (int i = 0; i < 4; i++) // PROMENIO SA 5 NA 4 - samo polja, ne i rešenje
                                {
                                    if (!OtvorenaPolja[k][i])
                                        neotvorenihPolja++;
                                }
                                int bodovizaKolonu = neotvorenihPolja + 2;
                                bonusBodovi += bodovizaKolonu;

                                char nazivKolone = (char)('A' + k);
                                detaljiKolona.AppendLine($"   • Kolona {nazivKolone}: {bodovizaKolonu} bodova");
                            }
                        }

                        // Otvori sva polja
                        for (int i = 0; i < 4; i++)
                        {
                            for (int j = 0; j < 5; j++)
                            {
                                OtvorenaPolja[i][j] = true;
                            }
                        }

                        osvojeniBodovi = 10 + bonusBodovi; // 10 za konačno + bonus za nepogodjene kolone
                        int prethodniUkupno = UkupniBodovi;
                        UkupniBodovi += osvojeniBodovi;

                        poruka = $" Čestitamo! Tačno konačno rešenje!\n" +
                                 $" Konačno rešenje: +10 bodova\n";

                        if (bonusBodovi > 0)
                        {
                            poruka += $" Bonus za nepogodjene kolone: +{bonusBodovi} bodova\n" +
                                     detaljiKolona.ToString();
                        }

                        poruka += $" Finalni rezultat: {prethodniUkupno} + {osvojeniBodovi} = {UkupniBodovi} BODOVA!";
                    }
                    else
                    {
                        poruka = " Netačno konačno rešenje.";
                    }
                }
                else if ("ABCD".Contains(oznaka) && oznaka.Length == 1)
                {
                    // Pokušaj rešavanja kolone
                    int kolona = oznaka[0] - 'A';
                    if (kolona >= 0 && kolona < 4)
                    {
                        if (odgovor.Equals(Kolone[kolona][4], StringComparison.OrdinalIgnoreCase))
                        {
                            // ISPRAVKA: Broji samo polja A1-A4 (indeksi 0-3), NE i rešenje kolone
                            int neotvorenihPolja = 0;
                            for (int i = 0; i < 4; i++) // PROMENIO SA 5 NA 4 - samo polja, ne i rešenje
                            {
                                if (!OtvorenaPolja[kolona][i])
                                    neotvorenihPolja++;
                            }

                            osvojeniBodovi = neotvorenihPolja + 2;
                            int prethodniUkupno = UkupniBodovi;
                            UkupniBodovi += osvojeniBodovi;

                            // Otvori celu kolonu
                            for (int i = 0; i < 5; i++)
                            {
                                OtvorenaPolja[kolona][i] = true;
                            }

                            poruka = $" Tačno rešenje kolone {oznaka}! Dobijate {osvojeniBodovi} bodova!\n" +
                                     $" Prethodno: {prethodniUkupno} + {osvojeniBodovi} = {UkupniBodovi} bodova ukupno";
                        }
                        else
                        {
                            poruka = $" Netačan odgovor za kolonu {oznaka}.";
                        }
                    }
                    else
                    {
                        poruka = " Nevalidna oznaka kolone.";
                    }
                }
                else
                {
                    poruka = " Nevalidna oznaka. Koristite A, B, C, D za kolone ili K za konačno rešenje.";
                }
            }
            else
            {
                // Otvaranje pojedinačnog polja - NEMA BODOVA
                if (unos.Length != 2 || !"ABCD".Contains(unos[0]) || !char.IsDigit(unos[1]))
                {
                    return (" Nevalidna oznaka polja. Format: A1, B2, itd.", 0);
                }

                int kolona = unos[0] - 'A';
                int red = unos[1] - '1';

                if (kolona < 0 || kolona > 3 || red < 0 || red > 3)
                {
                    return (" Oznaka polja nije u opsegu (A1-D4).", 0);
                }

                if (OtvorenaPolja[kolona][red])
                {
                    return (" Polje je već otvoreno.", 0);
                }

                OtvorenaPolja[kolona][red] = true;

                // Za pojedinačno polje: NEMA BODOVA
                osvojeniBodovi = 0;
                poruka = $" Otvoreno polje {unos}: {Kolone[kolona][red]}";
            }

            return (poruka, osvojeniBodovi);
        }

        public string PrikaziAsocijaciju()
        {
            StringBuilder sb = new StringBuilder();

            // Prikaz po kolonama
            for (int k = 0; k < 4; k++) // kolone A-D
            {
                sb.AppendLine($"=== KOLONA {(char)('A' + k)} ===");

                // Prikaz polja 1-4
                for (int r = 0; r < 4; r++) // redovi 1-4
                {
                    if (OtvorenaPolja[k][r])
                        sb.AppendLine($"{(char)('A' + k)}{r + 1}: {Kolone[k][r]}");
                    else
                        sb.AppendLine($"{(char)('A' + k)}{r + 1}: ???");
                }

                // Prikaz rešenja kolone
                if (OtvorenaPolja[k][4]) // poslednji indeks za rešenje kolone
                    sb.AppendLine($"REŠENJE {(char)('A' + k)}: {Kolone[k][4]}");
                else
                    sb.AppendLine($"REŠENJE {(char)('A' + k)}: ???");

                sb.AppendLine(); // Prazna linija između kolona
            }

            // Prikaz konačnog rešenja
            if (OtvorenaPolja.All(k => k.All(p => p)))
                sb.AppendLine($"KONAČNO REŠENJE: {KonacnoResenje}");
            else
                sb.AppendLine("KONAČNO REŠENJE: ???");

            // Prikaz trenutnih bodova
            sb.AppendLine($" TRENUTNI BODOVI: {UkupniBodovi}");

            return sb.ToString();
        }

        public bool JeIgraZavrsena()
        {
            return OtvorenaPolja.All(k => k.All(p => p));
        }
    }
}