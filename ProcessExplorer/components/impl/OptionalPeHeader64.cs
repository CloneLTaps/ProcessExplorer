using System;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;

namespace ProcessExplorer.components.impl
{
    class OptionalPeHeader64 : PluginInterface.SuperHeader
    {
        public OptionalPeHeader64(int startingPoint) : base("optional pe header 64", 21, 3)
        {
            StartPoint = startingPoint;

            Desc = new string[RowSize];
            Size = new int[RowSize];
            Size[0] = 8; Desc[0] = "ImageBase (8 bytes) prefered base address when loaded.";
            Size[1] = 4; Desc[1] = "SectionAlignment (4 bytes) memory section size (padding will be added if needed).";
            Size[2] = 4; Desc[2] = "FileAlignment (4 bytes) disk section size (padding will be added if needed).";
            Size[3] = 2; Desc[3] = "MajorOperatingSystemVersion (2 bytes) earliest windows os this file can run on.";
            Size[4] = 2; Desc[4] = "MinorOperatingSystemVersion (2 bytes) ^ <click for more details>";
            Size[5] = 2; Desc[5] = "MajorImageVersion (2 bytes) marjor version assicoated with this file.";
            Size[6] = 2; Desc[6] = "MinorImageVersion (2 bytes) minor version assicoated with this file.";
            Size[7] = 2; Desc[7] = "MajorSubsystemVersion (2 bytes) earliest windows os the gui can run on.";
            Size[8] = 2; Desc[8] = "MinorSubsystemVersion (2 bytes) ^ <click for more details> ";
            Size[9] = 4; Desc[9] = "Win32VersionValue (4 bytes) reserved, must be zero.";
            Size[10] = 4; Desc[10] = "SizeOfImage (4 bytes) size of entire file when loaded into memory (multiple of SectionAlignment).";
            Size[11] = 4; Desc[11] = "SizeOfHeaders (4 bytes) size of all the headers rounded up to a multiple of FileAlignment.";
            Size[12] = 4; Desc[12] = "CheckSum (4 bytes) integrity check, 0 means its modified or this was never done when compiling.";
            Size[13] = 2; Desc[13] = "Subsystem (2 bytes) required sub system to be able to run this <click for more details>.";
            Size[14] = 2; Desc[14] = "DllCharacteristics (2 bytes) dll characteristics <click for more details>.";
            Size[15] = 8; Desc[15] = "SizeOfStackReserve (8 bytes) amount of virtual stack memory reserved on startup (max stack size).";
            Size[16] = 8; Desc[16] = "SizeOfStackCommit (8 bytes) amount of initially commited physical memory for the stack.";
            Size[17] = 8; Desc[17] = "SizeOfHeapReserve (8 bytes) amount of virtual heap memory reserved on startup (max heap size).";
            Size[18] = 8; Desc[18] = "SizeOfHeapCommit (8 bytes) amount of initially commited physical memory for the heap.";
            Size[19] = 4; Desc[19] = "LoaderFlags (4 bytes) Reserved, must be zero.";
            Size[20] = 4; Desc[20] = "NumberOfRvaAndSizes (4 bytes) specifies how many entires our data directory contains.";

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

        public override void OpenForm(int row, PluginInterface.DataStorage dataStorage) 
        {
            if (row == 4)
            {
                string[] options = { "0x00000000 - No version information", "0x00000004 - Windows 95", "0x00100004 - Windows 98", "0x00000005 - Windows 2000",
                "0x00100005 - Windows XP", "0x00200005 - Windows XP x64", "0x00000006 - Windows Vista", "0x00100006 - Windows 7", "0x00200006 - Windows 8",
                    "0x00300006 - Windows 8.1", "0x00000010  - Windows 10" };

                string combinedHex = OptionsForm.GetBigEndianValue(GetData(row - 1, 1, PluginInterface.Enums.DataType.HEX, 1, false, dataStorage) + " " 
                    + GetData(row, 1, PluginInterface.Enums.DataType.HEX, 1, false, dataStorage));
                using (OptionsForm optionsForm = new OptionsForm(this, combinedHex, "Minimum Operating System Version", row, options, null, null, dataStorage))
                {
                    DialogResult result = optionsForm.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        return; // ToDo
                        string updatedComboBox = optionsForm.GetUpdatedComboBoxValue();
                        if (updatedComboBox != null) UpdateData(4, updatedComboBox, true, false, dataStorage);
                    }
                }
            }
            else if (row == 8)
            {
                string[] options = { "0x00000000 - No version information", "0x00000004 - Windows 95", "0x00100004 - Windows 98", "0x00000005 - Windows 2000",
                "0x00100005 - Windows XP", "0x00200005 - Windows XP x64", "0x00000006 - Windows Vista", "0x00100006 - Windows 7", "0x00200006 - Windows 8",
                    "0x00300006 - Windows 8.1", "0x00000010  - Windows 10" };

                string combinedHex = OptionsForm.GetBigEndianValue(GetData(row - 1, 1, PluginInterface.Enums.DataType.HEX, 1, false, dataStorage) + " " 
                    + GetData(row, 1, PluginInterface.Enums.DataType.HEX, 1, false, dataStorage));
                using (OptionsForm optionsForm = new OptionsForm(this, combinedHex, "Minimum Subsystem Version", row, options, null, null, dataStorage))
                {
                    DialogResult result = optionsForm.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        return; // ToDo
                        string updatedComboBox = optionsForm.GetUpdatedComboBoxValue();
                        if (updatedComboBox != null) UpdateData(8, updatedComboBox, true, false, dataStorage);
                    }
                }
            }
            else if (row == 13)
            {
                string[] options = { "0x0000 - IMAGE_SUBSYSTEM_UNKNOWN", "0x0001 - IMAGE_SUBSYSTEM_NATIVE", "0x0002 - IMAGE_SUBSYSTEM_WINDOWS_GUI", "0x0003 - IMAGE_SUBSYSTEM_WINDOWS_CUI",
                "0x0004 - IMAGE_SUBSYSTEM_OS2_CUI", "0x0005 - IMAGE_SUBSYSTEM_POSIX_CUI", "0x0006 - IMAGE_SUBSYSTEM_NATIVE_WINDOWS", "0x0007 - IMAGE_SUBSYSTEM_WINDOWS_CE_GUI",
                    "0x0008 - IMAGE_SUBSYSTEM_EFI_APPLICATION", "0x0009 - IMAGE_SUBSYSTEM_EFI_BOOT_SERVICE_DRIVER", "0x000A - IMAGE_SUBSYSTEM_EFI_RUNTIME_DRIVER",
                "0x000B - IMAGE_SUBSYSTEM_EFI_ROM", "0x000C - IMAGE_SUBSYSTEM_XBOX", "0x000D - IMAGE_SUBSYSTEM_WINDOWS_BOOT_APPLICATION" };

                using (OptionsForm optionsForm = new OptionsForm(this, null, "Sub System Required", row, options, null, null, dataStorage))
                {
                    DialogResult result = optionsForm.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        string updatedComboBox = optionsForm.GetUpdatedComboBoxValue();
                        if (updatedComboBox != null) UpdateData(13, updatedComboBox, true, false, dataStorage);
                    }
                }
            }
            else if(row == 14 && Characteristics != null)
            {
                string[] combinedStrings = Characteristics.Select(kv => $"{"0x" + (kv.Key).ToString("X")} - {kv.Value}").ToArray();

                using (OptionsForm optionsForm = new OptionsForm(this, null, "DLL Characteristics", row, null, combinedStrings, null, dataStorage))
                {
                    DialogResult result = optionsForm.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        string updatedCharacteristic = optionsForm.GetUpdatedCharacterisitcs();
                        UpdateData(14, updatedCharacteristic, true, false, dataStorage);
                    }
                }
            }
        }

    }
}
