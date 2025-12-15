using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PdfRedactionApp.ViewModels;
using PdfRedactionApp.Config;
using PdfRedactionApp.Services;
using PdfRedactionApp.Models;
using PdfiumViewer;
using System.IO;
using System;

namespace PdfRedactionApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private MainViewModel viewModel;
    private PdfViewer pdfOriginalViewer;
    private PdfViewer pdfRedactedViewer;
    
    public MainWindow()
    {
        LoggingService.Log("MainWindow 构造函数开始");
        System.Console.WriteLine("MainWindow 构造函数开始");
        InitializeComponent();
        LoggingService.Log("InitializeComponent 完成");
        viewModel = new MainViewModel();
        this.DataContext = viewModel;
        LoggingService.Log("DataContext 设置完成");
        
        // 初始化配置面板
        InitializeConfigPanel();
        LoggingService.Log("配置面板初始化完成");
        
        // 显示一个消息框确认应用程序启动
        System.Console.WriteLine("主窗口已初始化完成");
        LoggingService.Log("主窗口已初始化完成");
        // System.Windows.MessageBox.Show("主窗口已初始化完成", "调试信息"); // 注释掉这行以避免阻塞应用程序启动
        
        // 初始化PdfiumViewer控件
        InitializePdfViewers();
    }
    
    
    
    
    
    /// <summary>
    /// 初始化PdfiumViewer控件
    /// </summary>
    private void InitializePdfViewers()
    {
        try
        {
            System.Console.WriteLine("开始初始化PdfiumViewer控件");
            
            // 检查容器是否已初始化
            if (pdfOriginalContainer == null)
            {
                System.Console.WriteLine("pdfOriginalContainer为null");
                return;
            }
            
            if (pdfRedactedContainer == null)
            {
                System.Console.WriteLine("pdfRedactedContainer为null");
                return;
            }
            
            // 初始化原始PDF查看器
            System.Console.WriteLine("创建pdfOriginalViewer实例");
            pdfOriginalViewer = new PdfViewer();
            System.Console.WriteLine("设置pdfOriginalViewer.Dock");
            pdfOriginalViewer.Dock = System.Windows.Forms.DockStyle.Fill;
            
            // 使用WindowsFormsHost包装Windows Forms控件
            System.Console.WriteLine("创建windowsFormsHostOriginal实例");
            var windowsFormsHostOriginal = new System.Windows.Forms.Integration.WindowsFormsHost();
            System.Console.WriteLine("设置windowsFormsHostOriginal.Child");
            windowsFormsHostOriginal.Child = pdfOriginalViewer;
            
            // 清空容器并添加查看器
            System.Console.WriteLine("清空pdfOriginalContainer");
            pdfOriginalContainer.Children.Clear();
            System.Console.WriteLine("添加windowsFormsHostOriginal到pdfOriginalContainer");
            pdfOriginalContainer.Children.Add(windowsFormsHostOriginal);
            
            // 初始化脱敏后PDF查看器
            System.Console.WriteLine("创建pdfRedactedViewer实例");
            pdfRedactedViewer = new PdfViewer();
            System.Console.WriteLine("设置pdfRedactedViewer.Dock");
            pdfRedactedViewer.Dock = System.Windows.Forms.DockStyle.Fill;
            
            // 使用WindowsFormsHost包装Windows Forms控件
            System.Console.WriteLine("创建windowsFormsHostRedacted实例");
            var windowsFormsHostRedacted = new System.Windows.Forms.Integration.WindowsFormsHost();
            System.Console.WriteLine("设置windowsFormsHostRedacted.Child");
            windowsFormsHostRedacted.Child = pdfRedactedViewer;
            
            // 清空容器并添加查看器
            System.Console.WriteLine("清空pdfRedactedContainer");
            pdfRedactedContainer.Children.Clear();
            System.Console.WriteLine("添加windowsFormsHostRedacted到pdfRedactedContainer");
            pdfRedactedContainer.Children.Add(windowsFormsHostRedacted);
            
            System.Console.WriteLine("PdfiumViewer控件初始化完成");
            LoggingService.Log("PdfiumViewer控件初始化完成");
        }
        catch (DllNotFoundException ex)
        {
            System.Console.WriteLine($"初始化PdfiumViewer控件时出错 - DllNotFoundException: {ex.Message}");
            System.Console.WriteLine($"StackTrace: {ex.StackTrace}");
            LoggingService.Log($"初始化PdfiumViewer控件时出错 - DllNotFoundException: {ex.Message}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"初始化PdfiumViewer控件时出错: {ex.Message}");
            System.Console.WriteLine($"Exception Type: {ex.GetType().FullName}");
            System.Console.WriteLine($"StackTrace: {ex.StackTrace}");
            LoggingService.Log($"初始化PdfiumViewer控件时出错: {ex.Message}");
            LoggingService.Log($"Exception Type: {ex.GetType().FullName}");
            LoggingService.Log($"StackTrace: {ex.StackTrace}");
        }
    }
    
    /// <summary>
        /// 初始化配置面板
        /// </summary>
        private void InitializeConfigPanel()
        {
            // 获取当前应用程序实例的配置
            var app = (App)System.Windows.Application.Current;
            var config = app.GetConfiguration();
            
            // 添加空值检查
            if (config == null || config.Rules == null)
            {
                LoggingService.Log("警告: 配置或规则集合为null，使用默认设置");
                // 设置默认值
                chkName.IsChecked = true;
                chkIdCard.IsChecked = true;
                chkPhone.IsChecked = true;
                chkAddress.IsChecked = true;
                return;
            }
            
            // 设置复选框的初始状态
            chkName.IsChecked = config.Rules.Find(r => r.Type == Models.SensitiveInfoType.Name)?.IsEnabled ?? true;
            chkIdCard.IsChecked = config.Rules.Find(r => r.Type == Models.SensitiveInfoType.IdCard)?.IsEnabled ?? true;
            chkPhone.IsChecked = config.Rules.Find(r => r.Type == Models.SensitiveInfoType.PhoneNumber)?.IsEnabled ?? true;
            chkAddress.IsChecked = config.Rules.Find(r => r.Type == Models.SensitiveInfoType.Address)?.IsEnabled ?? true;
            
            // 加载自定义关键字
            var customRule = config.Rules.Find(r => r.Type == Models.SensitiveInfoType.Other);
            if (customRule != null && customRule.Keywords != null)
            {
                listKeywords.Items.Clear();
                foreach (var keyword in customRule.Keywords)
                {
                    listKeywords.Items.Add(keyword);
                }
            }
        }
    
    /// <summary>
        /// 打开PDF文件按钮点击事件
        /// </summary>
        private void BtnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "PDF文件|*.pdf";
            
            if (openFileDialog.ShowDialog() == true)
            {
                // 清理ViewModel中的所有相关属性
                viewModel.TempRedactedFilePath = string.Empty;
                viewModel.ProgressValue = 0;
                viewModel.StatusText = string.Empty;
                viewModel.SensitiveInfos = new List<SensitiveInfo>();
                viewModel.DisplaySensitiveInfos.Clear();
                viewModel.ManualRedactionAreas.Clear();
                
                viewModel.SelectedFilePath = openFileDialog.FileName;
                viewModel.StatusText = $"已选择文件: {viewModel.SelectedFilePath}";
                
                // 使用PdfiumViewer直接打开原始PDF
                try
                {
                    // 检查PDF查看器是否已初始化，如果未初始化则重新初始化
                    if (pdfOriginalViewer == null || pdfRedactedViewer == null)
                    {
                        InitializePdfViewers();
                    }
                    
                    // 再次检查以确保初始化成功
                    if (pdfOriginalViewer != null)
                    {
                        // 清理之前的脱敏结果
                        if (pdfRedactedViewer != null)
                        {
                            pdfRedactedViewer.Document = null;
                        }
                        
                        pdfOriginalViewer.Document = PdfiumViewer.PdfDocument.Load(viewModel.SelectedFilePath);
                        viewModel.StatusText = "PDF文件打开完成";
                    }
                    else
                    {
                        viewModel.StatusText = "PDF查看器初始化失败，无法打开文件";
                        System.Console.WriteLine("PDF查看器初始化失败，无法打开文件");
                    }
                }
                catch (Exception ex)
                {
                    viewModel.StatusText = $"打开PDF失败: {ex.Message}";
                    System.Console.WriteLine($"打开PDF失败: {ex.Message}");
                }
            }
        }
    
    /// <summary>
        /// 开始脱敏处理按钮点击事件
        /// </summary>
        private async void BtnProcess_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(viewModel.SelectedFilePath))
            {
                System.Windows.MessageBox.Show("请先选择一个PDF文件。", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            
            // 执行PDF脱敏处理
            await viewModel.ProcessPdfRedactionAsync();
            
            // 脱敏完成后，使用PdfiumViewer打开脱敏后的PDF
            if (!string.IsNullOrEmpty(viewModel.TempRedactedFilePath) && File.Exists(viewModel.TempRedactedFilePath))
            {
                try
                {
                    // 检查pdfRedactedViewer是否已初始化，如果未初始化则重新初始化
                    if (pdfRedactedViewer == null)
                    {
                        InitializePdfViewers();
                    }
                    
                    // 再次检查以确保初始化成功
                    if (pdfRedactedViewer != null)
                    {
                        pdfRedactedViewer.Document = PdfiumViewer.PdfDocument.Load(viewModel.TempRedactedFilePath);
                        viewModel.StatusText = "脱敏后的PDF已打开";
                    }
                    else
                    {
                        viewModel.StatusText = "PDF查看器初始化失败，无法打开脱敏后的文件";
                        System.Console.WriteLine("PDF查看器初始化失败，无法打开脱敏后的文件");
                    }
                }
                catch (Exception ex)
                {
                    viewModel.StatusText = $"打开脱敏PDF失败: {ex.Message}";
                    System.Console.WriteLine($"打开脱敏PDF失败: {ex.Message}");
                }
            }
        }
    
    /// <summary>
    /// 保存结果按钮点击事件
    /// </summary>
    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(viewModel.SelectedFilePath) || viewModel.ProgressValue < 100)
        {
            System.Windows.MessageBox.Show("请先进行脱敏处理。", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }
        
        Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
        saveFileDialog.Filter = "PDF文件|*.pdf";
        saveFileDialog.FileName = System.IO.Path.GetFileNameWithoutExtension(viewModel.SelectedFilePath) + "_脱敏版.pdf";
        
        if (saveFileDialog.ShowDialog() == true)
        {
            // 保存脱敏后的PDF
            await viewModel.SaveRedactedPdfAsync(saveFileDialog.FileName);
            viewModel.StatusText = $"文件已保存: {saveFileDialog.FileName}";
        }
    }
    
    /// <summary>
        /// 添加关键字按钮点击事件
        /// </summary>
        private void BtnAddKeyword_Click(object sender, RoutedEventArgs e)
        {
            string keyword = txtKeyword.Text.Trim();
            if (!string.IsNullOrEmpty(keyword) && !listKeywords.Items.Contains(keyword))
            {
                listKeywords.Items.Add(keyword);
                txtKeyword.Text = ""; // 清空输入框
                viewModel.StatusText = "关键字已添加";
            }
            else if (listKeywords.Items.Contains(keyword))
            {
                viewModel.StatusText = "该关键字已存在";
            }
            else
            {
                viewModel.StatusText = "请输入有效的关键字";
            }
        }
        
        /// <summary>
        /// 删除选中关键字按钮点击事件
        /// </summary>
        private void BtnRemoveKeyword_Click(object sender, RoutedEventArgs e)
        {
            if (listKeywords.SelectedItem != null)
            {
                listKeywords.Items.Remove(listKeywords.SelectedItem);
                viewModel.StatusText = "关键字已删除";
            }
            else
            {
                viewModel.StatusText = "请先选择要删除的关键字";
            }
        }
        
        /// <summary>
        /// 保存配置按钮点击事件
        /// </summary>
        private void BtnSaveConfig_Click(object sender, RoutedEventArgs e)
        {
            // 获取当前应用程序实例的配置和服务
            var app = (App)System.Windows.Application.Current;
            var config = app.GetConfiguration();
            
            // 更新配置
            var nameRule = config.Rules.Find(r => r.Type == Models.SensitiveInfoType.Name);
            if (nameRule != null)
                nameRule.IsEnabled = chkName.IsChecked ?? false;
                
            var idCardRule = config.Rules.Find(r => r.Type == Models.SensitiveInfoType.IdCard);
            if (idCardRule != null)
                idCardRule.IsEnabled = chkIdCard.IsChecked ?? false;
                
            var phoneRule = config.Rules.Find(r => r.Type == Models.SensitiveInfoType.PhoneNumber);
            if (phoneRule != null)
                phoneRule.IsEnabled = chkPhone.IsChecked ?? false;
                
            var addressRule = config.Rules.Find(r => r.Type == Models.SensitiveInfoType.Address);
            if (addressRule != null)
                addressRule.IsEnabled = chkAddress.IsChecked ?? false;
            
            // 保存自定义关键字到配置
            var customRule = config.Rules.Find(r => r.Type == Models.SensitiveInfoType.Other);
            if (customRule == null)
            {
                // 如果不存在自定义规则，创建一个新的
                customRule = new Config.RedactionRuleConfig
                {
                    Type = Models.SensitiveInfoType.Other,
                    Name = "自定义关键字脱敏",
                    IsEnabled = true
                };
                config.Rules.Add(customRule);
            }
            
            // 更新关键字列表
            customRule.Keywords.Clear();
            foreach (var item in listKeywords.Items)
            {
                customRule.Keywords.Add(item.ToString());
            }
            
            // 保存配置到文件
            config.Save(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "redaction.config"));
            
            // 更新视图模型中的规则
            viewModel.InitializeRules(config);
            
            viewModel.StatusText = "配置已保存";
        }
}