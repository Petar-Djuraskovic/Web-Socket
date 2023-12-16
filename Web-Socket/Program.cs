using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

public class User
{
    public string userName;
    public string password;
    public EndPoint? endPoint;
    public TcpClient? tcpClient = null;
    public bool connected = true;
    public DateTime lastConnected = DateTime.Now;
    public DateTime creationDate = DateTime.Now;
    public int messagesSent = 0;
    public float hoursConnected = 0;
}

public class Server
{
    public string serverName;
    public System.Net.IPEndPoint? endPoint;
    public System.Net.Sockets.TcpListener? tcpListener;
    public Tuple<int, int, int> softwareVersion = new Tuple<int, int, int>(1, 1, 0);
    public DateTime creationDate = DateTime.Now;
    public int? userCount;
    public int? connectedUserCount;
    public int? messageCount = 0;
    public float hoursOnline = 0;

    [JsonIgnore]
    public string? IPAddressString
    {
        get
        {
            return endPoint?.Address.ToString();
        }
    }

    [JsonProperty("IPAddressString")]
    private string? IPAddressStringForSerialization
    {
        get
        {
            return IPAddressString;
        }
        set
        {
            if (value != null)
            {
                System.Net.IPAddress ipAddress;
                if (System.Net.IPAddress.TryParse(value, out ipAddress))
                {
                    endPoint = new System.Net.IPEndPoint(ipAddress, endPoint?.Port ?? 0);
                }
            }
        }
    }
}


class Program
{
    public static string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Web-Socket");

    public static Tuple<int, int, int> softwareVersion = Tuple.Create(1, 1, 0);

    private static TcpListener Server = new TcpListener(IPAddress.Any, 48163);

    //private static List<TcpClient>? ClientList = new List<TcpClient>();

    // Define event args for server updates

    // Depricated event system

    /*public class ServerEventArgs : EventArgs
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

            // Method to trigger the ServerUpdate event
        static void OnServerUpdate(ServerEventArgs e)
        {
            ServerUpdate?.Invoke(null, e);
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
    }*/


    static void Main(string[] args)
    {
        if (!Directory.Exists(appDataFolder))
        {
            Directory.CreateDirectory(appDataFolder);
            Console.WriteLine($" | i | Created app data folder: {appDataFolder}");
            Console.WriteLine($" | ☺ | Welcome to Web-Socket, {Environment.UserName}! ");
        }

        Console.OutputEncoding = Encoding.UTF8;
        Console.Title = "////////Web-Socket/////////     | :3 |";

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

        Console.WriteLine("/////////֎ role : server ֎/////////");

        string serverDataFolder = Path.Combine(appDataFolder, "ServerData");
        if (!Directory.Exists(serverDataFolder))
        {
            ServerCreationWizard();
        }


        Thread listenerThread = new Thread(new ThreadStart(ListenForClients));
        listenerThread.Start();

        // Get local IP addresses of the machine
        IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());

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

