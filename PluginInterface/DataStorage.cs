using System;

namespace PluginInterface
{
    public class DataStorage
    {
        public Settings Settings { get; private set; }

        public bool Is64Bit { get; set; }

        public string FileName { get; private set; }
        public string[,] FilesHex { get; private set; }
        public string[,] FilesDecimal { get; private set; }
        public string[,] FilesBinary { get; private set; }

        public DataStorage(Settings settings, string[,] filesHex, string[,] filesDecimal, string[,] filesBinary, bool is64Bit, string fileName)
        {
            this.Settings = settings;
            this.FilesHex = filesHex;
            this.FilesDecimal = filesDecimal;
            this.FilesBinary = filesBinary;
            this.Is64Bit = is64Bit;
            this.FileName = fileName;
        }

        public int GetFilesRows()
        {
            return FilesHex.GetLength(0);
        }

        public int GetFilesColumns()
        {
            return FilesHex.GetLength(1);
        }

        public void UpdateArrays(string[,] filesHex, string[,] filesDecimal, string[,] filesBinary)
        {
            this.FilesHex = filesHex;
            this.FilesDecimal = filesDecimal;
            this.FilesBinary = filesBinary;
        }

        public string UpdateASCII(string hexString, int row)
        {
            string[] hexBytes = hexString.Split(' '); // Split by spaces

            string asciiString = "";
            foreach (string hexByte in hexBytes)
            {
                if (byte.TryParse(hexByte, System.Globalization.NumberStyles.HexNumber, null, out byte asciiByte))
                {
                    if (asciiByte >= 32 && asciiByte <= 128) asciiString += (char)asciiByte;
                    else asciiString += ".";
                }
                else asciiString += ".";
            }
            FilesHex[row, 2] = asciiString;
            return asciiString;
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
