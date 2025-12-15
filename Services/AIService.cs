using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PdfRedactionApp.Models;
using PdfRedactionApp.Services;

namespace PdfRedactionApp.Services
{
    /// <summary>
    /// AI服务，用于调用DeepSeek API进行敏感信息识别
    /// </summary>
    public class AIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiUrl;
        
        public AIService(string apiKey, string apiUrl = "https://api.deepseek.com/v1/chat/completions")
        {
            _httpClient = new HttpClient();
            _apiKey = apiKey;
            _apiUrl = apiUrl;
        }
        
        /// <summary>
        /// 识别文本中的敏感信息
        /// </summary>
        /// <param name="text">要识别的文本</param>
        /// <param name="pages">包含位置信息的页面内容列表</param>
        /// <param name="customKeywords">自定义关键字列表</param>
        /// <returns>敏感信息列表</returns>
        public async Task<List<SensitiveInfo>> IdentifySensitiveInfoAsync(string text, List<PageContent> pages, List<string> customKeywords = null)
        {
            var sensitiveInfos = new List<SensitiveInfo>();
            
            try
            {
                // 构建API请求
                var systemPrompt = new StringBuilder("你是一个专业的敏感信息识别助手。请从用户提供的文本中识别出以下类型的敏感信息,要求返回所有的，如若有重复项，也需要返回：");
                systemPrompt.AppendLine("1. 姓名：中国人姓名");
                systemPrompt.AppendLine("2. 身份证号：18位身份证号码");
                systemPrompt.AppendLine("3. 电话号码：11位手机号码");
                systemPrompt.AppendLine("4. 地址：包含省市区的详细地址");
                
                // 添加自定义关键字
                if (customKeywords != null && customKeywords.Count > 0)
                {
                    systemPrompt.AppendLine("5. 自定义关键字（Other）：请识别出以下自定义关键字：");
                    foreach (var keyword in customKeywords)
                    {
                        systemPrompt.AppendLine($"   - {keyword}");
                    }
                }
                
                systemPrompt.AppendLine("请按照以下JSON格式返回结果：");
                systemPrompt.AppendLine("[{\"type\": \"敏感信息类型\", \"text\": \"敏感信息文本\"}]");
                
                var requestBody = new
                {
                    model = "deepseek-chat",
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = systemPrompt.ToString()
                        },
                        new
                        {
                            role = "user",
                            content = text
                        }
                    },
                    temperature = 0.1
                };
                
                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                // 设置请求头
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                
                // 发送请求
                var response = await _httpClient.PostAsync(_apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    // 解析响应
                    var apiResponse = JsonConvert.DeserializeObject<DeepSeekResponse>(responseContent);
                    if (apiResponse?.choices?.Count > 0)
                    {
                        var resultJson = apiResponse.choices[0].message.content;
                        var results = JsonConvert.DeserializeObject<List<SensitiveInfoResult>>(resultJson);
                        
                        foreach (var result in results)
                        {
                            // 创建敏感信息对象
                            var sensitiveInfo = new SensitiveInfo
                            {
                                Type = ParseSensitiveInfoType(result.Type),
                                OriginalText = result.Text,
                                RedactedText = "", // 将在后续步骤中填充
                                PageNumber = 1 // 默认值，将在匹配位置时更新
                            };
                            
                            // 尝试匹配位置信息
                            MatchPositionInfo(sensitiveInfo, pages);
                            
                            sensitiveInfos.Add(sensitiveInfo);
                        }
                    }
                }
                else
                {
                    throw new Exception($"API请求失败: {response.StatusCode}, {responseContent}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"识别敏感信息时出错: {ex.Message}", ex);
            }
            
            return sensitiveInfos;
        }
        
