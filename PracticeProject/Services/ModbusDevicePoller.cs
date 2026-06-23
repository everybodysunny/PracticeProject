using PracticeProject.EventServices;
using PracticeProject.Iservices;
using PracticeProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracticeProject.Services
{
    /// <summary>
    /// 单设备轮询器
    /// </summary>
    public class ModbusDevicePoller : IModbusDevicePoller, IDisposable
    {
        private readonly IModbusClientFactory _clientFactory;
        private readonly IRetryPolicy _retryPolicy;
        private readonly IEventAggregator _eventAggregator;

        private IModbusClient? _client;
        private CancellationTokenSource? _cts;
        private bool _disposed;

        public string DeviceId => Config.DeviceId;
        public ModbusDeviceConfig Config { get; }
        public bool IsRunning { get; private set; }
        public Action<DeviceDataSnapshot>? OnDataReceived { get; set; }
        public Action<Exception>? OnErrorReceived { get; set; }

        public ModbusDevicePoller(
            ModbusDeviceConfig config,
            IModbusClientFactory clientFactory,
            IRetryPolicy retryPolicy,
            IEventAggregator eventAggregator)
        {
            Config = config;
            _clientFactory = clientFactory;
            _retryPolicy = retryPolicy;
            _eventAggregator = eventAggregator;
        }

        public async Task StartAsync()
        {
            if (IsRunning) return;

            _cts = new CancellationTokenSource();
            IsRunning = true;

            try
            {
                await ConnectAsync();
                _ = PollLoopAsync(_cts.Token);
            }
            catch (Exception ex)
            {
                IsRunning = false;
                PublishError(ex);
                OnErrorReceived?.Invoke(ex);
            }
        }

        public async Task StopAsync()
        {
            if (!IsRunning) return;

            _cts?.Cancel();
            IsRunning = false;

            if (_client != null)
            {
                try
                {
                    await _client.DisconnectAsync();
                }
                catch
                {
                    // 断开时忽略错误
                }
            }
        }

        public async Task<DeviceDataSnapshot> ReadOnceAsync()
        {
            if (_client == null || !_client.IsConnected)
            {
                await ConnectAsync();
            }

            return await ReadAllRegistersAsync();
        }

        public async Task WriteRegisterAsync(ushort address, ushort value)
        {
            EnsureConnected();
            await _retryPolicy.ExecuteAsync(() =>
                _client!.WriteSingleRegisterAsync(address, value));
        }

        public async Task WriteRegistersAsync(ushort startAddress, ushort[] values)
        {
            EnsureConnected();
            await _retryPolicy.ExecuteAsync(() =>
                _client!.WriteMultipleRegistersAsync(startAddress, values));
        }

        private async Task PollLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var data = await ReadAllRegistersAsync();
                    PublishData(data);
                    OnDataReceived?.Invoke(data);
                }
                catch (Exception ex)
                {
                    PublishError(ex);
                    OnErrorReceived?.Invoke(ex);
                }

                try
                {
                    await Task.Delay(Config.PollingIntervalMs, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private async Task ConnectAsync()
        {
            _client = _clientFactory.CreateClient(Config);
            var success = await _client.ConnectAsync(Config.IpAddress, Config.Port);

            if (!success)
            {
                throw new Exception($"无法连接到设备 {Config.IpAddress}:{Config.Port}");
            }
        }

        private async Task<DeviceDataSnapshot> ReadAllRegistersAsync()
        {
            var result = new DeviceDataSnapshot
            {
                DeviceId = DeviceId,
                Timestamp = DateTime.Now,
                Registers = new Dictionary<string, ushort[]>(),
                IsSuccess = false
            };

            await _retryPolicy.ExecuteAsync(async () =>
            {
                var allData = new List<ushort>();

                foreach (var reg in Config.Registers)
                {
                    ushort[] data = reg.FunctionCode switch
                    {
                        FunctionCode.ReadHoldingRegisters =>
                            await _client!.ReadHoldingRegistersAsync(reg.StartAddress, reg.Count),
                        FunctionCode.ReadInputRegisters =>
                            await _client!.ReadInputRegistersAsync(reg.StartAddress, reg.Count),
                        FunctionCode.ReadCoils =>
                            Array.Empty<ushort>(),
                        FunctionCode.ReadDiscreteInputs =>
                            Array.Empty<ushort>(),
                        _ => await _client!.ReadHoldingRegistersAsync(reg.StartAddress, reg.Count)
                    };

                    result.Registers[reg.Name] = data;
                    allData.AddRange(data);
                }

                result.RawData = allData.ToArray();
                result.IsSuccess = true;
            });

            return result;
        }

        private void PublishData(DeviceDataSnapshot data)
        {
            var args = new DeviceDataEventArgs
            {
                DeviceId = DeviceId,
                Data = data
            };

            _eventAggregator.GetEvent<DeviceDataUpdatedEvent>().Publish(args);
        }

        private void PublishError(Exception ex)
        {
            var args = new DeviceErrorEventArgs
            {
                DeviceId = DeviceId,
                Exception = ex,
                Timestamp = DateTime.Now
            };

            _eventAggregator.GetEvent<DeviceErrorEvent>().Publish(args);
        }

        private void EnsureConnected()
        {
            if (_client == null || !_client.IsConnected)
                throw new InvalidOperationException("未连接到设备");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                StopAsync().GetAwaiter().GetResult();
            }
            catch
            {
                // 关闭时忽略错误
            }

            _cts?.Dispose();
        }
    }
}
