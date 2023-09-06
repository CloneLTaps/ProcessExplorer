using System;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;

namespace ProcessExplorer.components.impl
{
    class OptionalPeHeader64 : SuperHeader
    {
        public OptionalPeHeader64(ProcessHandler processHandler, int startingPoint) : base(processHandler, 21, 3, true)
        {
            string[,] sizeAndDesc = new string[21, 2];
            sizeAndDesc[0, 0] = "8"; sizeAndDesc[0, 1] = "ImageBase (8 bytes) prefered base address when loaded.";
            sizeAndDesc[1, 0] = "4"; sizeAndDesc[1, 1] = "SectionAlignment (4 bytes) memory section size (padding will be added if needed).";
            sizeAndDesc[2, 0] = "4"; sizeAndDesc[2, 1] = "FileAlignment (4 bytes) disk section size (padding will be added if needed).";
            sizeAndDesc[3, 0] = "2"; sizeAndDesc[3, 1] = "MajorOperatingSystemVersion (2 bytes) earliest windows os this file can run on.";
            sizeAndDesc[4, 0] = "2"; sizeAndDesc[4, 1] = "MinorOperatingSystemVersion (2 bytes) ^ <click for more details>";
            sizeAndDesc[5, 0] = "2"; sizeAndDesc[5, 1] = "MajorImageVersion (2 bytes) marjor version assicoated with this file.";
            sizeAndDesc[6, 0] = "2"; sizeAndDesc[6, 1] = "MinorImageVersion (2 bytes) minor version assicoated with this file.";
            sizeAndDesc[7, 0] = "2"; sizeAndDesc[7, 1] = "MajorSubsystemVersion (2 bytes) earliest windows os the gui can run on.";
            sizeAndDesc[8, 0] = "2"; sizeAndDesc[8, 1] = "MinorSubsystemVersion (2 bytes) ^ <click for more details> ";
            sizeAndDesc[9, 0] = "4"; sizeAndDesc[9, 1] = "Win32VersionValue (4 bytes) reserved, must be zero.";
            sizeAndDesc[10, 0] = "4"; sizeAndDesc[10, 1] = "SizeOfImage (4 bytes) size of entire file when loaded into memory (multiple of SectionAlignment).";
            sizeAndDesc[11, 0] = "4"; sizeAndDesc[11, 1] = "SizeOfHeaders (4 bytes) size of all the headers rounded up to a multiple of FileAlignment.";
            sizeAndDesc[12, 0] = "4"; sizeAndDesc[12, 1] = "CheckSum (4 bytes) integrity check, 0 means its modified or this was never done when compiling.";
            sizeAndDesc[13, 0] = "2"; sizeAndDesc[13, 1] = "Subsystem (2 bytes) required sub system to be able to run this <click for more details>.";
            sizeAndDesc[14, 0] = "2"; sizeAndDesc[14, 1] = "DllCharacteristics (2 bytes) dll characteristics <click for more details>.";
            sizeAndDesc[15, 0] = "8"; sizeAndDesc[15, 1] = "SizeOfStackReserve (8 bytes) amount of virtual stack memory reserved on startup (max stack size).";
            sizeAndDesc[16, 0] = "8"; sizeAndDesc[16, 1] = "SizeOfStackCommit (8 bytes) amount of initially commited physical memory for the stack.";
            sizeAndDesc[17, 0] = "8"; sizeAndDesc[17, 1] = "SizeOfHeapReserve (8 bytes) amount of virtual heap memory reserved on startup (max heap size).";
            sizeAndDesc[18, 0] = "8"; sizeAndDesc[18, 1] = "SizeOfHeapCommit (8 bytes) amount of initially commited physical memory for the heap.";
            sizeAndDesc[19, 0] = "4"; sizeAndDesc[19, 1] = "LoaderFlags (4 bytes) Reserved, must be zero.";
            sizeAndDesc[20, 0] = "4"; sizeAndDesc[20, 1] = "NumberOfRvaAndSizes (4 bytes) specifies how many entires our data directory contains.";

            StartPoint = startingPoint; // Must set the start point before populating the arrays
            populateArrays(sizeAndDesc);

            characteristics = new Dictionary<int, string> {
                { 0x0001, "IMAGE_DLLCHARACTERISTICS_HIGH_ENTROPY_VA" },
                { 0x0002, "IMAGE_DLLCHARACTERISTICS_DYNAMIC_BASE" },
                { 0x0004, "IMAGE_DLLCHARACTERISTICS_FORCE_INTEGRITY" },
                { 0x0008, "IMAGE_DLLCHARACTERISTICS_NX_COMPAT" },
                { 0x0010, "IMAGE_DLLCHARACTERISTICS_NO_ISOLATION" },
                { 0x0020, "IMAGE_DLLCHARACTERISTICS_NO_SEH" },
                { 0x0040, "IMAGE_DLLCHARACTERISTICS_NO_BIND" },
                { 0x0080, "IMAGE_DLLCHARACTERISTICS_APPCONTAINER" },
                { 0x0100, "IMAGE_DLLCHARACTERISTICS_WDM_DRIVER" },
                { 0x0200, "IMAGE_DLLCHARACTERISTICS_GUARD_CF" },
                { 0x0400, "IMAGE_DLLCHARACTERISTICS_TERMINAL_SERVER_AWARE" }
            };

        }

