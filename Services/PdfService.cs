using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using PdfRedactionApp.Models;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Drawing;

namespace PdfRedactionApp.Services
{
    /// <summary>
    /// PDF处理服务
    /// </summary>
    public class PdfService
    {
        /// <summary>
        /// 从PDF文件中提取文本和位置信息
        /// </summary>
        /// <param name="filePath">PDF文件路径</param>
        /// <returns>包含每页文本和位置信息的列表</returns>
        public List<PageContent> ExtractTextWithPosition(string filePath)
        {
            var pages = new List<PageContent>();
            
            using (var document = UglyToad.PdfPig.PdfDocument.Open(filePath))
            {
                for (int i = 0; i < document.NumberOfPages; i++)
                {
                    var page = document.GetPage(i + 1);
                    var pageContent = new PageContent
                    {
                        PageNumber = i + 1,
                        Width = page.Width,
                        Height = page.Height,
                        TextElements = new List<TextElement>()
                    };
                    
                    // 提取页面上的所有文字
                    var words = page.GetWords();
                    
                    // 调试输出页面尺寸信息
                    System.Console.WriteLine($"页面 {i+1} 尺寸：Width={page.Width}, Height={page.Height}");
                    System.Console.WriteLine($"转换为英寸：Width={page.Width/72:F2}, Height={page.Height/72:F2}");
                    
                    foreach (var word in words)
                    {
                        // PdfPig使用左上角为原点的坐标系（Top-Down）
                        // 坐标单位是点（Point），1英寸 = 72点
                        var textElement = new TextElement
                        {
                            Text = word.Text,
                            Bounds = new BoundingBox
                            {
                                Left = word.BoundingBox.Left,
                                Top = word.BoundingBox.Top, // 保持PdfPig的Top-Down坐标
                                Width = word.BoundingBox.Width,
                                Height = word.BoundingBox.Height
                            }
                        };
                        
                        pageContent.TextElements.Add(textElement);
                        
                        // 调试输出坐标信息
                        if (word.Text.Contains("许新胜"))
                        {
                            System.Console.WriteLine($"找到'许新胜'：Left={word.BoundingBox.Left}, Top={word.BoundingBox.Top}, Width={word.BoundingBox.Width}, Height={word.BoundingBox.Height}");
                            System.Console.WriteLine($"转换为英寸：Left={word.BoundingBox.Left/72:F2}, Top={word.BoundingBox.Top/72:F2}, Width={word.BoundingBox.Width/72:F2}, Height={word.BoundingBox.Height/72:F2}");
                            System.Console.WriteLine($"Adobe Acrobat坐标（用户报告）：Left=2.00英寸 (144点), Top=3.50英寸 (252点)");
                            
                            // 计算与Adobe Acrobat坐标的差异
                            double adobeLeftInPoints = 2.0 * 72;
                            double adobeTopInPoints = 3.5 * 72;
                            System.Console.WriteLine($"坐标差异：Left差={word.BoundingBox.Left - adobeLeftInPoints:F2}点, Top差={word.BoundingBox.Top - adobeTopInPoints:F2}点");
                            
                            // 计算从底部的坐标（模拟PDFSharp的坐标系）
                            double bottomY = page.Height - (word.BoundingBox.Top + word.BoundingBox.Height);
                            System.Console.WriteLine($"从底部开始的Y坐标：{bottomY:F2}点");
                            System.Console.WriteLine($"转换为英寸：{bottomY/72:F2}英寸");
                        }
                    }
                    
                    pages.Add(pageContent);
                }
            }
            
            return pages;
        }
        
