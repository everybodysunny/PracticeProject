using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracticeProject.Iservices
{
    public  interface IModbusClient
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        string DeviceId { get; }

        /// <summary>
        /// 是否已连接
        /// </summary>
        bool IsConnected { get; }

        // ========== 连接 ==========

        /// <summary>
        /// 连接到设备
        /// </summary>
        Task<bool> ConnectAsync(string ipAddress, int port);

        /// <summary>
        /// 断开连接
        /// </summary>
        Task DisconnectAsync();

        // ========== 读取 ==========

        /// <summary>
        /// 读取保持寄存器 (功能码 3)
        /// </summary>
        Task<ushort[]> ReadHoldingRegistersAsync(ushort startAddress, ushort count);

        /// <summary>
        /// 读取输入寄存器 (功能码 4)
        /// </summary>
        Task<ushort[]> ReadInputRegistersAsync(ushort startAddress, ushort count);

        /// <summary>
        /// 读取线圈 (功能码 1)
        /// </summary>
        Task<bool[]> ReadCoilsAsync(ushort startAddress, ushort count);

        /// <summary>
        /// 读取离散输入 (功能码 2)
        /// </summary>
        Task<bool[]> ReadDiscreteInputsAsync(ushort startAddress, ushort count);

        // ========== 写入 ==========

        /// <summary>
        /// 写单个线圈 (功能码 5)
        /// </summary>
        Task WriteSingleCoilAsync(ushort address, bool value);

        /// <summary>
        /// 写单个保持寄存器 (功能码 6)
        /// </summary>
        Task WriteSingleRegisterAsync(ushort address, ushort value);

        /// <summary>
        /// 写多个线圈 (功能码 15)
        /// </summary>
        Task WriteMultipleCoilsAsync(ushort startAddress, bool[] values);

        /// <summary>
        /// 写多个保持寄存器 (功能码 16)
        /// </summary>
        Task WriteMultipleRegistersAsync(ushort startAddress, ushort[] values);
    }
}
