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
        private static Dictionary<int, int> ukupniPoeniPoIgracima = new Dictionary<int, int>();
        private static int playerIdCounter = 1;

        static void Main(string[] args)
        {
            udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpSocket.Bind(new IPEndPoint(IPAddress.Any, 5000));
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
                    Console.WriteLine($"🎮 {igrac.ImeNadimak} je počeo da igra!");

                    List<string> igreZaIgraca = igrePoIgracima[playerId];
                    if (!ukupniPoeniPoIgracima.ContainsKey(playerId))
                        ukupniPoeniPoIgracima[playerId] = 0;

                    int ukupniPoeni = ukupniPoeniPoIgracima[playerId];

                    // ISPRAVKA: KVISKO može samo JEDNOM tokom cele sesije!
                    bool pomocKviska = false;

                    // Anagram igra
                    if (igreZaIgraca.Contains("an"))
                    {
                        igrac.UlozenKvisko = false;
                        if (!pomocKviska)
                        {
                            writer.WriteLine("Da li želite da uložite KVISKA za ovu igru? (da/ne)");
                            string odgovorKvisko = reader.ReadLine()?.Trim().ToLower();
                            if (odgovorKvisko == "da")
                            {
                                igrac.UlozenKvisko = true;
                                pomocKviska = true; // KVISKO je iskorišćen!
                            }
                        }

                        Anagram game = new Anagram();
                        game.UcitajRec("words.txt");
                        writer.WriteLine($"Pomešana slova: {game.GenerisiAnagram()}");

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
                                    writer.WriteLine(" Uložili ste KVISKA - osvojeni poeni su DUPLIRANI!");
                                }
                                ukupniPoeni += points;
                                ukupniPoeniPoIgracima[playerId] = ukupniPoeni;
                                writer.WriteLine($"Tačno! Osvojili ste {points} poena.");
                                writer.WriteLine(points);

                                Console.WriteLine($" {igrac.ImeNadimak} - ANAGRAM: +{points} bodova (ukupno: {ukupniPoeni})");
                            }
                            else
                            {
                                writer.WriteLine("Netačno. Pokušajte ponovo.");
                                writer.WriteLine(0);

                                Console.WriteLine($"❌ {igrac.ImeNadimak} - ANAGRAM: netačan odgovor");
                            }
                        }
                    }

                    // Pitanja i odgovori
                    if (igreZaIgraca.Contains("po"))
                    {
                        igrac.UlozenKvisko = false;
                        if (!pomocKviska)
                        {
                            writer.WriteLine("Da li želite da uložite KVISKA za ovu igru? (da/ne)");
                            string odgovorKvisko = reader.ReadLine()?.Trim().ToLower();
                            if (odgovorKvisko == "da")
                            {
                                igrac.UlozenKvisko = true;
                                pomocKviska = true; // KVISKO je iskorišćen!
                            }
                        }

                        PitanjaIOdgovori game = new PitanjaIOdgovori();
                        game.UcitajPitanja();
                        List<bool> prethodniOdgovori = new List<bool>();
                        int poeniPitanja = 0;

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
                            writer.WriteLine("Uložili ste KVISKA - osvojeni poeni su DUPLIRANI!");
                            igrac.UlozenKvisko = false;
                        }

                        ukupniPoeni += poeniPitanja;
                        ukupniPoeniPoIgracima[playerId] = ukupniPoeni;
                        writer.WriteLine($"Ukupno poena iz pitanja: {poeniPitanja}");
                        writer.WriteLine($"Vaši ukupni poeni: {ukupniPoeni}");

                        Console.WriteLine($" {igrac.ImeNadimak} - PITANJA: +{poeniPitanja} bodova (ukupno: {ukupniPoeni})");
                    }

                    // Asocijacije
                    if (igreZaIgraca.Contains("as"))
                    {
                        igrac.UlozenKvisko = false;
                        if (!pomocKviska)
                        {
                            writer.WriteLine("Da li želite da uložite KVISKA za ovu igru? (da/ne)");
                            string odgovorKvisko = reader.ReadLine()?.Trim().ToLower();
                            if (odgovorKvisko == "da")
                            {
                                igrac.UlozenKvisko = true;
                                pomocKviska = true; // KVISKO je iskorišćen!
                            }
                        }

                        Asocijacije asocijacije = new Asocijacije();
                        int poeniPre = ukupniPoeni;

                        while (true)
                        {
                            // Slanje trenutnog stanja
                            foreach (var linija in asocijacije.PrikaziAsocijaciju().Split('\n'))
                            {
                                if (!string.IsNullOrWhiteSpace(linija))
                                    writer.WriteLine(linija.Trim());
                            }
                            writer.WriteLine("END");

                            string unos = reader.ReadLine();
                            if (string.IsNullOrEmpty(unos) || unos.ToLower() == "izlaz")
                            {
                                int poeni = asocijacije.UkupniBodovi;
                                if (igrac.UlozenKvisko)
                                {
                                    poeni *= 2;
                                    writer.WriteLine("✅ Uložili ste KVISKA - osvojeni poeni su DUPLIRANI!");
                                }
                                igrac.UlozenKvisko = false;
                                ukupniPoeni = poeniPre + poeni;
                                ukupniPoeniPoIgracima[playerId] = ukupniPoeni;
                                writer.WriteLine("Napustili ste igru.");
                                writer.WriteLine($"Poeni iz asocijacija: {poeni}");
                                writer.WriteLine($"Vaši ukupni poeni: {ukupniPoeni}");
                                writer.WriteLine("END");

                                Console.WriteLine($" {igrac.ImeNadimak} - ASOCIJACIJE: +{poeni} bodova (ukupno: {ukupniPoeni})");
                                break;
                            }

                            var (poruka, bodovi) = asocijacije.OtvoriPolje(unos);
                            writer.WriteLine(poruka);
                            if (bodovi > 0)
                            {
                                writer.WriteLine($" Bodovi iz asocijacija: {asocijacije.UkupniBodovi}");
                                writer.WriteLine($" Vaši ukupni poeni: {poeniPre + asocijacije.UkupniBodovi}");
                            }
                            writer.WriteLine("END");

                            // Proveri kraj igre
                            if (asocijacije.JeIgraZavrsena())
                            {
                                int poeni = asocijacije.UkupniBodovi;
                                if (igrac.UlozenKvisko)
                                {
                                    poeni *= 2;
                                    writer.WriteLine(" Uložili ste KVISKA - osvojeni poeni su DUPLIRANI!");
                                }
                                igrac.UlozenKvisko = false;
                                ukupniPoeni = poeniPre + poeni;
                                ukupniPoeniPoIgracima[playerId] = ukupniPoeni;
                                writer.WriteLine(" Čestitamo! Rešili ste celu asocijaciju!");
                                writer.WriteLine($" Poeni iz asocijacija: {poeni}");
                                writer.WriteLine($" FINALNI REZULTAT: {ukupniPoeni} UKUPNIH BODOVA! ");
                                writer.WriteLine("END");

                                Console.WriteLine($" {igrac.ImeNadimak} - ASOCIJACIJE: +{poeni} bodova (ukupno: {ukupniPoeni})");
                                break;
                            }
                        }
                    }

                    writer.WriteLine($"\n FINALNI REZULTAT ZA {igrac.ImeNadimak.ToUpper()}: {ukupniPoeniPoIgracima[playerId]} UKUPNIH BODOVA! ");

                    // Prikaži sve igrače posle završetka
                    PrikaziSveIgrace();
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

        private static void PrikaziSveIgrace()
        {
            Console.WriteLine("\n=== TRENUTNI REZULTATI ===");
            foreach (var igrac in ukupniPoeniPoIgracima.OrderByDescending(x => x.Value))
            {
                string ime = igraci.ContainsKey(igrac.Key) ? igraci[igrac.Key].ImeNadimak : $"Igrac {igrac.Key}";
                Console.WriteLine($"{ime}: {igrac.Value} bodova");
            }
            Console.WriteLine("==========================\n");
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