using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using PdfRedactionApp.Models;
using PdfRedactionApp.Services;
using PdfRedactionApp.Config;

namespace PdfRedactionApp.ViewModels
{
    /// <summary>
    /// 主窗口视图模型
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        #region 私有字段
        
        private string _selectedFilePath;
        private string _tempRedactedFilePath;
        private string _statusText;
        private double _progressValue;
        private string _progressText;
        private List<SensitiveInfo> _sensitiveInfos;
        private List<IRedactionRule> _redactionRules;
        private ObservableCollection<ManualRedactionArea> _manualRedactionAreas;
        private ObservableCollection<SensitiveInfoViewModel> _displaySensitiveInfos;
        
        #endregion
        
        #region 属性
        
        /// <summary>
        /// 选中的文件路径
        /// </summary>
        public string SelectedFilePath
        {
            get => _selectedFilePath;
            set
            {
                _selectedFilePath = value;
                OnPropertyChanged();
            }
        }
        

        
        /// <summary>
        /// 临时脱敏PDF文件路径
        /// </summary>
        public string TempRedactedFilePath
        {
            get => _tempRedactedFilePath;
            set
            {
                _tempRedactedFilePath = value;
                OnPropertyChanged();
            }
        }
        
        /// <summary>
        /// 状态文本
        /// </summary>
        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }
        
        /// <summary>
        /// 进度值
        /// </summary>
        public double ProgressValue
        {
            get => _progressValue;
            set
            {
                _progressValue = value;
                OnPropertyChanged();
                ProgressText = $"{value:F0}%";
            }
        }
        
        /// <summary>
        /// 进度文本
        /// </summary>
        public string ProgressText
        {
            get => _progressText;
            set
            {
                _progressText = value;
                OnPropertyChanged();
            }
        }
        
        /// <summary>
        /// 敏感信息列表
        /// </summary>
        public List<SensitiveInfo> SensitiveInfos
        {
            get => _sensitiveInfos;
            set
            {
                _sensitiveInfos = value;
                OnPropertyChanged();
                UpdateDisplaySensitiveInfos();
            }
        }
        
        /// <summary>
        /// 脱敏规则列表
        /// </summary>
        public List<IRedactionRule> RedactionRules
        {
            get => _redactionRules;
            set
            {
                _redactionRules = value;
                OnPropertyChanged();
            }
        }
        
        /// <summary>
        /// 手动脱敏区域列表
        /// </summary>
        public ObservableCollection<ManualRedactionArea> ManualRedactionAreas
        {
            get => _manualRedactionAreas;
            set
            {
                _manualRedactionAreas = value;
                OnPropertyChanged();
                UpdateDisplaySensitiveInfos();
            }
        }
        
        /// <summary>
        /// 用于显示的敏感信息列表
        /// </summary>
        public ObservableCollection<SensitiveInfoViewModel> DisplaySensitiveInfos
        {
            get => _displaySensitiveInfos;
            set
            {
                _displaySensitiveInfos = value;
                OnPropertyChanged();
            }
        }
        
        #endregion
        
        #region 构造函数
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public MainViewModel()
        {
            // 初始化集合属性
            DisplaySensitiveInfos = new ObservableCollection<SensitiveInfoViewModel>();
            ManualRedactionAreas = new ObservableCollection<ManualRedactionArea>();
            SensitiveInfos = new List<SensitiveInfo>();
            
            // 获取当前应用程序实例
            var app = (App)System.Windows.Application.Current;
            var config = app.GetConfiguration();
            
            // 添加空值检查
            if (config == null)
            {
                LoggingService.Log("警告: 从App获取的配置为null，将使用默认配置");
            }
            
            InitializeRules(config);
            StatusText = "就绪";
            ProgressValue = 0;
        }
        
        #endregion
        
        #region 公共方法
        

        
        /// <summary>
        /// 处理PDF脱敏
        /// </summary>
        public async Task ProcessPdfRedactionAsync()
        {
            if (string.IsNullOrEmpty(SelectedFilePath))
            {
                StatusText = "请先选择一个PDF文件。";
                return;
            }
            
            try
            {
                // 获取当前应用程序实例的服务
                var app = (App)System.Windows.Application.Current;
                var pdfService = app.GetPdfService();
                var aiService = app.GetAIService();
                
                StatusText = "正在处理...";
                ProgressValue = 0;
                
                // 步骤1: 提取PDF文本
                StatusText = "正在提取PDF文本...";
                var pages = pdfService.ExtractTextWithPosition(SelectedFilePath);
                ProgressValue = 20;
                
                // 步骤2: 使用AI识别敏感信息
                StatusText = "正在识别敏感信息...";
                var allText = new StringBuilder();
                foreach (var page in pages)
                {
                    foreach (var element in page.TextElements)
                    {
                        allText.AppendLine(element.Text);
                    }
                }
                
                // 获取自定义关键字
                List<string> customKeywords = new List<string>();
                var config = ((App)System.Windows.Application.Current).GetConfiguration();
                if (config != null)
                {
                    var otherRule = config.Rules.FirstOrDefault(r => r.Type == SensitiveInfoType.Other);
                    if (otherRule != null && otherRule.Keywords != null)
                    {
                        customKeywords = otherRule.Keywords;
                    }
                }
                
                // 为了提高效率，可以按段落或页面分批处理
                SensitiveInfos = await aiService.IdentifySensitiveInfoAsync(allText.ToString(), pages, customKeywords);
                ProgressValue = 60;
                
                // 步骤3: 应用脱敏规则
                StatusText = "正在应用脱敏规则...";
                ApplyRedactionRules();
                ProgressValue = 80;
                
                // 步骤4: 生成临时脱敏PDF文件
                StatusText = "正在生成脱敏后的PDF...";
                
                // 创建临时文件路径
                TempRedactedFilePath = Path.GetTempFileName() + ".pdf";
                
                // 合并自动识别的敏感信息和手动脱敏区域
                var allRedactionInfos = new List<SensitiveInfo>();
                
                // 添加自动识别的敏感信息
                if (SensitiveInfos != null)
                {
                    allRedactionInfos.AddRange(SensitiveInfos);
                }
                
                // 将手动脱敏区域转换为SensitiveInfo
                if (ManualRedactionAreas != null)
                {
                    foreach (var area in ManualRedactionAreas)
                    {
                        allRedactionInfos.Add(new SensitiveInfo
                        {
                            Type = SensitiveInfoType.Name, // 使用任意类型，因为我们只关心坐标
                            OriginalText = "手动脱敏区域",
                            RedactedText = "已脱敏",
                            PageNumber = area.PageNumber,
                            Bounds = area.Bounds
                        });
                    }
                }
                
                // 生成脱敏PDF
                pdfService.CreateRedactedPdf(SelectedFilePath, TempRedactedFilePath, allRedactionInfos);
                
                ProgressValue = 100;
                
                StatusText = "脱敏处理完成";
            }
            catch (Exception ex)
            {
                StatusText = $"处理失败: {ex.Message}";
            }
        }
        
        /// <summary>
        /// 保存脱敏后的PDF
        /// </summary>
        public async Task SaveRedactedPdfAsync(string outputPath)
        {
            try
            {
                StatusText = "正在保存文件...";
                
                // 获取当前应用程序实例的服务
                var app = (App)System.Windows.Application.Current;
                var pdfService = app.GetPdfService();
                
                if (!string.IsNullOrEmpty(SelectedFilePath) && File.Exists(SelectedFilePath))
                {
                    // 合并自动识别的敏感信息和手动脱敏区域
                    var allRedactionInfos = new List<SensitiveInfo>();
                    
                    // 添加自动识别的敏感信息
                    if (SensitiveInfos != null)
                    {
                        allRedactionInfos.AddRange(SensitiveInfos);
                    }
                    
                    // 将手动脱敏区域转换为SensitiveInfo
                    if (ManualRedactionAreas != null)
                    {
                        foreach (var area in ManualRedactionAreas)
                        {
                            allRedactionInfos.Add(new SensitiveInfo
                            {
                                Type = SensitiveInfoType.Name, // 使用任意类型，因为我们只关心坐标
                                OriginalText = "手动脱敏区域",
                                RedactedText = "已脱敏",
                                PageNumber = area.PageNumber,
                                Bounds = area.Bounds
                            });
                        }
                    }
                    
                    // 调用实际的脱敏功能
                    pdfService.CreateRedactedPdf(SelectedFilePath, outputPath, allRedactionInfos);
                }
                
                await Task.Delay(100); // 模拟处理时间
                
                StatusText = "文件保存完成";
            }
            catch (Exception ex)
            {
                StatusText = $"保存失败: {ex.Message}";
            }
        }
        
        /// <summary>
        /// 添加手动脱敏区域
        /// </summary>
        public void AddManualRedactionArea(int pageNumber, BoundingBox bounds)
        {
            var area = new ManualRedactionArea
            {
                PageNumber = pageNumber,
                Bounds = bounds
            };
            
            ManualRedactionAreas.Add(area);
            StatusText = $"已添加手动脱敏区域: 第{pageNumber}页";
        }
        
        /// <summary>
        /// 移除手动脱敏区域
        /// </summary>
        public void RemoveManualRedactionArea(ManualRedactionArea area)
        {
            if (ManualRedactionAreas.Contains(area))
            {
                ManualRedactionAreas.Remove(area);
                StatusText = "已移除选定的手动脱敏区域";
            }
        }
        
        /// <summary>
        /// 清空所有手动脱敏区域
        /// </summary>
        public void ClearManualRedactionAreas()
        {
            ManualRedactionAreas.Clear();
            StatusText = "已清空所有手动脱敏区域";
        }
        
        /// <summary>
        /// 更新用于显示的敏感信息列表
        /// </summary>
        private void UpdateDisplaySensitiveInfos()
        {
            DisplaySensitiveInfos.Clear();
            
            // 添加自动识别的敏感信息
            if (SensitiveInfos != null)
            {
                foreach (var info in SensitiveInfos)
                {
                    DisplaySensitiveInfos.Add(new SensitiveInfoViewModel
                    {
                        Type = GetTypeInfo(info.Type),
                        OriginalText = info.OriginalText,
                        RedactedText = info.RedactedText,
                        PageNumber = info.PageNumber,
                        Position = $"({info.Bounds.Left:F1}, {info.Bounds.Top:F1})",
                        IsManualRedaction = false
                    });
                }
            }
            
            // 添加手动脱敏区域
            if (ManualRedactionAreas != null)
            {
                foreach (var area in ManualRedactionAreas)
                {
                    DisplaySensitiveInfos.Add(new SensitiveInfoViewModel
                    {
                        Type = "手动区域",
                        OriginalText = "手动选择区域",
                        RedactedText = "已脱敏",
                        PageNumber = area.PageNumber,
                        Position = $"({area.Bounds.Left:F1}, {area.Bounds.Top:F1})",
                        IsManualRedaction = true
                    });
                }
            }
        }
        
        /// <summary>
        /// 获取敏感信息类型的字符串表示
        /// </summary>
        private string GetTypeInfo(SensitiveInfoType type)
        {
            return type switch
            {
                SensitiveInfoType.Name => "姓名",
                SensitiveInfoType.IdCard => "身份证号",
                SensitiveInfoType.PhoneNumber => "电话号码",
                SensitiveInfoType.Address => "地址",
                _ => "其他"
            };
        }
        
        #endregion
        
        #region 私有方法
        
        /// <summary>
        /// 初始化脱敏规则
        /// </summary>
        public void InitializeRules(RedactionConfig config)
        {
            RedactionRules = new List<IRedactionRule>();
            
            // 添加空值检查
            if (config == null)
            {
                LoggingService.Log("配置对象为空，使用默认规则");
                return;
            }
            
            if (config.Rules == null)
            {
                LoggingService.Log("规则集合为空，使用默认规则");
                return;
            }
            
            // 根据配置初始化规则
            foreach (var ruleConfig in config.Rules)
            {
                // 添加规则配置的空值检查
                if (ruleConfig == null)
                {
                    LoggingService.Log("发现空的规则配置，已跳过");
                    continue;
                }
                
                IRedactionRule rule = ruleConfig.Type switch
                {
                    SensitiveInfoType.Name => new NameRedactionRule(),
                    SensitiveInfoType.IdCard => new IdCardRedactionRule(),
                    SensitiveInfoType.PhoneNumber => new PhoneRedactionRule(),
                    SensitiveInfoType.Address => new AddressRedactionRule(),
                    _ => null
                };
                
                if (rule != null)
                {
                    rule.IsEnabled = ruleConfig.IsEnabled;
                    RedactionRules.Add(rule);
                }
            }
        }
        
        /// <summary>
        /// 应用脱敏规则
        /// </summary>
        private void ApplyRedactionRules()
        {
            foreach (var info in SensitiveInfos)
            {
                var rule = GetRuleByType(info.Type);
                if (rule != null && rule.IsEnabled)
                {
                    info.RedactedText = rule.Redact(info.OriginalText);
                }
                else
                {
                    info.RedactedText = info.OriginalText;
                }
            }
        }
        
        /// <summary>
        /// 根据敏感信息类型获取对应的脱敏规则
        /// </summary>
        private IRedactionRule GetRuleByType(SensitiveInfoType type)
        {
            return type switch
            {
                SensitiveInfoType.Name => RedactionRules.Find(r => r is NameRedactionRule),
                SensitiveInfoType.IdCard => RedactionRules.Find(r => r is IdCardRedactionRule),
                SensitiveInfoType.PhoneNumber => RedactionRules.Find(r => r is PhoneRedactionRule),
                SensitiveInfoType.Address => RedactionRules.Find(r => r is AddressRedactionRule),
                _ => null
            };
        }
        
        #endregion
        
        #region INotifyPropertyChanged实现
        
        /// <summary>
        /// 属性更改事件
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        
        /// <summary>
        /// 触发属性更改事件
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        #endregion
    }
}