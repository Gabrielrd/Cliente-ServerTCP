using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Program
{
    //Visa garantir acesso exclusivo aos objetos criados
    static readonly object _lock = new object();
    //Dicionario para armazenar dados de conexao dos usuarios
    static readonly Dictionary<int, TcpClient> list_clients = new Dictionary<int, TcpClient>();
    //Dicionario para armazenar dados de entrada e facilitar manipulação
    static readonly Dictionary<int, string> list_usuarios = new Dictionary<int,string>();
   

    //Função princiapl que tem objetivo de receber e tratar pedido de conexão
    static void Main(string[] args)
    {
        int count = 1;

        TcpListener ServerSocket = new TcpListener(IPAddress.Any, 5000);
        ServerSocket.Start();

        //Servidor fica escutando a porta configurada até que o mesmo seja desligado
        while (true)
        {
            Console.WriteLine("Waiting for a connection...");
            TcpClient client = ServerSocket.AcceptTcpClient();
            lock (_lock) list_clients.Add(count, client);
            Console.WriteLine("Someone connected!!");
            //Uma nova thread e criada para toda conexao
            Thread t = new Thread(atenderUsuarios);
            t.Start(count);
            count++;
        }
    }
    //Função visa tratar todas as condições proposta no exercicio
    public static void atenderUsuarios(object o)
    {
        int id = (int)o;
        TcpClient client;
        string mensagem;
        int valida = 0;
        int id_busca;

        //Garantindo excluvisidade para recurso/conexão
        lock (_lock) client = list_clients[id];
        


        try
        {
            //Enquanto ouver envio o sistema opera
            while (true)
            {
                //Leitura dos dados enviados pelo cliente
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int byte_count = stream.Read(buffer, 0, buffer.Length);

                //Verifica se tem alguma entrada
                if (byte_count == 0)
                {
                    break;
                }

                string data = Encoding.ASCII.GetString(buffer, 0, byte_count);
                //Variavel utilizada para realizar primeira verificacao
                string verificaData = data.Substring(1);
                

                //Verificar se é o primeiro acesso do usuário
                if (data.Contains("0") && valida == 0)
                {
                    //Verifica se conta já existe e alerta o usuario
                    if (list_usuarios.ContainsValue(verificaData))
                    {
                        mensagem = "Nickname em uso, reinicie seu acesso";
                        envia(stream, mensagem);
                        remove(client, id);

                    }
                    //Adiciona novo usuario na sala
                    else
                    {
                        list_usuarios.Add(id,verificaData);
                        mensagem = "Bem vindo " + verificaData;
                        envia(stream, mensagem);
                        valida = 1;
                    }

                }
                //Mostrar os comandos disponiveis para usuario
                else if( data == "comandos")
                {
                    int i = 0;
                    string[] comandos = { "Para sair: quit", "Para listar usuarios conectados: lista" };
                    while (i<=2)
                    {
                        mensagem = comandos[i];
                        envia(stream, mensagem);
                        i++;
                    }
                }
                //Envia para todos os participantes da sala
                else if (data == "quit")
                {
                    //Desconecta usuário se digitar Quit
                    mensagem = "Voce saiu do chat";
                    list_usuarios.Remove(id);
                    envia(stream, mensagem);
                    remove(client, id);
                }
                //Lista os usuario conectados
                else if (data == "lista")
                {
                    mensagem = "Usuarios conectados:";
                    envia(stream, mensagem);
                    foreach (var (key, value) in list_usuarios)                       
                    {
                        mensagem = "Id: " + key + " Nome: "+value;
                        envia(stream, mensagem);
                    }
                    mensagem = "Para enviar msg coloque o id na frente ou 0 para geral!";
                    envia(stream, mensagem);
                }
                //Envia mensagem para todos
                else if (data != "")
                {
                    id_busca = Convert.ToInt32(data.Substring(0, 1));
                    if(id_busca == 0)
                    {
                        broadcast(data);
                    }
                    else if (list_usuarios.ContainsKey(id_busca))
                    {
                        foreach (var (key, value) in list_usuarios)
                        {
                            if (key == id_busca)
                            {
                                data = " Disse para " + value + data.Substring(2);
                                broadcast(data);
                            }
                        }
                    }
                   
                    //Código abaixo visava enviar mensagem somente para a pessoa
                    /*int id_busca = Convert.ToInt32(data.Substring(0,1));
                    if (list_usuarios.ContainsKey(id_busca))
                    {
                        Console.WriteLine("aqui");
                        foreach (var (key, value) in list_clients)
                        {
                            if(key == id_busca)
                            {
                                TcpClient cli3 = value;
                                NetworkStream stream3 = cli3.GetStream();
                                Byte[] reply = System.Text.Encoding.ASCII.GetBytes(data);
                                stream3.Write(reply, 0, reply.Length);
                                stream3.Flush();
                            }
                        } 
                    }
                    else
                    {                        
                        
                    }*/
                }

            }
            remove(client, id);
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception: {0}", e.ToString());
            client.Close();
        }
    }

    //Envie mensagem somente a thread em execução
    private static void envia(NetworkStream stream, string mensagem)
    {
        Byte[] reply = System.Text.Encoding.ASCII.GetBytes(mensagem + Environment.NewLine);
        stream.Write(reply, 0, reply.Length);
    }

    //Envia mensagem para todos os usuários conectados
    public static void broadcast(string data)
    {
        byte[] buffer = Encoding.ASCII.GetBytes(data + Environment.NewLine);

        lock (_lock)
        {
            foreach (TcpClient c in list_clients.Values)
            {
                NetworkStream stream = c.GetStream();

                stream.Write(buffer, 0, buffer.Length);
            }
        }
    }

    //Fecha as conexões e remove id de usuário da lista de clientes
    public static void remove(TcpClient client,int id)
    {
        lock (_lock) list_clients.Remove(id);
        client.Client.Shutdown(SocketShutdown.Both);
        client.Close();
    }
}