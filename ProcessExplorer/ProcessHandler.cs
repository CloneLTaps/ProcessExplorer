using System;
using System.Text;
using System.Data;
using System.Linq;
using System.IO;
using ProcessExplorer.components;
using ProcessExplorer.components.impl;
using System.Collections.Generic;

namespace ProcessExplorer
{
    class ProcessHandler
    {
        public string FileName { get; private set; }

        public bool RemoveZeros { get; set; }
        public bool OffsetsInHex { get; set; }

        public bool ReterunToTop { get; set; }

        public readonly SuperHeader everything, dosHeader, dosStub, peHeader, optionalPeHeader, optionalPeHeader64, optionalPeHeader32, optionalPeHeaderDataDirectories;
        public readonly Dictionary<SuperHeader.SectionTypes, List<SuperHeader>> sectionHeaders = new Dictionary<SuperHeader.SectionTypes, List<SuperHeader>>();
        private readonly FileStream file;

        public bool is64Bit;

        public ProcessHandler(FileStream file)
        {
            if (file == null) return;
            this.file = file;
            FileName = new FileInfo(file.Name).Name;

            // This specifies that the array will be 16 across and (file.Length / 16) down
            // I am precomputing these values so that I dont have to recompute them when the user switches windows or modes
            string[,] filesHex = new string[(int)Math.Ceiling(file.Length / 16.0), 3];
            string[,] filesDecimal = new string[(int)Math.Ceiling(file.Length / 16.0), 2];
            string[,] filesBinary = new string[(int)Math.Ceiling(file.Length / 16.0), 2];

            populateArrays(filesHex, filesDecimal, filesBinary); // Passes a reference to the memory address of the above arrays instead of their values

            int endPoint = 0; // Will be used for the sections

            everything = new Everything(this, filesHex, filesDecimal, filesBinary);
            dosHeader = new DosHeader(this);
            dosStub = new DosStub(this);
            peHeader = new PeHeader(this, dosStub.EndPoint); // I am making this a paramter to aid when I aid support for Rich Headers which come after the DosStub and before this

            optionalPeHeader = new OptionalPeHeader(this, peHeader.EndPoint);

            if (((OptionalPeHeader)optionalPeHeader).validHeader)
            { // This means we most likely have either 64 or 32 bit option headers
                if (((OptionalPeHeader)optionalPeHeader).peThirtyTwoPlus)
                {   // This means our optional header is valid and that we are using 64 bit headers
                    optionalPeHeader64 = new OptionalPeHeader64(this, optionalPeHeader.EndPoint);
                    optionalPeHeaderDataDirectories = new OptionalPeHeaderDataDirectories(this, optionalPeHeader64.EndPoint);
                }
                else
                {
                    optionalPeHeader32 = new OptionalPeHeader32(this, optionalPeHeader.EndPoint);
                    optionalPeHeaderDataDirectories = new OptionalPeHeaderDataDirectories(this, optionalPeHeader32.EndPoint);
                }
                endPoint = optionalPeHeaderDataDirectories.EndPoint;
            }
            else
            {
                optionalPeHeader = null; // This means we dont have a valid optional header
                endPoint = peHeader.EndPoint;
            }

            while(endPoint >= 0) // This will add all of the section headers
            {
                Console.WriteLine("\t previousEndPoint:" + endPoint);
                endPoint = AssignSectionHeaders(filesHex, endPoint);
            }

        }

