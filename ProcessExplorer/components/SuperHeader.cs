using System;
using System.Linq;
using System.Globalization;

namespace ProcessExplorer.components
{
    /* Super class for all other headers, sections, etc */
    class SuperHeader
    {
        protected ProcessHandler processHandler;

        public string[,] hexArray { get; private set; }
        public string[,] binaryArray { get; private set; }
        public string[,] deciArray { get; private set; }

        /* StartPoint and EndPoint will be saved using hex */
        public int StartPoint { get; protected set; }
        public int EndPoint { get; protected set; }

        public int RowSize { get; private set; }
        public int ColumnSize { get; private set; }

        public bool ShrinkDataSection { get; private set; } // This is for headers that show descriptions 

        public SuperHeader(ProcessHandler processHandler, int rowSize, int columnSize, bool shrinkDataSection)
        {
            this.ShrinkDataSection = shrinkDataSection;
            this.processHandler = processHandler;
            this.ColumnSize = columnSize;
            this.RowSize = rowSize;

            Console.WriteLine("SuperHeader 1 Shrink:" + shrinkDataSection);
            hexArray = new string[rowSize, columnSize];
            binaryArray = new string[rowSize, columnSize - 1]; // I subtract 1 here because theres no point saving the descriptions more than once
            deciArray = new string[rowSize, columnSize - 1];   // I subtract 1 here because theres no point saving the descriptions more than once
            Console.WriteLine("SuperHeader 2");
        }

