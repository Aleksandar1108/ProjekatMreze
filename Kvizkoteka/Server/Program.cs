using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kvizkoteka;
using System.IO;

namespace Server
{
    internal class Program
    {
       
        private static UdpClient udpListener; // Slusa UDP poruke
        private static TcpListener tcpListener; // Slusa TCP konekcije
        private static Dictionary<int, Igrac> igraci; // Skladisti informacije o igracima
        private static Dictionary<int, List<string>> igrePoIgracima; // Skladisti igre po ID-ovima igraca
        private static int playerIdCounter = 1; // Brojac ID-a igraca
        static void Main(string[] args)
        {
            udpListener = new UdpClient(5000);
            tcpListener = new TcpListener(IPAddress.Any, 5001);
            tcpListener.Start();

            igraci = new Dictionary<int, Igrac>();
            igrePoIgracima = new Dictionary<int, List<string>>();

            Console.WriteLine("Server pokrenut...");

            // Pokretanje UDP niti
            Thread udpThread = new Thread(() =>
            {
                HandleUdpRequests();
            });
            udpThread.Start();

            // Pokretanje TCP niti
            Thread tcpThread = new Thread(() =>
            {
                while (true)
                {
                    TcpClient client = tcpListener.AcceptTcpClient();
                    Thread clientThread = new Thread(() =>
                    {
                        HandleClient(client);
                    });
                    clientThread.Start();
                }
            });
            tcpThread.Start();
        }

        private static void HandleUdpRequests()
        {
            while (true)
            {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] receivedData = udpListener.Receive(ref remoteEndPoint);
                string message = Encoding.UTF8.GetString(receivedData);

                if (message.StartsWith("PRIJAVA:"))
                {
                    // Parsiranje poruke
                    string[] parts = message.Substring(8).Split(new[] { ',' }, 2);

                    if (parts.Length < 2)
                    {
                        string errorResponse = "Neispravna prijava. Format: PRIJAVA: [ime/nadimak], [igre]";
                        byte[] errorData = Encoding.UTF8.GetBytes(errorResponse);
                        udpListener.Send(errorData, errorData.Length, remoteEndPoint);
                        continue;
                    }

                    string name = parts[0].Trim();
                    string gamesList = parts[1].Trim();
                    string[] requestedGames = gamesList.Split(',').Select(g => g.Trim()).ToArray();

                    
                    bool validGames = requestedGames.All(g => g == "an" || g == "po" || g == "as");
                    if (!validGames)
                    {
                        string errorResponse = "Neispravna lista igara. Dozvoljene igre: an, po, as";
                        byte[] errorData = Encoding.UTF8.GetBytes(errorResponse);
                        udpListener.Send(errorData, errorData.Length, remoteEndPoint);
                        continue;
                    }

                    
                    Igrac igrac = new Igrac
                    {
                        Id = playerIdCounter++,
                        ImeNadimak = name
                    };
                    igraci[igrac.Id] = igrac;
                    igrePoIgracima[igrac.Id] = new List<string>(requestedGames);

                    // Slanje odgovora klijentu sa TCP informacijama
                    string response = $"TCP INFO: {GetLocalIpAddress()}:{((IPEndPoint)tcpListener.LocalEndpoint).Port}";
                    byte[] responseData = Encoding.UTF8.GetBytes(response);
                    udpListener.Send(responseData, responseData.Length, remoteEndPoint);
                }
            }
        }

        private static void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
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
                bool isTrainingGame = igrePoIgracima[playerId].Contains("as");

                
                string welcomeMessage = isTrainingGame
                    ? $"Dobrodošli u trening igru kviza Kviskoteka, današnji takmičar je {igrac.ImeNadimak}"
                    : $"Dobrodošli u igru kviza Kviskoteka, današnji takmičar je {igrac.ImeNadimak}";

                writer.WriteLine(welcomeMessage);

               
                writer.WriteLine("Unesite START da biste započeli igru.");

                
                string startMessage = reader.ReadLine()?.Trim(); // Čeka unos od klijenta(START)

                if (startMessage == "START")
                {
                    
                    Anagram game = new Anagram();
                    game.UcitajRec("words.txt"); 
                    string scrambledWord = game.GenerisiAnagram(); 

                    
                    writer.WriteLine($"Pomešana slova: {scrambledWord}");

                    // Cekamo unos anagrama od klijenta
                    string clientAnagram = reader.ReadLine()?.Trim(); 

                    if (!string.IsNullOrEmpty(clientAnagram))
                    {
                        game.PredloženAnagram = clientAnagram;
                        if (game.ProveriAnagram())
                        {
                            int points = game.IzracunajPoene();
                            writer.WriteLine($"Tačno! Osvojili ste {points} poena.");
                        }
                        else
                        {
                            writer.WriteLine("Netačno. Pokušajte ponovo.");
                        }
                    }
                    else
                    {
                        writer.WriteLine("Niste uneli ništa. Pokušajte ponovo.");
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
            finally
            {
                client.Close();
            }

        }

        private static string GetLocalIpAddress()
        {
            foreach (var item in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (item.AddressFamily == AddressFamily.InterNetwork) // Samo IPv4 adrese
                {
                    return item.ToString();
                }
            }
            return "127.0.0.1"; // Podrazumevano na localhost ako nije pronađena IPv4 adresa
        }

    }
}

