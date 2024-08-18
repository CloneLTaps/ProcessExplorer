using System;
using System.Linq;
using System.Text;

namespace PluginInterface
{
    public class DataStorage
    {
        public Settings Settings { get; private set; }

        public bool Is64Bit { get; set; }

        public string FileName { get; private set; }
        public string[,] FilesHex { get; private set; }

        public DataStorage(Settings settings, string[,] filesHex, bool is64Bit, string fileName)
        {
            this.Settings = settings;
            this.FilesHex = filesHex;
            this.Is64Bit = is64Bit;
            this.FileName = fileName;
        }

        public string GetFilesDecimal(int row, int column)
        {
            if (column == 2) return FilesHex[row, 2];

            if(column == 1)
            {   // Data column
                string[] hexBytes = FilesHex[row, 1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string[] decimalBytes = GetDecimalBytes(hexBytes);
                return string.Join(" ", decimalBytes);
            }

            if (column == 0)
            {   // Offset column
                return Convert.ToInt32(FilesHex[row, 0].Substring(2), 16).ToString();
            }
            return "";
        }

        public string GetFilesBinary(int row, int column)
        {
            if (column == 2) return FilesHex[row, 2];

            if (column == 1)
            {   // Data column
                string[] hexBytes = FilesHex[row, 1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string[] binaryBytes = GetDecimalBytes(hexBytes).Select(decimalByte => Convert.ToString(int.Parse(decimalByte), 2).PadLeft(8, '0')).ToArray();
                return string.Join(" ", binaryBytes);
            }

            if(column == 0)
            {   // Offset column
                int decimalValue = Convert.ToInt32(FilesHex[row, 0].Substring(2), 16);
                return Convert.ToString(decimalValue, 2).ToString();
            }
            return "";
        }

        private string[] GetDecimalBytes(string[] hexBytes)
        {
            return hexBytes.Select(hexByte =>
            {
                if (int.TryParse(hexByte, System.Globalization.NumberStyles.HexNumber, null, out int decimalValue)) return decimalValue.ToString();
                else return "0";
            }).ToArray();
        }

        public int GetFilesRows()
        {
            return FilesHex.GetLength(0);
        }

        public int GetFilesColumns()
        {
            return FilesHex.GetLength(1);
        }

        public void UpdateArrays(string[,] filesHex)
        {
            this.FilesHex = filesHex;
        }

        public string UpdateASCII(string hexString, int row)
        {
            string[] hexBytes = hexString.Split(' '); // Split by spaces

            StringBuilder asciiBuilder = new StringBuilder();
            foreach (string hexByte in hexBytes)
            {
                if (byte.TryParse(hexByte, System.Globalization.NumberStyles.HexNumber, null, out byte asciiByte))
                {
                    if (asciiByte >= 32 && asciiByte <= 128) asciiBuilder.Append((char)asciiByte);
                    else asciiBuilder.Append(".");
                }
                else asciiBuilder.Append(".");
            }
            FilesHex[row, 2] = asciiBuilder.ToString();
            return asciiBuilder.ToString();
        }

        public string ReplaceData(int difference, int dataByteLength, string data, string replacment, int originalLength, string type)
        {
            string[] originalBytes = data.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string[] replacementBytes = replacment.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (type == "dos stub" || type.ToString().Contains(" body"))
            {   // This means I just need to replace the data instead of switching out a few bytes
                if (replacementBytes.Length < originalLength)
                {   // This means the user is trying to reduce the data sections size for some reason
                    return string.Join(" ", replacementBytes);
                }

                for (int i = 0; i < replacementBytes.Length; i++)
                {
                    if (originalBytes.Length - 1 >= i) originalBytes[i] = replacementBytes[i];
                }
                return string.Join(" ", originalBytes);
            }

            if (difference >= 0 && dataByteLength > 0)
            {
                int byteLength = originalBytes.Length;
                if (difference >= 0 && dataByteLength > 0 && difference + dataByteLength <= byteLength)
                {
                    // Calculate the start and end indexes of the section to replace
                    int endIndex = difference + dataByteLength;

                    // Copy the original data
                    string[] modifiedBytes = new string[byteLength];
                    Array.Copy(originalBytes, modifiedBytes, byteLength);

                    // Replace the specified section with the replacement data
                    for (int i = difference; i < endIndex; i++)
                    {
                        if (i >= byteLength || i - difference >= replacementBytes.Length) break;
                        modifiedBytes[i] = replacementBytes[i - difference];
                    }

                    // Combine the modified bytes into a single string
                    return string.Join(" ", modifiedBytes);
                }
            }
            return data;
        }
    }
}
