using System;
using System.Linq;
using System.Globalization;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace ProcessExplorer.components
{
    /* Super class for all other headers, sections, etc */
    abstract class SuperHeader
    {
        protected ProcessHandler processHandler;

        /* StartPoint and EndPoint will be saved using hex */
        public int StartPoint { get; protected set; }
        public int EndPoint { get; protected set; }

        public int RowSize { get; protected set; }
        public int ColumnSize { get; private set; }

        public int[] Size { get; protected set; }
        public string[] Desc { get; protected set; }

        public bool FailedToInitlize { get; protected set; }

        public ProcessHandler.ProcessComponent Component { get; protected set; }

        public Dictionary<int, string> Characteristics { get; protected set; }

        public SuperHeader(ProcessHandler processHandler, ProcessHandler.ProcessComponent component, int rowSize, int columnSize)
        {
            this.processHandler = processHandler;
            this.ColumnSize = columnSize;
            this.Component = component;
            this.RowSize = rowSize;

            FailedToInitlize = false;
        }

        public abstract void OpenForm(int row);

        protected void SetEndPoint()
        {
            if (Size == null) return;

            int count = StartPoint;
            foreach (int i in Size)
            {
                count += i;
            }
            EndPoint = count;
        }

        private string GetBigEndian(string[] hexPairs)
        {
            if (Size != null)
            {
                Array.Reverse(hexPairs);
                string bigEndian = string.Concat(hexPairs);

                // Check if the result is all "0"s and convert to a single "0"
                if (processHandler.RemoveZeros && (bigEndian.Trim('0') == "" || (bigEndian = bigEndian.TrimStart('0')).Length == 0)) bigEndian = "0";
                return bigEndian;
            }

            // This is for headers and sections of data that dont have descriptions 
            string reversedHexValues = string.Join(" ",
            hexPairs.Where((value, index) => index % 2 == 1)
                .Select((value, index) =>
                {
                    // This part changes the extra 0's into a single 0
                    string firstPart = hexPairs[index * 2 + 1];
                    string secondPart = hexPairs[index * 2];
                    string combined = firstPart + secondPart;
                    if (processHandler.RemoveZeros)
                    {
                        if (secondPart == "00" && firstPart == "00") combined = "0";
                        else if ((combined = combined.TrimStart('0')).Length == 0) combined = "0";
                    }
                    return combined;
                }));
            return reversedHexValues;
        }

        /// <param name="column"> Column of 0 means offset, column of 1 means data, and column of 2 means description. </param>
        public string GetData(int row, int column, ProcessHandler.DataType dataType, bool bigEndian, bool inFileOffset)
        {
            //Console.WriteLine(" ");
            if(GetType().Name != "Everything" && column != 2 && column != 0)
            {
                Console.WriteLine("SubHeader:" + this.GetType().Name + " Row:" + row + " Column:" + column + " BigEndian:" + bigEndian + " DataType:" + dataType.ToString() +
                 " NullSize:" + (Size == null) + " FailedToInitlize:" + FailedToInitlize + " RowSize:" + (Size != null ? Size.Length : 0) + " StartPoint:" + StartPoint
                + " EndPoint:" + EndPoint);
            }
            if(FailedToInitlize) return "";  // Something went wrong with this file so don't return any text
            if (Size != null && row >= Size.Length) return "";  // This returns the blank text for index larger than the size of our headers

            if (Component == ProcessHandler.ProcessComponent.EVERYTHING)
            {
                if(column == 0 && processHandler.OffsetsInHex) return GetCorrectFormat(row, column, ProcessHandler.DataType.HEX, bigEndian);

            }
            int firstRow = (int)Math.Floor(StartPoint / 16.0);  // First row of our data
            if (GetType().Name != "Everything" && column != 2 && column != 0)
            {
                Console.WriteLine("\t Row:" + row + " FirstRow:" + firstRow + " Total:" + ((row + firstRow) * 16) + " Modolo:" + (StartPoint % 16));
            }

            if (Size == null && (row + firstRow) * 16 >= EndPoint) return "";  // This will handle blank spots in sections and the Dos Stub     

            // Something like Dos Stub or section bodies (mirrors the original data) while being aligned with 16 byte rows
            if (Size == null && StartPoint % 16 == 0)
            {
                if (column == 0) {
                    int off = row * 16;
                    return GetOffset(inFileOffset ? off + StartPoint : off, processHandler.OffsetsInHex ? ProcessHandler.DataType.HEX : dataType);
                }
                return GetCorrectFormat(row + firstRow, column, dataType, bigEndian);
            }

            if (Desc != null && column == 2) return Desc[row];  // This means its just asking for the custom description

            int relativeOfffset = 0;  // Amount of bytes into the row till we reach our target data relative to the start of this section
            int bytes = Size == null ? 16 : Size[row];  // How many bytes I need to read from the array
            if (Size != null)
            {   // The following is designed for headers
                for (int i = 0; i < row; i++)
                {
                    relativeOfffset += Size[i];  // Adding the byte size of our rows data to the offset
                }
            }

            if (column == 0)
            {
                Console.WriteLine("COLUMNS 0 GETOFFSET:" + GetOffset((inFileOffset ? relativeOfffset + StartPoint : relativeOfffset), dataType) + " RelativeOffset:" + relativeOfffset);
                return GetOffset((inFileOffset ? relativeOfffset + StartPoint : relativeOfffset), processHandler.OffsetsInHex ? ProcessHandler.DataType.HEX : dataType);  // This means we just need to return the offset
            }
            relativeOfffset += StartPoint % 16;

            // At this point we should only be working with data as the file offsets and descriptions should of been taken care of already
            int startingDataRow = firstRow + (int)Math.Floor(relativeOfffset / 16.0);  // Row where our data begins (data may extend onto additional rows)

            int fileStartOffset = relativeOfffset + (startingDataRow * 16);  // Target data's offset relative to the start of the file

            if (GetType().Name != "Everything" && column != 2 && column != 0)
            {
                Console.WriteLine("\t RelOffset:" + relativeOfffset + " FileOffset:" + fileStartOffset + " Bytes:" + bytes + " GetOffSet:" + 
                    (column == 0 ? GetOffset((inFileOffset ? relativeOfffset + StartPoint : relativeOfffset), dataType) : "") + " FirstRow:" + firstRow + " StartingDataRow:" + startingDataRow);
            }
            
            string[] data = null;
            int startingRowOffset = relativeOfffset - ((startingDataRow - firstRow) * 16);  // Offset relative to the starting row

            for (int i = startingDataRow; i<processHandler.FilesHex.GetLength(0); i++)
            {   // Looping through our rows
                if (data == null) data = GetCorrectFormat(i, 1, bigEndian ? ProcessHandler.DataType.HEX : dataType, false).Split(' ');
                else data = data.Concat(GetCorrectFormat(i, 1, bigEndian ? ProcessHandler.DataType.HEX : dataType, false).Split(' ')).ToArray();

                int weight = Size == null ? ((row - startingDataRow + 1) * 16) : data.Length;  // First half works for sections and Dos headers and second works for normal headers

                if (column != 2 && column != 0)
                {
                    Console.WriteLine("\t SubHeader:" + this.GetType().Name + " i:" + i + " Row:" + row + " StartingDataRow:" + startingDataRow + " RelOffset:" + relativeOfffset
                         + " StartRowOffset:" + startingRowOffset +  " Bytes:" + bytes + " Total:" + weight  + " DataSize:" + data.Length);
                }

                if (weight >= startingRowOffset + bytes) break;  // This means our data array now contains enough data to retrieve the requested data
            }

            string finalData = string.Join(" ", data.Skip(startingRowOffset).Take(bytes));

            if (column != 2 && column != 0)
            {
                Console.WriteLine("\t Result:" + string.Join(" ", data.Skip(startingRowOffset).Take(bytes)) + " RelOffset:" + relativeOfffset + " bytes:" + bytes 
                     + " StartingDataRow:" + startingDataRow + " DataSize:" + data.Length + " Data:" + string.Join(" ", data) + " BigEndian:" + bigEndian + " Data:" + finalData );
            }


            if (bigEndian)
            {
                string splitHex = GetBigEndian(finalData.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

                if (dataType == ProcessHandler.DataType.HEX) return splitHex;
                
                string[] hexArray = splitHex.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                switch (dataType)
                {
                    case ProcessHandler.DataType.DECIMAL: return string.Join(" ", hexArray.Select(hexPair => ulong.Parse(hexPair, NumberStyles.HexNumber)));
                    case ProcessHandler.DataType.BINARY: return string.Join(" ", hexArray.Select(hexPair => Convert.ToString(Convert.ToInt64(hexPair, 16), 2)));
                }
            }
            return finalData;
        }

        /// <summary>
        ///  Takes in a normal decimal integer for offset and returns the converted data type in string form
        /// </summary>
        private string GetOffset(int offset, ProcessHandler.DataType dataType)
        {
            switch(dataType)
            {
                case ProcessHandler.DataType.DECIMAL: return offset.ToString();
                case ProcessHandler.DataType.HEX: return "0x" + offset.ToString("X");
                case ProcessHandler.DataType.BINARY: return Convert.ToString(offset, 2);
            }
            return "";
        }

        private string GetCorrectFormat(int row, int column, ProcessHandler.DataType dataType, bool bigEndian)
        {
            if(bigEndian && column == 1)
            {
                string bigEndianHex = GetBigEndian(processHandler.FilesHex[row, column].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                string[] hexArray = bigEndianHex.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                switch (dataType)
                {
                    case ProcessHandler.DataType.HEX: return bigEndianHex;
                    case ProcessHandler.DataType.DECIMAL: return string.Join(" ", hexArray.Select(hexPair => ulong.Parse(hexPair, NumberStyles.HexNumber)));
                    case ProcessHandler.DataType.BINARY: return string.Join(" ", hexArray.Select(hexPair => Convert.ToString(Convert.ToInt64(hexPair, 16), 2)));
                }
            }

            if (column == 2) return processHandler.FilesHex[row, 2];

            return dataType switch
            {
                ProcessHandler.DataType.HEX => processHandler.FilesHex[row, column],
                ProcessHandler.DataType.DECIMAL => processHandler.FilesDecimal[row, column],
                ProcessHandler.DataType.BINARY => processHandler.FilesBinary[row, column],
                _ => "",
            };
        }

        protected string[] ReadCharacteristics(string hexString)
        {
            long characteristicsValue = Convert.ToInt64(hexString, 16);
            List<string> presentCharacteristics = new List<string>();

            foreach (var pair in Characteristics)
            {
                int characteristicFlag = pair.Key;
                string value = pair.Value;

                if ((characteristicsValue & characteristicFlag) != 0)
                {
                    string v = "0x" + characteristicFlag.ToString("X") + " - " + value;
                    presentCharacteristics.Add(v);
                }
            }

            return presentCharacteristics.ToArray();
        }

        public string GetFilesHex(int row, int column)
        {
            return processHandler.FilesHex[row, column];
        }

        public string GetFilesDecimal(int row, int column)
        {
            return processHandler.FilesDecimal[row, column];
        }

        public string GetFilesBinary(int row, int column)
        {
            return processHandler.FilesBinary[row, column];
        }

        public int GetFilesRows()
        {
            return processHandler.FilesHex.GetLength(0);
        }

        public int GetFilesColumns()
        {
            return processHandler.FilesHex.GetLength(1);
        }

        public static string GetSectionString(ProcessHandler.ProcessComponent type)
        {
            if (type == ProcessHandler.ProcessComponent.NULL_COMPONENT) return "";
            return "." + type.ToString().ToLower().Replace("_m", "$").Replace("_", " ");
        }


        protected class OptionsForm : Form
        {
            private readonly SuperHeader header;
            public readonly ComboBox comboBox;
            public readonly DateTimePicker dateTimePicker;
            public readonly CheckedListBox checkedListBox;
            private Button okButton;

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            public OptionsForm(SuperHeader header, string? bigEndianHex, string windowName, int selectedRow, string[] dropDownComboBoxArgs, string[] checkBoxComboBoxArgs, DateTime? initialDateTime)
            {
                Text = windowName;
                InitializeComponents();

                if (dropDownComboBoxArgs != null)
                {
                    comboBox = new ComboBox();
                    comboBox.Location = new Point(20, 20);

                    // Calculate the width required for the longest string
                    int maxWidth = dropDownComboBoxArgs.Max(str => TextRenderer.MeasureText(str, comboBox.Font).Width);
                    comboBox.Width = Math.Max(maxWidth + 20, 180); // Set minimum width and account for padding
                    comboBox.Items.AddRange(dropDownComboBoxArgs);

                    // Set the selected value 
                    string selectedValue = bigEndianHex != null ? bigEndianHex : header.GetData(selectedRow, 1, ProcessHandler.DataType.HEX, true, false); // This returns the big endian hex
                    Console.WriteLine("BigEndianHex:" + bigEndianHex + " GetValue:" + header.GetData(selectedRow, 1, ProcessHandler.DataType.HEX, true, false) +
                       " OldGetValue:" + GetBigEndianValue(header.GetData(selectedRow, 1, ProcessHandler.DataType.HEX, false, false)));
                    string matchingString = dropDownComboBoxArgs.FirstOrDefault(str => {
                        int hexPrefixLength = str.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? 2 : 0;
                        return str.Length > hexPrefixLength && str.Substring(hexPrefixLength).StartsWith(selectedValue, StringComparison.OrdinalIgnoreCase);
                    }) ?? ""; // Else return an empty string if no match is found
                    comboBox.SelectedItem = matchingString;

                    okButton.Location = new Point(comboBox.Right + 10, comboBox.Top); // Position next to the combo box
                    // Update the width to reflect the size of the ok button and the new width of the combo box
                    Width = Math.Max(comboBox.Right + okButton.Width + 30, 300); // Account for padding
                    Controls.Add(comboBox);
                }
                else if (checkBoxComboBoxArgs != null && header.Characteristics != null)
                {
                    checkedListBox = new CheckedListBox();
                    checkedListBox.Location = new Point(20, 20);

                    int maxWidth = checkBoxComboBoxArgs.Max(str => TextRenderer.MeasureText(str, checkedListBox.Font).Width);
                    checkedListBox.Width = Math.Max(maxWidth + 20, 180); // Set minimum width and account for padding

                    checkedListBox.Items.AddRange(checkBoxComboBoxArgs);

                    string selectedValue = bigEndianHex != null ? bigEndianHex : header.GetData(selectedRow, 1, ProcessHandler.DataType.HEX, true, false); // This returns the big endian hex
                    Console.WriteLine("BigEndianHex:" + bigEndianHex + " GetValue:" + header.GetData(selectedRow, 1, ProcessHandler.DataType.HEX, true, false) +
                        " OldGetValue:" + GetBigEndianValue(header.GetData(selectedRow, 1, ProcessHandler.DataType.HEX, false, false)));
                    string[] combinedStrings = header.ReadCharacteristics(selectedValue);  

                    int i = 0;
                    foreach (string combinedString in combinedStrings)
                    {
                        int indexToCheck = checkedListBox.FindStringExact(combinedString);

                        // Check the item with the specified index
                        if (indexToCheck != ListBox.NoMatches) checkedListBox.SetItemChecked(indexToCheck, true);
                        i++;
                    }

                    okButton.Location = new Point(checkedListBox.Right + 10, checkedListBox.Top); // Position next to the combo box
                    Width = Math.Max(checkedListBox.Right + okButton.Width + 30, 300); // Account for padding

                    // Set the form's height to match the preferred height of the CheckedListBox
                    ClientSize = new Size(ClientSize.Width, (checkedListBox.PreferredHeight / 2) + 70); // Add extra space
                    Controls.Add(checkedListBox);
                }
                else if(initialDateTime != null)
                {
                    Console.WriteLine("Date and time" + initialDateTime);

                    dateTimePicker = new DateTimePicker();
                    dateTimePicker.Location = new Point(20, 20);
                    dateTimePicker.Width = 200; 
                    dateTimePicker.Format = DateTimePickerFormat.Custom;
                    dateTimePicker.CustomFormat = "MM/dd/yyyy HH:mm:ss"; 

                    dateTimePicker.Value = (DateTime) initialDateTime;

                    okButton.Location = new Point(dateTimePicker.Right + 10, dateTimePicker.Top); // Position next to the combo box
                    Width = Math.Max(dateTimePicker.Right + okButton.Width + 30, 300); // Account for padding
                    Controls.Add(dateTimePicker);
                }

            }
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

            private void InitializeComponents()
            {
                Size = new Size(300, 100);

                FormBorderStyle = FormBorderStyle.FixedSingle; // Prevents resizing 
                MaximizeBox = false; 
                MinimizeBox = false; 

                okButton = new Button();
                okButton.Text = "OK";
                okButton.DialogResult = DialogResult.OK;
                okButton.Click += OkButton_Click;

                // Add controls to the form's controls collection
                Controls.Add(okButton);

                // Set the AcceptButton property to the OK button
                AcceptButton = okButton;
            }

            private void OkButton_Click(object sender, EventArgs e)
            {
/*                foreach (var item in checkedListBox.CheckedItems)
                {
                    item.
                }*/
                Close();
            }

            public string GetUpdatedCharacterisitcs()
            {
                long characteristicsValue = 0;

                foreach (var item in checkedListBox.CheckedItems)
                {
                    // Split the item's text to get the hex value part
                    string[] parts = item.ToString().Split('-');
                    if (parts.Length == 2)
                    {
                        // Trim and parse the hex value part
                        string hexValue = parts[0].Trim();
                        Console.WriteLine("HexValue:" + hexValue);
                        if (hexValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && long.TryParse(hexValue.Substring(2), NumberStyles.HexNumber, null, out long hexFlag))
                        {
                            // OR the hexFlag with the characteristicsValue
                            characteristicsValue |= hexFlag;
                        }
                    }
                }

                // Convert the resulting value to a hex string
                string hexString = characteristicsValue.ToString("X");

                return hexString;
            }

            public static string GetBigEndianValue(string littleEndian)
            {
                string[] values = littleEndian.Split(' ');
                Array.Reverse(values);
                string bigEndian = string.Concat(values).Replace(" ", "");
                return bigEndian;
            }
        }

    }
}