        protected void PopulateNonDescArrays()
        {
            //Console.WriteLine(" ");
            int startingIndex = StartPoint <= 0 ? 0 : (int) Math.Floor(StartPoint / 16.0); // Theres 16 bytes of hex per row
            int totalByteSize = StartPoint;
            int hexValuesSize = startingIndex * 16;
            //Console.WriteLine("Starting StartinIndex:" + startingIndex + " TotalByteSize:" + totalByteSize + " StartPoint:" + StartPoint + " EndPoint:" + EndPoint);

            // I need to first redefine the size of the arrays
            string[,] filesHex = processHandler.filesHex;
            int rowSize = GetRowCount(filesHex, startingIndex);
            //Console.WriteLine("RowCount:" + rowSize);
            hexArray = new string[rowSize, ColumnSize];
            binaryArray = new string[rowSize, ColumnSize - 1]; // I subtract 1 here because theres no point saving the descriptions more than once
            deciArray = new string[rowSize, ColumnSize - 1];   // I subtract 1 here because theres no point saving the descriptions more than once

            int rowStartingPoint = Convert.ToInt32(filesHex[startingIndex, 0], 16);
            //Console.WriteLine("RowStartingPoint:" + rowStartingPoint);

            long offsetSum;
            string[] splitHexData;
            int ourArrayIndex = 0;
            for (int i = startingIndex; i < filesHex.GetLength(0); i++)
            {
                string[] dataRow = filesHex[i, 1].Split(' ');
                //Console.WriteLine("Length:" + dataRow.Length + " DataRow:" + string.Join(" ", dataRow));
                string correctedHexData = "";
                int rowOffset = -1;
                for(int j = 0; j < 16; j++)
                {
                    int value = rowStartingPoint + ((i - startingIndex) * 16) + j + 1;
                    //Console.WriteLine("   Value:" + value + " EndPoint:" + EndPoint + " i:" + i + " j:" + j + " Data:" + dataRow[j]);
                    if (value >= EndPoint)
                    {
                        correctedHexData += dataRow[j] + " ";
                        offsetSum = int.Parse(filesHex[i, 0].Replace("0x", ""), NumberStyles.HexNumber) + (rowOffset < 0 ? 0 : rowOffset);
                        //Console.WriteLine("Exiting OffsetSum:" + offsetSum);
                        hexArray[ourArrayIndex, 0] = "0x" + (offsetSum).ToString("X");
                        hexArray[ourArrayIndex, 1] = correctedHexData;
                        hexArray[ourArrayIndex, 2] = filesHex[i, 2];

                        splitHexData = correctedHexData.Split(' ').Where(str => !string.IsNullOrWhiteSpace(str)).ToArray(); // Somehow a white space got in here or something...
                        deciArray[ourArrayIndex, 0] = offsetSum.ToString();
                        deciArray[ourArrayIndex, 1] = string.Join(" ", Array.ConvertAll(splitHexData, hex => long.Parse(hex, NumberStyles.HexNumber)));

                        //Console.WriteLine("NonDescArray Row:" + ourArrayIndex + " Offset:" + deciArray[ourArrayIndex, 0] + " Data:" + deciArray[ourArrayIndex, 1]);
                        binaryArray[ourArrayIndex, 0] = Convert.ToString(offsetSum, 2);
                        binaryArray[ourArrayIndex, 1] = string.Join(" ", splitHexData.Select(hex => Convert.ToString(long.Parse(hex, NumberStyles.HexNumber), 2)));
                        //Console.WriteLine("Leaving Early OffsetSum:" + offsetSum + " Offset:" + deciArray[ourArrayIndex, 0] + " HexData:" + deciArray[ourArrayIndex, 1] + " Value:" + value);
                        return;
                    }

                    if (value < StartPoint) continue;
                    if (rowOffset == -1) rowOffset = j; // Sets how many bytes into the row it took to reach our first value
                  
                    correctedHexData += dataRow[j] + " ";
                }

                //Console.WriteLine("FilesHexRowLength:" + filesHex.GetLength(0) + " HexRowLength:" + hexArray.GetLength(0) + " HexColumnLength:" + 
                //   hexArray.GetLength(1) + " RowOffset:" + rowOffset + " i:" + i + " OurArrayIndex:" + ourArrayIndex);
                offsetSum = long.Parse(filesHex[i, 0].Replace("0x", ""), NumberStyles.HexNumber) + (rowOffset < 0 ? 0 : rowOffset);
                hexArray[ourArrayIndex, 0] = "0x" + (offsetSum).ToString("X");
                hexArray[ourArrayIndex, 1] = correctedHexData;
                hexArray[ourArrayIndex, 2] = filesHex[i, 2];

                splitHexData = correctedHexData.Split(' ').Where(str => !string.IsNullOrWhiteSpace(str)).ToArray(); // Somehow a white space got in here or something...
                deciArray[ourArrayIndex, 0] = offsetSum.ToString();
                deciArray[ourArrayIndex, 1] = string.Join(" ", Array.ConvertAll(splitHexData, hex => long.Parse(hex, NumberStyles.HexNumber)));

                //Console.WriteLine("NonDescArray Row:" + ourArrayIndex + " Offset:" + deciArray[ourArrayIndex, 0] + " Data:" + deciArray[ourArrayIndex, 1]);

                binaryArray[ourArrayIndex, 0] = Convert.ToString(offsetSum, 2);
                binaryArray[ourArrayIndex, 1] = string.Join(" ", splitHexData.Select(hex => Convert.ToString(long.Parse(hex, NumberStyles.HexNumber), 2)));
                ourArrayIndex++;
            }

           
        }

        private int GetRowCount(string[,] filesHex, int startingIndex)
        {
            int finalCount = startingIndex;
            for (int i = startingIndex; i < filesHex.GetLength(0); i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    // (i - startingIndex) denotes the row, * 16 multiples it by 16 bytes per row, and + (j + 1) adds the individual row bytes
                    int value = StartPoint + ((i - startingIndex) * 16) + j + 1;
                    if (value >= EndPoint) return i + 1 - startingIndex;
                }
                finalCount = i;
            }
            return finalCount + 1 - startingIndex;
        }


