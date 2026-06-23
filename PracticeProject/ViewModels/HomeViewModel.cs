using PracticeProject.EventServices;
using PracticeProject.Iservices;
using PracticeProject.Models;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace PracticeProject.ViewModels
{
    /// <summary>
    /// 主页 ViewModel - 只做数据显示
    /// </summary>
    public class HomeViewModel : BindableBase, IDisposable
    {
        private readonly IModbusPollingService _pollingService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Dispatcher _dispatcher;

        private SubscriptionToken? _dataToken;
        private SubscriptionToken? _errorToken;
        private bool _disposed;

        #region 汇总信息

        private int _totalDevices;
        public int TotalDevices
        {
            get => _totalDevices;
            set
            {
                if (SetProperty(ref _totalDevices, value))
                {
                    RaisePropertyChanged(nameof(OverallStatusText));
                }
            }
        }

        private int _onlineDevices;
        public int OnlineDevices
        {
            get => _onlineDevices;
            set
            {
                if (SetProperty(ref _onlineDevices, value))
                {
                    RaisePropertyChanged(nameof(OverallStatusText));
                }
            }
        }

        private int _offlineDevices;
        public int OfflineDevices
        {
            get => _offlineDevices;
            set => SetProperty(ref _offlineDevices, value);
        }

        public string OverallStatusText => $"{OnlineDevices}/{TotalDevices} 设备在线";

        private long _totalReads;
        public long TotalReads
        {
            get => _totalReads;
            set
            {
                if (SetProperty(ref _totalReads, value))
                {
                    RaisePropertyChanged(nameof(TotalReadsText));
                }
            }
        }

        public string TotalReadsText => $"{TotalReads:N0} 次";

        private long _totalErrors;
        public long TotalErrors
        {
            get => _totalErrors;
            set
            {
                if (SetProperty(ref _totalErrors, value))
                {
                    RaisePropertyChanged(nameof(TotalErrorsText));
                    RaisePropertyChanged(nameof(SystemHealthText));
                }
            }
        }

        public string TotalErrorsText => $"{TotalErrors:N0} 次";

        public string SystemHealthText
        {
            get
            {
                if (TotalReads == 0) return "无数据";
                var rate = 1.0 - (double)TotalErrors / TotalReads;
                return $"{rate * 100:F1}%";
            }
        }

        private DateTime? _lastUpdate;
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
            LastUpdate.HasValue ? LastUpdate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "暂无数据";

        #endregion

        #region 集合

        public ObservableCollection<DeviceCardViewModel> DeviceCards { get; } = new();
        public ObservableCollection<ActivityItem> RecentActivities { get; } = new();

        #endregion

        public HomeViewModel(
            IModbusPollingService pollingService,
            IEventAggregator eventAggregator)
        {
            _pollingService = pollingService;
            _eventAggregator = eventAggregator;
            _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

            InitializeDeviceCards();

            // 订阅事件
            _dataToken = _eventAggregator
                .GetEvent<DeviceDataUpdatedEvent>()
                .Subscribe(OnDataReceived, ThreadOption.UIThread);

            _errorToken = _eventAggregator
                .GetEvent<DeviceErrorEvent>()
                .Subscribe(OnErrorReceived, ThreadOption.UIThread);

            // 加载已有数据
            LoadCachedData();
        }

        #region 初始化

        private void InitializeDeviceCards()
        {
            DeviceCards.Clear();
            TotalDevices = _pollingService.AllDeviceConfigs.Count;

            foreach (var config in _pollingService.AllDeviceConfigs.Values)
            {
                DeviceCards.Add(new DeviceCardViewModel
                {
                    DeviceId = config.DeviceId,
                    DeviceName = config.DeviceId,
                    IpAddress = config.IpAddress,
                    Port = config.Port,
                    PollingIntervalMs = config.PollingIntervalMs,
                    IsEnabled = config.IsEnabled,
                    IsOnline = false,
                    LastUpdate = null,
                    RegisterCount = config.Registers.Count
                });
            }
        }

        private void LoadCachedData()
        {
            foreach (var kvp in _pollingService.AllDeviceData)
            {
                UpdateDeviceCard(kvp.Key, kvp.Value);
            }
        }

        #endregion

        #region 事件处理

        private void OnDataReceived(DeviceDataEventArgs args)
        {
            _dispatcher.Invoke(() =>
            {
                if (args.Data == null) return;

                TotalReads++;
                LastUpdate = args.Data.Timestamp;
                UpdateDeviceCard(args.DeviceId, args.Data);

                AddActivity("数据更新", $"{args.DeviceId} 数据已更新", args.Data.Timestamp, true);
            });
        }

        private void OnErrorReceived(DeviceErrorEventArgs args)
        {
            _dispatcher.Invoke(() =>
            {
                TotalErrors++;
                UpdateDeviceCardError(args.DeviceId, args.Exception);

                AddActivity("通信错误",
                    $"{args.DeviceId}: {args.Exception?.Message ?? "未知错误"}",
                    args.Timestamp,
                    false);
            });
        }

        private void UpdateDeviceCard(string deviceId, DeviceDataSnapshot data)
        {
            var card = DeviceCards.FirstOrDefault(c => c.DeviceId == deviceId);
            if (card == null) return;

            card.IsOnline = data.IsSuccess;
            card.LastUpdate = data.Timestamp;
            card.LastError = data.ErrorMessage;
            card.RegisterValues.Clear();

            if (data.IsSuccess && data.Registers != null)
            {
                foreach (var kvp in data.Registers)
                {
                    var displayValue = kvp.Value != null && kvp.Value.Length > 0
                        ? (kvp.Value.Length == 1
                            ? kvp.Value[0].ToString()
                            : string.Join(", ", kvp.Value))
                        : "--";
                    card.RegisterValues.Add(new RegisterValueItem
                    {
                        Name = kvp.Key,
                        Value = displayValue
                    });
                }
            }

            // 重新计算在线数量
            OnlineDevices = DeviceCards.Count(c => c.IsOnline);
            OfflineDevices = DeviceCards.Count(c => !c.IsOnline);
        }

        private void UpdateDeviceCardError(string deviceId, Exception? ex)
        {
            var card = DeviceCards.FirstOrDefault(c => c.DeviceId == deviceId);
            if (card == null) return;

            card.IsOnline = false;
            card.LastError = ex?.Message ?? "未知错误";

            OnlineDevices = DeviceCards.Count(c => c.IsOnline);
            OfflineDevices = DeviceCards.Count(c => !c.IsOnline);
        }

        private void AddActivity(string type, string message, DateTime time, bool isSuccess)
        {
            RecentActivities.Insert(0, new ActivityItem
            {
                Type = type,
                Message = message,
                Timestamp = time,
                IsSuccess = isSuccess,
                TimestampText = time.ToString("HH:mm:ss")
            });

            // 限制活动数量
            while (RecentActivities.Count > 50)
            {
                RecentActivities.RemoveAt(RecentActivities.Count - 1);
            }
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
    /// 设备卡片 ViewModel（用于主页显示）
    /// </summary>
    public class DeviceCardViewModel : BindableBase
    {
        private string _deviceId = string.Empty;
        public string DeviceId
        {
            get => _deviceId;
            set => SetProperty(ref _deviceId, value);
        }

        private string _deviceName = string.Empty;
        public string DeviceName
        {
            get => _deviceName;
            set => SetProperty(ref _deviceName, value);
        }

        private string _ipAddress = string.Empty;
        public string IpAddress
        {
            get => _ipAddress;
            set => SetProperty(ref _ipAddress, value);
        }

        private int _port;
        public int Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        private int _pollingIntervalMs;
        public int PollingIntervalMs
        {
            get => _pollingIntervalMs;
            set
            {
                if (SetProperty(ref _pollingIntervalMs, value))
                {
                    RaisePropertyChanged(nameof(PollingIntervalText));
                }
            }
        }

        public string PollingIntervalText => $"{PollingIntervalMs} ms";

        private int _registerCount;
        public int RegisterCount
        {
            get => _registerCount;
            set => SetProperty(ref _registerCount, value);
        }

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (SetProperty(ref _isEnabled, value))
                {
                    RaisePropertyChanged(nameof(EnabledText));
                }
            }
        }

        public string EnabledText => IsEnabled ? "已启用" : "已禁用";

        private bool _isOnline;
        public bool IsOnline
        {
            get => _isOnline;
            set
            {
                if (SetProperty(ref _isOnline, value))
                {
                    RaisePropertyChanged(nameof(StatusText));
                    RaisePropertyChanged(nameof(StatusBrush));
                }
            }
        }

        public string StatusText => IsOnline ? "在线" : "离线";

        public Brush StatusBrush =>
            IsOnline ? new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)) : new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36));

        private DateTime? _lastUpdate;
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
            LastUpdate.HasValue ? LastUpdate.Value.ToString("HH:mm:ss") : "--";

        private string? _lastError;
        public string? LastError
        {
            get => _lastError;
            set => SetProperty(ref _lastError, value);
        }

        public ObservableCollection<RegisterValueItem> RegisterValues { get; } = new();
    }

    /// <summary>
    /// 寄存器值显示项
    /// </summary>
    public class RegisterValueItem : BindableBase
    {
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _value = "--";
        public string Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }

    /// <summary>
    /// 活动记录
    /// </summary>
    public class ActivityItem : BindableBase
    {
        private string _type = string.Empty;
        public string Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        private string _message = string.Empty;
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        private DateTime _timestamp;
        public DateTime Timestamp
        {
            get => _timestamp;
            set => SetProperty(ref _timestamp, value);
        }

        private string _timestampText = string.Empty;
        public string TimestampText
        {
            get => _timestampText;
            set => SetProperty(ref _timestampText, value);
        }

        private bool _isSuccess = true;
        public bool IsSuccess
        {
            get => _isSuccess;
            set
            {
                if (SetProperty(ref _isSuccess, value))
                {
                    RaisePropertyChanged(nameof(TypeBrush));
                }
            }
        }

        public Brush TypeBrush =>
            IsSuccess ? new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)) : new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36));
    }
}
