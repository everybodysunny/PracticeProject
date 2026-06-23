using PracticeProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracticeProject.EventServices
{
    /// <summary>
    /// 设备错误事件
    /// 当设备通信发生错误时发布
    /// </summary>
    public class DeviceErrorEvent : PubSubEvent<DeviceErrorEventArgs> { }
  
}
