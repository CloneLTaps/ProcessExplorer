using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

using static PluginInterface.Enums;
using System.Text;

namespace PluginInterface
{
    public abstract class SuperHeader
    {
        public int StartPoint { get; protected set; }
        public int EndPoint { get; protected set; }

        public int RowSize { get; protected set; }
        public int ColumnSize { get; private set; }

        public int[] Size { get; protected set; }
        public string[] Desc { get; protected set; }

        public bool FailedToInitlize { get; protected set; }

        public bool HasSetEndPoint { get; protected set; }

        public bool FileFormatedLittleEndian { get; protected set; }

        public string Component { get; protected set; }

        public readonly List<int> SkipList = new List<int>(); // This is used to specify index that should not have their data displayed 

        public Dictionary<int, string> Characteristics { get; protected set; }

        public SuperHeader(string component, int rowSize, int columnSize)
        {
            this.ColumnSize = columnSize;
            this.Component = component.ToLower();
            this.RowSize = rowSize;

            FailedToInitlize = false;
            FileFormatedLittleEndian = true;
        }

        public abstract void OpenForm(int row, DataStorage dataStorage);

        protected void SetEndPoint()
        {
            if (Size == null) return;

            int count = StartPoint;
            foreach (int i in Size)
            {
                count += i;
            }
            EndPoint = count;
        }

        public byte[] GetAllRowData(bool isBody, int row, DataStorage dataStorage)
        {
            string hex = "";
            StringBuilder hexBuilder = new StringBuilder();

            if (!isBody)
            {   // This is the easy case since all of the data can be queried and stored on a single line
                hex = GetData(row, 1, DataType.HEX, 1, true, dataStorage).Replace(" ", "");
                if (hex.Length % 2 != 0) throw new ArgumentException("Hex string must have an even number of characters.");

                Console.WriteLine("Type Hex:" + hex);
                return Enumerable.Range(0, hex.Length / 2).Select(i => Convert.ToByte(hex.Substring(i * 2, 2), 16)).ToArray();
            }

            for (int i = 0; i < RowSize; i++)
            {   // This handles bodies of data like in chunk bodies or section bodies
                string newHexLine = GetData(i, 1, DataType.HEX, 1, true, dataStorage).Replace(" ", "");
                if (hexBuilder.Length % 2 != 0) throw new ArgumentException("Hex string must have an even number of characters.");
                hexBuilder.Append(newHexLine);
            }

            hex = hexBuilder.ToString();
            if (EndPoint - StartPoint < 200) Console.WriteLine("Body Hex:" + hex);
            return Enumerable.Range(0, hex.Length / 2).Select(i => Convert.ToByte(hex.Substring(i * 2, 2), 16)).ToArray();
        }


