﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kvizkoteka;
using System.IO;

namespace Server
{
    public class Program
    {
        private static Socket udpSocket; // UDP socket
        private static Socket tcpSocket; // TCP socket
        private static Dictionary<int, Igrac> igraci; // Skladisti informacije o igracima
        private static Dictionary<int, List<string>> igrePoIgracima; // Skladisti igre po ID-ovima igraca
        private static int playerIdCounter = 1; // Brojac ID-a igraca

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
                    Socket clientSocket = tcpSocket.Accept();
                    Thread clientThread = new Thread(() =>
                    {
                        HandleClient(clientSocket);
                    });
                    clientThread.Start();
                }
            });
            tcpThread.Start();
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
                bool isTrainingGame = igrePoIgracima[playerId].Contains("as");

                string welcomeMessage = isTrainingGame
                    ? $"Dobrodošli u trening igru kviza Kviskoteka, današnji takmičar je {igrac.ImeNadimak}"
                    : $"Dobrodošli u igru kviza Kviskoteka, današnji takmičar je {igrac.ImeNadimak}";

                writer.WriteLine(welcomeMessage);
                writer.WriteLine("Unesite START da biste započeli igru.");

                string startMessage = reader.ReadLine()?.Trim();
                if (startMessage == "START")
                {
                    List<string> igreZaIgraca = igrePoIgracima[playerId];
                    int totalPoints = 0;

                    if (igreZaIgraca.Contains("an"))
                    {
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
                                totalPoints += points;
                                writer.WriteLine($"Tačno! Osvojili ste {points} poena.");
                            }
                            else
                            {
                                writer.WriteLine("Netačno. Pokušajte ponovo.");
                            }
                        }
                    }
                    else
                    {
                        writer.WriteLine("Niste uneli ništa. Pokušajte ponovo.");
                    }

                    if (igreZaIgraca.Contains("po"))
                    {
                        PitanjaIOdgovori game = new PitanjaIOdgovori();
                        game.UcitajPitanja();
                        List<bool> prethodniOdgovori = new List<bool>();

                        for (int i = 0; i < 10; i++)
                        {
                            if (!game.PostaviPitanje(prethodniOdgovori))
                            {
                                writer.WriteLine("Nema više pitanja.");
                                break;
                            }

                            writer.WriteLine($"Pitanje {i + 1}: {game.TekucePitanje}");
                            writer.WriteLine("a) Tacno");
                            writer.WriteLine("b) Netacno");

                            string clientAnswer = reader.ReadLine()?.Trim().ToLower();

                            if (clientAnswer == "a" || clientAnswer == "b")
                            {
                                int points = game.ProveriOdgovor(clientAnswer);
                                totalPoints += points;
                                writer.WriteLine($"Trenutni broj poena: {totalPoints}");

                                if (points > 0)
                                {
                                    writer.WriteLine("Tacno! Osvojili ste 4 poena.");
                                }
                                else
                                {
                                    writer.WriteLine("Netačno. Pokušajte ponovo.");
                                }

                                prethodniOdgovori.Add(points > 0);
                            }
                            else
                            {
                                writer.WriteLine("Neispravan odgovor. Pokušajte sa 'a' ili 'b'.");
                            }
                        }

                        writer.WriteLine($"Ukupno poena: {totalPoints}");
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