    static void ServerCreationWizard(bool ServerDataFolderExists = false)
    {
        string serverDataFolder = Path.Combine(appDataFolder, "ServerData");
        Console.Write(" | ! | no server data found. Would you like to create one? (y/n): ");
        if (Console.ReadKey().Key == ConsoleKey.Y)
        {
            Console.WriteLine("\n");
            try
            {
                string? serverName;
                if (!ServerDataFolderExists)
                {
                    Directory.CreateDirectory(serverDataFolder);
                    Console.WriteLine($" | i | Created server data folder: {serverDataFolder}");
                }

                Console.WriteLine(" | i | Server creation wizard started. All settings can be changed later. WARNING: IF YOU HAVE SERVERDATA SAVE IT ELSEWHERE AND CLOSE THE WINDOW NOW.");

                Console.Write("What should your server be called? : ");
                serverName = Console.ReadLine();
                if (serverName == null || serverName == "") { throw new Exception("| № | dumbass, you entered nothing"); }

                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress localIpAddress = host.AddressList[0];
                IPEndPoint localEndPoint = new IPEndPoint(localIpAddress, 0);

                Server server = new Server
                {
                    serverName = serverName,
                    endPoint = localEndPoint,
                    tcpListener = Server
                };

                Console.WriteLine($"serverName : {serverName}");
                Console.WriteLine($"endPoint : {localEndPoint}");
                Console.WriteLine($"tcpListener : {localEndPoint}");
                Console.WriteLine($"softwareVersion : {Program.softwareVersion}");
                Console.WriteLine($"creationDate : {DateTime.Now}");

                string server1 = Path.Combine(serverDataFolder, $"{serverName}");
                string jsonFilePath = Path.Combine(server1, $"{serverName}.json");
                Directory.CreateDirectory(server1);
                Console.WriteLine($" | i | Created {serverName} data folder: {server1}");

                string json = "Default json string";
                string? ipAddressString = server.IPAddressString;

                try
                {
                    Console.WriteLine(" | i | Serializing server object . . .");

                    // Serialize the Server object to JSON
                    json = JsonConvert.SerializeObject(server, Formatting.Indented, new JsonSerializerSettings
                    {
                        // Use a custom converter for IPAddress serialization
                        Converters = { new IPAddressJsonConverter() }
                    });
                }
                catch (Exception ex) { Console.WriteLine($" | ! | Exception while serializing the server object to JSON : {ex.GetType().Name} - {ex.Message}"); }

                try
                {
                    Console.WriteLine(" | i | Saving JSON to file . . .");
                    JsonEncodingTools.SaveJsonToFile(json, jsonFilePath);
                }
                catch (Exception ex) { Console.WriteLine($" | ! | Exception while saving the json to a file: {ex.GetType().Name} - {ex.Message} (file path: {jsonFilePath})"); }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" | ! | Something went wrong :{ex.Message}, try again? (y/n)");
                if (Console.ReadKey().Key == ConsoleKey.Y) 
                {
                    if (Directory.Exists(serverDataFolder))
                    {
                        Directory.Delete(serverDataFolder, true);
                    }
                    ServerCreationWizard(); 
                }
                else
                {
                    Main(new string[] { });
                }
            }
        }
    }

    static void ListenForClients()
    {
        Server.Start();

        while (true)
        {
            TcpClient tcpClient = Server.AcceptTcpClient();
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

        //ClientList.Add(tcpClient);

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

        //ClientList.Remove(tcpClient);

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

            if (ipAddress != null || ipAddress == "") // Check for null before dereferencing
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
                Console.WriteLine(" | № | Dumbass, you entered nothing.");
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
            else
            {
                Main(new string[] { });
            }
        }
    }
}

public static class JsonEncodingTools
{
    public static string SerializeToJson<T>(T obj)
    {
        // Serialize the object to JSON
        return JsonConvert.SerializeObject(obj, Formatting.Indented);
    }

    public static T DeserializeFromJson<T>(string json)
    {
        // Deserialize JSON to the object
        return JsonConvert.DeserializeObject<T>(json);
    }

    public static void SaveJsonToFile(string json, string filePath)
    {
        // Save the JSON to a file
        File.WriteAllText(filePath, json);
    }

    public static string ReadJsonFromFile(string filePath)
    {
        // Read the JSON from a file
        return File.ReadAllText(filePath);
    }
}

public class IPAddressJsonConverter : JsonConverter<IPAddress>
{
    public override IPAddress ReadJson(JsonReader reader, Type objectType, IPAddress existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.String)
        {
            string ipAddressString = (string)reader.Value;
            return IPAddress.Parse(ipAddressString);
        }

        throw new JsonSerializationException($"Unexpected token type: {reader.TokenType}");
    }

    public override void WriteJson(JsonWriter writer, IPAddress value, JsonSerializer serializer)
    {
        if (value != null)
        {
            string ipAddressString = value.ToString();
            writer.WriteValue(ipAddressString);
        }
        else
        {
            writer.WriteNull();
        }
    }
}

/*
public static async Task SendUpdateToClients(string jsonMessage)
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
}
*/