        /// <param name="column"> Column of 0 means offset, column of 1 means data, and column of 2 means description. </param>
        public string GetData(int row, int column, DataType dataType, byte fieldSize, bool inFileOffset, DataStorage dataStroage)
        {
            if (FailedToInitlize) return "";  // Something went wrong with this file so don't return any text
            if (Size != null && row >= Size.Length) return "";  // This returns the blank text for index larger than the size of our headers

            if (column == 1 && SkipList.Contains(row)) return ""; // This ensures certain header data rows that often contains tons of info wont get displayed 
            if (HasSetEndPoint && (column == 1 || column == 2) && StartPoint >= EndPoint) return ""; // This means this section does not contain any data

            if (Component == "everything")
            {
                if (column == 0 && dataStroage.Settings.OffsetsInHex) return GetCorrectFormat(row, column, DataType.HEX, fieldSize, dataStroage);
            }
            int firstRow = (int)Math.Floor(StartPoint / 16.0);  // First row of our data

            if (Size == null && (row + firstRow) * 16 >= EndPoint) return "";  // This will handle blank spots in sections and the Dos Stub     
            int mod16 = StartPoint % 16;

            if (Size == null)
            {
                if(mod16 == 0)
                {   // Something like Dos Stub or section bodies (mirrors the original data) while being aligned with 16 byte rows
                    if (column != 0) return GetCorrectFormat(row + firstRow, column, dataType, fieldSize, dataStroage);
                }

                if(column == 0)
                {
                    int off = row * 16 - (row > 0 ? mod16 : 0); // This compensates for the possible non 16 byte aligned headers
                    return GetOffset(inFileOffset ? off + StartPoint : off, dataStroage.Settings.OffsetsInHex ? DataType.HEX : dataType);
                }
            }

            if (Desc != null && column == 2) return Desc[row];  // This means its just asking for the custom description

            int relativeOfffset = 0;  // Amount of bytes into the row till we reach our target data relative to the start of this section
            int bytes = Size == null ? (row == 0 ? (mod16 > 0 ? 16 - mod16 : 0) : 16) : Size[row];  // How many bytes I need to read from the array

            if(Size == null && (column == 1 || column == 2))
            {   // Some body sections / chunks may have less than a full row of data
                int dif = EndPoint - StartPoint;
                if(dif < 16) bytes = row == 0 ? dif : Math.Min(bytes, dif); // Handles the edge case where we are reading less than a full row of data
                else bytes = Math.Min(bytes, EndPoint - (StartPoint + (row * 16) - mod16));
            }

            if (Size != null)
            {   // The following is designed for headers
                for (int i = 0; i < row; i++)
                {
                    relativeOfffset += Size[i];  // Adding the byte size of our rows data to the offset
                }
            }

            if (column == 0)
            {
                return GetOffset((inFileOffset ? relativeOfffset + StartPoint : relativeOfffset), dataStroage.Settings.OffsetsInHex ? DataType.HEX : dataType);  // This means we just need to return the offset
            }
            relativeOfffset += mod16;

            // This handles formating data for sections, dos stub, cert table, etc that dont start at an even 16 byte interval
            if (Size == null && row > 0) relativeOfffset = 16 * row;

            // At this point we should only be working with data as the file offsets and descriptions should of been taken care of already
            int startingDataRow = firstRow + (int)Math.Floor(relativeOfffset / 16.0);  // Row where our data begins (data may extend onto additional rows)

            int fileStartOffset = relativeOfffset + (startingDataRow * 16);  // Target data's offset relative to the start of the file

            string[] data = null;
            int startingRowOffset = relativeOfffset - ((startingDataRow - firstRow) * 16);  // Offset relative to the starting row

            for (int i = startingDataRow; i < dataStroage.GetFilesRows(); i++)
            {   // Looping through our rows
                if (data == null) data = SplitString(GetCorrectFormat(i, column, fieldSize > 1 ? DataType.HEX : dataType, 1, dataStroage), column);
                else data = data.Concat(SplitString(GetCorrectFormat(i, column, fieldSize > 1 ? DataType.HEX : dataType, 1, dataStroage), column)).ToArray();

                int weight = data.Length;
                if (weight >= startingRowOffset + bytes) break;  // This means our data array now contains enough data to retrieve the requested data
            }

            // We dont want to add spaces when dealing with the ASCII column
            if (column == 2) return string.Join("", data.Skip(startingRowOffset).Take(bytes));

            string finalData = string.Join(" ", data.Skip(startingRowOffset).Take(bytes));
            if (fieldSize > 1)
            {
                string splitHex = GetBigEndian(finalData.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), fieldSize, dataStroage.Settings.RemoveZeros);

                if (dataType == DataType.HEX) return splitHex;

                string[] hexArray = splitHex.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                switch (dataType)
                {
                    case DataType.DECIMAL: return string.Join(" ", hexArray.Select(hexPair => ulong.Parse(hexPair, NumberStyles.HexNumber)));
                    case DataType.BINARY: return string.Join(" ", hexArray.Select(hexPair => Convert.ToString(Convert.ToInt64(hexPair, 16), fieldSize)));
                }
            }
            return finalData;
        }

        private string[] SplitString(string data, int column)
        {
            if (column == 1) return data.Split(' ');
            return (data.ToCharArray().Select(c => c.ToString()).ToArray()).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        }

        private string GetCorrectFormat(int row, int column, DataType dataType, byte fieldSize, DataStorage data)
        {
            if (fieldSize > 1 && column == 1)
            {
                string bigEndianHex = GetBigEndian(data.FilesHex[row, column].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), fieldSize, data.Settings.RemoveZeros);
                string[] hexArray = bigEndianHex.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                switch (dataType)
                {
                    case DataType.HEX: return bigEndianHex;
                    case DataType.DECIMAL: return string.Join(" ", hexArray.Select(hexPair => ulong.Parse(hexPair, NumberStyles.HexNumber)));
                    case DataType.BINARY: return string.Join(" ", hexArray.Select(hexPair => Convert.ToString(Convert.ToInt64(hexPair, 16), 2)));
                }
            }

            if (column == 2) return data.FilesHex[row, 2];