        /* This will return the new end point and it will return -1 if we reached the last section */
        private int AssignSectionHeaders(string[,] filesHex, int startPoint)
        {
            int initialSkipAmount = startPoint % 16; // The amount we need to skip before we reach our target byte 
            int startingIndex = startPoint <= 0 ? 0 : (int)Math.Floor(startPoint / 16.0);
            string ascii = ""; // Section name
            int totalCount = 0;
            for (int row = startingIndex; row < filesHex.GetLength(0); row++) // Loop through the rows
            {
                string[] hexBytes = filesHex[row, 1].Split(' ');

                for (int j = initialSkipAmount; j < hexBytes.Length; j++) // Loop through each rows bytes
                {
                    if (byte.TryParse(hexBytes[j], System.Globalization.NumberStyles.HexNumber, null, out byte b))
                    {
                        char asciiChar = b >= 32 && b <= 126 ? (char)b : '.';
                        ascii += asciiChar;

                        if (totalCount++ > 8) return -1; // If we dont find it then we must of found all of the section headers

                        Console.WriteLine("\t ASCII:" + ascii);

                        if (SuperHeader.GetSectionType(ascii) != SuperHeader.SectionTypes.NULL_SECTION_TYPE)
                        {
                            SuperHeader.SectionTypes sectionType = SuperHeader.GetSectionType(ascii);
                            SectionHeader header = new SectionHeader(this, startPoint, sectionType);
                            List<SuperHeader> headerList = new List<SuperHeader>();
                            headerList.Add(header);
                            headerList.Add(new SectionBody(this, header.bodyStartPoint, header.bodyEndPoint, sectionType));

                            Console.WriteLine("HEADER:" + header.GetSectionType().ToString() );
                            sectionHeaders.Add(sectionType, headerList);
                            return header.EndPoint;
                        }
                    }
                }
                initialSkipAmount = 0;
            }
            return -1;
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
                byte[] buffer = new byte[16];
                int bytesRead;
                int down = 0;

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
                case ProcessComponent.OPITIONAL_PE_HEADER_DATA_DIRECTORIES: return optionalPeHeaderDataDirectories;
/*
                case ProcessComponent.TEXT_SECTION_HEADER: return sectionHeaders[SuperHeader.SectionTypes.TEXT][0];
                case ProcessComponent.DATA_SECTION_HEADER: return sectionHeaders[SuperHeader.SectionTypes.DATA][0];
                case ProcessComponent.RSRC_SECTION_HEADER: return sectionHeaders[SuperHeader.SectionTypes.RSRC][0];
                case ProcessComponent.TEXT_SECTION_BODY: return sectionHeaders[SuperHeader.SectionTypes.TEXT][1];
                case ProcessComponent.DATA_SECTION_BODY: return sectionHeaders[SuperHeader.SectionTypes.DATA][1];
                case ProcessComponent.RSRC_SECTION_BODY: return sectionHeaders[SuperHeader.SectionTypes.RSRC][1];*/

            }

            SuperHeader.SectionTypes comEnum;
            if(Enum.TryParse(component.ToString().Replace("_SECTION_HEADER", ""), true, out comEnum) && sectionHeaders.ContainsKey(comEnum))
            {
                return sectionHeaders[comEnum][0];
            }
            else if(Enum.TryParse(component.ToString().Replace("_SECTION_BODY", ""), true, out comEnum) && sectionHeaders.ContainsKey(comEnum))
            {
                return sectionHeaders[comEnum][1];
            }
            return null;
        }


        public enum ProcessComponent
        {
            EVERYTHING, DOS_HEADER, DOS_STUB, PE_HEADER, OPITIONAL_PE_HEADER, OPITIONAL_PE_HEADER_64, OPITIONAL_PE_HEADER_32, OPITIONAL_PE_HEADER_DATA_DIRECTORIES,
            SECTION_TEXT, SECTION_DATA, SECTION_RSRC, NULL_COMPONENT,

            TEXT_SECTION_HEADER, TEXT_SECTION_BODY,     // Executable code
            DATA_SECTION_HEADER, DATA_SECTION_BODY,     // Initlized data
            RDATA_SECTION_HEADER, RDATA_SECTION_BODY,   // Ready only data like constants 
            PDATA_SECTION_HEADER, PDATA_SECTION_BODY,   // Exception handling storage
            IDATA_SECTION_HEADER, IDATA_SECTION_BODY,   // Contains information relating to dynamic linking of imported functions from external dll's
            EDATA_SECTION_HEADER, EDATA_SECTION_BODY,   // Contains information about exported functions and data by this PE file
            XDATA_SECTION_HEADER, XDATA_SECTION_BODY,   // Contains additonal exception handling 
            RSRC_SECTION_HEADER, RSRC_SECTION_BODY,     // Resources like icons, bitmaps, strings, version info
            RELOC_SECTION_HEADER, RELOC_SECTION_BODY,   // Base relocations (preferred load address differs from actual load address in mem)

            TLS_SECTION_HEADER, TLS_SECTION_BODY,       // Thread local storage data used by multi-threaded programs
            DEBUG_SECTION_HEADER, DEBUG_SECTION_BODY,   // Stores debugging information like source code and line numbers
            ARCH_SECTION_HEADER, ARCH_SECTION_BODY,     // Architecture specific code / data
            BSS_SECTION_HEADER, BSS_SECTION_BODY,       // Data that will be initilized to 0 during program startup
            CORMETA_SECTION_HEADER, CORMETA_SECTION_BODY,// .NET metadata sructure
            SXDATA_SECTION_HEADER, SXDATA_SECTION_BODY, // Structured exception handling