        public override void OpenForm(int row) 
        {
            if (row == 4)
            {
                string[] options = { "0x00000000 - No version information", "0x00000004 - Windows 95", "0x00100004 - Windows 98", "0x00000005 - Windows 2000",
                "0x00100005 - Windows XP", "0x00200005 - Windows XP x64", "0x00000006 - Windows Vista", "0x00100006 - Windows 7", "0x00200006 - Windows 8",
                    "0x00300006 - Windows 8.1", "0x00000010  - Windows 10" };

                string combinedHex = OptionsForm.GetBigEndianValue(hexArray[row - 1, 1] + " " + hexArray[row, 1]);
                Console.WriteLine("CombinedHexBigEndian:" + combinedHex + " LittleEndian:" + (hexArray[row - 1, 1] + " " + hexArray[row, 1]));

                using (OptionsForm optionsForm = new OptionsForm(this, combinedHex, "Minimum Operating System Version", row, options, null, null))
                {
                    DialogResult result = optionsForm.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        Console.WriteLine("Open custom Options box");

                    }
                }
            }
            else if (row == 8)
            {
                string[] options = { "0x00000000 - No version information", "0x00000004 - Windows 95", "0x00100004 - Windows 98", "0x00000005 - Windows 2000",
                "0x00100005 - Windows XP", "0x00200005 - Windows XP x64", "0x00000006 - Windows Vista", "0x00100006 - Windows 7", "0x00200006 - Windows 8",
                    "0x00300006 - Windows 8.1", "0x00000010  - Windows 10" };

                string combinedHex = OptionsForm.GetBigEndianValue(hexArray[row - 1, 1] + " " + hexArray[row, 1]);
                Console.WriteLine("CombinedHexBigEndian:" + combinedHex + " LittleEndian:" + (hexArray[row - 1, 1] + " " + hexArray[row, 1]));

                using (OptionsForm optionsForm = new OptionsForm(this, combinedHex, "Minimum Subsystem Version", row, options, null, null))
                {
                    DialogResult result = optionsForm.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        Console.WriteLine("Open custom Options box");

                    }
                }
            }
            else if (row == 13)
            {
                string[] options = { "0x0000 - IMAGE_SUBSYSTEM_UNKNOWN", "0x0001 - IMAGE_SUBSYSTEM_NATIVE", "0x0002 - IMAGE_SUBSYSTEM_WINDOWS_GUI", "0x0003 - IMAGE_SUBSYSTEM_WINDOWS_CUI",
                "0x0004 - IMAGE_SUBSYSTEM_OS2_CUI", "0x0005 - IMAGE_SUBSYSTEM_POSIX_CUI", "0x0006 - IMAGE_SUBSYSTEM_NATIVE_WINDOWS", "0x0007 - IMAGE_SUBSYSTEM_WINDOWS_CE_GUI",
                    "0x0008 - IMAGE_SUBSYSTEM_EFI_APPLICATION", "0x0009 - IMAGE_SUBSYSTEM_EFI_BOOT_SERVICE_DRIVER", "0x000A - IMAGE_SUBSYSTEM_EFI_RUNTIME_DRIVER",
                "0x000B - IMAGE_SUBSYSTEM_EFI_ROM", "0x000C - IMAGE_SUBSYSTEM_XBOX", "0x000D - IMAGE_SUBSYSTEM_WINDOWS_BOOT_APPLICATION" };

                using (OptionsForm optionsForm = new OptionsForm(this, null, "Sub System Required", row, options, null, null))
                {
                    DialogResult result = optionsForm.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        Console.WriteLine("Open custom Options box");

                    }
                }
            }
            else if(row == 14 && characteristics != null)
            {
                string[] combinedStrings = characteristics.Select(kv => $"{"0x" + (kv.Key).ToString("X")} - {kv.Value}").ToArray();

                using (OptionsForm optionsForm = new OptionsForm(this, null, "DLL Characteristics", row, null, combinedStrings, null))
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
