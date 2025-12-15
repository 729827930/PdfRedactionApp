using System;
using System.IO;
using PdfRedactionApp.Services;
using PdfRedactionApp.Models;

namespace PdfRedactionApp.ConsoleTest
{
    /// <summary>
    /// PDF中文显示和坐标转换测试程序
    /// </summary>
    class Program
    {
        /// <summary>
        /// 主函数
        /// </summary>
        static void Main(string[] args)
        {
            Console.WriteLine("PDF中文显示和坐标转换测试程序");
            Console.WriteLine("=" + new string('=', 50));
            
            // 创建PdfService实例
            var pdfService = new PdfService();
            
            // 获取测试PDF文件路径
            string filePath;
            if (args.Length > 0)
            {
                filePath = args[0];
            }
            else
            {
                Console.Write("请输入测试PDF文件路径: ");
                filePath = Console.ReadLine();
            }
            
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"错误：文件 '{filePath}' 不存在！");
                Console.WriteLine("按任意键退出...");
                Console.ReadKey();
                return;
            }
            
            try
            {
                // 测试1: 提取文本和坐标
                Console.WriteLine("\n测试1: 提取文本和坐标信息");
                Console.WriteLine("-" + new string('-', 50));
                
                var pages = pdfService.ExtractTextWithPosition(filePath);
                Console.WriteLine($"共提取 {pages.Count} 页文本");
                
                // 查找特定文本"许新胜"
                bool found = false;
                SensitiveInfo testInfo = null;
                
                foreach (var page in pages)
                {
                    foreach (var element in page.TextElements)
                    {
                        if (element.Text.Contains("许新胜"))
                        {
                            found = true;
                            Console.WriteLine($"\n找到文本: {element.Text}");
                            Console.WriteLine($"原始坐标: Left={element.Bounds.Left:F2}, Top={element.Bounds.Top:F2}");
                            Console.WriteLine($"宽高: Width={element.Bounds.Width:F2}, Height={element.Bounds.Height:F2}");
                            
                            // 创建测试敏感信息
                            testInfo = new SensitiveInfo
                            {
                                Type = SensitiveInfoType.Name,
                                OriginalText = element.Text,
                                RedactedText = "***",
                                PageNumber = page.PageNumber,
                                Bounds = element.Bounds
                            };
                            break;
                        }
                    }
                    if (found)
                        break;
                }
                
                // 测试2: 渲染PDF页面
                Console.WriteLine("\n测试2: 渲染PDF页面");
                Console.WriteLine("-" + new string('-', 50));
                
                // 渲染第一页
                byte[] imageBytes = pdfService.RenderPageToImage(filePath, 1, 150);
                if (imageBytes != null && imageBytes.Length > 0)
                {
                    string imagePath = Path.Combine(Environment.CurrentDirectory, "test_render.png");
                    File.WriteAllBytes(imagePath, imageBytes);
                    Console.WriteLine($"页面渲染完成，保存为: {imagePath}");
                    Console.WriteLine("请检查图像是否正确显示中文");
                }
                else
                {
                    Console.WriteLine("页面渲染失败");
                }
                
                // 测试3: 坐标转换和脱敏
                Console.WriteLine("\n测试3: 坐标转换和脱敏");
                Console.WriteLine("-" + new string('-', 50));
                
                if (found && testInfo != null)
                {
                    string outputPath = Path.Combine(Environment.CurrentDirectory, "test_redacted.pdf");
                    pdfService.CreateRedactedPdf(filePath, outputPath, new List<SensitiveInfo> { testInfo });
                    Console.WriteLine($"脱敏PDF生成完成，保存为: {outputPath}");
                    Console.WriteLine("请检查PDF中'许新胜'是否被正确脱敏");
                }
                else
                {
                    Console.WriteLine("未找到测试文本'许新胜'，跳过脱敏测试");
                }
                
                Console.WriteLine("\n测试完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试过程中出错: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
}