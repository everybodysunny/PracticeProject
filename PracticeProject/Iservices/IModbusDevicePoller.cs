using PracticeProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracticeProject.Iservices
{
    public interface IModbusDevicePoller
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        string DeviceId { get; }

        /// <summary>
        /// 设备配置
        /// </summary>
        ModbusDeviceConfig Config { get; }

        /// <summary>
        /// 是否正在运行
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// 数据接收回调（用于 PollingService 缓存最新数据）
        /// </summary>
        Action<DeviceDataSnapshot>? OnDataReceived { get; set; }

        /// <summary>
        /// 错误接收回调
        /// </summary>
        Action<Exception>? OnErrorReceived { get; set; }

        // ========== 生命周期 ==========

        /// <summary>
        /// 启动轮询
        /// </summary>
        Task StartAsync();

        /// <summary>
        /// 停止轮询
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// 执行一次读取
        /// </summary>
        Task<DeviceDataSnapshot> ReadOnceAsync();

        // ========== 写入 ==========

        /// <summary>
        /// 写入单个寄存器
        /// </summary>
        Task WriteRegisterAsync(ushort address, ushort value);

        /// <summary>
        /// 写入多个寄存器
        /// </summary>
        Task WriteRegistersAsync(ushort startAddress, ushort[] values);
    }
}
