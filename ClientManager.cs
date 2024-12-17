using RIS;
using System.Collections.Concurrent;

public class ClientManager
{
    private ConcurrentDictionary<int, ConnectedClient> _clients = new ConcurrentDictionary<int, ConnectedClient>();

    public void AddClient(ConnectedClient client)
    {
        _clients.TryAdd(client.SocketId, client);
        WebSocketServer.Log($"Клиент {client.SocketId} добавлен.");
    }

    public void RemoveClient(int socketId)
    {
        if (_clients.TryRemove(socketId, out var removedClient))
        {
            WebSocketServer.Log($"Клиент {socketId} удалён.");
            removedClient.Socket.Dispose();
        }
    }

    public IEnumerable<ConnectedClient> GetAllClients()
    {
        return _clients.Values;
    }
}