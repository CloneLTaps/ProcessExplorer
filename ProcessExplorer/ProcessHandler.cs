using System;
using System.Text;
using System.Data;
using System.Linq;
using System.IO;
using ProcessExplorer.components;
using System.Globalization;

namespace ProcessExplorer
{
    class ProcessHandler
    {
        public string[,] filesHex { get; private set; }
        public string[,] filesBinary { get; private set; }
        public string[,] filesDecimal { get; private set; }
        public string FileName { get; private set; }

        public bool RemoveZeros { get; set; }
        public bool OffsetsInHex { get; set; }

        public readonly SuperHeader dosHeader, dosStub, peHeader, optionalPeHeader;
        private readonly FileStream file;

        public ProcessHandler(FileStream file)
        {
            if (file == null) return;
            this.file = file;
            FileName = new FileInfo(file.Name).Name;

            // This specifies that the array will be 16 across and (file.Length / 16) down
            // I am precomputing these values so that I dont have to recompute them when the user switches windows or modes
            //filesHex = new string[(int)Math.Ceiling(file.Length / 16.0), 16];
            filesHex = new string[(int)Math.Ceiling(file.Length / 16.0), 3];
            filesDecimal = new string[(int)Math.Ceiling(file.Length / 16.0), 2];
            filesBinary = new string[(int)Math.Ceiling(file.Length / 16.0), 2];

            populateArrays();

            Console.WriteLine("Starting DOS HEADER");
            dosHeader = new DosHeader(this);
            Console.WriteLine("Starting DOS STUB");
            dosStub = new DosStub(this);
            Console.WriteLine("Starting PE Header");
            peHeader = new PeHeader(this, dosStub.EndPoint); // I am making this a paramter to aid when I aid support for Rich Headers which come after the DosStub and before this
        }

        public string getHex(int row, int column, bool doubleByte)
        {
            if (row > filesHex.GetLength(0) - 1 || column > filesHex.GetLength(1)) return "";
            if (!doubleByte || column == 0 || column == 2) return filesHex[row, column]; // Returns the single byte data, offset, or the description
            return getBigEndian(filesHex[row, column]);
        }

        public string getDecimal(int row, int column, bool doubleByte)
        {
            if (row > filesDecimal.GetLength(0) - 1 || column > filesHex.GetLength(1)) return "";
            if (column > 1) return filesHex[row, column]; // This is here because this array does not contain the desc
            if (column == 0 && OffsetsInHex) return filesHex[row, 0]; // This means the offsets should be displayed in hex
            if (!doubleByte || column == 0) return filesDecimal[row, column]; // Returns the single byte data or the offset
            // This will reverse the decimal and switch it from little-endian to big-endian
            string bigEndianHex = getBigEndian(filesHex[row, column]);
            return string.Join(" ", bigEndianHex.Split(' ').Select(hexPair => int.Parse(hexPair, NumberStyles.HexNumber)));
        }

        public string getBinary(int row, int column, bool doubleByte)
        {
            if (row > filesBinary.GetLength(0) - 1 || column > filesHex.GetLength(1)) return "";
            if (column > 1) return filesHex[row, column]; // This is here because this array does not contain the desc
            if (column == 0 && OffsetsInHex) return filesHex[row, 0]; // This means the offsets should be displayed in hex
            if (!doubleByte || column == 0) return filesBinary[row, column]; // Returns the single byte data or the offset
            // This will reverse the binary and switch it from little-endian to big-endian
            string bigEndianHex = getBigEndian(filesHex[row, column]);
            return string.Join(" ", bigEndianHex.Split(' ').Select(hexPair => Convert.ToString(Convert.ToInt32(hexPair, 16), 2)));
        }

        private string getBigEndian(string start)
        {
            string[] hexPairs = start.Split(' ');
            string reversedHexValues = string.Join(" ",
            hexPairs.Where((value, index) => index % 2 == 1)
                .Select((value, index) =>
                {
                    // This part changes 0000 into a single 0
                    string firstPart = hexPairs[index * 2 + 1];
                    string secondPart = hexPairs[index * 2];
                    string combined = firstPart + secondPart;
                    if (RemoveZeros)
                    {
                        if (secondPart == "00" && firstPart == "00") combined = "0";
                        else if ((combined = combined.TrimStart('0')).Length == 0) combined = "0";
                    }
                    return combined;
                }));
            return reversedHexValues;
        }

        /* This will populate all our arrays  */
        private void populateArrays()
        {
            try
            {
                //byte[] buffer = new byte[1];
                byte[] buffer = new byte[16];
                int bytesRead;
                //int across = 0;
                int down = 0;

                //Console.WriteLine("Length0:" + filesHex.GetLength(0) + " Length1:" + filesHex.GetLength(1));
                while ((bytesRead = file.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string hex = BitConverter.ToString(buffer, 0, bytesRead).Replace("-", " ");
                    string[] hexBytes = hex.Split(' ');
                    string decimalNumbers = string.Join(" ", hexBytes.Select(hexByte => Convert.ToInt32(hexByte, 16).ToString()));
                    string binary = string.Join(" ", buffer.Take(bytesRead).Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));

                    // Now we need to generate the ASCII characters that will be added to our hex array 
                    string ascii = "";
                    foreach (byte b in buffer)
                    {
                        char asciiChar = b >= 32 && b <= 126 ? (char)b : '.';
                        ascii += asciiChar;
                    }

                    //Console.WriteLine("Dec:" + decimalNumbers);

                    // filesHex only gets the ascii since it will be the same for the other two arrays
                    // so theres no point wasting memory on adding it.
                    filesHex[down, 0] = "0x" + (down * 16).ToString("X");
                    filesHex[down, 1] = hex;
                    filesHex[down, 2] = ascii;

                    filesDecimal[down, 0] = (down * 16).ToString();
                    filesDecimal[down, 1] = decimalNumbers;

                    filesBinary[down, 0] = Convert.ToString(down * 16, 2);
                    filesBinary[down, 1] = binary;

                    down++;
                }
                Console.WriteLine("DownCount:" + down);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }


        void ModifyExecutable(string originalPath, string newPath)
        {
            if (file == null) return;
            try
            {
                // Read the original executable into a byte array
                byte[] originalBytes = File.ReadAllBytes(originalPath);

                // Identify the offset and length of the data to modify
                int offset = 0; // Replace with the correct offset
                byte[] newData = Encoding.UTF8.GetBytes("New String"); // Convert your new data

                // Copy the new data into the byte array
                Array.Copy(newData, 0, originalBytes, offset, newData.Length);

                // Write the modified byte array to a new executable file
                File.WriteAllBytes(newPath, originalBytes);

                Console.WriteLine("Executable modified and saved as a new version.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }


        public enum ProcessComponent
        {
            EVERYTHING, DOS_HEADER, DOS_STUB, PE_HEADER, OPITIONAL_PE_HEADER, 
            SECTION_TEXT, SECTION_DATA, SECTION_RSRC, NULL_COMPONENT
        }

        public static ProcessComponent getProcessComponent(string name)
        {
            switch(name.ToLower())
            {
                case "everything": return ProcessComponent.EVERYTHING;
                case "dos header": return ProcessComponent.DOS_HEADER;
                case "dos stub": return ProcessComponent.DOS_STUB;
                case "pe header": return ProcessComponent.PE_HEADER;
                case "optional pe header": return ProcessComponent.OPITIONAL_PE_HEADER;
            }
            return ProcessComponent.NULL_COMPONENT;
        }
    }
}
