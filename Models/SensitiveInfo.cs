using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfRedactionApp.Models
{
    /// <summary>
    /// 敏感信息类型枚举
    /// </summary>
    public enum SensitiveInfoType
    {
        /// <summary>
        /// 姓名
        /// </summary>
        Name,
        
        /// <summary>
        /// 身份证号
        /// </summary>
        IdCard,
        
        /// <summary>
        /// 电话号码
        /// </summary>
        PhoneNumber,
        
        /// <summary>
        /// 地址
        /// </summary>
        Address,
        
        /// <summary>
        /// 其他
        /// </summary>
        Other
    }
    
    /// <summary>
    /// 敏感信息类
    /// </summary>
    public class SensitiveInfo
    {
        /// <summary>
        /// 敏感信息类型
        /// </summary>
        public SensitiveInfoType Type { get; set; }
        
        /// <summary>
        /// 原始文本
        /// </summary>
        public string OriginalText { get; set; }
        
        /// <summary>
        /// 脱敏后的文本
        /// </summary>
        public string RedactedText { get; set; }
        
        /// <summary>
        /// 在PDF中的页码（从1开始）
        /// </summary>
        public int PageNumber { get; set; }
        
        /// <summary>
        /// 文本在页面上的边界框（坐标单位为点）
        /// </summary>
        public BoundingBox Bounds { get; set; }
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public SensitiveInfo()
        {
            Bounds = new BoundingBox();
        }
    }
    
    /// <summary>
    /// 边界框类，表示文本在页面上的位置和大小
    /// </summary>
    public class BoundingBox
    {
        /// <summary>
        /// 左侧X坐标
        /// </summary>
        public double Left { get; set; }
        
        /// <summary>
        /// 顶部Y坐标
        /// </summary>
        public double Top { get; set; }
        
        /// <summary>
        /// 宽度
        /// </summary>
        public double Width { get; set; }
        
        /// <summary>
        /// 高度
        /// </summary>
        public double Height { get; set; }
        
        /// <summary>
        /// 右侧X坐标
        /// </summary>
        public double Right => Left + Width;
        
        /// <summary>
        /// 底部Y坐标
        /// </summary>
        public double Bottom => Top + Height;
    }
}