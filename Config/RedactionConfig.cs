using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PdfRedactionApp.Models;

namespace PdfRedactionApp.Config
{
    /// <summary>
    /// 脱敏配置类
    /// </summary>
    public class RedactionConfig
    {
        /// <summary>
        /// 脱敏规则集合
        /// </summary>
        public List<RedactionRuleConfig> Rules { get; set; }
        
        /// <summary>
        /// DeepSeek API密钥
        /// </summary>
        public string DeepSeekApiKey { get; set; }
        
        /// <summary>
        /// DeepSeek API端点
        /// </summary>
        public string DeepSeekApiEndpoint { get; set; }
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public RedactionConfig()
        {
            Rules = new List<RedactionRuleConfig>();
            DeepSeekApiKey = "";
            DeepSeekApiEndpoint = "https://api.deepseek.com/v1/chat/completions";
        }
        
        /// <summary>
        /// 加载配置文件
        /// </summary>
        public static RedactionConfig Load(string configPath)
        {
            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                return JsonConvert.DeserializeObject<RedactionConfig>(json);
            }
            
            // 如果配置文件不存在，返回默认配置
            return GetDefaultConfig();
        }
        
        /// <summary>
        /// 保存配置文件
        /// </summary>
        public void Save(string configPath)
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(configPath, json);
        }
        
        /// <summary>
        /// 获取默认配置
        /// </summary>
        public static RedactionConfig GetDefaultConfig()
        {
            var config = new RedactionConfig();
            
            // 添加默认规则
            config.Rules.Add(new RedactionRuleConfig
            {
                Type = SensitiveInfoType.Name,
                Name = "姓名脱敏",
                IsEnabled = true,
                Pattern = ""
            });
            
            config.Rules.Add(new RedactionRuleConfig
            {
                Type = SensitiveInfoType.IdCard,
                Name = "身份证号脱敏",
                IsEnabled = true,
                Pattern = ""
            });
            
            config.Rules.Add(new RedactionRuleConfig
            {
                Type = SensitiveInfoType.PhoneNumber,
                Name = "电话号码脱敏",
                IsEnabled = true,
                Pattern = ""
            });
            
            config.Rules.Add(new RedactionRuleConfig
            {
                Type = SensitiveInfoType.Address,
                Name = "地址脱敏",
                IsEnabled = true,
                Pattern = ""
            });
            
            return config;
        }
    }
    
    /// <summary>
    /// 脱敏规则配置类
    /// </summary>
    public class RedactionRuleConfig
    {
        /// <summary>
        /// 敏感信息类型
        /// </summary>
        public SensitiveInfoType Type { get; set; }
        
        /// <summary>
        /// 规则名称
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; }
        
        /// <summary>
        /// 自定义模式（正则表达式等）
        /// </summary>
        public string Pattern { get; set; }
        
        /// <summary>
        /// 自定义关键字列表
        /// </summary>
        public List<string> Keywords { get; set; }
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public RedactionRuleConfig()
        {
            Keywords = new List<string>();
        }
    }
}