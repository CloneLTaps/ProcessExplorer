using System;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;

namespace ProcessExplorer.components.impl
{
    class OptionalPeHeader32 : SuperHeader
    {
        public OptionalPeHeader32(ProcessHandler processHandler, int startingPoint) : base(processHandler, "optional pe header 32", 22, 3)
        {
            StartPoint = startingPoint;

            Desc = new string[RowSize];
            Size = new int[RowSize];
            Size[0] = 4; Desc[0] = "BaseOfData (4 bytes) points to starting relative virtual address of the .data section";
            Size[1] = 4; Desc[1] = "ImageBase (4 bytes) prefered base address when loaded.";
            Size[2] = 4; Desc[2] = "SectionAlignment (4 bytes) memory section size (padding will be added if needed).";
            Size[3] = 4; Desc[3] = "FileAlignment (4 bytes) disk section size (padding will be added if needed).";
            Size[4] = 2; Desc[4] = "MajorOperatingSystemVersion (2 bytes) earliest windows os this file can run on.";
            Size[5] = 2; Desc[5] = "MinorOperatingSystemVersion (2 bytes) ^ <click for more details>";
            Size[6] = 2; Desc[6] = "MajorImageVersion (2 bytes) marjor version assicoated with this file.";
            Size[7] = 2; Desc[7] = "MinorImageVersion (2 bytes) minor version assicoated with this file.";
            Size[8] = 2; Desc[8] = "MajorSubsystemVersion (2 bytes) earliest windows os the gui can run on.";
            Size[9] = 2; Desc[9] = "MinorSubsystemVersion (2 bytes) ^ <click for more details> ";
            Size[10] = 4; Desc[10] = "Win32VersionValue (4 bytes) reserved, must be zero.";
            Size[11] = 4; Desc[11] = "SizeOfImage (4 bytes) size of entire file when loaded into memory (multiple of SectionAlignment).";
            Size[12] = 4; Desc[12] = "SizeOfHeaders (4 bytes) size of all the headers rounded up to a multiple of FileAlignment.";
            Size[13] = 4; Desc[13] = "CheckSum (4 bytes) integrity check, 0 means its modified or this was never done when compiling.";
            Size[14] = 2; Desc[14] = "Subsystem (2 bytes) required sub system to be able to run this <click for more details>.";
            Size[15] = 2; Desc[15] = "DllCharacteristics (2 bytes) dll characteristics <click for more details>.";
            Size[16] = 4; Desc[16] = "SizeOfStackReserve (4 bytes) amount of virtual stack memory reserved on startup (max stack size).";
            Size[17] = 4; Desc[17] = "SizeOfStackCommit (4 bytes) amount of initially commited physical memory for the stack.";
            Size[18] = 4; Desc[18] = "SizeOfHeapReserve (4 bytes) amount of virtual heap memory reserved on startup (max heap size).";
            Size[19] = 4; Desc[19] = "SizeOfHeapCommit (4 bytes) amount of initially commited physical memory for the heap.";
            Size[20] = 4; Desc[20] = "LoaderFlags (4 bytes) Reserved, must be zero.";
            Size[21] = 4; Desc[21] = "NumberOfRvaAndSizes (4 bytes) specifies how many entires our data directory contains.";

            SetEndPoint();

            Characteristics = new Dictionary<int, string> {
                { 0x0020, "IMAGE_DLLCHARACTERISTICS_HIGH_ENTROPY_VA" },
                { 0x0040, "IMAGE_DLLCHARACTERISTICS_DYNAMIC_BASE" },
                { 0x0080, "IMAGE_DLLCHARACTERISTICS_FORCE_INTEGRITY" },
                { 0x0100, "IMAGE_DLLCHARACTERISTICS_NX_COMPAT" },
                { 0x0200, "IMAGE_DLLCHARACTERISTICS_NO_ISOLATION" },
                { 0x0400, "IMAGE_DLLCHARACTERISTICS_NO_SEH" },
                { 0x0800, "IMAGE_DLLCHARACTERISTICS_NO_BIND" },
                { 0x1000, "IMAGE_DLLCHARACTERISTICS_APPCONTAINER" },
                { 0x2000, "IMAGE_DLLCHARACTERISTICS_WDM_DRIVER" },
                { 0x8000, "IMAGE_DLLCHARACTERISTICS_TERMINAL_SERVER_AWARE" }
            };
        }

