using System;
using System.Linq;
using System.Globalization;
using System.Windows.Forms;
using System.Collections.Generic;

namespace ProcessExplorer.components
{
    class PeHeader : SuperHeader
    {
        public PeHeader(ProcessHandler processHandler, int startingPoint) : base(processHandler, 8, 3, true)
        {
            if(processHandler.everything.EndPoint <= processHandler.dosStub.EndPoint + 24)
            {   // This means this file will not contain the nessary PE Header fields thus making this invalid
                FailedToInitlize = true;
                return;
            }

            string[,] sizeAndDesc = new string[8, 2];
            sizeAndDesc[0, 0] = "4"; sizeAndDesc[0, 1] = "Signature (4 bytes) \"PE\0\0\" denotes start of the PE.";
            sizeAndDesc[1, 0] = "2"; sizeAndDesc[1, 1] = "Machine (2 bytes) taget machines architecture <click for more details>.";
            sizeAndDesc[2, 0] = "2"; sizeAndDesc[2, 1] = "NumberOfSections (2 bytes) total number of sections.";
            sizeAndDesc[3, 0] = "4"; sizeAndDesc[3, 1] = "TimeDateStamp (4 bytes) <click for more details>.";
            sizeAndDesc[4, 0] = "4"; sizeAndDesc[4, 1] = "PointerToSymbolTable (4 bytes) present in .obj files but not in final build.";
            sizeAndDesc[5, 0] = "4"; sizeAndDesc[5, 1] = "NumberOfSymbols (4 bytes) ^ examples of symbols are function and variable names.";
            sizeAndDesc[6, 0] = "2"; sizeAndDesc[6, 1] = "SizeOfOptionalHeader (2 bytes) size of the optional header which precedes this.";
            sizeAndDesc[7, 0] = "2"; sizeAndDesc[7, 1] = "Characteristics (2 bytes) target architecture <click for more details>.";

            StartPoint = startingPoint; // Must set the start point before populating the arrays
            Console.WriteLine("PeHeader StartPoint:" + startingPoint);
            populateArrays(sizeAndDesc);

            string[] signatureHex = hexArray[0, 1].Split(" ");
            if(signatureHex[0] != "50" || signatureHex[1] != "45" && signatureHex[2] != "00" || signatureHex[3] != "00")
            {   // This means this is not a valid PE since the header signature was incorrect
                FailedToInitlize = true;
                Console.WriteLine("FAILED");    
                return;
            }

            int bit = int.Parse(OptionsForm.GetBigEndianValue(hexArray[1, 1]), NumberStyles.HexNumber);
            processHandler.is64Bit = bit == 0x8664;

            // None of our headers have more than 1 characteristic type structure 
            characteristics = new Dictionary<int, string> {
                { 0x0001, "IMAGE_FILE_RELOCS_STRIPPED" },
                { 0x0002, "IMAGE_FILE_EXECUTABLE_IMAGE" },
                { 0x0004, "IMAGE_FILE_LINE_NUMS_STRIPPED" },
                { 0x0008, "IMAGE_FILE_LOCAL_SYMS_STRIPPED" },
                { 0x0010, "IMAGE_FILE_AGGRESIVE_WS_TRIM" },
                { 0x0020, "IMAGE_FILE_LARGE_ADDRESS_AWARE" },
                { 0x0040, processHandler.is64Bit ? "IMAGE_FILE_RELOCS_STRIPPED" : "IMAGE_FILE_BYTES_REVERSED_HI"},
                { 0x0080, "IMAGE_FILE_BYTES_REVERSED_LO" },
                { 0x0100, "IMAGE_FILE_32BIT_MACHINE" },
                { 0x0200, "IMAGE_FILE_DEBUG_STRIPPED" },
                { 0x0400, "IMAGE_FILE_REMOVABLE_RUN_FROM_SWAP" },
                { 0x0800, "IMAGE_FILE_NET_RUN_FROM_SWAP" },
                { 0x1000, "IMAGE_FILE_SYSTEM" },
                { 0x2000, "IMAGE_FILE_DLL" },
                { 0x4000, "IMAGE_FILE_UP_SYSTEM_ONLY" },
                { 0x8000, "IMAGE_FILE_BYTES_REVERSED_HI" }
            };

            Console.WriteLine("PE Header" + " StartPoint:" + StartPoint + " EndPoint:" + EndPoint);
        }

