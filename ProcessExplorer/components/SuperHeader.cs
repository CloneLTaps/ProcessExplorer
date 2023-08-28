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

        public SuperHeader(ProcessHandler processHandler, int rowSize, int columnSize)
        {
            this.processHandler = processHandler;
            this.ColumnSize = columnSize;
            this.RowSize = rowSize;

            hexArray = new string[rowSize, columnSize];
            binaryArray = new string[rowSize, columnSize - 1]; // I subtract 1 here because theres no point saving the descriptions more than once
            deciArray = new string[rowSize, columnSize - 1];   // I subtract 1 here because theres no point saving the descriptions more than once
        }

        protected void PopulateNonDescArrays()
        {
            int startingIndex = StartPoint <= 0 ? 0 : (int)Math.Floor(StartPoint / 16.0); // Theres 16 bytes of hex per row
            int totalByteSize = StartPoint;
            int hexValuesSize = startingIndex * 16;
            Console.WriteLine("Starting StartinIndex:" + startingIndex + " TotalByteSize:" + totalByteSize + " EndPoint:" + EndPoint);

            string[,] filesHex = processHandler.filesHex;


            for(int i = startingIndex; i < filesHex.GetLength(0); i++)
            {
                string[] dataRow = filesHex[i, 1].Split(' '); 
                for(int j = 0; j < 16; j++)
                {
                    int value = Convert.ToInt32(dataRow[j], 16);


                }
            }
        }

        /// <param name="sizeAndDesc"> will be passed from the sub class and will contain the size in bytes  
        /// of each field and the description that explains what it does </param>
        protected void populateArrays(string[,] sizeAndDesc)
        {
            string[] hexValues = new string[0];
            Console.WriteLine("Length:" + hexArray.GetLength(0));

            /** The following loop will be used to increase the size of the hexValues array. It will determine how many bytes 
             * this header file will need it will then add a new row of 16 after each 
             */
            int startingIndex = StartPoint <= 0 ? 0 : (int)Math.Floor(StartPoint / 16.0); // 2
            int totalByteSize = StartPoint;
            int hexValuesSize = startingIndex * 16;
            Console.WriteLine("Starting StartinIndex:" + startingIndex + " TotalByteSize:" + totalByteSize + " HexValuesSize:" + hexValues.Length + " ArrayLegnth:"
                + hexValues.Length + " SizeAndDescLength:" + sizeAndDesc.GetLength(0));
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
                    Console.WriteLine("i:" + i + " index:" + index + " HexSize:" + hexValuesSize + " TotalByteSize:" + totalByteSize + " NewArrayLength:" + newArray.Length +
                        " Array:" + string.Join(", ", newArray));
                }
            }
            Console.WriteLine("Final StartinIndex:" + startingIndex + " TotalByteSize:" + totalByteSize + " HexValuesSize:" + hexValues.Length + " ArrayLegnth:"
                + hexValues.Length + "   " + string.Join(", ", hexValues) + " \n");


            int previousBytesize = 0;
            for (int i = 0; i < hexArray.GetLength(0); i++)
            {
                int newByteSize = Convert.ToInt32(sizeAndDesc[i, 0]);
                int previousOffset = (i > 0 ? (int.Parse(hexArray[i - 1, 0].Replace("0x", ""), NumberStyles.HexNumber)) : 0);
                int newOffset = previousBytesize + previousOffset;

                string newHexValues = string.Join(" ", hexValues.Skip(i > 0 ? newOffset : 0).Take(newByteSize));
                hexArray[i, 0] = "0x" + (i > 0 ? (newOffset).ToString("X") : "0");
                hexArray[i, 1] = newHexValues;
                hexArray[i, 2] = sizeAndDesc[i, 1];

                int deciOffset = i > 0 ? newOffset : 0;
                deciArray[i, 0] = deciOffset.ToString();
                deciArray[i, 1] = string.Join(" ", Array.ConvertAll(newHexValues.Split(' '), hex => int.Parse(hex, NumberStyles.HexNumber)));

                binaryArray[i, 0] = Convert.ToString(deciOffset, 2); // 2 specifies to use binary
                binaryArray[i, 1] = string.Join(" ", newHexValues.Split(' ').Select(hex => Convert.ToString(int.Parse(hex, NumberStyles.HexNumber), 2)));

                Console.WriteLine("i:" + i + " Offset:" + hexArray[i, 0] + " Data:" + hexArray[i, 1] + " NewOffset:" + newOffset + " NewByteSize:"
                    + newByteSize + " PreviousOffset:" + previousOffset);
                previousBytesize = newByteSize;
            }

            // This will set the ending point
            int lastOffset = int.Parse(hexArray[RowSize, 0].Replace("0x", ""), NumberStyles.HexNumber);
            int lastByteSize = Convert.ToInt32(sizeAndDesc[RowSize, 0]);
            EndPoint = lastOffset + lastByteSize;
            Console.WriteLine("EndPoint:" + EndPoint);
        }

        public virtual string getHex(int row, int column, bool doubleByte)
        {
            if (row > hexArray.GetLength(0) - 1 || column > hexArray.GetLength(1)) return "";
            if (!doubleByte || column == 0 || column == 2) return hexArray[row, column]; // Returns the single byte data, offset, or the description
            // This will reverse the hex and switch it from little-endian to big-endian
            return getBigEndian(hexArray[row, column]);
        }

        public virtual string getBinary(int row, int column, bool doubleByte)
        {
            if (row > binaryArray.GetLength(0) - 1 || column > hexArray.GetLength(1)) return "";
            if(column > 1) return hexArray[row, column]; // This is here because this array does not contain the desc
            if (column == 0 && processHandler.OffsetsInHex) return hexArray[row, 0]; // This means the offsets should be displayed in hex
            if (!doubleByte || column == 0) return binaryArray[row, column]; // Returns the single byte data or the offset
            // This will reverse the binary and switch it from little-endian to big-endian
            string bigEndianHex = getBigEndian(hexArray[row, column]);
            return Convert.ToString(Convert.ToInt32(bigEndianHex, 16), 2);
        }

        public virtual string getDecimal(int row, int column, bool doubleByte)
        {
            if (row > deciArray.GetLength(0) - 1 || column > hexArray.GetLength(1)) return "";
            if (column > 1) return hexArray[row, column]; // This is here because this array does not contain the desc
            if (column == 0 && processHandler.OffsetsInHex) return hexArray[row, 0]; // This means the offsets should be displayed in hex
            if (!doubleByte || column == 0) return deciArray[row, column]; // Returns the single byte data or the offset
            // This will reverse the decimal and switch it from little-endian to big-endian
            string bigEndianHex = getBigEndian(hexArray[row, column]);
            return Convert.ToInt32(bigEndianHex, 16).ToString();
        }

        private string getBigEndian(string start)
        {
            string[] values = start.Split(' ');
            Array.Reverse(values);
            string bigEndian = string.Concat(values);

            // Check if the result is all "0"s and convert to a single "0"
            if (processHandler.RemoveZeros && bigEndian.Trim('0') == "") bigEndian = "0";
            return bigEndian; ;
        }

/*        public int getRowCount()
        {
            return hexArray.GetLength(0);
        }*/

    }
}