        public override void OpenForm(int row)
        {
            if (row == 5)
            {
                string[] options = { "0x00000000 - No version information", "0x00000004 - Windows 95", "0x00100004 - Windows 98", "0x00000005 - Windows 2000",
                "0x00100005 - Windows XP", "0x00200005 - Windows XP x64", "0x00000006 - Windows Vista", "0x00100006 - Windows 7", "0x00200006 - Windows 8",
                    "0x00300006 - Windows 8.1", "0x00000010  - Windows 10" };

                string combinedHex = OptionsForm.GetBigEndianValue(GetData(row - 1, 1, ProcessHandler.DataType.HEX, false, false) + " " + GetData(row, 1, ProcessHandler.DataType.HEX, false, false));
                using (OptionsForm optionsForm = new OptionsForm(this, combinedHex, "Minimum Operating System Version", row, options, null, null))
                {
                    DialogResult result = optionsForm.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        return; // ToDo
                        string updatedComboBox = optionsForm.GetUpdatedComboBoxValue();
                        if (updatedComboBox != null) UpdateData(5, updatedComboBox, true, false);
                    }
                }
            }
            else if (row == 9)

            {
                string[] options = { "0x00000000 - No version information", "0x00000004 - Windows 95", "0x00100004 - Windows 98", "0x00000005 - Windows 2000",
                "0x00100005 - Windows XP", "0x00200005 - Windows XP x64", "0x00000006 - Windows Vista", "0x00100006 - Windows 7", "0x00200006 - Windows 8",
                    "0x00300006 - Windows 8.1", "0x00000010  - Windows 10" };

                string combinedHex = OptionsForm.GetBigEndianValue(GetData(row - 1, 1, ProcessHandler.DataType.HEX, false, false) + " " + GetData(row, 1, ProcessHandler.DataType.HEX, false, false));
                using (OptionsForm optionsForm = new OptionsForm(this, combinedHex, "Minimum Subsystem Version", row, options, null, null))
                {
                    DialogResult result = optionsForm.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        return; // ToDo
                        string updatedComboBox = optionsForm.GetUpdatedComboBoxValue();
                        if (updatedComboBox != null) UpdateData(9, updatedComboBox, true, false);
                    }
                }
            }
            else if (row == 14)
            {
                string[] options = { "0x0000 - IMAGE_SUBSYSTEM_UNKNOWN", "0x0001 - IMAGE_SUBSYSTEM_NATIVE", "0x0002 - IMAGE_SUBSYSTEM_WINDOWS_GUI", "0x0003 - IMAGE_SUBSYSTEM_WINDOWS_CUI",
                "0x0004 - IMAGE_SUBSYSTEM_OS2_CUI", "0x0005 - IMAGE_SUBSYSTEM_POSIX_CUI", "0x0006 - IMAGE_SUBSYSTEM_NATIVE_WINDOWS", "0x0007 - IMAGE_SUBSYSTEM_WINDOWS_CE_GUI",
                    "0x0008 - IMAGE_SUBSYSTEM_EFI_APPLICATION", "0x0009 - IMAGE_SUBSYSTEM_EFI_BOOT_SERVICE_DRIVER", "0x000A - IMAGE_SUBSYSTEM_EFI_RUNTIME_DRIVER",
                "0x000B - IMAGE_SUBSYSTEM_EFI_ROM", "0x000C - IMAGE_SUBSYSTEM_XBOX", "0x000D - IMAGE_SUBSYSTEM_WINDOWS_BOOT_APPLICATION" };

                using (OptionsForm optionsForm = new OptionsForm(this, null, "SubSystems", row, options, null, null))
                {
                    DialogResult result = optionsForm.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        string updatedComboBox = optionsForm.GetUpdatedComboBoxValue();
                        if (updatedComboBox != null) UpdateData(14, updatedComboBox, true, false);
                    }
                }
            }
            else if (row == 15 && Characteristics != null)
            {
                string[] combinedStrings = Characteristics.Select(kv => $"{"0x" + (kv.Key).ToString("X")} - {kv.Value}").ToArray();

                using (OptionsForm optionsForm = new OptionsForm(this, null, "DLL Characteristics", row, null, combinedStrings, null))
                {
                    DialogResult result = optionsForm.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        string updatedCharacteristic = optionsForm.GetUpdatedCharacterisitcs();
                        UpdateData(15, updatedCharacteristic, true, false);
                    }
                }
            }
        }
    }
}
