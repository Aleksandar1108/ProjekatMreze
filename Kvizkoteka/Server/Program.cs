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
        private static Dictionary<int, int> kviskoPoeniPoIgracima = new Dictionary<int, int>(); // NOVA VARIJABLA!
        private static int playerIdCounter = 1;
        private static int ukupanBrojIgraca = 0;
        private static int zavrseniIgraci = 0;
        private static bool anagramPrviZavrsen = false;

        // Brojač grešaka i završetak asocijacija
        private static int brojGreskaKonacnoResenje = 0;
        private static readonly int maxGresaka = 5;
        private static bool asocijacijaZavrsenaDueToErrors = false;
        private static bool asocijacijaZavrsenaDueToSolution = false;
        private static string pobednikAsocijacija = "";

        private static readonly object lockObject = new object();

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

                    lock (lockObject)
                    {
                        ukupanBrojIgraca++;
                    }

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
                    if (!kviskoPoeniPoIgracima.ContainsKey(playerId))
                        kviskoPoeniPoIgracima[playerId] = 0; // INICIJALIZUJ KVISKO BODOVE

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

                                // Proveri da li je prvi koji je završio anagram
                                bool dobijaBonusZaPrvog = false;
                                lock (lockObject)
                                {
                                    if (!anagramPrviZavrsen)
                                    {
                                        anagramPrviZavrsen = true;
                                        dobijaBonusZaPrvog = true;
                                    }
                                }

                                // Dodaj bonus za prvog
                                if (dobijaBonusZaPrvog)
                                {
                                    int bonus = (int)(points * 0.1); // 10% bonus
                                    points += bonus;
                                    writer.WriteLine($"🎯 PRVI STE ZAVRŠILI ANAGRAM! Bonus: +{bonus} bodova!");
                                    Console.WriteLine($"🎯 {igrac.ImeNadimak} je PRVI završio anagram! Bonus: +{bonus} bodova");
                                }

                                if (igrac.UlozenKvisko)
                                {
                                    int kviskoBonus = points; // KVISKO BONUS = originalni poeni
                                    points *= 2;
                                    kviskoPoeniPoIgracima[playerId] += kviskoBonus; // DODAJ KVISKO BODOVE!
                                    igrac.UlozenKvisko = false;
                                    writer.WriteLine("✅ Uložili ste KVISKA - osvojeni poeni su DUPLIRANI!");
                                    Console.WriteLine($"💎 {igrac.ImeNadimak} - KVISKO bonus: +{kviskoBonus} bodova!");
                                }

                                ukupniPoeni += points;
                                ukupniPoeniPoIgracima[playerId] = ukupniPoeni;
                                writer.WriteLine($"Tačno! Osvojili ste {points} poena.");
                                writer.WriteLine(points);

                                Console.WriteLine($"✅ {igrac.ImeNadimak} - ANAGRAM: +{points} bodova (ukupno: {ukupniPoeni})");
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
                            int kviskoBonus = poeniPitanja; // KVISKO BONUS = originalni poeni
                            poeniPitanja *= 2;
                            kviskoPoeniPoIgracima[playerId] += kviskoBonus; // DODAJ KVISKO BODOVE!
                            writer.WriteLine("✅ Uložili ste KVISKA - osvojeni poeni su DUPLIRANI!");
                            igrac.UlozenKvisko = false;
                            Console.WriteLine($"💎 {igrac.ImeNadimak} - KVISKO bonus: +{kviskoBonus} bodova!");
                        }

                        ukupniPoeni += poeniPitanja;
                        ukupniPoeniPoIgracima[playerId] = ukupniPoeni;
                        writer.WriteLine($"Ukupno poena iz pitanja: {poeniPitanja}");
                        writer.WriteLine($"Vaši ukupni poeni: {ukupniPoeni}");

                        Console.WriteLine($"📝 {igrac.ImeNadimak} - PITANJA: +{poeniPitanja} bodova (ukupno: {ukupniPoeni})");
                    }

                    // Asocijacije - SVAKI IGRAČ IMA SVOJU, ALI GLOBALNI ZAVRŠETAK
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

                        Asocijacije asocijacije = new Asocijacije(); // Svaki igrač ima svoju!
                        int poeniPre = ukupniPoeni;

                        while (true)
                        {
                            // Proveri da li je igra završena zbog grešaka ili rešenja
                            lock (lockObject)
                            {
                                if (asocijacijaZavrsenaDueToErrors)
                                {
                                    int poeni = asocijacije.UkupniBodovi;
                                    if (igrac.UlozenKvisko && poeni > 0)
                                    {
                                        int kviskoBonus = poeni; // KVISKO BONUS = originalni poeni
                                        poeni *= 2;
                                        kviskoPoeniPoIgracima[playerId] += kviskoBonus; // DODAJ KVISKO BODOVE!
                                        writer.WriteLine("✅ Uložili ste KVISKA - osvojeni poeni su DUPLIRANI!");
                                        Console.WriteLine($"💎 {igrac.ImeNadimak} - KVISKO bonus: +{kviskoBonus} bodova!");
                                    }
                                    igrac.UlozenKvisko = false;
                                    ukupniPoeni = poeniPre + poeni;
                                    ukupniPoeniPoIgracima[playerId] = ukupniPoeni;
                                    writer.WriteLine("🔗 Asocijacija je završena zbog previše grešaka za konačno rešenje!");
                                    writer.WriteLine($"Poeni iz asocijacija: {poeni}");
                                    writer.WriteLine($"Vaši ukupni poeni: {ukupniPoeni}");
                                    writer.WriteLine("END");

                                    Console.WriteLine($"🔗 {igrac.ImeNadimak} - ASOCIJACIJE: +{poeni} bodova (ukupno: {ukupniPoeni})");
                                    break;
                                }

                                // NOVA PROVERA - da li je neko pogodio konačno rešenje
                                if (asocijacijaZavrsenaDueToSolution)
                                {
                                    int poeni = asocijacije.UkupniBodovi;
                                    if (igrac.UlozenKvisko && poeni > 0)
                                    {
                                        int kviskoBonus = poeni; // KVISKO BONUS = originalni poeni
                                        poeni *= 2;
                                        kviskoPoeniPoIgracima[playerId] += kviskoBonus; // DODAJ KVISKO BODOVE!
                                        writer.WriteLine("✅ Uložili ste KVISKA - osvojeni poeni su DUPLIRANI!");
                                        Console.WriteLine($"💎 {igrac.ImeNadimak} - KVISKO bonus: +{kviskoBonus} bodova!");
                                    }
                                    igrac.UlozenKvisko = false;
                                    ukupniPoeni = poeniPre + poeni;
                                    ukupniPoeniPoIgracima[playerId] = ukupniPoeni;
                                    writer.WriteLine($"🎉 {pobednikAsocijacija} je pogodio konačno rešenje! Igra je završena!");
                                    writer.WriteLine($"Poeni iz asocijacija: {poeni}");
                                    writer.WriteLine($"Vaši ukupni poeni: {ukupniPoeni}");
                                    writer.WriteLine("END");

                                    Console.WriteLine($"🔗 {igrac.ImeNadimak} - ASOCIJACIJE: +{poeni} bodova (ukupno: {ukupniPoeni})");
                                    break;
                                }
                            }

                            // Slanje trenutnog stanja
                            foreach (var linija in asocijacije.PrikaziAsocijaciju().Split('\n'))
                            {
                                if (!string.IsNullOrWhiteSpace(linija))
                                    writer.WriteLine(linija.Trim());
                            }

                            // Prikaži broj grešaka
                            lock (lockObject)
                            {
                                writer.WriteLine($"⚠️  Ukupno grešaka za konačno rešenje: {brojGreskaKonacnoResenje}/{maxGresaka}");
                            }
                            writer.WriteLine("END");

                            string unos = reader.ReadLine();
                            if (string.IsNullOrEmpty(unos) || unos.ToLower() == "izlaz")
                            {
                                int poeni = asocijacije.UkupniBodovi;
                                if (igrac.UlozenKvisko && poeni > 0)
                                {
                                    int kviskoBonus = poeni; // KVISKO BONUS = originalni poeni
                                    poeni *= 2;
                                    kviskoPoeniPoIgracima[playerId] += kviskoBonus; // DODAJ KVISKO BODOVE!
                                    writer.WriteLine("✅ Uložili ste KVISKA - osvojeni poeni su DUPLIRANI!");
                                    Console.WriteLine($"💎 {igrac.ImeNadimak} - KVISKO bonus: +{kviskoBonus} bodova!");
                                }
                                igrac.UlozenKvisko = false;
                                ukupniPoeni = poeniPre + poeni;
                                ukupniPoeniPoIgracima[playerId] = ukupniPoeni;
                                writer.WriteLine("Napustili ste igru.");
                                writer.WriteLine($"Poeni iz asocijacija: {poeni}");
                                writer.WriteLine($"Vaši ukupni poeni: {ukupniPoeni}");
                                writer.WriteLine("END");

                                Console.WriteLine($"🔗 {igrac.ImeNadimak} - ASOCIJACIJE: +{poeni} bodova (ukupno: {ukupniPoeni})");
                                break;
                            }

                            var (poruka, bodovi) = asocijacije.OtvoriPolje(unos);

                            // Proveri da li je pokušaj konačnog rešenja
                            if (unos.StartsWith("K:"))
                            {
                                if (poruka.Contains("Tačno"))
                                {
                                    // NEKO JE POGODIO KONAČNO REŠENJE - ZAVRŠI ZA SVE!
                                    lock (lockObject)
                                    {
                                        asocijacijaZavrsenaDueToSolution = true;
                                        pobednikAsocijacija = igrac.ImeNadimak;
                                    }
                                    Console.WriteLine($"🎉 {igrac.ImeNadimak} je pogodio konačno rešenje asocijacije! Igra završena za sve!");
                                }
                                else
                                {
                                    // Greška za konačno rešenje
                                    lock (lockObject)
                                    {
                                        brojGreskaKonacnoResenje++;
                                        Console.WriteLine($"❌ {igrac.ImeNadimak} - pogrešno konačno rešenje! Greška {brojGreskaKonacnoResenje}/{maxGresaka}");

                                        if (brojGreskaKonacnoResenje >= maxGresaka)
                                        {
                                            asocijacijaZavrsenaDueToErrors = true;
                                            Console.WriteLine("🔗 Asocijacije završene zbog previše grešaka za konačno rešenje!");
                                        }
                                    }
                                }
                            }

                            writer.WriteLine(poruka);
                            if (bodovi > 0)
                            {
                                writer.WriteLine($"💰 Bodovi iz asocijacija: {asocijacije.UkupniBodovi}");
                                writer.WriteLine($"💰 Vaši ukupni poeni: {poeniPre + asocijacije.UkupniBodovi}");
                            }
                            writer.WriteLine("END");

                            // Proveri kraj igre
                            if (asocijacije.JeIgraZavrsena())
                            {
                                int poeni = asocijacije.UkupniBodovi;
                                if (igrac.UlozenKvisko)
                                {
                                    int kviskoBonus = poeni; // KVISKO BONUS = originalni poeni
                                    poeni *= 2;
                                    kviskoPoeniPoIgracima[playerId] += kviskoBonus; // DODAJ KVISKO BODOVE!
                                    writer.WriteLine("✅ Uložili ste KVISKA - osvojeni poeni su DUPLIRANI!");
                                    Console.WriteLine($"💎 {igrac.ImeNadimak} - KVISKO bonus: +{kviskoBonus} bodova!");
                                }
                                igrac.UlozenKvisko = false;
                                ukupniPoeni = poeniPre + poeni;
                                ukupniPoeniPoIgracima[playerId] = ukupniPoeni;
                                writer.WriteLine("🎉 Čestitamo! Rešili ste celu asocijaciju!");
                                writer.WriteLine($"📊 Poeni iz asocijacija: {poeni}");
                                writer.WriteLine($"🎯 FINALNI REZULTAT: {ukupniPoeni} UKUPNIH BODOVA! 🎯");
                                writer.WriteLine("END");

                                Console.WriteLine($"🔗 {igrac.ImeNadimak} - ASOCIJACIJE ZAVRŠENE: +{poeni} bodova (ukupno: {ukupniPoeni})");
                                break;
                            }
                        }
                    }

                    writer.WriteLine($"\n🎯 FINALNI REZULTAT ZA {igrac.ImeNadimak.ToUpper()}: {ukupniPoeniPoIgracima[playerId]} UKUPNIH BODOVA! 🎯");

                    // Prikaži sve igrače posle završetka
                    PrikaziSveIgrace();

                    // Proveri da li su svi završili
                    lock (lockObject)
                    {
                        zavrseniIgraci++;
                        Console.WriteLine($"🏁 {igrac.ImeNadimak} je završio igru! ({zavrseniIgraci}/{ukupanBrojIgraca})");

                        if (zavrseniIgraci == ukupanBrojIgraca && ukupanBrojIgraca >= 2)
                        {
                            ProglasiPobednika();
                        }
                    }
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
                int kviskoPoeni = kviskoPoeniPoIgracima.ContainsKey(igrac.Key) ? kviskoPoeniPoIgracima[igrac.Key] : 0;
                Console.WriteLine($"{ime}: {igrac.Value} bodova (KVISKO: {kviskoPoeni})");
            }
            Console.WriteLine("==========================\n");
        }

        private static void ProglasiPobednika()
        {
            Console.WriteLine("\n" + new string('=', 50));
            Console.WriteLine("🏆 SVI IGRAČI SU ZAVRŠILI - FINALNI REZULTATI! 🏆");
            Console.WriteLine(new string('=', 50));

            var sortiraniIgraci = ukupniPoeniPoIgracima.OrderByDescending(x => x.Value).ToList();

            if (sortiraniIgraci.Count >= 2)
            {
                var pobednik = sortiraniIgraci[0];
                var drugiMesto = sortiraniIgraci[1];

                string imePobednika = igraci.ContainsKey(pobednik.Key) ? igraci[pobednik.Key].ImeNadimak : $"Igrac {pobednik.Key}";
                string imeDrugog = igraci.ContainsKey(drugiMesto.Key) ? igraci[drugiMesto.Key].ImeNadimak : $"Igrac {drugiMesto.Key}";

                int razlika = pobednik.Value - drugiMesto.Value;

                if (razlika == 0)
                {
                    // NEREŠENO - PROVERI KVISKO BODOVE!
                    int kviskoPoeniPobednik = kviskoPoeniPoIgracima.ContainsKey(pobednik.Key) ? kviskoPoeniPoIgracima[pobednik.Key] : 0;
                    int kviskoPoeniDrugi = kviskoPoeniPoIgracima.ContainsKey(drugiMesto.Key) ? kviskoPoeniPoIgracima[drugiMesto.Key] : 0;

                    Console.WriteLine($"🤝 NEREŠENO! Oba igrača imaju {pobednik.Value} bodova!");
                    Console.WriteLine($"💎 KVISKO TIEBREAKER:");
                    Console.WriteLine($"   {imePobednika}: {kviskoPoeniPobednik} KVISKO bodova");
                    Console.WriteLine($"   {imeDrugog}: {kviskoPoeniDrugi} KVISKO bodova");

                    if (kviskoPoeniPobednik > kviskoPoeniDrugi)
                    {
                        Console.WriteLine($"🥇 POBEDNIK (KVISKO): {imePobednika} sa {kviskoPoeniPobednik} KVISKO bodova!");
                        Console.WriteLine($"🥈 DRUGO MESTO: {imeDrugog} sa {kviskoPoeniDrugi} KVISKO bodova!");
                    }
                    else if (kviskoPoeniDrugi > kviskoPoeniPobednik)
                    {
                        Console.WriteLine($"🥇 POBEDNIK (KVISKO): {imeDrugog} sa {kviskoPoeniDrugi} KVISKO bodova!");
                        Console.WriteLine($"🥈 DRUGO MESTO: {imePobednika} sa {kviskoPoeniPobednik} KVISKO bodova!");
                    }
                    else
                    {
                        Console.WriteLine("🤝 POTPUNO NEREŠENO! Isti broj bodova I KVISKO bodova!");
                    }
                }
                else
                {
                    Console.WriteLine($"🥇 POBEDNIK: {imePobednika} sa {pobednik.Value} bodova!");
                    Console.WriteLine($"🥈 DRUGO MESTO: {imeDrugog} sa {drugiMesto.Value} bodova!");
                    Console.WriteLine($"📊 Razlika: {razlika} bodova");
                }

                // Prikaži kompletnu tabelu
                Console.WriteLine("\n📋 KOMPLETNA TABELA:");
                for (int i = 0; i < sortiraniIgraci.Count; i++)
                {
                    var igrac = sortiraniIgraci[i];
                    string ime = igraci.ContainsKey(igrac.Key) ? igraci[igrac.Key].ImeNadimak : $"Igrac {igrac.Key}";
                    int kviskoPoeni = kviskoPoeniPoIgracima.ContainsKey(igrac.Key) ? kviskoPoeniPoIgracima[igrac.Key] : 0;
                    string medal = i == 0 ? "🥇" : i == 1 ? "🥈" : "🥉";
                    Console.WriteLine($"{medal} {i + 1}. {ime}: {igrac.Value} bodova (KVISKO: {kviskoPoeni})");
                }
            }

            Console.WriteLine(new string('=', 50));
            Console.WriteLine("🎉 HVALA SVIMA NA IGRANJU! 🎉");
            Console.WriteLine(new string('=', 50) + "\n");
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