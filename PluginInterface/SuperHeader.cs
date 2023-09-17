using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

using static PluginInterface.Enums;

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

        public string Component { get; protected set; }

        public Dictionary<int, string> Characteristics { get; protected set; }

        public SuperHeader(string component, int rowSize, int columnSize)
        {
            this.ColumnSize = columnSize;
            this.Component = component.ToLower();
            this.RowSize = rowSize;

            FailedToInitlize = false;
        }

        public abstract void OpenForm(int row, PluginInterface.DataStorage dataStorage);

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

        /// <param name="column"> Column of 0 means offset, column of 1 means data, and column of 2 means description. </param>
        public string GetData(int row, int column, DataType dataType, bool bigEndian, bool inFileOffset, DataStorage dataStroage)
        {
            if (FailedToInitlize) return "";  // Something went wrong with this file so don't return any text
            if (Size != null && row >= Size.Length) return "";  // This returns the blank text for index larger than the size of our headers

            if (Component == "everything")
            {
                if (column == 0 && dataStroage.Settings.OffsetsInHex) return GetCorrectFormat(row, column, DataType.HEX, bigEndian, dataStroage);
            }
            int firstRow = (int)Math.Floor(StartPoint / 16.0);  // First row of our data

            if (Size == null && (row + firstRow) * 16 >= EndPoint) return "";  // This will handle blank spots in sections and the Dos Stub     

            // Something like Dos Stub or section bodies (mirrors the original data) while being aligned with 16 byte rows
            if (Size == null && StartPoint % 16 == 0)
            {
                if (column == 0)
                {
                    int off = row * 16;
                    return GetOffset(inFileOffset ? off + StartPoint : off, dataStroage.Settings.OffsetsInHex ? DataType.HEX : dataType);
                }
                return GetCorrectFormat(row + firstRow, column, dataType, bigEndian, dataStroage);
            }

            if (Desc != null && column == 2) return Desc[row];  // This means its just asking for the custom description

            int relativeOfffset = 0;  // Amount of bytes into the row till we reach our target data relative to the start of this section
            int bytes = Size == null ? 16 : Size[row];  // How many bytes I need to read from the array
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
            relativeOfffset += StartPoint % 16;

            // At this point we should only be working with data as the file offsets and descriptions should of been taken care of already
            int startingDataRow = firstRow + (int)Math.Floor(relativeOfffset / 16.0);  // Row where our data begins (data may extend onto additional rows)

            int fileStartOffset = relativeOfffset + (startingDataRow * 16);  // Target data's offset relative to the start of the file

            string[] data = null;
            int startingRowOffset = relativeOfffset - ((startingDataRow - firstRow) * 16);  // Offset relative to the starting row

            for (int i = startingDataRow; i < dataStroage.GetFilesRows(); i++)
            {   // Looping through our rows
                if (data == null) data = GetCorrectFormat(i, 1, bigEndian ? DataType.HEX : dataType, false, dataStroage).Split(' ');
                else data = data.Concat(GetCorrectFormat(i, 1, bigEndian ? DataType.HEX : dataType, false, dataStroage).Split(' ')).ToArray();

                int weight = Size == null ? ((row - startingDataRow + 1) * 16) : data.Length;  // First half works for sections and Dos headers and second works for normal headers

                if (weight >= startingRowOffset + bytes) break;  // This means our data array now contains enough data to retrieve the requested data
            }

            string finalData = string.Join(" ", data.Skip(startingRowOffset).Take(bytes));

            if (bigEndian)
            {
                string splitHex = GetBigEndian(finalData.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), dataStroage.Settings.RemoveZeros);

                if (dataType == DataType.HEX) return splitHex;

                string[] hexArray = splitHex.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                switch (dataType)
                {
                    case DataType.DECIMAL: return string.Join(" ", hexArray.Select(hexPair => ulong.Parse(hexPair, NumberStyles.HexNumber)));
                    case DataType.BINARY: return string.Join(" ", hexArray.Select(hexPair => Convert.ToString(Convert.ToInt64(hexPair, 16), 2)));
                }
            }
            return finalData;
        }

        private string GetCorrectFormat(int row, int column, DataType dataType, bool bigEndian, DataStorage data)
        {
            if (bigEndian && column == 1)
            {
                string bigEndianHex = GetBigEndian(data.FilesHex[row, column].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), data.Settings.RemoveZeros);
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
                DataType.DECIMAL => data.FilesDecimal[row, column],
                DataType.BINARY => data.FilesBinary[row, column],
                _ => "",
            };
        }

        private string GetBigEndian(string[] hexPairs, bool removeZeros)
        {
            if (Size != null)
            {
                Array.Reverse(hexPairs);
                string bigEndian = string.Concat(hexPairs);

                // Check if the result is all "0"s and convert to a single "0"
                if (removeZeros && (bigEndian.Trim('0') == "" || (bigEndian = bigEndian.TrimStart('0')).Length == 0)) bigEndian = "0";
                return bigEndian;
            }

            // This is for headers and sections of data that dont have descriptions 
            string reversedHexValues = string.Join(" ",
            hexPairs.Where((value, index) => index % 2 == 1)
                .Select((value, index) =>
                {
                    // This part changes the extra 0's into a single 0
                    string firstPart = hexPairs[index * 2 + 1];
                    string secondPart = hexPairs[index * 2];
                    string combined = firstPart + secondPart;
                    if (removeZeros)
                    {
                        if (secondPart == "00" && firstPart == "00") combined = "0";
                        else if ((combined = combined.TrimStart('0')).Length == 0) combined = "0";
                    }
                    return combined;
                }));
            return reversedHexValues;
        }

        public void UpdateData(int row, string data, bool isHexChecked, bool isDecimalChecked, DataStorage dataStorage)
        {
            int orignalLength = (GetData(row, 1, DataType.HEX, false, false, dataStorage).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)).Length;
            string[,] values = GetValueVariations(data, isHexChecked, isDecimalChecked);

            int offset = int.Parse(GetData(row, 0, DataType.DECIMAL, false, true, dataStorage)); // This gets the file offset in decimal form
            int everythingRow = (int)Math.Floor(offset / 16.0);
            int everythingOffset = int.Parse(dataStorage.FilesDecimal[everythingRow, 0]);
            int dataByteLength = (GetData(row, 1, DataType.HEX, false, false, dataStorage).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)).Length;
            int difference = offset - everythingOffset;

            dataStorage.FilesHex[everythingRow, 1] = dataStorage.ReplaceData(difference, dataByteLength, dataStorage.FilesHex[everythingRow, 1], values[0, 0], orignalLength, Component);
            dataStorage.FilesDecimal[everythingRow, 1] = dataStorage.ReplaceData(difference, dataByteLength, dataStorage.FilesDecimal[everythingRow, 1], values[0, 1], orignalLength, Component);
            dataStorage.FilesBinary[everythingRow, 1] = dataStorage.ReplaceData(difference, dataByteLength, dataStorage.FilesBinary[everythingRow, 1], values[0, 2], orignalLength, Component);

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
                values[0, 1] = string.Join(" ", Array.ConvertAll(bytes, hex => long.Parse(hex, System.Globalization.NumberStyles.HexNumber))); // Decimal
                values[0, 2] = string.Join(" ", bytes.Select(hexByte => Convert.ToString(long.Parse(hexByte, System.Globalization.NumberStyles.HexNumber), 2).PadLeft(8, '0'))); // Binary
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

    }

}
