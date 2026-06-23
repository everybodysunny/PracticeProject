using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracticeProject.Models
{
    public enum FunctionCode:byte
    {
        // ========== 读取 ==========

        /// <summary>读取线圈 (功能码 1)</summary>
        ReadCoils = 1,

        /// <summary>读取离散输入 (功能码 2)</summary>
        ReadDiscreteInputs = 2,

        /// <summary>读取保持寄存器 (功能码 3) - 最常用</summary>
        ReadHoldingRegisters = 3,

        /// <summary>读取输入寄存器 (功能码 4)</summary>
        ReadInputRegisters = 4,

        // ========== 写入 ==========

        /// <summary>写单个线圈 (功能码 5)</summary>
        WriteSingleCoil = 5,

        /// <summary>写单个保持寄存器 (功能码 6)</summary>
        WriteSingleRegister = 6,

        /// <summary>写多个线圈 (功能码 15)</summary>
        WriteMultipleCoils = 15,

        /// <summary>写多个保持寄存器 (功能码 16)</summary>
        WriteMultipleRegisters = 16,
    }
}

