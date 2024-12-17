using RIS;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

public class MessageHandler
{
    private ConcurrentQueue<(ConnectedClient Client, byte[] MessageBuffer)> _messageQueue = new ConcurrentQueue<(ConnectedClient, byte[])>();
    private SemaphoreSlim _queueSemaphore = new SemaphoreSlim(0);
    private SemaphoreSlim _processingSemaphore = new SemaphoreSlim(500);

    public MessageHandler()
    {
        Task.Run(ProcessMessageQueueAsync);
    }

    public void EnqueueMessage(ConnectedClient client, byte[] messageBuffer)
    {
        if (!_processingSemaphore.Wait(0))
        {
            _ = client.Socket.SendAsync(Encoding.UTF8.GetBytes("Сервер перегружен. Попробуйте позже."), WebSocketMessageType.Text, true, CancellationToken.None);

            WebSocketServer.Log($"Клиент {client.SocketId}: сообщение отклонено из-за перегрузки сервера.");
            return;
        }

        try
        {
            _messageQueue.Enqueue((client, messageBuffer));
            _queueSemaphore.Release();
        }
        catch
        {
            _processingSemaphore.Release();
            throw;
        }
    }

    private async Task ProcessMessageQueueAsync()
    {
        while (true)
        {
            await _queueSemaphore.WaitAsync();
            if (_messageQueue.TryDequeue(out var item))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessMessageAsync(item.Client, item.MessageBuffer);
                    }
                    finally
                    {
                        _processingSemaphore.Release(); 
                    }
                });
            }
        }
    }

    private async Task ProcessMessageAsync(ConnectedClient client, byte[] messageBuffer)
    {
        try
        {
            client.AddImage(messageBuffer);

            if (client.IsFirstImageSet && client.IsSecondImageSet)
            {
                var resultMessage = client.CompareImages(new GradientComparator());
                await client.Socket.SendAsync(Encoding.UTF8.GetBytes(resultMessage), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            WebSocketServer.Log($"Ошибка обработки изображения для клиента {client.SocketId}: {ex.Message}");
        }
    }
}
