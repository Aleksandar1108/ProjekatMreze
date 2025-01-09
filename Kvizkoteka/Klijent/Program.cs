using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Klijent
{
    internal class Program
    {
        static void Main(string[] args)
        {
            UdpClient udpClient = new UdpClient();
            IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000);

            // Unos imena i igara sa tastature
            Console.Write("Unesite ime/nadimak: ");
            string ime = Console.ReadLine();

            Console.Write("Unesite igre koje želite da igrate (odvojene zarezima): ");
            string igre = Console.ReadLine();

            // Formiranje prijave
            string prijava = $"PRIJAVA: {ime}, {igre}";
            byte[] sendData = Encoding.UTF8.GetBytes(prijava);
            udpClient.Send(sendData, sendData.Length, serverEndpoint);

            byte[] receiveData = udpClient.Receive(ref serverEndpoint);
            string response = Encoding.UTF8.GetString(receiveData);
            Console.WriteLine("Odgovor servera: " + response);

            // Uspostavljanje TCP veze
            if (response.StartsWith("TCP INFO:"))
            {
                string[] tcpInfo = response.Substring(10).Split(':');
                string ip = tcpInfo[0];
                int port = int.Parse(tcpInfo[1]);

                TcpClient tcpClient = new TcpClient(ip, port);
                NetworkStream stream = tcpClient.GetStream();

                // Slanje ID-a igrača
                Console.Write("Unesite vaš ID (broj koji vam je dodeljen od servera): ");
                string playerId = Console.ReadLine();  // Unos ID-a igrača
                byte[] idData = Encoding.UTF8.GetBytes(playerId);
                stream.Write(idData, 0, idData.Length);

                // Čitanje poruke od servera
                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string serverMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine("Poruka servera: " + serverMessage);

                tcpClient.Close();
            }

        }
    }
       
}
