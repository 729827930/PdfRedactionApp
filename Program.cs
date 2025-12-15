using System;
using System.Windows;
using PdfRedactionApp.Services;

namespace PdfRedactionApp
{
    public partial class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                LoggingService.Log("应用程序启动开始...");
                Console.WriteLine("正在启动应用程序...");
                var app = new App();
                LoggingService.Log("已创建应用程序实例");
                Console.WriteLine("已创建应用程序实例");
                
                // 订阅应用程序事件以获取更多信息
                app.Startup += (sender, e) => {
                    LoggingService.Log("应用程序启动事件触发");
                    Console.WriteLine("应用程序启动事件触发");
                };
                app.Activated += (sender, e) => {
                    LoggingService.Log("应用程序激活事件触发");
                    Console.WriteLine("应用程序激活事件触发");
                };
                app.Deactivated += (sender, e) => {
                    LoggingService.Log("应用程序失活事件触发");
                    Console.WriteLine("应用程序失活事件触发");
                };
                app.Exit += (sender, e) => {
                    LoggingService.Log($"应用程序退出事件触发，退出代码: {e.ApplicationExitCode}");
                    Console.WriteLine($"应用程序退出事件触发，退出代码: {e.ApplicationExitCode}");
                };
                
                LoggingService.Log("正在运行应用程序...");
                Console.WriteLine("正在运行应用程序...");
                // 不再手动创建主窗口，让App.OnStartup来处理
                int result = app.Run();
                LoggingService.Log($"应用程序已退出，返回代码: {result}");
                Console.WriteLine($"应用程序已退出，返回代码: {result}");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "应用程序启动失败");
                Console.WriteLine($"应用程序启动失败: {ex}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                Console.ReadLine(); // 等待用户按键
            }
        }
    }
}