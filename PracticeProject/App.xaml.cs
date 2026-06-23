using PracticeProject.Iservices;
using PracticeProject.Models;
using PracticeProject.Services;
using PracticeProject.ViewModels;
using PracticeProject.Views;
using Prism.DryIoc;
using System.Configuration;
using System.Data;
using System.Windows;

namespace PracticeProject
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
           return Container.Resolve<MainView>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // ============================================================
            // 1. 基础设施服务
            // ============================================================

            // 重试策略
            containerRegistry.Register<IRetryPolicy, ExponentialBackoffRetryPolicy>();

            // 客户端工厂
            containerRegistry.RegisterSingleton<IModbusClientFactory, ModbusTcpClientFactory>();

            // Prism EventAggregator（自动注册）
            containerRegistry.RegisterInstance<IEventAggregator>(Container.Resolve<IEventAggregator>());

            // ============================================================
            // 2. 设备配置
            // ============================================================

            var deviceConfigs = GetDeviceConfigs();
            containerRegistry.RegisterInstance<Dictionary<string, ModbusDeviceConfig>>(deviceConfigs);

            // ============================================================
            // 3. 轮询服务（单例）
            // ============================================================

            containerRegistry.RegisterSingleton<IModbusPollingService, ModbusPollingService>();

            // ============================================================
            // 4. 导航注册
            // ============================================================

            containerRegistry.RegisterForNavigation<HomeView, HomeViewModel>();
            containerRegistry.RegisterForNavigation<PlcConnectionView, PlcConnectionViewModel>();
            containerRegistry.RegisterForNavigation<SqlConnectionView, SqlConnectionViewModel>();
            containerRegistry.RegisterForNavigation<PlcSettingPage,PlcSettingViewModel>();
        }

        /// <summary>
        /// 获取设备配置
        /// </summary>
        private Dictionary<string, ModbusDeviceConfig> GetDeviceConfigs()
        {
            return new Dictionary<string, ModbusDeviceConfig>
            {
                ["PLC_1"] = new ModbusDeviceConfig
                {
                    DeviceId = "PLC_1",
                    IpAddress = "192.168.1.100",
                    Port = 502,
                    SlaveId = 1,
                    PollingIntervalMs = 1000,
                    IsEnabled = true,
                    Registers = new List<RegisterMapping>
                    {
                        new() { Name = "温度", StartAddress = 0, Count = 2, FunctionCode = FunctionCode.ReadHoldingRegisters },
                        new() { Name = "压力", StartAddress = 10, Count = 2, FunctionCode = FunctionCode.ReadHoldingRegisters },
                        new() { Name = "流量", StartAddress = 20, Count = 1, FunctionCode = FunctionCode.ReadHoldingRegisters },
                    }
                },

                ["PLC_2"] = new ModbusDeviceConfig
                {
                    DeviceId = "PLC_2",
                    IpAddress = "192.168.1.101",
                    Port = 502,
                    SlaveId = 1,
                    PollingIntervalMs = 500,
                    IsEnabled = true,
                    Registers = new List<RegisterMapping>
                    {
                        new() { Name = "速度", StartAddress = 0, Count = 1, FunctionCode = FunctionCode.ReadHoldingRegisters },
                        new() { Name = "位置", StartAddress = 5, Count = 2, FunctionCode = FunctionCode.ReadHoldingRegisters },
                    }
                },

                ["PLC_3"] = new ModbusDeviceConfig
                {
                    DeviceId = "PLC_3",
                    IpAddress = "192.168.1.102",
                    Port = 502,
                    SlaveId = 1,
                    PollingIntervalMs = 2000,
                    IsEnabled = false,
                    Registers = new List<RegisterMapping>()
                }
            };
        }
    }
}
