using System;
using System.IO;
using System.Windows;
using Newtonsoft.Json;
using PdfRedactionApp.Config;
using PdfRedactionApp.Models;
using PdfRedactionApp.Services;
using PdfRedactionApp.ViewModels;

namespace PdfRedactionApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private RedactionConfig _config;
        private PdfService _pdfService;
        private AIService _aiService;

        /// <summary>
        /// 应用程序启动事件
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                LoggingService.Log("App.OnStartup 开始");
                System.Console.WriteLine("App.OnStartup 开始");
                base.OnStartup(e);
                LoggingService.Log("base.OnStartup 完成");
                System.Console.WriteLine("base.OnStartup 完成");
                
                // 加载配置
                LoggingService.Log("正在加载配置...");
                System.Console.WriteLine("正在加载配置...");
                LoadConfiguration();
                LoggingService.Log("配置加载完成");
                System.Console.WriteLine("配置加载完成");
                
                // 初始化服务
                LoggingService.Log("正在初始化服务...");
                System.Console.WriteLine("正在初始化服务...");
                InitializeServices();
                LoggingService.Log("服务初始化完成");
                System.Console.WriteLine("服务初始化完成");
                
                // 创建主窗口
                LoggingService.Log("正在创建主窗口...");
                System.Console.WriteLine("正在创建主窗口...");
                var mainWindow = new MainWindow();
                LoggingService.Log("主窗口创建完成");
                System.Console.WriteLine("主窗口创建完成");
                mainWindow.Show();
                LoggingService.Log("主窗口已显示");
                System.Console.WriteLine("主窗口已显示");
                
                LoggingService.Log("App.OnStartup 完成");
                System.Console.WriteLine("App.OnStartup 完成");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "App.OnStartup 发生异常");
                System.Console.WriteLine($"App.OnStartup 发生异常: {ex}");
                System.Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                System.Windows.MessageBox.Show($"App.OnStartup 发生异常: {ex.Message}", "错误");
            }
        }

        /// <summary>
        /// 加载配置文件
        /// </summary>
        private void LoadConfiguration()
        {
            LoggingService.Log("LoadConfiguration 开始");
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "redaction.config");
            LoggingService.Log($"配置文件路径: {configPath}");
            _config = RedactionConfig.Load(configPath);
            LoggingService.Log("LoadConfiguration 完成");
        }

        /// <summary>
        /// 初始化服务
        /// </summary>
        private void InitializeServices()
        {
            LoggingService.Log("InitializeServices 开始");
            _pdfService = new PdfService();
            
            // 添加空值检查以防止配置为null时出错
            string apiKey = _config?.DeepSeekApiKey ?? "";
            string apiEndpoint = _config?.DeepSeekApiEndpoint ?? "https://api.deepseek.com/v1/chat/completions";
            _aiService = new AIService(apiKey, apiEndpoint);
            
            LoggingService.Log("InitializeServices 完成");
        }

        /// <summary>
        /// 获取配置对象
        /// </summary>
        public RedactionConfig GetConfiguration() => _config;

        /// <summary>
        /// 获取PDF服务实例
        /// </summary>
        public PdfService GetPdfService() => _pdfService;

        /// <summary>
        /// 获取AI服务实例
        /// </summary>
        public AIService GetAIService() => _aiService;
    }
}

