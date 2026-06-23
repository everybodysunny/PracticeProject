using PracticeProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracticeProject.EventServices
{
    public class DeviceDataUpdatedEvent:PubSubEvent<DeviceDataEventArgs>
    {

    }
}
