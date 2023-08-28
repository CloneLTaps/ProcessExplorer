using System;
using System.Linq;
using System.Globalization;

namespace ProcessExplorer.components
{
    class DosStub : SuperHeader
    {

        public DosStub(ProcessHandler processHandler, int startPoint) : base(processHandler, 0, 0)
        {

            StartPoint = startPoint;
            string hexEndPoint = processHandler.dosHeader.hexArray[processHandler.dosHeader.RowSize, 1];
            EndPoint = int.Parse(hexEndPoint.Replace("0x", ""), NumberStyles.HexNumber);
            PopulateNonDescArrays();
        }

       /* private int pageSize()
        {
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
        }*/

        public override string getHex(int row, int column, bool doubleByte)
        {
            if (row > hexArray.GetLength(0) - 1 || column > hexArray.GetLength(1)) return "";
            if (!doubleByte || column == 0 || column == 2) return hexArray[row, column]; // Returns the single byte data, offset, or the description
            // This will reverse the hex and switch it from little-endian to big-endian
            return null;
            //return getBigEndian(hexArray[row, column]);
        }

        public override string getBinary(int row, int column, bool doubleByte)
        {
            if (row > binaryArray.GetLength(0) - 1 || column > hexArray.GetLength(1)) return "";
            if (column > 1) return hexArray[row, column]; // This is here because this array does not contain the desc
            if (column == 0 && processHandler.OffsetsInHex) return hexArray[row, 0]; // This means the offsets should be displayed in hex
            if (!doubleByte || column == 0) return binaryArray[row, column]; // Returns the single byte data or the offset
            // This will reverse the binary and switch it from little-endian to big-endian
            string bigEndianHex = getBigEndian(hexArray[row, column]);
            return Convert.ToString(Convert.ToInt32(bigEndianHex, 16), 2);
        }

        public override string getDecimal(int row, int column, bool doubleByte)
        {
            if (row > deciArray.GetLength(0) - 1 || column > hexArray.GetLength(1)) return "";
            if (column > 1) return hexArray[row, column]; // This is here because this array does not contain the desc
            if (column == 0 && processHandler.OffsetsInHex) return hexArray[row, 0]; // This means the offsets should be displayed in hex
            if (!doubleByte || column == 0) return deciArray[row, column]; // Returns the single byte data or the offset
            // This will reverse the decimal and switch it from little-endian to big-endian
            string bigEndianHex = getBigEndian(hexArray[row, column]);
            return Convert.ToInt32(bigEndianHex, 16).ToString();
        }


    }
}
