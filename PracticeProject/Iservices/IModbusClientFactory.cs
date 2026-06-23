using PracticeProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracticeProject.Iservices
{ 
    public interface IModbusClientFactory
    {
        /// <summary>
        /// 根据配置创建设备客户端
        /// </summary>
        IModbusClient CreateClient(ModbusDeviceConfig config);
    }
}
