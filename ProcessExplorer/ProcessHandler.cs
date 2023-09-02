using System;
using System.Text;
using System.Data;
using System.Linq;
using System.IO;
using ProcessExplorer.components;
using ProcessExplorer.components.impl;
using System.Globalization;

namespace ProcessExplorer
{
    class ProcessHandler
    {
        public string FileName { get; private set; }

        public bool RemoveZeros { get; set; }
        public bool OffsetsInHex { get; set; }

        public bool ReterunToTop { get; set; }

        public readonly SuperHeader everything, dosHeader, dosStub, peHeader, optionalPeHeader, optionalPeHeader64, optionalPeHeader32, optionalPeHeaderDataDirectories;
        private readonly FileStream file;

        public bool is64Bit;

        public ProcessHandler(FileStream file)
        {
            if (file == null) return;
            this.file = file;
            FileName = new FileInfo(file.Name).Name;

            // This specifies that the array will be 16 across and (file.Length / 16) down
            // I am precomputing these values so that I dont have to recompute them when the user switches windows or modes
            //filesHex = new string[(int)Math.Ceiling(file.Length / 16.0), 16];
            string[,] filesHex = new string[(int)Math.Ceiling(file.Length / 16.0), 3];
            string[,] filesDecimal = new string[(int)Math.Ceiling(file.Length / 16.0), 2];
            string[,] filesBinary = new string[(int)Math.Ceiling(file.Length / 16.0), 2];

            populateArrays(filesHex, filesDecimal, filesBinary); // Passes a reference to the memory address of these arrays instead of their values

            everything = new Everything(this, filesHex, filesDecimal, filesBinary);
            Console.WriteLine("Starting DOS HEADER");
            dosHeader = new DosHeader(this);
            Console.WriteLine("Starting DOS STUB");
            dosStub = new DosStub(this);
            Console.WriteLine("Starting PE Header");
            peHeader = new PeHeader(this, dosStub.EndPoint); // I am making this a paramter to aid when I aid support for Rich Headers which come after the DosStub and before this
            Console.WriteLine("Starting PE Optional Header");
            optionalPeHeader = new OptionalPeHeader(this, peHeader.EndPoint);

            if(((OptionalPeHeader)optionalPeHeader).validHeader)
            { // This means we most likely have either 64 or 32 bit option headers
                if (((OptionalPeHeader)optionalPeHeader).peThirtyTwoPlus)
                {   // This means our optional header is valid and that we are using 64 bit headers
                    Console.WriteLine("Starting PE Optional Header 64");
                    optionalPeHeader64 = new OptionalPeHeader64(this, optionalPeHeader.EndPoint);
                    optionalPeHeaderDataDirectories = new OptionalHeaderDataDirectories(this, optionalPeHeader64.EndPoint);
                }
                else
                {
                    optionalPeHeader32 = new OptionalPeHeader32(this, optionalPeHeader.EndPoint);
                    optionalPeHeaderDataDirectories = new OptionalHeaderDataDirectories(this, optionalPeHeader32.EndPoint);
                }
            }
            else optionalPeHeader = null; // This means we dont have a valid optional header

        }

        public SuperHeader GetSuperHeader(ProcessComponent component)
        {
            switch (component)
            {
                case ProcessComponent.EVERYTHING: return everything;
                case ProcessComponent.DOS_HEADER: return dosHeader;
                case ProcessComponent.DOS_STUB: return dosStub;
                case ProcessComponent.PE_HEADER: return peHeader;
                case ProcessComponent.OPITIONAL_PE_HEADER: return optionalPeHeader;
                case ProcessComponent.OPITIONAL_PE_HEADER_64: return optionalPeHeader64;
                case ProcessComponent.OPITIONAL_PE_HEADER_32: return optionalPeHeader32;
            }
            return null;
        }

        public int GetComponentsRowIndexCount(ProcessComponent component)
        {
            SuperHeader header = GetSuperHeader(component);
            if (header == null) return 0;
            return header.hexArray.GetLength(0) - 1;
        }

        public int GetComponentsColumnIndexCount(ProcessComponent component)
        {
            SuperHeader header = GetSuperHeader(component);
            if (header == null) return 0;
            return header.hexArray.GetLength(1) - 1;
        }


        /* Returns data that will fill up the DataDisplayView */
        public string GetValue(int row, int column, bool doubleByte, ProcessComponent component, DataType type)
        {
            SuperHeader header = GetSuperHeader(component);
            if (header == null) return "";

            switch(type)
            {
                case DataType.HEX: return header.getHex(row, column, doubleByte);
                case DataType.DECIMAL: 
                    if(column == 2) return header.getHex(row, 2, doubleByte);
                    return header.getDecimal(row, column, doubleByte);
                case DataType.BINARY:
                    if (column == 2) return header.getHex(row, 2, doubleByte);
                    return header.getBinary(row, column, doubleByte);
            }
            return "";
        }
         
        public void OpenDescrptionForm(ProcessComponent component, int row)
        {
            SuperHeader header = GetSuperHeader(component);
            if(header != null) header.OpenForm(row);
        }


        /* This will populate all our arrays  */
        private void populateArrays(string[,] filesHex, string[,] filesDecimal, string[,] filesBinary)
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
            EVERYTHING, DOS_HEADER, DOS_STUB, PE_HEADER, OPITIONAL_PE_HEADER, OPITIONAL_PE_HEADER_64, OPITIONAL_PE_HEADER_32,
            SECTION_TEXT, SECTION_DATA, SECTION_RSRC, NULL_COMPONENT
        }

        public enum DataType
        {
            HEX, DECIMAL, BINARY
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
                case "optional pe header 64": return ProcessComponent.OPITIONAL_PE_HEADER_64;
                case "optional pe header 32": return ProcessComponent.OPITIONAL_PE_HEADER_32;
            }
            return ProcessComponent.NULL_COMPONENT;
        }
    }
}
