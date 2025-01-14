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
