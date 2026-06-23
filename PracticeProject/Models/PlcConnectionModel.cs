using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracticeProject.Models
{
    public  class PlcConnectionModel:BindableBase
    {
        //ip地址
        //端口号
        //西门子专用的机架号
        //西门子专用的槽号
        private string  _ipAddress="192.168.1.100";

		public string  ipAddress
		{
			get => _ipAddress;
            set => SetProperty(ref _ipAddress, value);
        }
        private int  _port=502;

        public int Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
        }
        //西门子专用的机架号
        private int _rack=0;
        public int rack
        {
           get => _rack;
           set=> SetProperty(ref _rack, value);
        }
        //西门子专用卡槽
        private int _slot=0;
        public int Slot
        {
            get => _slot;
            set => SetProperty(ref _slot, value);
        }

        //PLC类型选择 当选择Modbus TCP时，机架号和槽号不显示
        private string _selectedPlcType = "S7-1200/1500";
        public string SelectedPlcType
        {
            get => _selectedPlcType;
            set
            {
                if (SetProperty(ref _selectedPlcType, value))
                    RaisePropertyChanged(nameof(IsS7Protocol));
            }
        }

        public bool IsS7Protocol => SelectedPlcType != "Modbus TCP";



    }
}
