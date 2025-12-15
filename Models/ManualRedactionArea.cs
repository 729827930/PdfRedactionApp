using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfRedactionApp.Models
{
    /// <summary>
    /// 手动脱敏区域类
    /// </summary>
    public class ManualRedactionArea
    {
        /// <summary>
        /// 区域ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 页码
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// 区域边界框
        /// </summary>
        public BoundingBox Bounds { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public ManualRedactionArea()
        {
            Id = Guid.NewGuid().ToString();
            Bounds = new BoundingBox();
            CreatedAt = DateTime.Now;
        }
    }
}