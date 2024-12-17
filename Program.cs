using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace RIS
{
    class Program
    {
        static async Task Main(string[] args)
        {
            [DllImport("kernel32.dll")]
            static extern bool AllocConsole();

            AllocConsole();

            AppDomain.CurrentDomain.ProcessExit += async (sender, e) => await WebSocketServer.StopAsync();
            Console.CancelKeyPress += async (sender, e) =>
            {
                e.Cancel = true; 
                await WebSocketServer.StopAsync();
            };

            try
            {
                WebSocketServer.Start("http://localhost:8080/");
                Console.WriteLine("Сервер запущен. Нажмите Ctrl+C для выхода...\n");

                await Task.Delay(-1);
            }
            catch (OperationCanceledException)
            {
                // Игнорируем, если задача была отменена
            }
            catch (Exception ex)
            {
                ReportException(ex);
            }
        }

        public static void ReportException(Exception ex, [CallerMemberName] string location = "(Имя вызывающего метода не установлено)")
        {
            Console.WriteLine($"\n{location}:\n  Исключение {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null)
                Console.WriteLine($"  Внутреннее исключение {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
        }
    }
}
