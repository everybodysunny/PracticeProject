using PracticeProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracticeProject.Iservices
{
    public interface IModbusPollingService
    {
        /// <summary>
        /// 所有设备最新数据（只读字典）
        /// </summary>
        IReadOnlyDictionary<string, DeviceDataSnapshot> AllDeviceData { get; }

        /// <summary>
        /// 所有设备配置
        /// </summary>
        IReadOnlyDictionary<string, ModbusDeviceConfig> AllDeviceConfigs { get; }

        // ========== 生命周期 ==========

        /// <summary>
        /// 启动所有设备轮询
        /// </summary>
        Task StartAsync();

        /// <summary>
        /// 停止所有设备轮询
        /// </summary>
        Task StopAsync();

        // ========== 数据查询 ==========

        /// <summary>
        /// 尝试获取指定设备的数据
        /// </summary>
        bool TryGetDeviceData(string deviceId, out DeviceDataSnapshot? data);

        /// <summary>
        /// 手动触发单设备读取
        /// </summary>
        Task<DeviceDataSnapshot?> ReadSingleDeviceAsync(string deviceId);

        /// <summary>
        /// 获取指定设备的轮询器（用于写入寄存器等直接操作）
        /// </summary>
        IModbusDevicePoller? GetPoller(string deviceId);
    }
}