        /// <summary>
        /// 将PDF页面渲染为图像
        /// </summary>
        /// <param name="filePath">PDF文件路径</param>
        /// <param name="pageNumber">页码（从1开始）</param>
        /// <param name="dpi">渲染分辨率</param>
        /// <returns>图像字节数组</returns>
        public byte[] RenderPageToImage(string filePath, int pageNumber, int dpi = 96)
        {
            try
            {
                using (var document = UglyToad.PdfPig.PdfDocument.Open(filePath))
                {
                    if (pageNumber < 1 || pageNumber > document.NumberOfPages)
                        return new byte[0];

                    var page = document.GetPage(pageNumber);
                    
                    // 设置渲染分辨率
                    var scale = dpi / 72.0; // 1英寸 = 72点
                    var width = (int)(page.Width * scale);
                    var height = (int)(page.Height * scale);
                    
                    // 创建SkiaSharp位图用于绘制
                    using (var bitmap = new SkiaSharp.SKBitmap(width, height, SkiaSharp.SKColorType.Rgba8888, SkiaSharp.SKAlphaType.Premul))
                    {
                        // 创建SkiaSharp画布
                        using (var canvas = new SkiaSharp.SKCanvas(bitmap))
                        {
                            // 设置白色背景
                            canvas.Clear(SkiaSharp.SKColors.White);
                            
                            // 缩放画布以匹配所需分辨率
                            canvas.Scale((float)scale, (float)scale);
                            
                            // 提取页面文本元素
                            var textElements = ExtractTextWithPosition(filePath)
                                .FirstOrDefault(p => p.PageNumber == pageNumber)?.TextElements;
                            
                            if (textElements != null)
                            {
                                // 创建文本画笔
                                using (var paint = new SkiaSharp.SKPaint())
                                {
                                    // 设置文本颜色为黑色
                                    paint.Color = SkiaSharp.SKColors.Black;
                                    paint.IsAntialias = true;
                                    paint.TextEncoding = SkiaSharp.SKTextEncoding.Utf8;
                                    
                                    // 尝试使用多种中文字体
                                    string[] fontFamilies = { "Microsoft YaHei", "SimSun", "SimHei", "KaiTi", "Arial" };
                                    SkiaSharp.SKTypeface typeface = null;
                                    
                                    // 寻找可用的字体
                                    foreach (var fontFamily in fontFamilies)
                                    {
                                        typeface = SkiaSharp.SKTypeface.FromFamilyName(fontFamily);
                                        if (typeface != null)
                                            break;
                                    }
                                    
                                    // 如果找不到任何字体，使用默认字体
                                    if (typeface == null)
                                        typeface = SkiaSharp.SKTypeface.Default;
                                    
                                    paint.Typeface = typeface;
                                    
                                    // 绘制所有文本元素
                                    foreach (var element in textElements)
                                    {
                                        // 设置字体大小（基于文本高度）
                                        paint.TextSize = (float)element.Bounds.Height;
                                        
                                        try
                                        {
                                            // 绘制文本（注意：PdfPig使用左上角为原点）
                                            canvas.DrawText(element.Text, (float)element.Bounds.Left, (float)(element.Bounds.Top + element.Bounds.Height), paint);
                                        }
                                        catch (Exception ex)
                                        {
                                            // 跳过无法渲染的字符
                                            System.Console.WriteLine($"绘制文本'{element.Text}'时出错: {ex.Message}");
                                        }
                                    }
                                }
                            }
                        }
                        
                        // 将SkiaSharp位图转换为字节数组
                        using (var stream = new MemoryStream())
                        {
                            using (var data = bitmap.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100))
                            {
                                data.SaveTo(stream);
                                return stream.ToArray();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录异常但不抛出，返回空数组
                System.Console.WriteLine($"渲染PDF页面时出错: {ex.Message}");
                System.Console.WriteLine($"异常堆栈: {ex.StackTrace}");
                return new byte[0];
            }
        }
        
        /// <summary>
        /// 创建脱敏后的PDF
        /// </summary>
        /// <param name="inputPath">输入PDF文件路径</param>
        /// <param name="outputPath">输出PDF文件路径</param>
        /// <param name="sensitiveInfos">敏感信息列表</param>
        public void CreateRedactedPdf(string inputPath, string outputPath, List<SensitiveInfo> sensitiveInfos)
        {
            // 打开原始PDF文件
            using (var document = PdfReader.Open(inputPath, PdfDocumentOpenMode.Modify))
            {
                // 按页面分组敏感信息
                var sensitiveInfosByPage = sensitiveInfos.Where(s => s.Bounds != null && s.PageNumber > 0)
                    .GroupBy(s => s.PageNumber)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // 遍历每一页
                for (int pageIndex = 0; pageIndex < document.Pages.Count; pageIndex++)
                {
                    var page = document.Pages[pageIndex];
                    int pageNumber = pageIndex + 1;

                    // 检查当前页是否有需要脱敏的信息
                    if (sensitiveInfosByPage.TryGetValue(pageNumber, out var pageSensitiveInfos))
                    {
                        // 创建PDF绘图对象
                        using (var gfx = XGraphics.FromPdfPage(page))
                        {
                            // 创建黑色画笔用于覆盖敏感信息
                            var blackBrush = new XSolidBrush(XColors.Black);

                            // 调试输出页面尺寸信息
                            System.Console.WriteLine($"PDFSharp页面 {pageNumber} 尺寸：Width={page.Width.Point}, Height={page.Height.Point}");
                            System.Console.WriteLine($"转换为英寸：Width={page.Width.Point/72:F2}, Height={page.Height.Point/72:F2}");
                            
                            // 遍历当前页的所有敏感信息
                            foreach (var sensitiveInfo in pageSensitiveInfos)
                            {
                                // 坐标转换说明：
                                // - PdfPig使用左上角为原点的Top-Down坐标系
                                // - PDFSharp使用左下角为原点的Bottom-Up坐标系
                                // - 两者都使用点（Point）作为单位，1英寸=72点
                                
                                // 将Top-Down坐标转换为Bottom-Up坐标
                                // 正确的转换公式：
                                // x保持不变，y = 页面总高度 - 从顶部开始的距离
                                double x = sensitiveInfo.Bounds.Left;
                                double y = page.Height.Point - sensitiveInfo.Bounds.Top;
                                double width = sensitiveInfo.Bounds.Width;
                                double height = sensitiveInfo.Bounds.Height;
                                
                                // 调试：显示当前坐标转换过程
                                System.Console.WriteLine($"Top-Down坐标：Left={sensitiveInfo.Bounds.Left}, Top={sensitiveInfo.Bounds.Top}, Width={width}, Height={height}");
                                System.Console.WriteLine($"Bottom-Up坐标：x={x}, y={y}, Width={width}, Height={height}");

                                // 调试输出坐标转换信息
                                if (sensitiveInfo.OriginalText.Contains("许新胜"))
                                {
                                    System.Console.WriteLine($"脱敏'许新胜'：原始Left={sensitiveInfo.Bounds.Left}, Top={sensitiveInfo.Bounds.Top}");
                                    System.Console.WriteLine($"转换后x={x}, y={y}, width={width}, height={height}");
                                    System.Console.WriteLine($"PDFSharp页面高度：{page.Height.Point} 点");
                                    
                                    // 计算Adobe Acrobat坐标对应的PDFSharp坐标
                                    double adobeLeftInPoints = 2.0 * 72;
                                    double adobeTopInPoints = 3.5 * 72;
                                    double adobeWidthInPoints = 1.06 * 72;
                                    double adobeHeightInPoints = 0.35 * 72;
                                    
                                    // Adobe Acrobat使用左下角为原点，所以y坐标需要调整
                                    double adobeYInPdfSharp = adobeTopInPoints;
                                    
                                    System.Console.WriteLine($"Adobe Acrobat坐标（点）：Left={adobeLeftInPoints}, Top={adobeTopInPoints}, Width={adobeWidthInPoints}, Height={adobeHeightInPoints}");
                                    System.Console.WriteLine($"对应的PDFSharp坐标：x={adobeLeftInPoints}, y={adobeYInPdfSharp}, width={adobeWidthInPoints}, height={adobeHeightInPoints}");
                                    
                                    // 计算坐标差异
                                    System.Console.WriteLine($"X坐标差异：{x - adobeLeftInPoints:F2}点");
                                    System.Console.WriteLine($"Y坐标差异：{y - adobeYInPdfSharp:F2}点");
                                }

                                // 绘制黑色矩形覆盖敏感信息
                                gfx.DrawRectangle(blackBrush, new XRect(x, y, width, height));
                            }
                        }
                    }
                }

                // 保存脱敏后的PDF文件
                document.Save(outputPath);
            }
        }
    }
    
    /// <summary>
    /// 页面内容类
    /// </summary>
    public class PageContent
    {
        /// <summary>
        /// 页码
        /// </summary>
        public int PageNumber { get; set; }
        
        /// <summary>
        /// 页面宽度
        /// </summary>
        public double Width { get; set; }
        
        /// <summary>
        /// 页面高度
        /// </summary>
        public double Height { get; set; }
        
        /// <summary>
        /// 文本元素列表
        /// </summary>
        public List<TextElement> TextElements { get; set; }
    }
    
    /// <summary>
    /// 文本元素类
    /// </summary>
    public class TextElement
    {
        /// <summary>
        /// 文本内容
        /// </summary>
        public string Text { get; set; }
        
        /// <summary>
        /// 文本边界框
        /// </summary>
        public BoundingBox Bounds { get; set; }
    }
}