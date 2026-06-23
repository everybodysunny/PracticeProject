using Modbus.Device;
using PracticeProject.Iservices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


namespace PracticeProject.Services
{
    // <summary>
    /// Modbus TCP 客户端实现（NModbus4）
    /// </summary>
    public class ModbusTcpClient : IModbusClient, IDisposable
    {
        private TcpClient? _tcpClient;
        private ModbusIpMaster? _master;
        private byte _slaveId = 1;
        private bool _disposed;

        public string DeviceId { get; }
        public bool IsConnected => _tcpClient?.Connected ?? false;
        public ModbusTcpClient(string deviceId)
        {
            DeviceId = deviceId;
        }
        public async Task<bool> ConnectAsync(string ipAddress, int port)
        {
            try
            {
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(ipAddress, port);

                // NModbus4 创建方式
                _master = ModbusIpMaster.CreateIp(_tcpClient);
                _master.Transport.Retries = 3;
                _master.Transport.ReadTimeout = 3000;
                _master.Transport.WriteTimeout = 3000;

                return true;
            }
            catch
            {
                return false;
            }
        }
        public Task DisconnectAsync()
        {
            Dispose();
            return Task.CompletedTask;
        }
        // ========== 读取（同步方法，包装为异步）==========
        public Task<ushort[]> ReadHoldingRegistersAsync(ushort startAddress, ushort count)
        {
            EnsureConnected();
            var result = _master!.ReadHoldingRegisters(_slaveId, startAddress, count);
            return Task.FromResult(result);
        }
        public Task<ushort[]> ReadInputRegistersAsync(ushort startAddress, ushort count)
        {
            EnsureConnected();
            var result = _master!.ReadInputRegisters(_slaveId, startAddress, count);
            return Task.FromResult(result);
        }
        public Task<bool[]> ReadCoilsAsync(ushort startAddress, ushort count)
        {
            EnsureConnected();
            var result = _master!.ReadCoils(_slaveId, startAddress, count);
            return Task.FromResult(result);
        }
        public Task<bool[]> ReadDiscreteInputsAsync(ushort startAddress, ushort count)
        {
            EnsureConnected();
            var result = _master!.ReadInputs(_slaveId, startAddress, count);
            return Task.FromResult(result);
        }
        // ========== 写入 ==========
        public Task WriteSingleCoilAsync(ushort address, bool value)
        {
            EnsureConnected();
            _master!.WriteSingleCoil(_slaveId, address, value);
            return Task.CompletedTask;
        }
        public Task WriteSingleRegisterAsync(ushort address, ushort value)
        {
            EnsureConnected();
            _master!.WriteSingleRegister(_slaveId, address, value);
            return Task.CompletedTask;
        }
        public Task WriteMultipleCoilsAsync(ushort startAddress, bool[] values)
        {
            EnsureConnected();
            _master!.WriteMultipleCoils(_slaveId, startAddress, values);
            return Task.CompletedTask;
        }
        public Task WriteMultipleRegistersAsync(ushort startAddress, ushort[] values)
        {
            EnsureConnected();
            _master!.WriteMultipleRegisters(_slaveId, startAddress, values);
            return Task.CompletedTask;
        }
        private void EnsureConnected()
        {
            if (_master == null || !IsConnected)
                throw new InvalidOperationException("未连接到设备");
        }
        public void SetSlaveId(byte slaveId)
        {
            _slaveId = slaveId;
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _master?.Dispose();
            _tcpClient?.Dispose();
            _master = null;
            _tcpClient = null;
        }
    }
}

