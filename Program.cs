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
                Console.WriteLine("������ �������. ������� Ctrl+C ��� ������...\n");

                await Task.Delay(-1);
            }
            catch (OperationCanceledException)
            {
                // ����������, ���� ������ ���� ��������
            }
            catch (Exception ex)
            {
                ReportException(ex);
            }
        }

        public static void ReportException(Exception ex, [CallerMemberName] string location = "(��� ����������� ������ �� �����������)")
        {
            Console.WriteLine($"\n{location}:\n  ���������� {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null)
                Console.WriteLine($"  ���������� ���������� {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
        }
    }
}
