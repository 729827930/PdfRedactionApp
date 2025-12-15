using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PdfRedactionApp.Models;

namespace PdfRedactionApp.Models
{
    /// <summary>
    /// 脱敏规则接口
    /// </summary>
    public interface IRedactionRule
    {
        /// <summary>
        /// 规则名称
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// 是否启用该规则
        /// </summary>
        bool IsEnabled { get; set; }
        
        /// <summary>
        /// 对敏感信息进行脱敏处理
        /// </summary>
        /// <param name="text">原始文本</param>
        /// <returns>脱敏后的文本</returns>
        string Redact(string text);
    }
    
    /// <summary>
    /// 姓名脱敏规则
    /// </summary>
    public class NameRedactionRule : IRedactionRule
    {
        public string Name => "姓名脱敏";
        public bool IsEnabled { get; set; } = true;
        
        public string Redact(string text)
        {
            if (!IsEnabled || string.IsNullOrEmpty(text))
                return text;
                
            // 保留姓氏，名字用*替换
            if (text.Length > 1)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(text[0]); // 保留第一个字符（姓氏）
                for (int i = 1; i < text.Length; i++)
                {
                    sb.Append("*");
                }
                return sb.ToString();
            }
            else if (text.Length == 1)
            {
                return "*";
            }
            
            return text;
        }
    }
    
    /// <summary>
    /// 身份证号脱敏规则
    /// </summary>
    public class IdCardRedactionRule : IRedactionRule
    {
        public string Name => "身份证号脱敏";
        public bool IsEnabled { get; set; } = true;
        
        public string Redact(string text)
        {
            if (!IsEnabled || string.IsNullOrEmpty(text))
                return text;
                
            // 保留前6位和后4位，中间用*替换
            if (text.Length > 10)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(text.Substring(0, 6)); // 保留前6位
                
                // 中间部分用*替换
                for (int i = 6; i < text.Length - 4; i++)
                {
                    sb.Append("*");
                }
                
                sb.Append(text.Substring(text.Length - 4)); // 保留后4位
                return sb.ToString();
            }
            
            return text;
        }
    }
    
    /// <summary>
    /// 电话号码脱敏规则
    /// </summary>
    public class PhoneRedactionRule : IRedactionRule
    {
        public string Name => "电话号码脱敏";
        public bool IsEnabled { get; set; } = true;
        
        public string Redact(string text)
        {
            if (!IsEnabled || string.IsNullOrEmpty(text))
                return text;
                
            // 保留前3位和后4位，中间4位用*替换
            if (text.Length >= 11)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(text.Substring(0, 3)); // 保留前3位
                
                // 中间4位用*替换
                for (int i = 0; i < 4; i++)
                {
                    sb.Append("*");
                }
                
                sb.Append(text.Substring(text.Length - 4)); // 保留后4位
                return sb.ToString();
            }
            
            return text;
        }
    }
    
    /// <summary>
    /// 地址脱敏规则
    /// </summary>
    public class AddressRedactionRule : IRedactionRule
    {
        public string Name => "地址脱敏";
        public bool IsEnabled { get; set; } = true;
        
        public string Redact(string text)
        {
            if (!IsEnabled || string.IsNullOrEmpty(text))
                return text;
                
            // 保留省市，模糊详细街道
            // 这里简化处理，保留前6个字符，后面用*替换
            if (text.Length > 6)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(text.Substring(0, 6)); // 保留前6个字符
                
                // 后面部分用*替换
                for (int i = 6; i < text.Length; i++)
                {
                    sb.Append("*");
                }
                
                return sb.ToString();
            }
            
            return text;
        }
    }
}