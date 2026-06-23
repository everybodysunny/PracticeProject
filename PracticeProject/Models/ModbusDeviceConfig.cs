using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracticeProject.Models
{
    public class ModbusDeviceConfig
    {
        /// <summary>
        /// 设备唯一标识符（必填）
        /// </summary>
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// 设备 IP 地址
        /// </summary>
        public string IpAddress { get; set; } = "192.168.1.1";

        /// <summary>
        /// Modbus TCP 端口，默认 502
        /// </summary>
        public int Port { get; set; } = 502;

        /// <summary>
        /// 从站地址 (1-255)
        /// 通常为 1，部分设备需要其他值
        /// </summary>
        public byte SlaveId { get; set; } = 1;

        /// <summary>
        /// 轮询间隔时间（毫秒）
        /// 默认 1000ms = 1秒
        /// </summary>
        public int PollingIntervalMs { get; set; } = 1000;

        /// <summary>
        /// 连接超时时间（毫秒）
        /// </summary>
        public int ConnectionTimeoutMs { get; set; } = 3000;

        /// <summary>
        /// 要读取的寄存器列表
        /// </summary>
        public List<RegisterMapping> Registers { get; set; } = new();

        /// <summary>
        /// 是否启用该设备
        /// </summary>
        public bool IsEnabled { get; set; } = true;
    }
}