            // Fairly rare to see structures
            CRT_SECTION_HEADER, CRT_SECTION_BODY,       // Contains code that initilizes global and static objects used in Visual Studio's C++
            BIND_SECTION_HEADER, BIND_SECTION_BODY,     // Contains info relating to binding or linking with dll's
            CET_SECTION_HEADER, CET_SECTION_BODY,       // Control flow integrity checks for programs using Control Flor Gaurd (CFG)
            SDATA_SECTION_HEADER, SDATA_SECTION_BODY,   // Contains data that can be shared among different instnaces of the same exe
            GLUE_7T_SECTION_HEADER, GLUE_7T_SECTION_BODY,// Contains glue code used for transitioning between ARM and Thumb code
            SBSS_SECTION_HEADER, SBSS_SECTION_BODY,     // Contains uninitialized data similar to ".bss" but may be used for smaller or specialized purpose
            SBDATA_SECTION_HEADER, SBDATA_SECTION_BODY, // Contains initialized data similar to ".data" but may be used for smaller or specialized purposes.
            SBSS_M_SECTION_HEADER, SBSS_M_SECTION_BODY, // Variants of ".sbss" for specific data initialization needs.
            SBDATA_M_SECTION_HEADER, SBDATA_M_SECTION_BODY, // Variants of ".sbdata" for specific data initialization needs.
            RODATA1_SECTION_HEADER, RODATA1_SECTION_BODY, // Variants of ".rdata" for specialized read-only data storage.
            VSDATA_SECTION_HEADER, VSDATA_SECTION_BODY, // Contains data related to virtual function tables in C++ programs.

            TBSS_SECTION_HEADER, TBSS_SECTION_BODY,     // Similar to ".tls" but for thread-local uninitialized data.
            TDATA_SECTION_HEADER, TDATA_SECTION_BODY,   // Similar to ".tls" but for thread-local initialized data.
            VFDATA_SECTION_HEADER, VFDATA_SECTION_BODY, // Contains data related to virtual function tables in C++ programs.

            GLUE_7_SECTION_HEADER, GLUE_7_SECTION_BODY, // Contains glue code for ARM code execution.
            VITAL_SECTION_HEADER, VITAL_SECTION_BODY,   // Contains data related to virtual function tables in C++ programs.
            ROBASE_SECTION_HEADER, ROBASE_SECTION_BODY, // Contains read-only base relocation information.
            RANDOM_SECTION_HEADER, RANDOM_SECTION_BODY, // Used in some security features and mechanisms to enhance program randomization.
            BOLT_SECTION_HEADER, BOLT_SECTION_BODY,     // Used by the Binary Optimization and Layout Tool (BOLT) for performance optimization.
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
                case "optional pe header data directories": return ProcessComponent.OPITIONAL_PE_HEADER_DATA_DIRECTORIES;

                case ".text section header": return ProcessComponent.TEXT_SECTION_HEADER;
                case ".text section body": return ProcessComponent.TEXT_SECTION_BODY;
                case ".data section header": return ProcessComponent.DATA_SECTION_HEADER;
                case ".data section body": return ProcessComponent.DATA_SECTION_BODY;
                case ".rdata section header": return ProcessComponent.RDATA_SECTION_HEADER;
                case ".rdata section body": return ProcessComponent.RDATA_SECTION_BODY;
                case ".pdata section header": return ProcessComponent.PDATA_SECTION_HEADER;
                case ".pdata section body": return ProcessComponent.PDATA_SECTION_BODY;
                case ".idata section header": return ProcessComponent.IDATA_SECTION_HEADER;
                case ".idata section body": return ProcessComponent.IDATA_SECTION_BODY;
                case ".edata section header": return ProcessComponent.EDATA_SECTION_HEADER;
                case ".edata section body": return ProcessComponent.EDATA_SECTION_BODY;
                case ".xdata section header": return ProcessComponent.XDATA_SECTION_HEADER;
                case ".xdata section body": return ProcessComponent.XDATA_SECTION_BODY;
                case ".rsrc section header": return ProcessComponent.RSRC_SECTION_HEADER;
                case ".rsrc section body": return ProcessComponent.RSRC_SECTION_BODY;

