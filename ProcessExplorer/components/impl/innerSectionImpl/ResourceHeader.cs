using PluginInterface;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using static PluginInterface.Enums;

namespace ProcessExplorer.components.impl.innerImpl //0x2F4
{
    public class ResourceHeader : SuperHeader
    {
        public ResourceHeader(int startingPoint, DataStorage dataStorage) : base("resource header", 6, 3)
        {   // base class will initialzie our fields
            StartPoint = startingPoint;

            Desc = new string[RowSize];
            Size = new int[RowSize];
            Size[0] = 4; Desc[0] = "Characteristics (4 bytes) not typically used.";
            Size[1] = 4; Desc[1] = "TimeDateStamp (4 bytes) <click for more details>.";
            Size[2] = 2; Desc[2] = "MajorVersion (2 bytes) primary version number.";
            Size[3] = 2; Desc[3] = "MinorVersion (2 bytes) secondary version number.";
            Size[4] = 2; Desc[4] = "NumberOfNamedEntries (2 bytes) entries that are referenced by their name.";
            Size[5] = 2; Desc[5] = "NumberOfIdEntries (2 bytes) entries that are referenced by an ID.";

            SetEndPoint();
        }

        public void ParseResources(DataStorage dataStorage)
        {
            int rowIndex = 0;
            bool continueParsing = true;
            while (continueParsing)
            {
                string hexData = GetData(rowIndex, 1, DataType.HEX, 1, false, dataStorage); // Represents a max of 16 bytes of hex where each byte is seperated by a space
                if (string.IsNullOrEmpty(hexData))
                {
                    continueParsing = false;
                    continue;
                }

                //ParseResourceEntry(hexData);
                rowIndex++;
            }
        }

        public override void OpenForm(int row, DataStorage dataStorage)
        {
            if(row == 1)
            {
                string hexValue = GetData(row, 1, DataType.HEX, 2, false, dataStorage);
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
        }

    }
}
