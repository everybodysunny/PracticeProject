using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracticeProject.Models
{
    public class DeviceDataEventArgs
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// 数据快照
        /// </summary>
        public DeviceDataSnapshot? Data { get; set; }
    }
}
