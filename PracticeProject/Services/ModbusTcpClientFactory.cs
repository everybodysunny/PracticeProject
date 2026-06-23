using PracticeProject.Iservices;
using PracticeProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracticeProject.Services
{
    public class ModbusTcpClientFactory:IModbusClientFactory
    {
        public IModbusClient CreateClient(ModbusDeviceConfig config)
        {
            var client = new ModbusTcpClient(config.DeviceId);
            client.SetSlaveId(config.SlaveId);
            return client;
        }
    }
}
