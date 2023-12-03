using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using Newtonsoft.Json;



class Program
{
    private static TcpListener server = new TcpListener(IPAddress.Any, 48163);

    private static List<TcpClient>? ClientList = new List<TcpClient>();

    // Define event args for server updates

    public class ServerEventArgs : EventArgs
    {
        public string UpdateMessage { get; set; }
        
        public ServerEventArgs(string updateMessage)
        {
            UpdateMessage = updateMessage;
        }
    }

    public class MessageUpdateArgs : EventArgs
    {
        public string UpdateMessage { get; set; }
        public TcpClient TcpClient { get; }
        public DateTime DateTime { get; }
        public string DisplayTime { get; }

        public MessageUpdateArgs(string message, TcpClient tcpClient, DateTime dateTime)
        {
            UpdateMessage = message;
            TcpClient = tcpClient;
            DateTime = dateTime;
            DisplayTime = dateTime.ToString("H:mm");
        }
    }

    // Define an event for server updates
    public static event EventHandler<ServerEventArgs>? ServerUpdate;
    public static event EventHandler<MessageUpdateArgs>? MessageUpdate;

    // Method to trigger the MessageUpdate event
    protected static async Task OnMessageUpdate(MessageUpdateArgs e)
    {
        MessageUpdate?.Invoke(null, e);

        // Convert MessageUpdateArgs to JSON
        string jsonMessage = JsonConvert.SerializeObject(e);

        // Send the JSON message over the WebSocket
        await SendUpdateToClients(jsonMessage);
    }


    private static async Task SendUpdateToClients(string jsonMessage)
    {
        foreach (var client in ClientList)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] data = Encoding.UTF8.GetBytes(jsonMessage);
                await stream.WriteAsync(data, 0, data.Length);
            }
            catch (SocketException ex)
            {
                Console.WriteLine($" | ! | SocketException while sending update to {client.Client.RemoteEndPoint}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($" | ! | Exception while sending update to {client.Client.RemoteEndPoint}: {ex.Message}");
            }
        }
    }


    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.Title = "////////Web-Socket/////////      :3";

        Console.Write("Enter 'server' or 'client': ");
        string? role = Console.ReadLine(); // Mark as nullable if needed

        if (role != null) // Check for null before dereferencing
        {
            if (role.ToLower() == "server")
            {
                StartServer();
            }
            else if (role.ToLower() == "client")
            {
                StartClient();
            }
            else
            {
                Console.WriteLine(" | № | Invalid role. Please enter 'server' or 'client'.");
                Main(args);
            }
        }
    }

    static void StartServer()
    {
        Thread listenerThread = new Thread(new ThreadStart(ListenForClients));
        listenerThread.Start();

        // Get local IP addresses of the machine
        IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());

        // Display the IP addresses in the console
        Console.WriteLine("/////////֎ role : server ֎/////////");
        Console.WriteLine("Server IP addresses:");
        foreach (IPAddress ipAddress in localIPs)
        {
            if (ipAddress.AddressFamily == AddressFamily.InterNetwork) // Display only IPv4 addresses
            {
                Console.WriteLine($"  {ipAddress}");
            }
        }

        Console.WriteLine("Server running . . .");

        // Simulate a server update and notify clients
        //OnServerUpdate(new ServerEventArgs("Server is now running."));

        Console.ReadLine();
    }

    static void ListenForClients()
    {
        server.Start();

        while (true)
        {
            TcpClient tcpClient = server.AcceptTcpClient();
            Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
            clientThread.Start(tcpClient);
        }
    }

    static void HandleClientComm(object clientObj)
    {
        TcpClient tcpClient = (TcpClient)clientObj;
        NetworkStream clientStream = tcpClient.GetStream();

        byte[] message = new byte[4096];
        int bytesRead;

        ClientList.Add(tcpClient);

        Console.WriteLine($" | ☺ | Client connected: {tcpClient.Client.RemoteEndPoint}");

        while (true)
        {
            bytesRead = 0;

            try
            {
                bytesRead = clientStream.Read(message, 0, 4096);
            }
            catch (SocketException ex)
            {
                Console.WriteLine($" | ! | SocketException while reading from {tcpClient.Client.RemoteEndPoint}: {ex.Message}");
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($" | ! | Exception while reading from {tcpClient.Client.RemoteEndPoint}: {ex.Message}");
                break;
            }

            if (bytesRead == 0)
                break;

            string clientMessage = Encoding.UTF8.GetString(message, 0, bytesRead);
            Console.WriteLine($"Received message from {tcpClient.Client.RemoteEndPoint}: {clientMessage}");
        }

        ClientList.Remove(tcpClient);

        Console.WriteLine($"Client disconnected: {tcpClient.Client.RemoteEndPoint}");

        // Simulate a server update and notify clients
        //OnServerUpdate(new ServerEventArgs($"Client {tcpClient.Client.RemoteEndPoint} disconnected."));

        tcpClient.Close();
    }

    static void StartClient()
    {
        Console.WriteLine("/////////֎ role : client ֎////////");

        TryConnectingToASpecifiedServer();

        static void TryConnectingToASpecifiedServer()
        {
            Console.Write("Enter the server IP address: (press enter to connect to self)");
            string? ipAddress = Console.ReadLine(); // Mark as nullable if needed

            if (ipAddress != null) // Check for null before dereferencing
            {

                TryConnectingAgain(ipAddress);

                static void TryConnectingAgain(string? ipAddress)
                {

                    try
                    {
                        TcpClient client = new TcpClient(ipAddress, 48163);
                        NetworkStream stream = client.GetStream();

                        Console.WriteLine("Connected to server.");

                        while (true)
                        {
                            Console.Write("Enter a message: ");
                            string? message = Console.ReadLine(); // Mark as nullable if needed

                            if (message != null) // Check for null before dereferencing
                            {
                                byte[] data = Encoding.UTF8.GetBytes(message);
                                stream.Write(data, 0, data.Length);
                            }
                        }
                    }
                    catch (SocketException ex)
                    {
                        Console.WriteLine($" | ! | SocketException while connecting to the server: {ex.Message}");

                        TryConnectingToASpecifiedServer();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($" | ! | Exception while connecting to the server: {ex.Message}");
                        TryConnectingToASpecifiedServer();
                    }
                }
            }
            else
            {
                Console.WriteLine("Invalid IP address.");
                TryConnectingToASpecifiedServer();
            }
        }

        static void AskoToTryConnectingAgain()
        {
            Console.WriteLine("try again? (y/n)");
            if (Console.ReadKey().Key == ConsoleKey.Y)
            {
                //tryconnectingagain();
            }
        }

        // Method to trigger the ServerUpdate event
        static void OnServerUpdate(ServerEventArgs e)
        {
            ServerUpdate?.Invoke(null, e);
        }
    }
}
