using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfRedactionApp.ViewModels
{
    /// <summary>
    /// 用于在UI中显示的敏感信息视图模型
    /// </summary>
    public class SensitiveInfoViewModel
    {
        /// <summary>
        /// 敏感信息类型
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 原始文本
        /// </summary>
        public string OriginalText { get; set; }

        /// <summary>
        /// 脱敏后的文本
        /// </summary>
        public string RedactedText { get; set; }

        /// <summary>
        /// 页码
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// 位置信息
        /// </summary>
        public string Position { get; set; }

        /// <summary>
        /// 是否为手动脱敏区域
        /// </summary>
        public bool IsManualRedaction { get; set; }
    }
}