        /// <param name="sizeAndDesc"> will be passed from the sub class and will contain the size in bytes  
        /// of each field and the description that explains what it does </param>
        protected void populateArrays(string[,] sizeAndDesc)
        {
            string[] hexValues = new string[0];
            //Console.WriteLine("PopulateArrays 1 Length:" + hexArray.GetLength(0));

            /** The following loop will be used to increase the size of the hexValues array. It will determine how many bytes 
             * this header file will need it will then add a new row of 16 after each 
             */
            int startingIndex = StartPoint <= 0 ? 0 : (int)Math.Floor(StartPoint / 16.0); // 2
            int totalByteSize = StartPoint;
            int hexValuesSize = startingIndex * 16;
/*            Console.WriteLine("Starting StartinIndex:" + startingIndex + " TotalByteSize:" + totalByteSize + " HexValuesSize:" + hexValues.Length + " ArrayLegnth:"
                + hexValues.Length + " SizeAndDescLength:" + sizeAndDesc.GetLength(0));*/
            for (int i = 0; i < sizeAndDesc.GetLength(0); i++)
            {
                totalByteSize += Convert.ToInt32(sizeAndDesc[i, 0]); //int.Parse(sizeAndDesc[i].Replace("0x", ""), NumberStyles.HexNumber); // 36
                //Console.WriteLine("i:" + i + " HexSize:" + hexValuesSize + " totalByteSize:" +  totalByteSize + " NewSize:" + sizeAndDesc[i]);
                if (totalByteSize > hexValuesSize)
                {
                    int index = hexValuesSize <= 0 ? 0 : (hexValuesSize / 16); // Prevents a divide by 0 error
                    string[] newArray = processHandler.filesHex[index, 1].Split(' '); // Adds the next row
                    Array.Resize(ref hexValues, hexValues.Length + newArray.Length);
                    Array.Copy(newArray, 0, hexValues, hexValues.Length - newArray.Length, newArray.Length);
                    hexValuesSize += 16; // This adds an extra row aka 16 elements (we call this after getting index to avoid needing to subtract 1)
                    /*Console.WriteLine("i:" + i + " index:" + index + " HexSize:" + hexValuesSize + " TotalByteSize:" + totalByteSize + " NewArrayLength:" + newArray.Length +
                        " Array:" + string.Join(", ", newArray));*/
                }
            }
            /*            Console.WriteLine("Final StartinIndex:" + startingIndex + " TotalByteSize:" + totalByteSize + " HexValuesSize:" + hexValues.Length + " ArrayLegnth:"
                            + hexValues.Length + "   " + string.Join(", ", hexValues) + " \n");*/

            int previousBytesize = 0;
            for (int i = 0; i < hexArray.GetLength(0); i++)
            {
                int newByteSize = Convert.ToInt32(sizeAndDesc[i, 0]);
                // I need to subtract StartPoint because I am using the offsets to gage the sizes 
                int previousOffset = (i > 0 ? (int.Parse(hexArray[i - 1, 0].Replace("0x", ""), NumberStyles.HexNumber)) : 0) - StartPoint;
                int newOffset = previousBytesize + previousOffset;

                string newHexValues = string.Join(" ", hexValues.Skip(i > 0 ? newOffset : 0).Take(newByteSize));
                hexArray[i, 0] = "0x" + (i > 0 ? (newOffset + StartPoint).ToString("X") : (StartPoint).ToString("X"));
                hexArray[i, 1] = newHexValues;
                hexArray[i, 2] = sizeAndDesc[i, 1];

                long deciOffset = (i > 0 ? (newOffset + StartPoint) : StartPoint);
                string[] splitNewHexValues = newHexValues.Split(' ').Where(str => !string.IsNullOrWhiteSpace(str)).ToArray(); // Somehow a white space got in here or something...;
                deciArray[i, 0] = deciOffset.ToString();
                deciArray[i, 1] = string.Join(" ", Array.ConvertAll(splitNewHexValues, hex => long.Parse(hex, NumberStyles.HexNumber)));
                binaryArray[i, 0] = Convert.ToString(deciOffset, 2); // 2 specifies to use binary
                binaryArray[i, 1] = string.Join(" ", splitNewHexValues.Select(hex => Convert.ToString(long.Parse(hex, NumberStyles.HexNumber), 2)));

              /*  Console.WriteLine("i:" + i + " Offset:" + hexArray[i, 0] + " Data:" + hexArray[i, 1] + " NewOffset:" + newOffset + " NewByteSize:"
                    + newByteSize + " PreviousOffset:" + previousOffset + " StartPoint:" + StartPoint);*/
                previousBytesize = newByteSize;
            }
            //Console.WriteLine("PopulateArrays 3 sizeAndDesc:" + sizeAndDesc.GetLength(0) + " RowSize:" + RowSize  + " HexArrayLength:" + hexArray.GetLength(0));
            // This will set the ending point
            int lastOffset = int.Parse((hexArray[RowSize - 1, 0]).Replace("0x", ""), NumberStyles.HexNumber) - StartPoint;
            int lastByteSize = Convert.ToInt32(sizeAndDesc[RowSize - 1, 0]);
            EndPoint = lastOffset + lastByteSize;
        }

