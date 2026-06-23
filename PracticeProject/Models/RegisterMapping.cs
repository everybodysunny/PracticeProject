using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracticeProject.Models
{
    public class RegisterMapping
    {
        /// <summary>
        /// 寄存器名称，用于界面显示和字典 Key
        /// 例如：温度、压力、流量
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 起始地址（0-based）
        /// </summary>
        public ushort StartAddress { get; set; }

        /// <summary>
        /// 读取的寄存器数量
        /// </summary>
        public ushort Count { get; set; } = 1;

        /// <summary>
        /// 功能码，决定读取哪种寄存器
        /// </summary>
        public FunctionCode FunctionCode { get; set; } = FunctionCode.ReadHoldingRegisters;
    }
}
