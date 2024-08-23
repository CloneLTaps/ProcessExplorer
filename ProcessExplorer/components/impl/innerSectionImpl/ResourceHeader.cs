using PluginInterface;
using ProcessExplorer.components.impl.innerSectionImpl;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using static PluginInterface.Enums;

namespace ProcessExplorer.components.impl.innerImpl 
{
    public class ResourceHeader : SuperHeader
    {
        public TreeNode RsrcNode { get; set; }

        public int NumberOfResources { get; private set; }
        public int NameEntryCount { get; private set; }

        private readonly DataStorage dataStorage; 

        public ResourceHeader(uint startingPoint, string headerName, DataStorage dataStorage) : base(headerName, 6, 3)
        {   // base class will initialzie our fields
            this.dataStorage = dataStorage;
            StartPoint = startingPoint;

            Desc = new string[RowSize];
            Size = new int[RowSize];
            Size[0] = 4; Desc[0] = "Characteristics (4 bytes) not typically used.";
            Size[1] = 4; Desc[1] = "TimeDateStamp (4 bytes) <click for more details>.";
            Size[2] = 2; Desc[2] = "MajorVersion (2 bytes) primary version number.";
            Size[3] = 2; Desc[3] = "MinorVersion (2 bytes) secondary version number.";
            Size[4] = 2; Desc[4] = "NumberOfNamedEntries (2 bytes) entries that are referenced by their name.";
            Size[5] = 2; Desc[5] = "NumberOfIdEntries (2 bytes) entries that are referenced by an ID.";

            NameEntryCount = int.Parse(GetData(4, 1, DataType.DECIMAL, 2, true, dataStorage));
            int idEntryCount = int.Parse(GetData(5, 1, DataType.DECIMAL, 2, true, dataStorage));
            Console.WriteLine($"NamedEntries:{NameEntryCount} IdEntries:{idEntryCount}");
            NumberOfResources = idEntryCount + NameEntryCount;

            SetEndPoint();
        }

        public void AddResources(uint rsrcBaseAddress, string tableName, Dictionary<string, SuperHeader> componentMap, TreeNode rsrcNode, ref int count)
        {
            count++;
            TreeNode headerNode = new TreeNode(this.Component);
            ResourceTable rsrcTable = new ResourceTable(EndPoint, NameEntryCount, NumberOfResources, tableName);
            componentMap[rsrcTable.Component] = rsrcTable;
            Console.WriteLine($"### TableAdded:{rsrcTable.Component} Count:{count}");

            TreeNode tableNode = new TreeNode(rsrcTable.Component);

            string name = "";
            for(int i=0; i<NumberOfResources * 2; i++)
            {
                if (i % 2 == 0)
                {   // Name offsets occur on even indexes
                    uint nameOffset = Convert.ToUInt32(rsrcTable.GetData(i, 1, DataType.HEX, 2, true, dataStorage), 16);
                    bool isName = (nameOffset & 0x80000000) != 0;

                    if (isName)
                    {
                        nameOffset &= 0x7FFFFFFF; // Clear the high bit for actual offset
                        string strName = $"resource string ({count})"; //{(tableName == "base resource table" ? "base" : $"({count - 1})")}
                        ResourceString rsrcString = ReadName(componentMap, nameOffset, strName);
                        name = rsrcString.ReadUnicodeString();
                        rsrcString.UpdateComponent(name);

                        tableNode.Nodes.Add(rsrcString.Component);
                        componentMap[rsrcString.Component] = rsrcString;
                    }
                    else name = nameOffset.ToString();
                }
                else
                {   // The data offsets occur on odd indexes 
                    uint dataOffset = Convert.ToUInt32(rsrcTable.GetData(i, 1, DataType.HEX, 2, true, dataStorage), 16);
                    bool isSubdirectory = (dataOffset & 0x80000000) != 0;
                    Console.WriteLine($"AddResources Name:{name} i:{i} isSub:{isSubdirectory} Data:{rsrcTable.GetData(i, 1, DataType.HEX, 1, true, dataStorage)} DataOffset:{dataOffset}" +
                        $" DataOffsetAdjusted:{dataOffset & 0x7FFFFFFF} FinalOffset:{(dataOffset & 0x7FFFFFFF) + rsrcBaseAddress}");
                    dataOffset &= 0x7FFFFFFF; // Clear the high bit for actual offset
                    dataOffset += (uint) rsrcBaseAddress;

                    if(!isSubdirectory)
                    {   // This is the easy case since our resource is not nestled in a sub directory so we can now set the actual resource's contents
                        count--;
                        ResourceBodyHeader rsrcEntry = new ResourceBodyHeader(dataOffset, (tableName == "base resource table" ? "base resource body header" : $"resource body header ({count}) {name}"), dataStorage);
                        componentMap[rsrcEntry.Component] = rsrcEntry;
                        tableNode.Nodes.Add(rsrcEntry.Component);

                        SectionHeader sectionHeader = (SectionHeader)componentMap[".rsrc section header"];
                        uint bodyStart = (uint)(sectionHeader.BodyStartPoint + (rsrcEntry.RVA - sectionHeader.VirtualAddress));

                        ResourceBody rsrcBody = new ResourceBody(bodyStart, (uint)(rsrcEntry.BodySize + bodyStart), (tableName == "base resource table" ? "base resource body" : $"resource body ({count}) {name}"));
                        componentMap[rsrcBody.Component] = rsrcBody;
                        tableNode.Nodes.Add(rsrcBody.Component);

                        Console.WriteLine($"***** EntryAdded:{rsrcEntry.Component}");
                        count++;
                    }
                    else
                    {   // Otherwise we will need to parse a new header and table
                        ResourceHeader rsrcHeader = new ResourceHeader(dataOffset, $"resource header ({count}) {name}", dataStorage);
                        componentMap[rsrcHeader.Component] = rsrcHeader;
                        Console.WriteLine($"! HeaderAdded:{rsrcHeader.Component}");
                        rsrcHeader.AddResources(rsrcBaseAddress, $"resource table ({count}) {name}", componentMap, tableNode, ref count);
                    }
                }
            }
            headerNode.Nodes.Add(tableNode);
            rsrcNode.Nodes.Add(headerNode);
            Console.WriteLine("Completed AddResources");
        }

        /// <summary> Takes the name offset and adds it to the base address of the rsrc section to get the 2 byte length field followed by the Unicode string. </summary>
        private ResourceString ReadName(Dictionary<string, SuperHeader> componentMap, long nameOffset, string name)
        {
            SectionBody sectionBody = (SectionBody)componentMap[".rsrc section body"];
            ResourceString rsrcString = new ResourceString(sectionBody.StartPoint + (uint) nameOffset, name, dataStorage);
            Console.WriteLine($"^^^^ ResourceString NameOffset:{nameOffset} Start:{sectionBody.StartPoint + nameOffset} StringLength:{rsrcString.StringLength}");
            return rsrcString;
        }

        /// <summary> This gets called in a loop when the superNode is rsrc section node. </summary>
        public static string AddNodes(TreeNode superNode, SuperHeader body)
        {
            string newCompString = body.Component;
            if (newCompString == "resource header") newCompString = "Resource Header";
            else if (newCompString == "resource table") newCompString = "Resource Table";

            TreeNode sectionNode = new TreeNode(newCompString);

            return newCompString;
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