        public virtual string getHex(int row, int column, bool doubleByte)
        {
            if (row > hexArray.GetLength(0) - 1 || column > hexArray.GetLength(1)) return "";
            if (!doubleByte || column == 0 || column == 2) return hexArray[row, column]; // Returns the single byte data, offset, or the description
            // This will reverse the hex and switch it from little-endian to big-endian
            string[] values = hexArray[row, column].Split(' ');
            return getBigEndian(values);
        }

        public virtual string getBinary(int row, int column, bool doubleByte)
        {
            if (row > binaryArray.GetLength(0) - 1 || column > hexArray.GetLength(1)) return "";
            if(column > 1) return hexArray[row, column]; // This is here because this array does not contain the desc
            if (column == 0 && processHandler.OffsetsInHex) return hexArray[row, 0]; // This means the offsets should be displayed in hex
            if (!doubleByte || column == 0) return binaryArray[row, column]; // Returns the single byte data or the offset
            // This will reverse the binary and switch it from little-endian to big-endian
            string[] values = hexArray[row, column].Split(' ');//.Where(str => !string.IsNullOrWhiteSpace(str)).ToArray();
            string bigEndianHex = getBigEndian(values);
            return string.Join(" ", bigEndianHex.Split(' ').Select(hexPair => Convert.ToString(Convert.ToInt32(hexPair, 16), 2)));
        }

        public virtual string getDecimal(int row, int column, bool doubleByte)
        {
            if (row > deciArray.GetLength(0) - 1 || column > hexArray.GetLength(1)) return "";
            if (column > 1) return hexArray[row, column]; // This is here because this array does not contain the desc
            if (column == 0 && processHandler.OffsetsInHex) return hexArray[row, 0]; // This means the offsets should be displayed in hex
            if (!doubleByte || column == 0)
            {
                Console.WriteLine("SingleByteDeci Row:" + row + " Column:" + column + " Value:" + deciArray[row, column]);
                return deciArray[row, column]; // Returns the single byte data or the offset
            }
            // This will reverse the decimal and switch it from little-endian to big-endian
            string[] values = hexArray[row, column].Split(' ');
            string bigEndianHex = getBigEndian(values);
            Console.WriteLine("BigEndianHex:" + bigEndianHex + " DoubleByte:" + doubleByte + " Row:" + row + " Column:" + column);
            return string.Join(" ", bigEndianHex.Split(' ').Select(hexPair => ulong.Parse(hexPair, NumberStyles.HexNumber)));
        }

        private string getBigEndian(string[] hexPairs)
        {
            if (ShrinkDataSection)
            {
                Array.Reverse(hexPairs);
                string bigEndian = string.Concat(hexPairs);

                // Check if the result is all "0"s and convert to a single "0"
                //if (processHandler.RemoveZeros && (bigEndian.Trim('0') == "" || (bigEndian = bigEndian.TrimStart('0')).Length == 0)) bigEndian = "0";
                return bigEndian;
            }

            // This is for headers and sections of data that dont have descriptions 
            string reversedHexValues = string.Join(" ",
            hexPairs.Where((value, index) => index % 2 == 1)
                .Select((value, index) =>
                {
                    // This part changes 0000 into a single 0
                    string firstPart = hexPairs[index * 2 + 1];
                    string secondPart = hexPairs[index * 2];
                    string combined = firstPart + secondPart;
                    if (processHandler.RemoveZeros)
                    {
                        if (secondPart == "00" && firstPart == "00") combined = "0";
                       // else if ((combined = combined.TrimStart('0')).Length == 0) combined = "0";
                    }
                    return combined;
                }));
            return reversedHexValues;
        }

    }
}
