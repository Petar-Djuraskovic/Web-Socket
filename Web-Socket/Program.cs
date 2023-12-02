using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Program
{
    private static TcpListener server = new TcpListener(IPAddress.Any, 48163);

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
                Console.WriteLine("Invalid role. Please enter 'server' or 'client'.");
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
        Console.WriteLine("////////// role : server //////////");
        Console.WriteLine("Server IP addresses:");
        foreach (IPAddress ipAddress in localIPs)
        {
            if (ipAddress.AddressFamily == AddressFamily.InterNetwork) // Display only IPv4 addresses
            {
                Console.WriteLine($"  {ipAddress}");
            }
        }
        Console.WriteLine("Server running . . .");
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

        Console.WriteLine($"Client connected: {tcpClient.Client.RemoteEndPoint}");

        while (true)
        {
            bytesRead = 0;

            try
            {
                bytesRead = clientStream.Read(message, 0, 4096);
            }
            catch
            {
                break; 
            }

            if (bytesRead == 0)
                break;

            string clientMessage = Encoding.UTF8.GetString(message, 0, bytesRead);
            Console.WriteLine($"Received message from {tcpClient.Client.RemoteEndPoint}: {clientMessage}");
        }

        Console.WriteLine($"Client disconnected: {tcpClient.Client.RemoteEndPoint}");

        tcpClient.Close();
    }

    static void StartClient()
    {
        Console.WriteLine("////////// role : client /////////");
        Console.Write("Enter the server IP address: ");
        string? ipAddress = Console.ReadLine(); // Mark as nullable if needed

        if (ipAddress != null) // Check for null before dereferencing
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
    }
}
