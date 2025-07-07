using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace Server
{
    public class Program
    {
        private static Socket udpSocket;
        private static Socket tcpSocket;
        private static Dictionary<int, Igrac> igraci;
        private static Dictionary<int, List<string>> igrePoIgracima;
        private static Dictionary<int, int> ukupniPoeniPoIgracima = new Dictionary<int, int>(); // NOVO - čuva ukupne poene
        private static int playerIdCounter = 1;

        static void Main(string[] args)
        {
            // Pokretanje UDP soketa
            udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpSocket.Bind(new IPEndPoint(IPAddress.Any, 5000));

            // Pokretanje TCP soketa
            tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            tcpSocket.Bind(new IPEndPoint(IPAddress.Any, 5001));
            tcpSocket.Listen(10);

            igraci = new Dictionary<int, Igrac>();
            igrePoIgracima = new Dictionary<int, List<string>>();

            Console.WriteLine("Server pokrenut...");

            _ = Task.Run(() => HandleUdpRequests());

            while (true)
            {
                Socket clientSocket = tcpSocket.Accept();
                Task.Run(() => HandleClient(clientSocket));
            }
        }

        private static void HandleUdpRequests()
        {
            byte[] buffer = new byte[1024];

            while (true)
            {
                EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                int receivedBytes = udpSocket.ReceiveFrom(buffer, ref remoteEndPoint);
                string message = Encoding.UTF8.GetString(buffer, 0, receivedBytes);

                if (message.StartsWith("PRIJAVA:"))
                {
                    string[] parts = message.Substring(8).Split(new[] { ',' }, 2);
                    if (parts.Length < 2)
                    {
                        string errorResponse = "Neispravna prijava. Format: PRIJAVA: [ime/nadimak], [igre]";
                        udpSocket.SendTo(Encoding.UTF8.GetBytes(errorResponse), remoteEndPoint);
                        continue;
                    }

                    string name = parts[0].Trim();
                    string gamesList = parts[1].Trim();
                    string[] requestedGames = gamesList.Split(',').Select(g => g.Trim()).ToArray();

                    bool validGames = requestedGames.All(g => g == "an" || g == "po" || g == "as");
                    if (!validGames)
                    {
                        string errorResponse = "Neispravna lista igara. Dozvoljene igre: an, po, as";
                        udpSocket.SendTo(Encoding.UTF8.GetBytes(errorResponse), remoteEndPoint);
                        continue;
                    }

                    Igrac igrac = new Igrac
                    {
                        Id = playerIdCounter++,
                        ImeNadimak = name
                    };

                    igraci[igrac.Id] = igrac;
                    igrePoIgracima[igrac.Id] = new List<string>(requestedGames);

                    Console.WriteLine($"Registrovan igrac: {name} (ID: {igrac.Id}) - Igre: {string.Join(", ", requestedGames)}");

                    string response = $"TCP INFO: {GetLocalIpAddress()}:{((IPEndPoint)tcpSocket.LocalEndPoint).Port}";
                    udpSocket.SendTo(Encoding.UTF8.GetBytes(response), remoteEndPoint);
                }
            }
        }

        private static void HandleClient(Socket clientSocket)
        {
            NetworkStream stream = new NetworkStream(clientSocket);
            StreamReader reader = new StreamReader(stream);
            StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

            try
            {
                string playerIdMessage = reader.ReadLine();
                if (string.IsNullOrEmpty(playerIdMessage) || !int.TryParse(playerIdMessage, out int playerId) || !igraci.ContainsKey(playerId))
                {
                    writer.WriteLine("Neispravan ID igrača.");
                    return;
                }

                Igrac igrac = igraci[playerId];
                Console.WriteLine($"Klijent povezan: {igrac.ImeNadimak} (ID: {playerId})");

                string welcomeMessage = $"Dobrodošli u igru kviza Kviskoteka, današnji takmičar je {igrac.ImeNadimak}";
                writer.WriteLine(welcomeMessage);

                writer.WriteLine("Unesite START da biste započeli igru.");

                string startMessage = reader.ReadLine()?.Trim();

                if (startMessage == "START")
                {
                    List<string> igreZaIgraca = igrePoIgracima[playerId];

                    // Inicijalizuj ukupne poene za igrača ako ne postoje
                    if (!ukupniPoeniPoIgracima.ContainsKey(playerId))
                    {
                        ukupniPoeniPoIgracima[playerId] = 0;
                    }

                    int ukupniPoeni = ukupniPoeniPoIgracima[playerId]; // Učitaj postojeće poene

                  
                    // Anagram igra
                    if (igreZaIgraca.Contains("an"))
                    {
                        igrac.UlozenKvisko = false; 

                        Console.WriteLine($"Igrac {igrac.ImeNadimak} počinje sa {ukupniPoeni} poena");
                        writer.WriteLine("Da li želite da uložite KVISKA za ovu igru? (da/ne)");
                        string odgovorKvisko = reader.ReadLine()?.Trim().ToLower();
                        igrac.UlozenKvisko = (odgovorKvisko == "da");
                        

                        Anagram game = new Anagram();
                        game.UcitajRec("words.txt");
                        string scrambledWord = game.GenerisiAnagram();
                        writer.WriteLine($"Pomešana slova: {scrambledWord}");

                        string clientAnagram = reader.ReadLine()?.Trim();
                        if (!string.IsNullOrEmpty(clientAnagram))
                        {
                            game.PredloženAnagram = clientAnagram;
                            if (game.ProveriAnagram())
                            {
                               
                                int points = game.IzracunajPoene();
                                if (igrac.UlozenKvisko)
                                {
                                    points *= 2;
                                    igrac.UlozenKvisko = false;
                                    writer.WriteLine("✅ Uložili ste KVISKA - osvojeni poeni su DUPLIRANI!");
                                }
                                ukupniPoeni += points; // Dodaj na ukupne poene
                                ukupniPoeniPoIgracima[playerId] = ukupniPoeni; // Sačuvaj

                                writer.WriteLine($"Tačno! Osvojili ste {points} poena.");
                                writer.WriteLine(points);
                                Console.WriteLine($"Igrac {igrac.ImeNadimak} - Anagram: +{points} poena, ukupno: {ukupniPoeni}");
                            }
                            else
                            {
                                writer.WriteLine("Netačno. Pokušajte ponovo.");
                                writer.WriteLine(0);
                            }
                        }
                    }
                   

                    // Pitanja i odgovori
                    if (igreZaIgraca.Contains("po"))
                    {
                        igrac.UlozenKvisko = false;

                        writer.WriteLine("Da li želite da uložite KVISKA za ovu igru? (da/ne)");
                        string odgovorKvisko = reader.ReadLine()?.Trim().ToLower();
                        igrac.UlozenKvisko = (odgovorKvisko == "da"); 

                        PitanjaIOdgovori game = new PitanjaIOdgovori();
                        game.UcitajPitanja();
                        List<bool> prethodniOdgovori = new List<bool>();
                        int poeniPitanja = 0; // Lokalni brojač za ovu igru

                        for (int i = 0; i < 10; i++)
                        {
                            if (!game.PostaviPitanje(prethodniOdgovori))
                            {
                                writer.WriteLine("Nema više pitanja.");
                                break;
                            }

                            writer.WriteLine($"Pitanje {i + 1}: {game.TekucePitanje}");
                            string clientAnswer = reader.ReadLine()?.Trim().ToLower();

                            if (clientAnswer == "a" || clientAnswer == "b")
                            {
                                bool isCorrect = game.ProveriOdgovor(clientAnswer);
                                if (isCorrect)
                                {   
                                        poeniPitanja += 4;
                                      //  ukupniPoeni += 4; // Dodaj na ukupne poene
                                        writer.WriteLine("Tačno! Osvojili ste 4 poena.");   
                                }
                                else
                                {
                                    writer.WriteLine("Netačno.");
                                }
                            }
                            else
                            {
                                writer.WriteLine("Neispravan odgovor. Pokušajte sa 'a' ili 'b'.");
                            }
                        }

                        if (igrac.UlozenKvisko)
                        {
                             poeniPitanja *= 2;
                             ukupniPoeni +=poeniPitanja ;
                            
                            igrac.UlozenKvisko = false;
                            
                            writer.WriteLine("Uložili ste KVISKA - osvojeni poeni su DUPLIRANI!");
                            writer.WriteLine($"Ukupno osvojenih poena: {ukupniPoeni}");
                            ukupniPoeniPoIgracima[playerId] = ukupniPoeni;

                        }

                        ukupniPoeniPoIgracima[playerId] = ukupniPoeni; // Sačuvaj ukupne poene
                        writer.WriteLine($"Ukupno poena iz pitanja: {poeniPitanja}");
                        writer.WriteLine($"Vaši ukupni poeni: {ukupniPoeni}");
                        Console.WriteLine($"Igrac {igrac.ImeNadimak} - Pitanja: +{poeniPitanja} poena, ukupno: {ukupniPoeni}");
                    }

                    
                    // Asocijacije
                    if (igreZaIgraca.Contains("as"))
                    {
                        igrac.UlozenKvisko = false;
                        Console.WriteLine($"Igrac {igrac.ImeNadimak} počinje sa {ukupniPoeni} poena");
                        writer.WriteLine("Da li želite da uložite KVISKA za ovu igru? (da/ne)");
                        string odgovorKvisko = reader.ReadLine()?.Trim().ToLower();
                        igrac.UlozenKvisko = (odgovorKvisko == "da");

                        Console.WriteLine($"Pokretanje Asocijacije igre za {igrac.ImeNadimak}");
                        Asocijacije asocijacije = new Asocijacije();
                        bool krajIgre = false;
                        int poeniPredAsocijacije = ukupniPoeni; // Zapamti poene pre asocijacija

                        while (!krajIgre)
                        {
                            // Pošalji trenutno stanje igre
                            string trenutnoStanje = asocijacije.PrikaziAsocijaciju();
                            foreach (string linija in trenutnoStanje.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                writer.WriteLine(linija);
                            }
                            writer.WriteLine("END");

                            // Čekaj unos od klijenta
                            string unos = reader.ReadLine();
                            if (string.IsNullOrEmpty(unos))
                            {
                                Console.WriteLine("Klijent je prekinuo konekciju");
                                break;
                            }

                            Console.WriteLine($"Primljen unos od {igrac.ImeNadimak}: {unos}");

                            if (unos.Equals("izlaz", StringComparison.OrdinalIgnoreCase))
                            {
                                // Dodaj poene iz asocijacija na ukupne poene
                                int poeniAsocijacije = asocijacije.UkupniBodovi;
                                ukupniPoeni = poeniPredAsocijacije + poeniAsocijacije;
                                ukupniPoeniPoIgracima[playerId] = ukupniPoeni;

                                writer.WriteLine("Napustili ste igru.");
                                writer.WriteLine($"Poeni iz asocijacija: {poeniAsocijacije}");
                                writer.WriteLine($"Vaši ukupni poeni: {ukupniPoeni}");
                                writer.WriteLine("END");
                                Console.WriteLine($"Igrac {igrac.ImeNadimak} - Asocijacije: +{poeniAsocijacije} poena, ukupno: {ukupniPoeni}");
                                break;
                            }

                            // Obradi unos
                            var (poruka, bodovi) = asocijacije.OtvoriPolje(unos);

                            // Pošalji poruku o rezultatu
                            writer.WriteLine(poruka);
                            if (bodovi > 0)
                            {
                                writer.WriteLine($"💰 Bodovi iz asocijacija: {asocijacije.UkupniBodovi}");
                                writer.WriteLine($"💰 Vaši ukupni poeni: {poeniPredAsocijacije + asocijacije.UkupniBodovi}");
                            }
                            writer.WriteLine("END");

                            // Proveri da li je igra završena
                            if (asocijacije.JeIgraZavrsena())
                            {
                                // Dodaj poene iz asocijacija na ukupne poene
                                int poeniAsocijacije = asocijacije.UkupniBodovi;
                                if (igrac.UlozenKvisko)
                                {
                                    poeniAsocijacije *= 2;
                                    igrac.UlozenKvisko= false;
                                    writer.WriteLine(" Uložili ste KVISKA - osvojeni poeni su DUPLIRANI!");
                                }
                                ukupniPoeni = poeniPredAsocijacije + poeniAsocijacije;
                                ukupniPoeniPoIgracima[playerId] = ukupniPoeni;

                                writer.WriteLine(" Čestitamo! Rešili ste celu asocijaciju!");
                                writer.WriteLine($" Poeni iz asocijacija: {poeniAsocijacije}");
                                writer.WriteLine($" FINALNI REZULTAT: {ukupniPoeni} UKUPNIH BODOVA! ");
                                writer.WriteLine("END");
                                krajIgre = true;
                                Console.WriteLine($"{igrac.ImeNadimak} je završio Asocijacije igru sa {poeniAsocijacije} bodova iz asocijacija, ukupno: {ukupniPoeni}");
                            }
                        }
                    }

                    // Na kraju svih igara, prikaži finalne rezultate
                    writer.WriteLine($"\n🎯 FINALNI REZULTAT ZA {igrac.ImeNadimak.ToUpper()}: {ukupniPoeniPoIgracima[playerId]} UKUPNIH BODOVA! 🎯");
                    Console.WriteLine($"=== FINALNI REZULTAT === {igrac.ImeNadimak}: {ukupniPoeniPoIgracima[playerId]} ukupnih bodova");
                }
                else
                {
                    writer.WriteLine("Niste poslali START. Pokušajte ponovo.");
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Greška u komunikaciji sa klijentom: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Neočekivana greška: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Zatvaranje konekcije sa klijentom");
                clientSocket.Close();
            }
        }

        private static string GetLocalIpAddress()
        {
            foreach (var item in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (item.AddressFamily == AddressFamily.InterNetwork)
                {
                    return item.ToString();
                }
            }
            return "127.0.0.1";
        }
    }

   
}