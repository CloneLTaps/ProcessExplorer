using System;
using System.Linq;
using System.Globalization;
using System.Windows.Forms;
using System.Collections.Generic;

namespace ProcessExplorer.components
{
    class PeHeader : PluginInterface.SuperHeader
    {
        public int SectionAmount { get; private set; }
        public PeHeader(ProcessHandler processHandler, PluginInterface.DataStorage dataStorage, int startingPoint) : base("pe header", 8, 3)
        {
            if(processHandler.GetComponentFromMap("everything").EndPoint <= 
                processHandler.GetComponentFromMap("dos stub").EndPoint + 24)
            {   // This means this file will not contain the nessary PE Header fields thus making this invalid
                FailedToInitlize = true;
                return;
            }

            StartPoint = startingPoint;

            Desc = new string[RowSize];
            Size = new int[RowSize];
            Size[0] = 4; Desc[0] = "Signature (4 bytes) \"PE\0\0\" denotes start of the PE.";
            Size[1] = 2; Desc[1] = "Machine (2 bytes) taget machines architecture <click for more details>.";
            Size[2] = 2; Desc[2] = "NumberOfSections (2 bytes) total number of sections.";
            Size[3] = 4; Desc[3] = "TimeDateStamp (4 bytes) <click for more details>.";
            Size[4] = 4; Desc[4] = "PointerToSymbolTable (4 bytes) present in .obj files but not in final build.";
            Size[5] = 4; Desc[5] = "NumberOfSymbols (4 bytes) ^ examples of symbols are function and variable names.";
            Size[6] = 2; Desc[6] = "SizeOfOptionalHeader (2 bytes) size of the optional header which precedes this.";
            Size[7] = 2; Desc[7] = "Characteristics (2 bytes) target architecture <click for more details>.";

            SetEndPoint();

            string[] signatureHex = GetData(0, 1, PluginInterface.Enums.DataType.HEX, 1, true, dataStorage).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (signatureHex[0] != "50" || signatureHex[1] != "45" && signatureHex[2] != "00" || signatureHex[3] != "00")
            {   // This means this is not a valid PE since the header signature was incorrect
                FailedToInitlize = true; 
                return;
            }

            int bit = int.Parse(GetBigEndianValue(GetData(1, 1, PluginInterface.Enums.DataType.HEX, 1, true, dataStorage)), NumberStyles.HexNumber);
            dataStorage.Is64Bit = (bit == 0x8664);

            SectionAmount = int.Parse(GetData(2, 1, PluginInterface.Enums.DataType.DECIMAL, 2, true, dataStorage)); // Sets the amount of sections

            // None of our headers have more than 1 characteristic type structure 
            Characteristics = new Dictionary<int, string> {
                { 0x0001, "IMAGE_FILE_RELOCS_STRIPPED" },
                { 0x0002, "IMAGE_FILE_EXECUTABLE_IMAGE" },
                { 0x0004, "IMAGE_FILE_LINE_NUMS_STRIPPED" },
                { 0x0008, "IMAGE_FILE_LOCAL_SYMS_STRIPPED" },
                { 0x0010, "IMAGE_FILE_AGGRESIVE_WS_TRIM" },
                { 0x0020, "IMAGE_FILE_LARGE_ADDRESS_AWARE" },
                { 0x0040, dataStorage.Is64Bit ? "IMAGE_FILE_RELOCS_STRIPPED" : "IMAGE_FILE_BYTES_REVERSED_HI"},
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
        }

        public override void OpenForm(int row, PluginInterface.DataStorage dataStorage)
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

                using OptionsForm optionsForm = new OptionsForm(this, null, "Machine", row, options, null, null, dataStorage);
                DialogResult result = optionsForm.ShowDialog();
                if (result == DialogResult.OK)
                {
                    string updatedComboBox = optionsForm.GetUpdatedComboBoxValue();
                    if (updatedComboBox != null) UpdateData(1, updatedComboBox, true, false, dataStorage);
                }
            }
            else if(row == 3)
            {
                string hexValue = GetData(row, 1, PluginInterface.Enums.DataType.HEX, 2, false, dataStorage);
                uint unixTimestamp = uint.Parse(hexValue, NumberStyles.HexNumber);

                DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime dateTime = unixEpoch.AddSeconds(unixTimestamp);

                Console.WriteLine(dateTime.Date.ToString());

                using OptionsForm optionsForm = new OptionsForm(this, null, "Date and Time", row, null, null, dateTime, dataStorage);
                DialogResult result = optionsForm.ShowDialog();
                if (result == DialogResult.OK)
                {
                    string updatedDate = optionsForm.GetUpdatedDateAndTime();
                    UpdateData(3, updatedDate, true, false, dataStorage);
                }
            }
            else if(row == 7 && Characteristics != null)
            {
                string[] combinedStrings = Characteristics.Select(kv => $"{"0x" + (kv.Key).ToString("X")} - {kv.Value}").ToArray();

                using OptionsForm optionsForm = new OptionsForm(this, null, "Characteristics", row, null, combinedStrings, null, dataStorage);
                DialogResult result = optionsForm.ShowDialog();
                if (result == DialogResult.OK)
                {
                    string updatedCharacteristic = optionsForm.GetUpdatedCharacterisitcs();
                    UpdateData(7, updatedCharacteristic, true, false, dataStorage);
                }
            }
        }


    }
}
