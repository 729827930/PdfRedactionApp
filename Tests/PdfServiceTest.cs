using System;
using System.IO;
using PdfRedactionApp.Services;

namespace PdfRedactionApp.Tests
{
    /// <summary>
    /// PDF服务测试类
    /// </summary>
    public class PdfServiceTest
    {
        /// <summary>
        /// 测试中文显示和坐标转换
        /// </summary>
        public static void TestChineseDisplayAndCoordinate()
        {
            Console.WriteLine("开始测试PDF中文显示和坐标转换...");
            
            // 创建PdfService实例
            var pdfService = new PdfService();
            
            // 输入PDF文件路径
            Console.Write("请输入测试PDF文件路径: ");
            string filePath = Console.ReadLine();
            
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"错误：文件 '{filePath}' 不存在！");
                return;
            }
            
            Console.WriteLine("\n=== 测试1：提取文本和坐标 ===");
            try
            {
                // 提取文本和位置信息
                var pages = pdfService.ExtractTextWithPosition(filePath);
                
                // 统计中文文本
                int chineseCount = 0;
                int totalCount = 0;
                
                foreach (var page in pages)
                {
                    Console.WriteLine($"\n页面 {page.PageNumber} 包含 {page.TextElements.Count} 个文本元素");
                    
                    foreach (var element in page.TextElements)
                    {
                        totalCount++;
                        if (ContainsChinese(element.Text))
                        {
                            chineseCount++;
                            Console.WriteLine($"中文文本：'{element.Text}'，坐标：({element.Bounds.Left:F2}, {element.Bounds.Top:F2})");
                        }
                        else
                        {
                            Console.WriteLine($"英文/数字文本：'{element.Text}'，坐标：({element.Bounds.Left:F2}, {element.Bounds.Top:F2})");
                        }
                    }
                }
                
                Console.WriteLine($"\n统计结果：总共 {totalCount} 个文本元素，其中 {chineseCount} 个包含中文");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"提取文本时出错：{ex.Message}");
            }
            
            Console.WriteLine("\n=== 测试2：渲染PDF页面为图像 ===");
            try
            {
                // 渲染第一页为图像
                Console.Write("请输入要渲染的页码(1-2): ");
                int pageNumber = int.Parse(Console.ReadLine());
                
                byte[] imageBytes = pdfService.RenderPageToImage(filePath, pageNumber, 300);
                
                if (imageBytes != null && imageBytes.Length > 0)
                {
                    // 保存图像到文件
                    string outputPath = Path.Combine(Environment.CurrentDirectory, $"test_page_{pageNumber}.png");
                    File.WriteAllBytes(outputPath, imageBytes);
                    Console.WriteLine($"成功将第 {pageNumber} 页渲染为图像，保存到: {outputPath}");
                }
                else
                {
                    Console.WriteLine("渲染图像失败");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"渲染图像时出错：{ex.Message}");
            }
            
            Console.WriteLine("\n测试完成，按任意键退出...");
            Console.ReadKey();
        }
        
        /// <summary>
        /// 判断字符串是否包含中文字符
        /// </summary>
        private static bool ContainsChinese(string text)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(text, "[\u4e00-\u9fa5]");
        }
    }
}