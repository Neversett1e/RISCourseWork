using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace RIS
{
    public class GradientComparator
    {
        private double[,] ReferenceGradients;
        private Size ReferenceSize;

        /// <summary>
        /// Загружает эталонное изображение и вычисляет его ориентированные градиенты.
        /// </summary>
        /// <param name="referenceImage">Эталонное изображение.</param>
        public void LoadReferenceImage(Bitmap referenceImage)
        {
            ReferenceSize = referenceImage.Size;
            ReferenceGradients = CalculateGradientOrientations(referenceImage);
        }

        /// <summary>
        /// Сравнивает эталонное изображение с другим изображением.
        /// </summary>
        /// <param name="targetImage">Изображение для сравнения.</param>
        /// <returns>Сходство между изображениями (от 0 до 1).</returns>
        public double CompareWithTargetImage(Bitmap targetImage)
        {
            // Изменение размера изображения, если необходимо
            if (targetImage.Size != ReferenceSize)
            {
                targetImage = ResizeImage(targetImage, ReferenceSize);
            }

            double[,] targetGradients = null;

            var gradientTask = Task.Run(() =>
            {
                targetGradients = CalculateGradientOrientations(targetImage);
            });

            gradientTask.Wait(); 

            return CompareGradients(ReferenceGradients, targetGradients);
        }

        /// <summary>
        /// Изменяет размер изображения до заданных размеров.
        /// </summary>
        /// <param name="image">Исходное изображение.</param>
        /// <param name="newSize">Новый размер.</param>
        /// <returns>Масштабированное изображение.</returns>
        private Bitmap ResizeImage(Bitmap image, Size newSize)
        {
            var resizedImage = new Bitmap(newSize.Width, newSize.Height);
            using (var graphics = Graphics.FromImage(resizedImage))
            {
                graphics.DrawImage(image, 0, 0, newSize.Width, newSize.Height);
            }
            return resizedImage;
        }

        /// <summary>
        /// Вычисляет ориентированные градиенты изображения.
        /// </summary>
        /// <param name="image">Изображение.</param>
        /// <returns>Массив с углами градиентов (в радианах).</returns>
        private double[,] CalculateGradientOrientations(Bitmap image)
        {
            int width = image.Width;
            int height = image.Height;
            var gradients = new double[width, height];

            // Преобразование в градации серого
            var grayImage = ConvertToGrayscale(image);

            Parallel.For(1, height - 1, y =>
            {
                /*Console.WriteLine($"Задача для строки {y} выполняется на потоке {Thread.CurrentThread.ManagedThreadId}");*/
                for (int x = 1; x < width - 1; x++)
                {
                    // Собель X
                    int gx = (-1 * grayImage[x - 1, y - 1] + 1 * grayImage[x + 1, y - 1]) +
                             (-2 * grayImage[x - 1, y] + 2 * grayImage[x + 1, y]) +
                             (-1 * grayImage[x - 1, y + 1] + 1 * grayImage[x + 1, y + 1]);

                    // Собель Y
                    int gy = (-1 * grayImage[x - 1, y - 1] + -2 * grayImage[x, y - 1] + -1 * grayImage[x + 1, y - 1]) +
                             (1 * grayImage[x - 1, y + 1] + 2 * grayImage[x, y + 1] + 1 * grayImage[x + 1, y + 1]);

                    // Угол ориентации градиента
                    gradients[x, y] = Math.Atan2(gy, gx); // Угол в радианах
                }
            });

            /*Thread.Sleep(3000);
            Console.WriteLine();*/

            return gradients;
        }

        /// <summary>
        /// Сравнивает массивы градиентов двух изображений.
        /// </summary>
        /// <param name="gradients1">Градиенты первого изображения.</param>
        /// <param name="gradients2">Градиенты второго изображения.</param>
        /// <returns>Сходство между изображениями (от 0 до 1).</returns>
        private double CompareGradients(double[,] gradients1, double[,] gradients2)
        {
            int width = gradients1.GetLength(0);
            int height = gradients1.GetLength(1);

            double totalDifference = 0.0;
            int totalPixels = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    totalDifference += Math.Abs(gradients1[x, y] - gradients2[x, y]);
                    totalPixels++;
                }
            }

            // Нормализация разницы
            double averageDifference = totalDifference / totalPixels;
            return Math.Max(0, 1 - averageDifference / Math.PI); // Сходство от 0 до 1
        }

        /// <summary>
        /// Преобразует изображение в градации серого.
        /// </summary>
        /// <param name="image">Изображение.</param>
        /// <returns>Массив пикселей в градациях серого.</returns>
        private int[,] ConvertToGrayscale(Bitmap image)
        {
            int width = image.Width;
            int height = image.Height;
            var grayPixels = new int[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color color = image.GetPixel(x, y);
                    int gray = (int)(0.299 * color.R + 0.587 * color.G + 0.114 * color.B);
                    grayPixels[x, y] = gray;
                }
            }

            return grayPixels;
        }
    }
}
