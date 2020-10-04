using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace cli1
{
    class Program
    {
        static void Main(string[] args)
        {
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            int port = 5000;
            TcpClient client = new TcpClient();
            client.Connect(ip, port);
            Console.WriteLine("client connected!!");
            NetworkStream ns = client.GetStream();
            Thread thread = new Thread(o => ReceiveData((TcpClient)o));

            thread.Start(client);

            string sinal = "0";
            string s;

            Menu();
            while (!string.IsNullOrEmpty(s = Console.ReadLine()))
            {
                //Envio um sinal para identificar primeiro acesso
                if (sinal == "0")
                {
                    EnviarData(ns, sinal + s);
                    sinal = "1";
                }
                //Caso não seja primeiro acesso mensagens enviadas normalmente
                else
                {
                    EnviarData(ns, s);
                }
            }

            client.Client.Shutdown(SocketShutdown.Send);
            thread.Join();
            ns.Close();
            client.Close();
            Console.WriteLine("disconnect from server!!");
            Console.ReadKey();
        }

        static void ReceiveData(TcpClient client)
        {
            NetworkStream ns = client.GetStream();
            byte[] receivedBytes = new byte[1024];
            int byte_count;

            while ((byte_count = ns.Read(receivedBytes, 0, receivedBytes.Length)) > 0)
            {
                Console.Write(Encoding.ASCII.GetString(receivedBytes, 0, byte_count));
            }
        }

        static void EnviarData(NetworkStream ns, string msg)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(msg);
            ns.Write(buffer, 0, buffer.Length);
        }

        static void Menu()
        {
            Console.WriteLine("Bem vindo ao Chat 1.0");
            Console.WriteLine("Digite comandos para ver as opcoes:");
            Console.WriteLine("Digite o seu nickname e antes de enviar mensagem colque 0:");
        }
    }
}
