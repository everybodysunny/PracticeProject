using PracticeProject.Iservices;
using PracticeProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracticeProject.Services
{
    public class ModbusPollingService : IModbusPollingService, IDisposable
    {
        private readonly IModbusClientFactory _clientFactory;
        private readonly IRetryPolicy _retryPolicy;
        private readonly IEventAggregator _eventAggregator;
        private readonly Dictionary<string, ModbusDeviceConfig> _configs;

        private readonly Dictionary<string, IModbusDevicePoller> _pollers = new();
        private readonly Dictionary<string, DeviceDataSnapshot> _allDeviceData = new();
        private bool _disposed;

        public IReadOnlyDictionary<string, DeviceDataSnapshot> AllDeviceData => _allDeviceData;
        public IReadOnlyDictionary<string, ModbusDeviceConfig> AllDeviceConfigs => _configs;

        public ModbusPollingService(
            Dictionary<string, ModbusDeviceConfig> configs,
            IModbusClientFactory clientFactory,
            IRetryPolicy retryPolicy,
            IEventAggregator eventAggregator)
        {
            _configs = configs;
            _clientFactory = clientFactory;
            _retryPolicy = retryPolicy;
            _eventAggregator = eventAggregator;
        }

        public async Task StartAsync()
        {
            foreach (var config in _configs.Values.Where(c => c.IsEnabled))
            {
                var poller = CreatePoller(config);
                _pollers[config.DeviceId] = poller;

                // 注册数据回调：更新缓存
                poller.OnDataReceived = data => UpdateDeviceData(config.DeviceId, data);
                poller.OnErrorReceived = ex => UpdateDeviceError(config.DeviceId, ex);

                await poller.StartAsync();
            }
        }

        public async Task StopAsync()
        {
            foreach (var poller in _pollers.Values)
            {
                await poller.StopAsync();
            }
            _pollers.Clear();
        }

        public bool TryGetDeviceData(string deviceId, out DeviceDataSnapshot? data)
        {
            return _allDeviceData.TryGetValue(deviceId, out data);
        }

        public async Task<DeviceDataSnapshot?> ReadSingleDeviceAsync(string deviceId)
        {
            if (_pollers.TryGetValue(deviceId, out var poller))
            {
                var data = await poller.ReadOnceAsync();
                if (data != null)
                {
                    UpdateDeviceData(deviceId, data);
                }
                return data;
            }
            return null;
        }

        public IModbusDevicePoller? GetPoller(string deviceId)
        {
            _pollers.TryGetValue(deviceId, out var poller);
            return poller;
        }

        private IModbusDevicePoller CreatePoller(ModbusDeviceConfig config)
        {
            return new ModbusDevicePoller(
                config,
                _clientFactory,
                _retryPolicy,
                _eventAggregator);
        }

        private void UpdateDeviceData(string deviceId, DeviceDataSnapshot data)
        {
            _allDeviceData[deviceId] = data;
        }

        private void UpdateDeviceError(string deviceId, Exception ex)
        {
            if (_allDeviceData.TryGetValue(deviceId, out var snapshot))
            {
                snapshot.IsSuccess = false;
                snapshot.ErrorMessage = ex.Message;
                snapshot.Timestamp = DateTime.Now;
            }
            else
            {
                _allDeviceData[deviceId] = new DeviceDataSnapshot
                {
                    DeviceId = deviceId,
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    Timestamp = DateTime.Now
                };
            }
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

            foreach (var poller in _pollers.Values)
            {
                if (poller is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _pollers.Clear();
        }
    }
}
