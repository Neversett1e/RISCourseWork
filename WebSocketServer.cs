using RIS;
using System.Linq;
using System.Net.WebSockets;
using System.Net;

public static class WebSocketServer
{
    private static HttpListener Listener;

    private static bool ServerIsRunning = true;

    private static ClientManager ClientManager = new ClientManager();

    private static MessageHandler MessageHandler = new MessageHandler();

    private static ServerConfig Config = new ServerConfig();

    private static int SocketCounter = 0;

    private static readonly DateTime StartTime = DateTime.UtcNow;

    public static void Start(string uriPrefix)
    {
        Config.UriPrefix = uriPrefix;
        Listener = new HttpListener();
        Listener.Prefixes.Add(Config.UriPrefix);
        Listener.Start();

        if (Listener.IsListening)
        {
            Log($"Сервер запущен: {Config.UriPrefix}");
            Task.Run(() => ListenerProcessingLoopAsync());
        }
        else
        {
            Log("Не удалось запустить сервер.");
        }
    }

    private static async Task ListenerProcessingLoopAsync()
    {
        while (ServerIsRunning)
        {
            var context = await Listener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
            {
                var wsContext = await context.AcceptWebSocketAsync(subProtocol: null);
                var client = new ConnectedClient(Interlocked.Increment(ref SocketCounter), wsContext.WebSocket);
                ClientManager.AddClient(client);
                _ = Task.Run(() => SocketProcessingLoopAsync(client));
            }
        }
    }

    private static async Task SocketProcessingLoopAsync(ConnectedClient client)
    {
        var buffer = new byte[Config.BufferSize];
        var messageBuffer = new List<byte>();

        try
        {
            while (client.Socket.State == WebSocketState.Open)
            {
                var result = await client.Socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    ClientManager.RemoveClient(client.SocketId);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    messageBuffer.AddRange(buffer.Take(result.Count));
                    if (result.EndOfMessage)
                    {
                        MessageHandler.EnqueueMessage(client, messageBuffer.ToArray());
                        messageBuffer.Clear();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Сокет {client.SocketId}: Ошибка - {ex.Message}");
        }
    }

    public static async Task StopAsync()
    {
        ServerIsRunning = false;
        Listener.Stop();
        foreach (var client in ClientManager.GetAllClients())
        {
            await client.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Сервер завершает работу", CancellationToken.None);
        }
    }

    /// <summary>
    /// Унифицированный вывод сообщений в консоль с отметкой времени.
    /// </summary>
    /// <param name="message">Сообщение для вывода.</param>
    public static void Log(string message)
    {
        var elapsedTime = DateTime.UtcNow - StartTime;
        Console.WriteLine($"[{elapsedTime:hh\\:mm\\:ss}] {message}");
    }
}