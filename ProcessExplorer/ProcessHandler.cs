using System;
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

        public bool Is64Bit { get; set; }

        public OffsetType Offset { get; set; }

        private int HeaderEndPoint { get; set; }

        // These following fields are all only initlized inside the constructor and thus marked 'readonly' 
        public readonly Dictionary<ProcessComponent, SuperHeader> componentMap = new Dictionary<ProcessComponent, SuperHeader>();
        private readonly FileStream file;

        /* This prevents us from getting an error if the map does not contain a specific ProcessComponent */
        public SuperHeader GetComponentFromMap(ProcessComponent comp)
        {
            if (!componentMap.ContainsKey(comp)) return null;
            return componentMap[comp];
        }

        public void RecalculateHeaders(SuperHeader comp)
        {
            if (comp.Component == ProcessComponent.DOS_HEADER) componentMap[ProcessComponent.DOS_HEADER] = new DosHeader(this);
            else if (comp.Component == ProcessComponent.DOS_STUB) componentMap[ProcessComponent.DOS_STUB] = new DosStub(this);
            else if (comp.Component == ProcessComponent.PE_HEADER) componentMap[ProcessComponent.PE_HEADER] = new PeHeader(this, GetComponentFromMap(ProcessComponent.DOS_STUB).EndPoint);
            else if (comp.Component == ProcessComponent.OPITIONAL_PE_HEADER) componentMap[ProcessComponent.OPITIONAL_PE_HEADER] = new OptionalPeHeader(this, GetComponentFromMap(ProcessComponent.PE_HEADER).EndPoint);
            else if (comp.Component == ProcessComponent.OPITIONAL_PE_HEADER_64) componentMap[ProcessComponent.OPITIONAL_PE_HEADER_64] = new OptionalPeHeader64(this, GetComponentFromMap(ProcessComponent.OPITIONAL_PE_HEADER).EndPoint);
            else if (comp.Component == ProcessComponent.OPITIONAL_PE_HEADER_32) componentMap[ProcessComponent.OPITIONAL_PE_HEADER_32] = new OptionalPeHeader64(this, GetComponentFromMap(ProcessComponent.OPITIONAL_PE_HEADER).EndPoint);
            else if (comp.Component == ProcessComponent.OPITIONAL_PE_HEADER_DATA_DIRECTORIES)
            {
                if (((OptionalPeHeader)GetComponentFromMap(ProcessComponent.OPITIONAL_PE_HEADER)).peThirtyTwoPlus)
                {
                    componentMap[ProcessComponent.OPITIONAL_PE_HEADER_DATA_DIRECTORIES] =
                        new OptionalPeHeaderDataDirectories(this, GetComponentFromMap(ProcessComponent.OPITIONAL_PE_HEADER_64).EndPoint);
                }
                else
                {
                    componentMap[ProcessComponent.OPITIONAL_PE_HEADER_DATA_DIRECTORIES] =
                        new OptionalPeHeaderDataDirectories(this, GetComponentFromMap(ProcessComponent.OPITIONAL_PE_HEADER_32).EndPoint);
                }
            }
            else
            {
                AssignSectionHeaders(GetComponentFromMap(ProcessComponent.EVERYTHING).hexArray, HeaderEndPoint);
            }
        }

        public ProcessHandler(FileStream file)
        {
            if (file == null) return;
            this.file = file;
            FileName = new FileInfo(file.Name).Name;

            Offset = OffsetType.FILE_OFFSET;

            // This specifies that the array will be 16 across and (file.Length / 16) down
            // I am precomputing these values so that I dont have to recompute them when the user switches windows or modes
            string[,] filesHex = new string[(int)Math.Ceiling(file.Length / 16.0), 3];
            string[,] filesDecimal = new string[(int)Math.Ceiling(file.Length / 16.0), 2];
            string[,] filesBinary = new string[(int)Math.Ceiling(file.Length / 16.0), 2];

            PopulateArrays(filesHex, filesDecimal, filesBinary); // Passes a reference to the memory address of the above arrays instead of their values

            componentMap.Add(ProcessComponent.EVERYTHING, new Everything(this, filesHex, filesDecimal, filesBinary));
            if (GetComponentFromMap(ProcessComponent.EVERYTHING).EndPoint <= 2) return;            // The file is esentially blank  

            componentMap.Add(ProcessComponent.DOS_HEADER, new DosHeader(this));
            if (GetComponentFromMap(ProcessComponent.DOS_HEADER).FailedToInitlize) return;         // This means this is not a PE

            componentMap.Add(ProcessComponent.DOS_STUB, new DosStub(this));                 // This means our PE does not contain a dos stub which is not normal
            if (GetComponentFromMap(ProcessComponent.DOS_STUB).FailedToInitlize) return;
            // Possible To:Do look into finding a PE that uses RichHeaders since those can sometimes appear before the PE Header
            componentMap.Add(ProcessComponent.PE_HEADER, new PeHeader(this, GetComponentFromMap(ProcessComponent.DOS_STUB).EndPoint));
            if (GetComponentFromMap(ProcessComponent.PE_HEADER).FailedToInitlize) return;          // This means our PE Header is either too short or does not contain the signature

            componentMap.Add(ProcessComponent.OPITIONAL_PE_HEADER, new OptionalPeHeader(this, GetComponentFromMap(ProcessComponent.PE_HEADER).EndPoint));

            int endPoint;
            if (((OptionalPeHeader)GetComponentFromMap(ProcessComponent.OPITIONAL_PE_HEADER)).validHeader)
            { // This means we most likely have either 64 or 32 bit option headers
                if (((OptionalPeHeader)GetComponentFromMap(ProcessComponent.OPITIONAL_PE_HEADER)).peThirtyTwoPlus)
                {   // This means our optional header is valid and that we are using 64 bit headers
                    componentMap.Add(ProcessComponent.OPITIONAL_PE_HEADER_64, new OptionalPeHeader64(this, GetComponentFromMap(ProcessComponent.OPITIONAL_PE_HEADER).EndPoint));
                    componentMap.Add(ProcessComponent.OPITIONAL_PE_HEADER_DATA_DIRECTORIES, 
                        new OptionalPeHeaderDataDirectories(this, GetComponentFromMap(ProcessComponent.OPITIONAL_PE_HEADER_64).EndPoint));
                }
                else
                {
                    componentMap.Add(ProcessComponent.OPITIONAL_PE_HEADER_32, new OptionalPeHeader32(this, GetComponentFromMap(ProcessComponent.OPITIONAL_PE_HEADER).EndPoint));
                    componentMap.Add(ProcessComponent.OPITIONAL_PE_HEADER_DATA_DIRECTORIES,
                        new OptionalPeHeaderDataDirectories(this, GetComponentFromMap(ProcessComponent.OPITIONAL_PE_HEADER_32).EndPoint));
                }
                endPoint = GetComponentFromMap(ProcessComponent.OPITIONAL_PE_HEADER_DATA_DIRECTORIES).EndPoint;
            }
            else
            {
                endPoint = GetComponentFromMap(ProcessComponent.PE_HEADER).EndPoint;
            }
            HeaderEndPoint = endPoint;

            // Recursively adds sections
            AssignSectionHeaders(filesHex, endPoint);
        }

        /* This will return the new end point and it will return -1 if we reached the last section */
        private void AssignSectionHeaders(string[,] filesHex, int startPoint)
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

                        if (totalCount++ > 8) return; // If we dont find it then we must of found all of the section headers

                        if (GetProcessComponent(ascii + " section header") != ProcessComponent.NULL_COMPONENT)
                        {
                            ProcessComponent sectionType = GetProcessComponent(ascii + " section header");
                            ProcessComponent sectionBodyType = GetProcessComponent(ascii + " section body");
                            SectionHeader header = new SectionHeader(this, startPoint, sectionType);
                            SectionBody body = new SectionBody(this, header.bodyStartPoint, header.bodyEndPoint, sectionBodyType);

                            if (componentMap.ContainsKey(sectionType)) componentMap[sectionType] = header;
                            else componentMap.Add(sectionType, header);

                            if(componentMap.ContainsKey(sectionBodyType)) componentMap[sectionBodyType] = body;
                            else componentMap.Add(sectionBodyType, body);

                            AssignSectionHeaders(filesHex, header.EndPoint);
                            return;
                        }
                    }
                }
                initialSkipAmount = 0;
            }
            return;
        }

        public int GetComponentsRowIndexCount(ProcessComponent component)
        {
            SuperHeader header = GetComponentFromMap(component);
            if (header == null) return 0;
            return header.hexArray.GetLength(0) - 1;
        }

        public int GetComponentsColumnIndexCount(ProcessComponent component)
        {
            SuperHeader header = GetComponentFromMap(component);
            if (header == null) return 0;
            return header.hexArray.GetLength(1) - 1;
        }


        /* Returns data that will fill up the DataDisplayView */
        public string GetValue(int row, int column, bool doubleByte, ProcessComponent component, DataType type)
        {
            SuperHeader header = GetComponentFromMap(component);
            if (header == null) return "";

            switch(type)
            {
                case DataType.HEX: return header.GetHex(row, column, doubleByte);
                case DataType.DECIMAL: 
                    if(column == 2) return header.GetHex(row, 2, doubleByte);
                    return header.GetDecimal(row, column, doubleByte);
                case DataType.BINARY:
                    if (column == 2) return header.GetHex(row, 2, doubleByte);
                    return header.GetBinary(row, column, doubleByte);
            }
            return "";
        }
         
        public void OpenDescrptionForm(ProcessComponent component, int row)
        {
            SuperHeader header = GetComponentFromMap(component);
            if(header != null) header.OpenForm(row);
        }



        /* This will populate all our arrays  */
        private void PopulateArrays(string[,] filesHex, string[,] filesDecimal, string[,] filesBinary)
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
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }

        public void SaveFile(string outputPath)
        {
            int length = GetComponentFromMap(ProcessComponent.EVERYTHING).hexArray.GetLength(0);
            string[] hexDataArray = new string[length];

            for (int row = 0; row < length; row++)
            {
                hexDataArray[row] = GetComponentFromMap(ProcessComponent.EVERYTHING).hexArray[row, 1];
            }

            try
            {
                using (FileStream outputFileStream = new FileStream(outputPath, FileMode.Create))
                {
                    foreach (string hexRow in hexDataArray)
                    {
                        // Remove any spaces or other non-hex characters
                        string cleanedHexRow = hexRow.Replace(" ", "");

                        // Convert the cleaned hex string to bytes
                        byte[] bytes = new byte[cleanedHexRow.Length / 2];
                        for (int i = 0; i < bytes.Length; i++)
                        {
                            bytes[i] = Convert.ToByte(cleanedHexRow.Substring(i * 2, 2), 16);
                        }

                        // Write the bytes to the output file
                        outputFileStream.Write(bytes, 0, bytes.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }


        public enum ProcessComponent
        {
            EVERYTHING, DOS_HEADER, DOS_STUB, PE_HEADER, OPITIONAL_PE_HEADER, OPITIONAL_PE_HEADER_64, OPITIONAL_PE_HEADER_32, OPITIONAL_PE_HEADER_DATA_DIRECTORIES,
            SECTION_BODY, SECTION_HEADER, NULL_COMPONENT,

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

        public enum OffsetType
        {
            FILE_OFFSET, RELATIVE_OFFSET
        }

        public enum DataType
        {
            HEX, DECIMAL, BINARY
        }

        public static ProcessComponent GetProcessComponent(string name)
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
