using System;
using System.IO;

namespace PdfRedactionApp.Services
{
    public static class LoggingService
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");

        public static void Log(string message)
        {
            try
            {
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
                File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // 如果日志记录失败，至少在控制台输出错误
                Console.WriteLine($"日志记录失败: {ex.Message}");
            }
        }

        public static void LogError(Exception ex, string additionalMessage = "")
        {
            try
            {
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - ERROR: {additionalMessage} - {ex}";
                File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
                
                // 同时输出到控制台
                Console.WriteLine(logEntry);
            }
            catch (Exception logEx)
            {
                Console.WriteLine($"日志记录失败: {logEx.Message}");
            }
        }
    }
}