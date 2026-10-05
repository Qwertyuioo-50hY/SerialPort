using System;
namespace Tomography.Business.Models.Enums
{
    /// <summary>
    /// 校验类型。
    /// </summary>
    public enum ChecksumType
    {
        /// <summary>
        /// 不用校验。
        /// </summary>
        None = 0,
        /// <summary>
        /// 异或校验（Block Check Character），也称为模256校验，是一种简单的校验方法，通过对数据块中的所有字节进行异或运算得到一个校验值。接收方可以使用相同的方法计算校验值，并与发送方提供的校验值进行比较，以验证数据的完整性。
        /// </summary>
        Bcc = 1,
        /// <summary>
        /// 累加和校验（Sum Check），也称为模256累加和校验，是一种简单的校验方法，通过对数据块中的所有字节进行累加运算得到一个校验值。接收方可以使用相同的方法计算校验值，并与发送方提供的校验值进行比较，以验证数据的完整性。
        /// </summary>
        Sum = 2,
        /// <summary>
        /// 长度冗余校验（Longitudinal Redundancy Check），是一种通过对数据块中的所有字节进行按位累加运算得到一个校验值的方法。接收方可以使用相同的方法计算校验值，并与发送方提供的校验值进行比较，以验证数据的完整性。
        /// </summary>
        Lrc = 3,
        /// <summary>
        /// CRC16 校验（Cyclic Redundancy Check），是一种通过对数据块中的所有字节进行多项式运算得到一个校验值的方法。接收方可以使用相同的方法计算校验值，并与发送方提供的校验值进行比较，以验证数据的完整性。
        /// </summary>
        Crc16 = 4
    }
}
