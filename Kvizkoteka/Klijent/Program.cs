using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Klijent
{
    public class Program
    {
        public static void Main(string[] args)
        {

            int totalPoints = 0;
            // Unos imena/nadimka i igara
            Console.Write("Unesite ime/nadimak: ");
            string ime = Console.ReadLine();

            Console.Write("Unesite igre koje želite da igrate (odvojene zarezima): ");
            string igre = Console.ReadLine();

            // UDP deo
            UdpClient udpClient = new UdpClient();
            IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000);
            string prijava = $"PRIJAVA: {ime}, {igre}";

            // Slanje prijave serveru
            udpClient.Send(Encoding.UTF8.GetBytes(prijava), prijava.Length, serverEndpoint);

            // Prijem odgovora sa servera
            string udpResponse = Encoding.UTF8.GetString(udpClient.Receive(ref serverEndpoint));
            Console.WriteLine("Odgovor servera: " + udpResponse);

            // Obrada TCP informacija
            if (udpResponse.StartsWith("TCP INFO:"))
            {
                try
                {
                    string[] tcpInfo = udpResponse.Substring(10).Split(':'); // Split na IP i port

                    if (tcpInfo.Length == 2)
                    {
                        string ip = tcpInfo[0];
                        int port = int.Parse(tcpInfo[1]);

                        // Povezivanje na TCP server
                        TcpClient tcpClient = new TcpClient(ip, port);
                        NetworkStream stream = tcpClient.GetStream();
                        StreamReader reader = new StreamReader(stream);
                        StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

                        Console.Write("Unesite vaš ID (broj koji vam je dodeljen od servera): ");
                        string playerId = Console.ReadLine();
                        writer.WriteLine(playerId);

                        // Čitanje dobrodošlice sa servera
                        string welcomeMessage = reader.ReadLine();
                        Console.WriteLine(welcomeMessage); // Dobrodošli poruka

                        // Čitanje poruke za unos komande
                        string startPrompt = reader.ReadLine();
                        Console.WriteLine(startPrompt); // Unesite START poruka

                        // Unos komande za početak igre
                        string startCommand = Console.ReadLine(); // Unos START
                        writer.WriteLine(startCommand);
                        
                        if(igre.Contains("an"))
                        {
                            // Čekanje na pomešana slova od servera
                            string mixedLetters = reader.ReadLine();
                            Console.WriteLine("" + mixedLetters);

                            // Unos anagrama
                            Console.Write("Unesite vaš anagram: ");
                            string anagram = Console.ReadLine();
                            writer.WriteLine(anagram);

                            // Prikazivanje rezultata
                            string result = reader.ReadLine();
                            Console.WriteLine("Odgovor servera: " + result);

                            if (int.TryParse(reader.ReadLine(), out int anagramPoints))
                            {
                                totalPoints += anagramPoints;
                                Console.WriteLine($"Poeni osvojeni u anagramu: {anagramPoints}");
                            }

                        }

                       if(igre.Contains("po"))
                        {

                            // Ako je igra "Pitanja i odgovori", čeka pitanja i odgovara
                             Console.WriteLine("Odgovorite na sledeće pitanje: ");
                            for (int i = 0; i < 10; i++) // Postavljanje 10 pitanja
                            { 



                                string pitanje = reader.ReadLine(); // Pitanje od servera
                                if (pitanje == "Nema više pitanja.") break;

                                // Prikazivanje pitanja i opcija
                                Console.WriteLine(pitanje); // Ispisivanje pitanja
                                Console.WriteLine("a) Tačno");
                                Console.WriteLine("b) Netačno");

                                string odgovor = Console.ReadLine(); // Unos odgovora (A ili B)
                                writer.WriteLine(odgovor);

                                // Čitanje rezultata od servera
                                string odgovorServera = reader.ReadLine();
                                Console.WriteLine(odgovorServera);

                                // Ako je odgovor tačan, dodaj 4 poena
                                if (odgovorServera.Contains("Tačno"))
                                {
                                    totalPoints += 4; // Dodaj 4 poena za tačan odgovor
                                }
                                else
                                {
                                    // Ako nije tačno, poeni ostaju isti
                                    Console.WriteLine("Odgovor je netačan. Poeni ostaju isti.");
                                }

                                // Prikazivanje trenutnih bodova
                                Console.WriteLine($"Trenutni broj poena: {totalPoints}");
                                Console.WriteLine(); // Prazna linija za lepu separaciju između pitanja
                            }

                            // Prikazivanje ukupnog broja poena
                            Console.WriteLine($"Ukupno poena: {totalPoints}");
                        } 

                       if(igre.Contains("as"))
                        {
                            // Čekanje na trenutno stanje asocijacije (polja koja su otvorena)
                            string currentState = reader.ReadLine();
                            Console.WriteLine(currentState); // Ispisivanje trenutnog stanja asocijacije

                            // Igra Asocijacija
                            while (true)
                            {


                                // Primi kompletno stanje igre od servera
                                // Console.WriteLine("\nTrenutno stanje igre:\n");

                                // Primi kompletno stanje igre od servera
                                string linija;
                                while ((linija = reader.ReadLine()) != null && linija != "END")
                                {
                                    Console.WriteLine(linija); // Prikaz svake linije stanja igre
                                }

                                // Čekaj unos od korisnika
                                Console.Write("\nUnesite otvaranje (npr. A1, B2) ili pokušajte rešenje kolone/resenja (K: odgovor): ");
                                string unos = Console.ReadLine();

                                // Pošalji unos serveru
                                writer.WriteLine(unos);

                                // Ako je unos "izlaz", prekid igre
                                if (unos.Equals("izlaz", StringComparison.OrdinalIgnoreCase))
                                {
                                    Console.WriteLine("Napustili ste igru.");
                                    break;
                                }

                                // Primi odgovor servera i prikaži ga
                                Console.WriteLine("\nOdgovor servera:");
                                while ((linija = reader.ReadLine()) != null && linija != "END")
                                {
                                    Console.WriteLine(linija);
                                }

                                // Proveri da li je igra završena
                                if (linija != null && (linija.Contains("Kraj igre") || linija.Contains("Pobedili ste!")))
                                {
                                    Console.WriteLine("\nIgra je završena!");
                                    break;
                                }

                                /*
                                // Primi kompletno stanje igre od servera
                                string stanje = reader.ReadLine();
                                Console.WriteLine("\nTrenutno stanje igre:\n");
                                Console.WriteLine(stanje); // Ispisivanje kompletne tabele igre

                                // Unos komande od strane korisnika
                                Console.Write("\nUnesite komandu (polje, resenje, ili izlaz): ");
                                string unos = Console.ReadLine();
                                writer.WriteLine(unos);

                                // Ako je unos "izlaz", prekid igre
                                if (unos.Equals("izlaz", StringComparison.OrdinalIgnoreCase))
                                {
                                    Console.WriteLine("Napustili ste igru.");
                                    break;
                                }

                                // Primi odgovor servera i prikaži ga
                                string odgovor = reader.ReadLine();
                                Console.WriteLine("\nOdgovor servera: ");
                                Console.WriteLine(odgovor);

                                // Proveri da li je igra završena
                                if (odgovor.Contains("Kraj igre") || odgovor.Contains("Pobedili ste!"))
                                {
                                    Console.WriteLine("\nIgra je završena!");
                                    break;
                                }

                                */


                                /* // Primi kompletno stanje igre od servera
                                 string stanje = reader.ReadLine();
                                 Console.WriteLine("\nTrenutno stanje igre:\n");
                                 Console.WriteLine(stanje); // Ispisivanje kompletne tabele igre

                                 // Unos komande od strane korisnika
                                 Console.Write("\nUnesite komandu (polje, resenje, ili izlaz): ");
                                 string unos = Console.ReadLine();
                                 writer.WriteLine(unos);

                                 // Ako je unos "izlaz", prekid igre
                                 if (unos.Equals("izlaz", StringComparison.OrdinalIgnoreCase))
                                 {
                                     Console.WriteLine("Napustili ste igru.");
                                     break;
                                 }

                                 // Primi odgovor servera i prikaži ga
                                 string odgovor = reader.ReadLine();
                                 Console.WriteLine("\nOdgovor servera: ");
                                 Console.WriteLine(odgovor);

                                 // Proveri da li je igra završena
                                 if (odgovor.Contains("Kraj igre") || odgovor.Contains("Pobedili ste!"))
                                 {
                                     Console.WriteLine("\nIgra je završena!");
                                     break;
                                 }*/











                                /*
                                // Čekanje na unos od servera za sledeće polje ili rešenje
                                string prompt = reader.ReadLine();
                                if (prompt == "Kraj igre" || prompt == "Pobedili ste!")
                                {
                                    break;
                                }

                                Console.WriteLine(prompt); // Ispisivanje instrukcija ili trenutnog stanja

                                // Unos odgovora za otvaranje polja (npr. "A1", "B3", itd.)
                                Console.Write("Unesite oznaku polja (npr. A1, B2...): ");
                                string field = Console.ReadLine();
                                writer.WriteLine(field); // Slanje odgovora serveru

                                // Čitanje odgovora servera
                                string result = reader.ReadLine();
                                Console.WriteLine(result);

                                // Ako je igrač rešio asocijaciju, treba da se prikaže konačan rezultat
                                if (result.Contains("Konačno rešenje"))
                                {
                                    break;
                                }

                                // Možemo dodati i procenu poena ako server vraća podatke o poenima
                                if (int.TryParse(reader.ReadLine(), out int pointsForField))
                                {
                                    totalPoints += pointsForField;
                                    Console.WriteLine($"Poeni osvojeni za ovo polje: {pointsForField}");
                                }
                                */
                            }

                            // Nakon što su svi odgovori poslati, ispisujemo ukupne poene
                            Console.WriteLine($"Ukupno poena u Asocijacijama: {totalPoints}");
                        }

                       
                    }
                    else
                    {
                        Console.WriteLine("Neispravan odgovor od servera. Format odgovora nije ispravan.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Greška prilikom obrade odgovora servera: " + ex.Message);
                }
            }
            else
            {
                Console.WriteLine("Neispravan odgovor od servera.");
            }
        }
    }
}
