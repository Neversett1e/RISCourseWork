using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RIS
{
    public class ConnectedClient
    {
        public ConnectedClient(int socketId, WebSocket socket)
        {
            SocketId = socketId;
            Socket = socket;
        }

        public int SocketId { get; private set; }
        public WebSocket Socket { get; private set; }
        private Bitmap FirstImage { get; set; }
        private Bitmap SecondImage { get; set; }

        public bool IsFirstImageSet => FirstImage != null;
        public bool IsSecondImageSet => SecondImage != null;

        /// <summary>
        /// Добавляет изображение в контекст клиента.
        /// </summary>
        public void AddImage(byte[] imageData)
        {
            using (var ms = new MemoryStream(imageData))
            {
                var image = new Bitmap(ms);

                if (FirstImage == null)
                {
                    FirstImage = image;
                    WebSocketServer.Log($"Сокет {SocketId}: Первое изображение успешно получено.");
                }
                else if (SecondImage == null)
                {
                    SecondImage = image;
                    WebSocketServer.Log($"Сокет {SocketId}: Второе изображение успешно получено.");
                }
                else
                {
                    throw new InvalidOperationException($"Сокет {SocketId}: Оба изображения уже установлены.");
                }
            }
        }

        /// <summary>
        /// Выполняет сравнение изображений и возвращает результат.
        /// </summary>
        public string CompareImages(GradientComparator comparator)
        {
            if (FirstImage == null || SecondImage == null)
                throw new InvalidOperationException($"Сокет {SocketId}: Не хватает изображений для сравнения.");

            comparator.LoadReferenceImage(FirstImage);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            double similarity = comparator.CompareWithTargetImage(SecondImage);
            stopwatch.Stop();

            WebSocketServer.Log($"Сокет {SocketId}: Время, затраченное на сравнение изображений: {stopwatch.ElapsedMilliseconds} мс");

            FirstImage.Dispose();
            SecondImage.Dispose();
            FirstImage = null;
            SecondImage = null;

            return $"Сокет {SocketId}: Сходство между изображениями: {similarity * 100:F1}%";
        }

    }
}