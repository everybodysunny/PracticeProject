using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracticeProject.Models
{
    public class DeviceDataSnapshot
    {
        /// <summary>
        /// 所属设备ID
        /// </summary>
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// 数据采集时间
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 是否读取成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 错误消息（读取失败时）
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 寄存器名称 → 读取的数据值
        /// </summary>
        public Dictionary<string, ushort[]> Registers { get; set; } = new();

        /// <summary>
        /// 所有寄存器的原始数据合并
        /// </summary>
        public ushort[]? RawData { get; set; }
    }
}
