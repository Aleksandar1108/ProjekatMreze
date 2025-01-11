using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Klijent
{
    public class Program
    {
        public void SendAnagram(string anagram, NetworkStream stream)
        {
            byte[] anagramBytes = Encoding.ASCII.GetBytes(anagram);
            stream.Write(anagramBytes, 0, anagramBytes.Length);

            // Čekanje na odgovor
            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            Console.WriteLine("Odgovor servera: " + response);
        }
        static void Main(string[] args)
        {

            Console.Write("Unesite ime/nadimak: ");
            string ime = Console.ReadLine();

            Console.Write("Unesite igre koje želite da igrate (odvojene zarezima): ");
            string igre = Console.ReadLine();

            // UDP deo
            UdpClient udpClient = new UdpClient();
            IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000);
            string prijava = $"PRIJAVA: {ime}, {igre}";
            udpClient.Send(Encoding.UTF8.GetBytes(prijava), prijava.Length, serverEndpoint);

            string udpResponse = Encoding.UTF8.GetString(udpClient.Receive(ref serverEndpoint));
            Console.WriteLine("Odgovor servera: " + udpResponse);

            // TCP deo
            if (udpResponse.StartsWith("TCP INFO:"))
            {
                string[] tcpInfo = udpResponse.Substring(10).Split(':');
                string ip = tcpInfo[0];
                int port = int.Parse(tcpInfo[1]);

                 TcpClient tcpClient = new TcpClient(ip, port);
                 NetworkStream stream = tcpClient.GetStream();
                 StreamReader reader = new StreamReader(stream);
                 StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

                Console.Write("Unesite vaš ID (broj koji vam je dodeljen od servera): ");
                string playerId = Console.ReadLine();
                writer.WriteLine(playerId);

                // Čitanje poruka sa servera
                Console.WriteLine(reader.ReadLine()); // Dobrodošli
                Console.WriteLine(reader.ReadLine()); // Unesite START 

                string startCommand = Console.ReadLine(); // Unos START
                writer.WriteLine(startCommand);

                // Čekanje na pomešana slova od servera
                Console.WriteLine(reader.ReadLine()); 

                
                Console.Write("Unesite vaš anagram: ");
                string anagram = Console.ReadLine();
                writer.WriteLine(anagram);

                // Prikazivanje rezultata
                Console.WriteLine("Odgovor servera: " + reader.ReadLine());
            }

        }
    }
       
}
