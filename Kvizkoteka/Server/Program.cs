using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kvizkoteka;

namespace Server
{
    internal class Program
    {
        // Deklaracija atributa
        private static UdpClient udpListener; // Sluša UDP poruke
        private static TcpListener tcpListener; // Sluša TCP konekcije
        private static Dictionary<int, Igrac> igraci; // Skladišti informacije o igračima
        private static Dictionary<int, List<string>> igrePoIgracima; // Skladišti igre po ID-ovima igrača
        private static int playerIdCounter = 1; // Brojač ID-a igrača
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

                    // Validacija igara
                    bool validGames = requestedGames.All(g => g == "an" || g == "po" || g == "as");
                    if (!validGames)
                    {
                        string errorResponse = "Neispravna lista igara. Dozvoljene igre: an, po, as";
                        byte[] errorData = Encoding.UTF8.GetBytes(errorResponse);
                        udpListener.Send(errorData, errorData.Length, remoteEndPoint);
                        continue;
                    }

                    // Kreiranje igrača i dodavanje u rečnike
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
            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string playerIdMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

            // Provera da li je ID validan i odgovara igraču
            if (string.IsNullOrEmpty(playerIdMessage) || !int.TryParse(playerIdMessage, out int playerId) || !igraci.ContainsKey(playerId))
            {
                string errorMessage = "Neispravan ID igrača.\n";
                stream.Write(Encoding.UTF8.GetBytes(errorMessage), 0, Encoding.UTF8.GetBytes(errorMessage).Length);
            }
            else
            {
                Igrac igrac = igraci[playerId];
                // Provera da li igrač izabrao "as" (trening igra)
                bool isTrainingGame = igrePoIgracima[playerId].Contains("as");

                string welcomeMessage = isTrainingGame
                    ? $"Dobrodošli u trening igru kviza Kviskoteka, današnji takmičar je {igrac.ImeNadimak}\n"
                    : $"Dobrodošli u igru kviza Kviskoteka, današnji takmičar je {igrac.ImeNadimak}\n";

                byte[] welcomeData = Encoding.UTF8.GetBytes(welcomeMessage);
                stream.Write(welcomeData, 0, welcomeData.Length);

                // Provera za igru "Asocijacije" ako je više puta izabrana u treningu
                if (igrePoIgracima[playerId].Count(g => g == "as") > 1)
                {
                    string errorMessage = "U treningu ne možete izabrati igru 'Asocijacije' više od jednom.\n";
                    byte[] errorData = Encoding.UTF8.GetBytes(errorMessage);
                    stream.Write(errorData, 0, errorData.Length);
                }
            }

            client.Close();
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

