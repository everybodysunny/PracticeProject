using PracticeProject.EventServices;
using PracticeProject.Iservices;
using PracticeProject.Models;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace PracticeProject.ViewModels
{
    /// <summary>
    /// PLC 通讯 ViewModel
    /// 负责连接配置、轮询控制、写入寄存器、显示数据
    /// </summary>
    public class PlcConnectionViewModel : BindableBase, IDisposable
    {
        private readonly IModbusPollingService _pollingService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Dictionary<string, ModbusDeviceConfig> _deviceConfigs;
        private readonly Dispatcher _dispatcher;

        private SubscriptionToken? _dataToken;
        private SubscriptionToken? _errorToken;
        private bool _disposed;

        #region 状态属性

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (SetProperty(ref _isRunning, value))
                {
                    RaisePropertyChanged(nameof(IsStopped));
                    RaisePropertyChanged(nameof(StatusText));
                }
            }
        }

        public bool IsStopped => !IsRunning;

        private string _statusText = "未启动";
        public string StatusText => IsRunning ? "运行中" : _statusText;

        private string _lastError = string.Empty;
        public string LastError
        {
            get => _lastError;
            set => SetProperty(ref _lastError, value);
        }

        private int _successCount;
        public int SuccessCount
        {
            get => _successCount;
            set => SetProperty(ref _successCount, value);
        }

        private int _errorCount;
        public int ErrorCount
        {
            get => _errorCount;
            set => SetProperty(ref _errorCount, value);
        }

        private DateTime? _lastUpdateTime;
        public DateTime? LastUpdateTime
        {
            get => _lastUpdateTime;
            set
            {
                if (SetProperty(ref _lastUpdateTime, value))
                {
                    RaisePropertyChanged(nameof(LastUpdateText));
                }
            }
        }

        public string LastUpdateText =>
            LastUpdateTime.HasValue ? LastUpdateTime.Value.ToString("HH:mm:ss") : "--:--:--";

        #endregion

        #region 写入测试

        private string _writeAddress = "0";
        public string WriteAddress
        {
            get => _writeAddress;
            set => SetProperty(ref _writeAddress, value);
        }

        private string _writeValue = "0";
        public string WriteValue
        {
            get => _writeValue;
            set => SetProperty(ref _writeValue, value);
        }

        private string _selectedDevice = "PLC_1";
        public string SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (SetProperty(ref _selectedDevice, value))
                {
                    LoadDeviceData();
                }
            }
        }

        #endregion

        #region 集合

        public ObservableCollection<string> AvailableDevices { get; } = new();
        public ObservableCollection<RegisterDisplayItem> RegisterItems { get; } = new();
        public ObservableCollection<string> LogMessages { get; } = new();

        #endregion

        #region 命令

        public AsyncDelegateCommand StartCommand { get; }
        public AsyncDelegateCommand StopCommand { get; }
        public AsyncDelegateCommand RefreshCommand { get; }
        public AsyncDelegateCommand WriteCommand { get; }
        public AsyncDelegateCommand ClearLogCommand { get; }

        #endregion

        public PlcConnectionViewModel(
            IModbusPollingService pollingService,
            IEventAggregator eventAggregator,
            Dictionary<string, ModbusDeviceConfig> deviceConfigs)
        {
            _pollingService = pollingService;
            _eventAggregator = eventAggregator;
            _deviceConfigs = deviceConfigs;
            _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

            // 加载设备列表
            foreach (var config in deviceConfigs.Values)
            {
                AvailableDevices.Add(config.DeviceId);
            }

            // 初始化命令
            StartCommand = new AsyncDelegateCommand(StartAsync, () => IsStopped);
            StopCommand = new AsyncDelegateCommand(StopAsync, () => IsRunning);
            RefreshCommand = new AsyncDelegateCommand(RefreshAsync, () => IsRunning);
            WriteCommand = new AsyncDelegateCommand(WriteRegisterAsync, () => IsRunning);
            ClearLogCommand = new AsyncDelegateCommand(ClearLogAsync);

            // 订阅事件
            SubscribeEvents();

            // 加载初始数据
            LoadDeviceData();
            AddLog("系统", "PLC 通讯 ViewModel 已初始化");
        }

        #region 命令实现

        private async Task StartAsync()
        {
            try
            {
                AddLog("系统", "正在启动轮询服务...");
                await _pollingService.StartAsync();
                IsRunning = true;
                _statusText = "运行中";
                RaisePropertyChanged(nameof(StatusText));
                AddLog("成功", "轮询服务已启动");
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                ErrorCount++;
                AddLog("错误", $"启动失败: {ex.Message}");
            }
        }

        private async Task StopAsync()
        {
            try
            {
                AddLog("系统", "正在停止轮询服务...");
                await _pollingService.StopAsync();
                IsRunning = false;
                _statusText = "已停止";
                RaisePropertyChanged(nameof(StatusText));
                AddLog("成功", "轮询服务已停止");
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                AddLog("错误", $"停止失败: {ex.Message}");
            }
        }

        private async Task RefreshAsync()
        {
            try
            {
                AddLog("系统", $"手动刷新设备 {SelectedDevice} 数据...");
                var data = await _pollingService.ReadSingleDeviceAsync(SelectedDevice);
                if (data != null)
                {
                    LastUpdateTime = data.Timestamp;
                    UpdateRegisterDisplay(data);
                    AddLog("成功", "数据已刷新");
                }
            }
            catch (Exception ex)
            {
                AddLog("错误", $"刷新失败: {ex.Message}");
            }
        }

        private async Task WriteRegisterAsync()
        {
            if (!ushort.TryParse(WriteAddress, out var address))
            {
                AddLog("错误", "写入地址格式错误，应为 0-65535 的整数");
                return;
            }

            if (!ushort.TryParse(WriteValue, out var value))
            {
                AddLog("错误", "写入值格式错误，应为 0-65535 的整数");
                return;
            }

            try
            {
                AddLog("写入", $"向 {SelectedDevice} 地址 {address} 写入值 {value}");
                var poller = GetPoller(SelectedDevice);
                if (poller != null)
                {
                    await poller.WriteRegisterAsync(address, value);
                    AddLog("成功", $"写入完成: 地址={address}, 值={value}");
                }
                else
                {
                    AddLog("错误", $"未找到设备 {SelectedDevice} 的轮询器");
                }
            }
            catch (Exception ex)
            {
                AddLog("错误", $"写入失败: {ex.Message}");
            }
        }

        private Task ClearLogAsync()
        {
            LogMessages.Clear();
            return Task.CompletedTask;
        }

        #endregion

        #region 事件订阅

        private void SubscribeEvents()
        {
            _dataToken = _eventAggregator
                .GetEvent<DeviceDataUpdatedEvent>()
                .Subscribe(OnDeviceDataReceived, ThreadOption.UIThread);

            _errorToken = _eventAggregator
                .GetEvent<DeviceErrorEvent>()
                .Subscribe(OnDeviceErrorReceived, ThreadOption.UIThread);
        }

        private void OnDeviceDataReceived(DeviceDataEventArgs args)
        {
            try
            {
                if (args.DeviceId != SelectedDevice) return;
                if (args.Data == null) return;

                _dispatcher.Invoke(() =>
                {
                    LastUpdateTime = args.Data.Timestamp;
                    SuccessCount++;
                    UpdateRegisterDisplay(args.Data);
                });
            }
            catch (Exception ex)
            {
                AddLog("错误", $"处理数据事件异常: {ex.Message}");
            }
        }

        private void OnDeviceErrorReceived(DeviceErrorEventArgs args)
        {
            try
            {
                _dispatcher.Invoke(() =>
                {
                    ErrorCount++;
                    LastError = args.Exception?.Message ?? "未知错误";
                    AddLog("错误", $"[{args.Timestamp:HH:mm:ss}] {args.DeviceId}: {args.Exception?.Message}");

                    if (args.DeviceId == SelectedDevice)
                    {
                        // 当前设备错误，更新显示
                        LoadDeviceData();
                    }
                });
            }
            catch
            {
                // 忽略
            }
        }

        #endregion

        #region 辅助方法

        private IModbusDevicePoller? GetPoller(string deviceId)
        {
            return _pollingService.GetPoller(deviceId);
        }

        private void LoadDeviceData()
        {
            RegisterItems.Clear();

            if (!_deviceConfigs.TryGetValue(SelectedDevice, out var config))
                return;

            foreach (var reg in config.Registers)
            {
                RegisterItems.Add(new RegisterDisplayItem
                {
                    Name = reg.Name,
                    StartAddress = reg.StartAddress,
                    Count = reg.Count,
                    DisplayValue = "--",
                    HexValue = "--",
                    LastUpdate = null
                });
            }

            // 尝试从 Service 加载已有数据
            if (_pollingService.TryGetDeviceData(SelectedDevice, out var data) && data != null)
            {
                LastUpdateTime = data.Timestamp;
                UpdateRegisterDisplay(data);
            }
        }

        private void UpdateRegisterDisplay(DeviceDataSnapshot data)
        {
            if (!_deviceConfigs.TryGetValue(SelectedDevice, out var config))
                return;

            RegisterItems.Clear();

            foreach (var reg in config.Registers)
            {
                var item = new RegisterDisplayItem
                {
                    Name = reg.Name,
                    StartAddress = reg.StartAddress,
                    Count = reg.Count,
                    LastUpdate = data.Timestamp,
                    IsSuccess = data.IsSuccess
                };

                if (data.IsSuccess && data.Registers.TryGetValue(reg.Name, out var values) && values != null)
                {
                    item.DisplayValue = values.Length == 1
                        ? values[0].ToString()
                        : string.Join(", ", values);
                    item.HexValue = values.Length == 1
                        ? $"0x{values[0]:X4}"
                        : string.Join(", ", values.Select(v => $"0x{v:X4}"));
                }
                else
                {
                    item.DisplayValue = data.IsSuccess ? "--" : "读取失败";
                    item.HexValue = data.IsSuccess ? "--" : "--";
                }

                RegisterItems.Add(item);
            }
        }

        private void AddLog(string level, string message)
        {
            _dispatcher.Invoke(() =>
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                LogMessages.Insert(0, $"[{timestamp}] [{level}] {message}");

                while (LogMessages.Count > 200)
                {
                    LogMessages.RemoveAt(LogMessages.Count - 1);
                }
            });
        }

        #endregion

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_dataToken != null)
            {
                _eventAggregator.GetEvent<DeviceDataUpdatedEvent>().Unsubscribe(_dataToken);
            }
            if (_errorToken != null)
            {
                _eventAggregator.GetEvent<DeviceErrorEvent>().Unsubscribe(_errorToken);
            }
        }
    }

    /// <summary>
    /// 寄存器显示项
    /// </summary>
    public class RegisterDisplayItem : BindableBase
    {
        private string _name = string.Empty;
        private ushort _startAddress;
        private ushort _count;
        private string _displayValue = "--";
        private string _hexValue = "--";
        private DateTime? _lastUpdate;
        private bool _isSuccess = true;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public ushort StartAddress
        {
            get => _startAddress;
            set => SetProperty(ref _startAddress, value);
        }

        public ushort Count
        {
            get => _count;
            set => SetProperty(ref _count, value);
        }

        public string DisplayValue
        {
            get => _displayValue;
            set => SetProperty(ref _displayValue, value);
        }

        public string HexValue
        {
            get => _hexValue;
            set => SetProperty(ref _hexValue, value);
        }

        public DateTime? LastUpdate
        {
            get => _lastUpdate;
            set
            {
                if (SetProperty(ref _lastUpdate, value))
                {
                    RaisePropertyChanged(nameof(LastUpdateText));
                }
            }
        }

        public string LastUpdateText =>
            LastUpdate.HasValue ? LastUpdate.Value.ToString("HH:mm:ss.fff") : "--:--:--";

        public bool IsSuccess
        {
            get => _isSuccess;
            set
            {
                if (SetProperty(ref _isSuccess, value))
                {
                    RaisePropertyChanged(nameof(StatusText));
                }
            }
        }

        public string StatusText => IsSuccess ? "正常" : "失败";
    }
}