        public override void OpenForm(int row)
        {
            if(row == 1)
            {
                string[] options = { "0x8664 - IMAGE_FILE_MACHINE_AMD64 (x64)", "0xEBC - IMAGE_FILE_MACHINE_EBC", "0x14C - IMAGE_FILE_MACHINE_I386 (x86)",
                "0x268 - IMAGE_FILE_MACHINE_IA64 (Itanium, x64)", "0x169 - IMAGE_FILE_MACHINE_R3000_BE (MIPS R3000, BE, 32-bit)", 
                    "0x183 - IMAGE_FILE_MACHINE_R4000_BE (MIPS R4000, BE, 32-bit)", "0x1A2 - IMAGE_FILE_MACHINE_R10000_BE (MIPS R10000, BE, 32-bit)",
                "0x1C0 - IMAGE_FILE_MACHINE_WCEMIPSV2 (MIPS WCE v2, 32-bit)", "0x1F0 - IMAGE_FILE_MACHINE_ALPHA (Alpha AXP, 64-bit)", "0x1F1 - IMAGE_FILE_MACHINE_SH3 (SH3, 32-bit)",
                    "0x1F2 - IMAGE_FILE_MACHINE_SH3DSP (SH3DSP, 32-bit)", "0x1F3 - IMAGE_FILE_MACHINE_SH3E (SH3E, 32-bit)", "0x1F4 - IMAGE_FILE_MACHINE_SH4 (SH4, 32-bit)",
                "0x1F5 - IMAGE_FILE_MACHINE_SH5 (SH5, 32-bit)", "0x1F6 - IMAGE_FILE_MACHINE_ARM (ARM, 32-bit)", "0x1F7 - IMAGE_FILE_MACHINE_THUMB (Thumb, 32-bit)",
                    "0x1F8 - IMAGE_FILE_MACHINE_ARMNT (ARM or Thumb 32-bit)", "0x1F9 - IMAGE_FILE_MACHINE_ARM64 (ARM64, 64-bit)",
                "0x1FA - IMAGE_FILE_MACHINE_THUMB2 (Thumb-2, 32-bit)", "0x1FB - IMAGE_FILE_MACHINE_AM33 (AM33, 32-bit)", "0x1FC - IMAGE_FILE_MACHINE_POWERPC (PowerPC, 32-bit)",
                    "0x1FD - IMAGE_FILE_MACHINE_POWERPCFP (PowerPCFP, 32-bit)", "0x200 - IMAGE_FILE_MACHINE_IA64 (Itanium, 64-bit)",  "0x266 - IMAGE_FILE_MACHINE_MIPS16 (MIPS16, 32-bit)",
                "0x284 - IMAGE_FILE_MACHINE_ALPHA64 (Alpha AXP, 64-bit)", "0x366 - IMAGE_FILE_MACHINE_MIPSFPU (MIPSFPU, 32-bit)", "0x466 - IMAGE_FILE_MACHINE_MIPSFPU16 (MIPSFPU16, 32-bit)",
                    "0x500 - IMAGE_FILE_MACHINE_TRICORE (TriCore, 32-bit)", "0x5A0 - IMAGE_FILE_MACHINE_CEF (CEF, 32-bit)", "0x600 - IMAGE_FILE_MACHINE_EBC",
                "0x9041 - IMAGE_FILE_MACHINE_M32R (M32R, 32-bit)", "0xC0EE - IMAGE_FILE_MACHINE_CEE (CEE, 32-bit)"};

                using (OptionsForm optionsForm = new OptionsForm(this, null, "Machine", row, options, null, null))
                {
                    DialogResult result = optionsForm.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        Console.WriteLine("Open custom Options box");

                    }
                }
            }
            else if(row == 3)
            {
                string hexValue = OptionsForm.GetBigEndianValue(hexArray[row, 1]);
                uint unixTimestamp = uint.Parse(hexValue, System.Globalization.NumberStyles.HexNumber);

                DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime dateTime = unixEpoch.AddSeconds(unixTimestamp);

                using (OptionsForm optionsForm = new OptionsForm(this, null, "Date and Time", row, null, null, dateTime))
                {
                    DialogResult result = optionsForm.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        Console.WriteLine("Open custom Options box");

                    }
                }
            }
            else if(row == 7 && characteristics != null)
            {
                string[] combinedStrings = characteristics.Select(kv => $"{"0x" + (kv.Key).ToString("X")} - {kv.Value}").ToArray();

                using (OptionsForm optionsForm = new OptionsForm(this, null, "Characteristics", row, null, combinedStrings, null))
                {
                    DialogResult result = optionsForm.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        Console.WriteLine("Open custom Options box");
        
                    }
                }
            }
        }


    }
}
