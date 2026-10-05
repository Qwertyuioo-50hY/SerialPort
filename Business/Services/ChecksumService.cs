using System;
using Tomography.Business.Models.Enums;
using Tomography.Business.Interfaces;
using Tomography.Business.Helpers;

namespace Tomography.Business.Services
{
    /// <summary>
    /// 报文校验服务实现。
    /// </summary>
    public class ChecksumService : IChecksumService
    {
        /// <summary>
        /// 计算指定校验类型的校验字节。
        /// </summary>
        public byte[] ComputeChecksumBytes(byte[]? data, ChecksumType checksumType)
        {
            var payload = data ?? Array.Empty<byte>();

            return checksumType switch
            {
                ChecksumType.None => Array.Empty<byte>(),
                ChecksumType.Bcc => new[] { ComputeBcc(payload) },
                ChecksumType.Sum => new[] { ComputeSum(payload) },
                ChecksumType.Lrc => new[] { ComputeLrc(payload) },
                ChecksumType.Crc16 => BitConverter.GetBytes(ComputeCrc16(payload)),
                _ => throw new ArgumentOutOfRangeException(nameof(checksumType), checksumType, "未知校验类型。")
            };
        }

        /// <summary>
        /// 计算校验值并转换为十六进制字符串。
        /// </summary>
        public string ComputeChecksumHex(byte[]? data, ChecksumType checksumType, bool upperCase = true, string separator = " ")
        {
            var checksumBytes = ComputeChecksumBytes(data, checksumType);
            return HexConverter.ToHexString(checksumBytes, upperCase, separator);
        }

        /// <summary>
        /// 验证实际校验值与期望校验值是否一致。
        /// </summary>
        public bool ValidateChecksum(byte[]? data, ChecksumType checksumType, byte[]? expectedChecksum)
        {
            var actual = ComputeChecksumBytes(data, checksumType);
            var expected = expectedChecksum ?? Array.Empty<byte>();
            return actual.SequenceEqual(expected);
        }

        /// <summary>
        /// 计算 BCC（按字节异或）。
        /// </summary>
        private static byte ComputeBcc(ReadOnlySpan<byte> data)
        {
            byte checksum = 0;
            foreach (var b in data)
            {
                checksum ^= b;
            }

            return checksum;
        }

        /// <summary>
        /// 计算 SUM（字节累加取低 8 位）。
        /// </summary>
        private static byte ComputeSum(ReadOnlySpan<byte> data)
        {
            var sum = 0;
            foreach (var b in data)
            {
                sum += b;
            }

            return (byte)(sum & 0xFF);
        }

        /// <summary>
        /// 计算 LRC（累加和二补码）。
        /// </summary>
        private static byte ComputeLrc(ReadOnlySpan<byte> data)
        {
            var sum = 0;
            foreach (var b in data)
            {
                sum += b;
            }

            return (byte)((-sum) & 0xFF);
        }

        /// <summary>
        /// 计算 CRC16（Modbus，初值 0xFFFF，多项式 0xA001）。
        /// </summary>
        private static ushort ComputeCrc16(ReadOnlySpan<byte> data)
        {
            const ushort polynomial = 0xA001;
            ushort crc = 0xFFFF;

            foreach (var b in data)
            {
                crc ^= b;
                for (var i = 0; i < 8; i++)
                {
                    var lsb = (crc & 0x0001) == 0x0001;
                    crc >>= 1;
                    if (lsb)
                    {
                        crc ^= polynomial;
                    }
                }
            }

            return crc;
        }
    }
}
