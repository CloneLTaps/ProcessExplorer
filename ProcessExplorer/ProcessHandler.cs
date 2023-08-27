using System;
using System.Text;
using System.Data;
using System.Linq;
using System.IO;
using System.Globalization;

namespace ProcessExplorer
{
    class ProcessHandler
    {
        public string[,] filesHex { get; private set; }
        public string[,] filesBinary { get; private set; }
        public string[,] filesDecimal { get; private set; }
        public string FileName { get; private set; }
        private FileStream file;
  
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

                    Console.WriteLine("Dec:" + decimalNumbers);

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
