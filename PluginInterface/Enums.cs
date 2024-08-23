using System;

namespace PluginInterface
{
    public class Enums
    {
        public enum OffsetType
        {
            FILE_OFFSET, RELATIVE_OFFSET
        }

        public enum DataType
        {
            HEX, DECIMAL, BINARY
        }

        /// <summary>
        ///  Takes in a normal decimal integer for offset and returns the converted data type in string form
        /// </summary>
        public static string GetOffset(uint offset, DataType dataType)
        {
            switch (dataType)
            {
                case DataType.DECIMAL: return offset.ToString();
                case DataType.HEX: return "0x" + offset.ToString("X");
                case DataType.BINARY: return Convert.ToString(offset, 2);
            }
            return "";
        }
    }
}
