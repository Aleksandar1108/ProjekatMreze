﻿using System;
using System.Data.SqlTypes;
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

            Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000);
            EndPoint remoteEndpoint = (EndPoint)serverEndpoint;

            string prijava = $"PRIJAVA: {ime}, {igre}";
            byte[] prijavaBytes = Encoding.UTF8.GetBytes(prijava);

            // Slanje prijave serveru
            udpSocket.SendTo(prijavaBytes, remoteEndpoint);

            // Prijem odgovora sa servera
            byte[] buffer = new byte[1024];
            int receivedLength = udpSocket.ReceiveFrom(buffer, ref remoteEndpoint);
            string udpResponse = Encoding.UTF8.GetString(buffer, 0, receivedLength);

            Console.WriteLine("Odgovor servera: " + udpResponse);

            // Obrada TCP informacija
            if (udpResponse.StartsWith("TCP INFO:"))
            {
                try
                {
                    string[] tcpInfo = udpResponse.Substring(10).Split(':');
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
                        Console.WriteLine(welcomeMessage);

                        // Čitanje poruke za unos komande
                        string startPrompt = reader.ReadLine();
                        Console.WriteLine(startPrompt);

                        // Unos komande za početak igre
                        string startCommand = Console.ReadLine();
                        writer.WriteLine(startCommand);

                        if (igre.Contains("an"))
                        {
                            string pitanjeKviska = reader.ReadLine();
                            Console.WriteLine(pitanjeKviska);
                            string odgovorKviska = Console.ReadLine();
                            writer.WriteLine(odgovorKviska);

                            // Čekanje na pomešana slova od servera
                            string mixedLetters = reader.ReadLine();
                            Console.WriteLine("" + mixedLetters);

                            // Unos anagrama
                            Console.Write("Unesite vaš anagram: ");
                            string anagram = Console.ReadLine();
                            writer.WriteLine(anagram);

                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                Console.WriteLine("Odgovor servera: " + line);
                                // Ako je linija broj, to su poeni
                                if (int.TryParse(line, out int anagramPoints))
                                {
                                    totalPoints += anagramPoints;
                                    Console.WriteLine($"Poeni osvojeni u anagramu: {anagramPoints}");
                                    break; // Izađi iz petlje kada nađeš poene
                                }
                            }
                        }

                        if (igre.Contains("po"))
                        {
                            string linija = reader.ReadLine();
                            string prvoPitanje = null;

                            if (linija.StartsWith("Da li želite da uložite KVISKA"))
                            {
                                Console.WriteLine(linija);
                                string odgovorKviska = Console.ReadLine();
                                writer.WriteLine(odgovorKviska);
                                // Odmah pročitaj prvo pitanje posle odgovora na KVISKA
                                prvoPitanje = reader.ReadLine();
                            }
                            else
                            {
                                // Nema KVISKA, ovo je prvo pitanje
                                prvoPitanje = linija;
                            }

                            for (int i = 0; i < 10; i++)
                            {
                                string pitanje;
                                if (i == 0 && prvoPitanje != null)
                                {
                                    pitanje = prvoPitanje;
                                }
                                else
                                {
                                    pitanje = reader.ReadLine();
                                }

                                if (pitanje == "Nema više pitanja.") break;

                                Console.WriteLine(pitanje);
                                Console.WriteLine("a) Tačno");
                                Console.WriteLine("b) Netačno");
                                string odgovor = Console.ReadLine();
                                writer.WriteLine(odgovor);

                                string odgovorServera = reader.ReadLine();
                                Console.WriteLine(odgovorServera);

                                if (odgovorServera.Contains("Tačno"))
                                {
                                    totalPoints += 4;
                                    Console.WriteLine("Trenutni broj poena: " + totalPoints);
                                }
                                else
                                {
                                    Console.WriteLine("Odgovor je netačan. Poeni ostaju isti.");
                                    Console.WriteLine("Trenutni broj poena: " + totalPoints);
                                }
                                Console.WriteLine();
                            }

                            Console.WriteLine($"Ukupno poena: {totalPoints}");


                        }

                        if (igre.Contains("as"))
                        {
                            // ISPRAVKA: Pravilno čitanje KVISKA pitanja za asocijacije
                            string kviskaLinija = reader.ReadLine();
                            if (kviskaLinija.StartsWith("Da li želite da uložite KVISKA"))
                            {
                                Console.WriteLine(kviskaLinija);
                                string odgovorKviska = Console.ReadLine();
                                writer.WriteLine(odgovorKviska);
                            }

                            Console.WriteLine("🎯 === ASOCIJACIJE ===");
                            Console.WriteLine("📋 SISTEM BODOVANJA:");
                            Console.WriteLine("   • Rešavanje kolone: (broj neotvorenih polja + 2) bodova");
                            Console.WriteLine("   • Konačno rešenje: +10 bodova");
                            Console.WriteLine();
                            Console.WriteLine("⌨  KOMANDE:");
                            Console.WriteLine("   • Za otvaranje polja: A1, B2, C3, D4, itd.");
                            Console.WriteLine("   • Za rešavanje kolone: A:odgovor, B:odgovor, itd.");
                            Console.WriteLine("   • Za konačno rešenje: K:odgovor");
                            Console.WriteLine("   • Za izlaz: izlaz");
                            Console.WriteLine();

                            // Igra Asocijacija
                            while (true)
                            {
                                Console.WriteLine("═══════════════════════════════════════════════════════");
                                Console.WriteLine("🎮 TRENUTNO STANJE IGRE:");
                                Console.WriteLine("═══════════════════════════════════════════════════════");

                                // Čitaj stanje igre od servera
                                string linija;
                                while ((linija = reader.ReadLine()) != null && linija != "END")
                                {
                                    Console.WriteLine(linija);
                                }

                                Console.WriteLine("═══════════════════════════════════════════════════════");
                                Console.Write("⚡ Unesite komandu: ");
                                string unos = Console.ReadLine();

                                // Ako je unos "izlaz", prekid igre
                                if (unos.Equals("izlaz", StringComparison.OrdinalIgnoreCase))
                                {
                                    writer.WriteLine(unos);
                                    // Čitaj završnu poruku
                                    while ((linija = reader.ReadLine()) != null && linija != "END")
                                    {
                                        Console.WriteLine(linija);
                                    }
                                    Console.WriteLine("👋 Hvala vam na igranju!");
                                    break;
                                }

                                // Pošalji unos serveru
                                writer.WriteLine(unos);

                                // Primi odgovor servera (poruke o rezultatu)
                                Console.WriteLine("\n📊 === REZULTAT ===");
                                bool igraZavrsena = false;
                                while ((linija = reader.ReadLine()) != null && linija != "END")
                                {
                                    Console.WriteLine(linija);
                                    // Proveri da li je igra završena
                                    if (linija.Contains("Čestitamo! Rešili ste celu asocijaciju") ||
                                        linija.Contains("FINALNI REZULTAT"))
                                    {
                                        igraZavrsena = true;
                                    }
                                }

                                if (igraZavrsena)
                                {
                                    Console.WriteLine("🎉 IGRA JE ZAVRŠENA! 🎉");
                                    Console.WriteLine("Pritisnite bilo koji taster za izlaz...");
                                    Console.ReadKey();
                                    break;
                                }
                            }
                        }

                        tcpClient.Close();
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