                case ".reloc section header": return ProcessComponent.RELOC_SECTION_HEADER;
                case ".reloc section body": return ProcessComponent.RELOC_SECTION_BODY;
                case ".tls section header": return ProcessComponent.TLS_SECTION_HEADER;
                case ".tls section body": return ProcessComponent.TLS_SECTION_BODY;
                case ".debug section header": return ProcessComponent.DEBUG_SECTION_HEADER;
                case ".debug section body": return ProcessComponent.DEBUG_SECTION_BODY;
                case ".arch section header": return ProcessComponent.ARCH_SECTION_HEADER;
                case ".arch section body": return ProcessComponent.ARCH_SECTION_BODY;
                case ".bss section header": return ProcessComponent.BSS_SECTION_HEADER;
                case ".bss section body": return ProcessComponent.BSS_SECTION_BODY;
                case ".cormeta section header": return ProcessComponent.CORMETA_SECTION_HEADER;
                case ".cormeta section body": return ProcessComponent.CORMETA_SECTION_BODY;
                case ".sxdata section header": return ProcessComponent.SXDATA_SECTION_HEADER;
                case ".sxdata section body": return ProcessComponent.SXDATA_SECTION_BODY;
                case ".crt section header": return ProcessComponent.CRT_SECTION_HEADER;
                case ".crt section body": return ProcessComponent.CRT_SECTION_BODY;

                case ".bind section header": return ProcessComponent.BIND_SECTION_HEADER;
                case ".bind section body": return ProcessComponent.BIND_SECTION_BODY;
                case ".cet section header": return ProcessComponent.CET_SECTION_HEADER;
                case ".cet section body": return ProcessComponent.CET_SECTION_BODY;
                case ".sdata section header": return ProcessComponent.SDATA_SECTION_HEADER;
                case ".sdata section body": return ProcessComponent.SDATA_SECTION_BODY;
                case ".glue_7t section header": return ProcessComponent.GLUE_7T_SECTION_HEADER;
                case ".glue_7t section body": return ProcessComponent.GLUE_7T_SECTION_BODY;
                case ".sbss section header": return ProcessComponent.SBSS_SECTION_HEADER;
                case ".sbss section body": return ProcessComponent.SBSS_SECTION_BODY;
                case ".sbdata section header": return ProcessComponent.SBDATA_SECTION_HEADER;
                case ".sbdata section body": return ProcessComponent.SBDATA_SECTION_BODY;
                case ".sbss$ section header": return ProcessComponent.SBSS_M_SECTION_HEADER;
                case ".sbss$ section body": return ProcessComponent.SBSS_M_SECTION_BODY;
                case ".sbdata$ section header": return ProcessComponent.SBDATA_M_SECTION_HEADER;
                case ".sbdata$ section body": return ProcessComponent.SBDATA_M_SECTION_BODY;

                case ".rodata1 section header": return ProcessComponent.RODATA1_SECTION_HEADER;
                case ".rodata1 section body": return ProcessComponent.RODATA1_SECTION_BODY;
                case ".vsdata section header": return ProcessComponent.VSDATA_SECTION_HEADER;
                case ".vsdata section body": return ProcessComponent.VSDATA_SECTION_BODY;
                case ".tbss section header": return ProcessComponent.TBSS_SECTION_HEADER;
                case ".tbss section body": return ProcessComponent.TBSS_SECTION_BODY;
                case ".tdata section header": return ProcessComponent.TDATA_SECTION_HEADER;
                case ".tdata section body": return ProcessComponent.TDATA_SECTION_BODY;
                case ".vfdata section header": return ProcessComponent.VFDATA_SECTION_HEADER;
                case ".vfdata section body": return ProcessComponent.VFDATA_SECTION_BODY;

                case ".glue_7 section header": return ProcessComponent.GLUE_7_SECTION_HEADER;
                case ".glue_7 section body": return ProcessComponent.GLUE_7_SECTION_BODY;
                case ".vital section header": return ProcessComponent.VITAL_SECTION_HEADER;
                case ".vital section body": return ProcessComponent.VITAL_SECTION_BODY;
                case ".robase section header": return ProcessComponent.ROBASE_SECTION_HEADER;
                case ".robase section body": return ProcessComponent.ROBASE_SECTION_BODY;
                case ".random section header": return ProcessComponent.RANDOM_SECTION_HEADER;
                case ".random section body": return ProcessComponent.RANDOM_SECTION_BODY;
                case ".bolt section header": return ProcessComponent.BOLT_SECTION_HEADER;
                case ".bolt section body": return ProcessComponent.BOLT_SECTION_BODY;
            }
            return ProcessComponent.NULL_COMPONENT;
        }

    }
}