        /// <summary>
        /// 匹配敏感信息的位置信息
        /// </summary>
        /// <param name="sensitiveInfo">敏感信息对象</param>
        /// <param name="pages">包含位置信息的页面内容列表</param>
        private void MatchPositionInfo(SensitiveInfo sensitiveInfo, List<PageContent> pages)
        {
            // 遍历所有页面查找匹配的文本
            foreach (var page in pages)
            {
                // 在当前页面的文本元素中查找最佳匹配项
                TextElement bestMatch = null;
                double bestScore = 0;
                
                foreach (var element in page.TextElements)
                {
                    // 计算匹配分数
                    double score = CalculateMatchScore(sensitiveInfo.OriginalText, element.Text);
                    
                    // 如果当前匹配更好，则更新最佳匹配
                    if (score > bestScore && score > 0.5) // 设置最小匹配阈值
                    {
                        bestScore = score;
                        bestMatch = element;
                    }
                }
                
                // 如果找到了足够好的匹配项
                if (bestMatch != null)
                {
                    sensitiveInfo.PageNumber = page.PageNumber;
                    sensitiveInfo.Bounds = new BoundingBox
                    {
                        Left = bestMatch.Bounds.Left,
                        Top = bestMatch.Bounds.Top,
                        Width = bestMatch.Bounds.Width,
                        Height = bestMatch.Bounds.Height
                    };
                    return; // 找到匹配项后立即返回
                }
            }
            
            // 如果没有找到精确匹配，则设置默认值
            if (sensitiveInfo.Bounds == null)
            {
                sensitiveInfo.Bounds = new BoundingBox();
            }
        }
        
        /// <summary>
        /// 计算两个字符串的匹配分数（0-1之间）
        /// </summary>
        /// <param name="text1">第一个文本</param>
        /// <param name="text2">第二个文本</param>
        /// <returns>匹配分数</returns>
        private double CalculateMatchScore(string text1, string text2)
        {
            // 处理空字符串情况
            if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
                return 0;
            
            // 转换为小写进行比较
            string t1 = text1.ToLower().Trim();
            string t2 = text2.ToLower().Trim();
            
            // 完全匹配
            if (t1 == t2)
                return 1.0;
            
            // 一个包含另一个
            if (t1.Contains(t2) || t2.Contains(t1))
                return 0.9;
            
            // 计算编辑距离相似度
            int maxLength = Math.Max(t1.Length, t2.Length);
            if (maxLength == 0)
                return 1.0;
            
            int distance = ComputeLevenshteinDistance(t1, t2);
            double similarity = 1.0 - (double)distance / maxLength;
            
            return similarity;
        }
        
        /// <summary>
        /// 计算两个字符串的Levenshtein距离
        /// </summary>
        /// <param name="s">第一个字符串</param>
        /// <param name="t">第二个字符串</param>
        /// <returns>Levenshtein距离</returns>
        private int ComputeLevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s))
                return string.IsNullOrEmpty(t) ? 0 : t.Length;
            
            if (string.IsNullOrEmpty(t))
                return s.Length;
            
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];
            
            // 初始化边界条件
            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 0; j <= m; d[0, j] = j++) ;
            
            // 动态规划计算距离
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            
            return d[n, m];
        }
        
        /// <summary>
        /// 将字符串类型转换为敏感信息类型枚举
        /// </summary>
        private SensitiveInfoType ParseSensitiveInfoType(string type)
        {
            string lowerType = type.ToLower();
            return lowerType switch
            {
                "name" or "姓名" => SensitiveInfoType.Name,
                "idcard" or "身份证号" or "身份证" => SensitiveInfoType.IdCard,
                "phonenumber" or "电话号码" or "手机号" => SensitiveInfoType.PhoneNumber,
                "address" or "地址" => SensitiveInfoType.Address,
                _ => SensitiveInfoType.Other
            };
        }
    }
    
    /// <summary>
    /// DeepSeek API响应类
    /// </summary>
    public class DeepSeekResponse
    {
        public string id { get; set; }
        public string @object { get; set; }
        public long created { get; set; }
        public string model { get; set; }
        public List<Choice> choices { get; set; }
        public Usage usage { get; set; }
    }
    
    /// <summary>
    /// 选择项类
    /// </summary>
    public class Choice
    {
        public int index { get; set; }
        public Message message { get; set; }
        public string finish_reason { get; set; }
    }
    
    /// <summary>
    /// 消息类
    /// </summary>
    public class Message
    {
        public string role { get; set; }
        public string content { get; set; }
    }
    
    /// <summary>
    /// 使用情况类
    /// </summary>
    public class Usage
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }
    }
    
    /// <summary>
    /// 敏感信息识别结果类
    /// </summary>
    public class SensitiveInfoResult
    {
        public string Type { get; set; }
        public string Text { get; set; }
    }
}