            return dataType switch
            {
                DataType.HEX => data.FilesHex[row, column],
                DataType.DECIMAL => data.GetFilesDecimal(row, column),
                DataType.BINARY => data.GetFilesBinary(row, column),
                _ => "",
            };
        }

        private string GetBigEndian(string[] hexPairs, byte fieldSize, bool removeZeros)
        {
            if (Size != null)
            {
                if(FileFormatedLittleEndian) Array.Reverse(hexPairs);
                string bigEndian = string.Concat(hexPairs);

                // Check if the result is all "0"s and convert to a single "0"
                if (removeZeros && (bigEndian.Trim('0') == "" || (bigEndian = bigEndian.TrimStart('0')).Length == 0)) bigEndian = "0";
                return bigEndian;
            }

            if (fieldSize != 2 && fieldSize != 4 && fieldSize != 8) throw new ArgumentException("Size must be 2, 4, or 8");

            StringBuilder finalString = new StringBuilder();
            StringBuilder stringBuilder = new StringBuilder();
            int index = 0;

            for (int i = 0; i < hexPairs.Length; i++)
            {
                bool atEnd = i == hexPairs.Length - 1;
                string pair = hexPairs[i];
                if (removeZeros && pair == "00") pair = "0";

                if (i % fieldSize == 0 || atEnd)
                {
                    if (atEnd) stringBuilder.Append(pair);
                    finalString.Append(GetBigEndianValueNoSpaces(stringBuilder.ToString()) + " ");
                    if (atEnd) break;

                    stringBuilder = new StringBuilder();
                    index++;
                }

                stringBuilder.Append(pair);
            }

            return finalString.ToString().TrimEnd();
        }

        public void UpdateData(int row, string data, bool isHexChecked, bool isDecimalChecked, DataStorage dataStorage)
        {
            int orignalLength = (GetData(row, 1, DataType.HEX, 1, false, dataStorage).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)).Length;
            string[,] values = GetValueVariations(data, isHexChecked, isDecimalChecked);

            int offset = int.Parse(GetData(row, 0, DataType.DECIMAL, 1, true, dataStorage)); // This gets the file offset in decimal form
            int everythingRow = (int)Math.Floor(offset / 16.0);
            int everythingRowOffset = int.Parse(dataStorage.GetFilesDecimal(everythingRow, 0));
            int dataByteLength = values.GetLength(0);
            int difference = offset - everythingRowOffset; // Difference between the headers data's offset and the main rows offset  

            dataStorage.FilesHex[everythingRow, 1] = dataStorage.ReplaceData(difference, dataByteLength, dataStorage.FilesHex[everythingRow, 1], values[0, 0], orignalLength, Component);
            dataStorage.UpdateASCII(dataStorage.FilesHex[everythingRow, 1], everythingRow);
        }

        public string[,] GetValueVariations(string newValue, bool hexChecked, bool decimalChecked)
        {
            string[,] values = new string[1, 3];
            string[] bytes = newValue.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (hexChecked)
            {
                bytes = newValue.Replace(" ", "") // Remove spaces
                    .Select((c, index) => new { Char = c, Index = index })
                    .GroupBy(x => x.Index / 2).Select(group => new string(group.Select(x => x.Char).ToArray())).ToArray();

                values[0, 0] = string.Join(" ", bytes); // Hex
                values[0, 1] = string.Join(" ", Array.ConvertAll(bytes, hex => long.Parse(hex, NumberStyles.HexNumber))); // Decimal
                values[0, 2] = string.Join(" ", bytes.Select(hexByte => Convert.ToString(long.Parse(hexByte, NumberStyles.HexNumber), 2).PadLeft(8, '0'))); // Binary
            }
            else if (decimalChecked)
            {
                var decimalNumbers = bytes.Select(number => long.Parse(number)).ToList();
                values[0, 0] = string.Join(" ", decimalNumbers.Select(decimalValue => decimalValue.ToString("X"))); // Hex
                values[0, 1] = newValue; // Decimal
                values[0, 2] = string.Join(" ", decimalNumbers.Select(decimalValue => Convert.ToString(decimalValue, 2).PadLeft(8, '0'))); // Binary
            }
            else
            {
                var binaryBytes = bytes.Select(binary => Convert.ToByte(binary, 2)).ToArray();
                values[0, 0] = BitConverter.ToString(binaryBytes).Replace("-", " "); // Hex
                values[0, 1] = string.Join(" ", binaryBytes.Select(byteValue => byteValue.ToString())); // Decimal
                values[0, 2] = newValue; // Binary
            }
            return values;
        }

        public string[] ReadCharacteristics(string hexString)
        {
            long characteristicsValue = Convert.ToInt64(hexString, 16);
            List<string> presentCharacteristics = new List<string>();

            foreach (var pair in Characteristics)
            {
                int characteristicFlag = pair.Key;
                string value = pair.Value;

                if ((characteristicsValue & characteristicFlag) != 0)
                {
                    string v = "0x" + characteristicFlag.ToString("X") + " - " + value;
                    presentCharacteristics.Add(v);
                }
            }

            return presentCharacteristics.ToArray();
        }

        public static string GetBigEndianValue(string littleEndian)
        {
            string[] values = littleEndian.Split(' ');
            Array.Reverse(values);
            string bigEndian = string.Concat(values).Replace(" ", "");
            return bigEndian;
        }

        public static string GetBigEndianValueNoSpaces(string littleEndian)
        {
            List<string> pairs = new List<string>();

            for (int i = 0; i < littleEndian.Length; i += 2)
            {   // Process the string in pairs of characters
                if (i + 1 < littleEndian.Length)
                {   // If there's at least two characters left, add as a pair
                    pairs.Add(littleEndian.Substring(i, 2));
                }
                else
                {   // If only one character left, add it as a single character
                    pairs.Add(littleEndian.Substring(i, 1));
                }
            }
            pairs.Reverse();
            return string.Concat(pairs); ;
        }

    }

}
