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

        public string[,] hexArray { get; private set; }
        public string[,] binaryArray { get; private set; }
        public string[,] deciArray { get; private set; }

        /* StartPoint and EndPoint will be saved using hex */
        public int StartPoint { get; protected set; }
        public int EndPoint { get; protected set; }

        public int RowSize { get; private set; }
        public int ColumnSize { get; private set; }

        public bool ShrinkDataSection { get; private set; } // This is for headers that show descriptions 

        public bool FailedToInitlize { get; protected set; }

        public ProcessHandler.ProcessComponent Component { get; protected set; }

        public Dictionary<int, string> characteristics { get; protected set; }

        public SuperHeader(ProcessHandler processHandler, ProcessHandler.ProcessComponent component, int rowSize, int columnSize, bool shrinkDataSection)
        {
            this.ShrinkDataSection = shrinkDataSection;
            this.processHandler = processHandler;
            this.ColumnSize = columnSize;
            this.Component = component;
            this.RowSize = rowSize;

            hexArray = new string[rowSize, columnSize];
            binaryArray = new string[rowSize, columnSize - 1]; // I subtract 1 here because theres no point saving the descriptions more than once
            deciArray = new string[rowSize, columnSize - 1];   // I subtract 1 here because theres no point saving the descriptions more than once
            FailedToInitlize = false;
        }

        public abstract void OpenForm(int row);

        protected void SetAllArrays(string[,] hexArray, string[,] deciArray, string[,] binaryArray)
        {
            this.hexArray = hexArray;
            this.deciArray = deciArray;
            this.binaryArray = binaryArray;
        }

        protected void PopulateNonDescArrays()
        {
            int startingIndex = StartPoint <= 0 ? 0 : (int) Math.Floor(StartPoint / 16.0); // Theres 16 bytes of hex per row
            int totalByteSize = StartPoint;
            int hexValuesSize = startingIndex * 16;

            // I need to first redefine the size of the arrays
            string[,] filesHex = processHandler.GetComponentFromMap(ProcessHandler.ProcessComponent.EVERYTHING).hexArray;
            int rowSize = GetRowCount(filesHex, startingIndex);
            hexArray = new string[rowSize, ColumnSize];
            binaryArray = new string[rowSize, ColumnSize - 1]; // I subtract 1 here because theres no point saving the descriptions more than once
            deciArray = new string[rowSize, ColumnSize - 1];   // I subtract 1 here because theres no point saving the descriptions more than once

            int rowStartingPoint = Convert.ToInt32(filesHex[startingIndex, 0], 16); // defines the offset in the starting row

            long offsetSum;
            string[] splitHexData;
            int ourArrayIndex = 0;
            for (int i = startingIndex; i < filesHex.GetLength(0); i++)
            {
                string[] dataRow = filesHex[i, 1].Split(' ');
                string correctedHexData = "";
                int rowOffset = -1;
                for(int j = 0; j < 16; j++)
                {
                    int value = rowStartingPoint + ((i - startingIndex) * 16) + j + 1;
                    if (value >= EndPoint)
                    {
                        correctedHexData += dataRow[j] + " ";
                        offsetSum = int.Parse(filesHex[i, 0].Replace("0x", ""), NumberStyles.HexNumber) + (rowOffset < 0 ? 0 : rowOffset);
                        hexArray[ourArrayIndex, 0] = "0x" + (offsetSum).ToString("X");
                        hexArray[ourArrayIndex, 1] = correctedHexData;
                        hexArray[ourArrayIndex, 2] = filesHex[i, 2];

                        splitHexData = correctedHexData.Split(' ').Where(str => !string.IsNullOrWhiteSpace(str)).ToArray(); // Somehow a white space got in here or something...
                        deciArray[ourArrayIndex, 0] = offsetSum.ToString();
                        deciArray[ourArrayIndex, 1] = string.Join(" ", Array.ConvertAll(splitHexData, hex => long.Parse(hex, NumberStyles.HexNumber)));

                        binaryArray[ourArrayIndex, 0] = Convert.ToString(offsetSum, 2);
                        binaryArray[ourArrayIndex, 1] = string.Join(" ", splitHexData.Select(hex => Convert.ToString(long.Parse(hex, NumberStyles.HexNumber), 2)));
                        return;
                    }

                    if (value < StartPoint) continue;
                    if (rowOffset == -1) rowOffset = j; // Sets how many bytes into the row it took to reach our first value
                  
                    if(dataRow.Length - 1 >= j) correctedHexData += dataRow[j] + " ";
                }

                offsetSum = long.Parse(filesHex[i, 0].Replace("0x", ""), NumberStyles.HexNumber) + (rowOffset < 0 ? 0 : rowOffset);
                hexArray[ourArrayIndex, 0] = "0x" + (offsetSum).ToString("X");
                hexArray[ourArrayIndex, 1] = correctedHexData;
                hexArray[ourArrayIndex, 2] = filesHex[i, 2];

                splitHexData = correctedHexData.Split(' ').Where(str => !string.IsNullOrWhiteSpace(str)).ToArray(); // Somehow a white space got in here or something...
                deciArray[ourArrayIndex, 0] = offsetSum.ToString();
                deciArray[ourArrayIndex, 1] = string.Join(" ", Array.ConvertAll(splitHexData, hex => long.Parse(hex, NumberStyles.HexNumber)));

                binaryArray[ourArrayIndex, 0] = Convert.ToString(offsetSum, 2);
                binaryArray[ourArrayIndex, 1] = string.Join(" ", splitHexData.Select(hex => Convert.ToString(long.Parse(hex, NumberStyles.HexNumber), 2)));
                ourArrayIndex++;
            }
        }

        private int GetRowCount(string[,] filesHex, int startingIndex)
        {
            int finalCount = startingIndex;
            for (int i = startingIndex; i < filesHex.GetLength(0); i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    // (i - startingIndex) denotes the row, * 16 multiples it by 16 bytes per row, and + (j + 1) adds the individual row bytes
                    int value = StartPoint + ((i - startingIndex) * 16) + j + 1;
                    if (value >= EndPoint) return i + 1 - startingIndex;
                }
                finalCount = i;
            }
            return finalCount + 1 - startingIndex;
        }


        /// <param name="sizeAndDesc"> will be passed from the sub class and will contain the size in bytes  
        /// of each field and the description that explains what it does </param>
        protected void populateArrays(string[,] sizeAndDesc)
        {
            string[] hexValues = new string[0];

            /** The following loop will be used to increase the size of the hexValues array. It will determine how many bytes 
             * this header file will need it will then add a new row of 16 after each 
             */
            int startingIndex = StartPoint <= 0 ? 0 : (int)Math.Floor(StartPoint / 16.0); // 2
            int totalByteSize = StartPoint;
            int hexValuesSize = startingIndex * 16;

            for (int i = 0; i < sizeAndDesc.GetLength(0); i++)
            {
                totalByteSize += Convert.ToInt32(sizeAndDesc[i, 0]); 

                if (totalByteSize > hexValuesSize)
                {
                    int index = hexValuesSize <= 0 ? 0 : ((int) Math.Floor(hexValuesSize / 16.0)); // Prevents a divide by 0 issue
                    if (index >= processHandler.GetComponentFromMap(ProcessHandler.ProcessComponent.EVERYTHING).hexArray.GetLength(0)) break; // This means we have gone over the size of the array
                    
                    string[] newArray = processHandler.GetComponentFromMap(ProcessHandler.ProcessComponent.EVERYTHING).hexArray[index, 1].Split(' '); // Adds the next row
                    Array.Resize(ref hexValues, hexValues.Length + newArray.Length);
                    Array.Copy(newArray, 0, hexValues, hexValues.Length - newArray.Length, newArray.Length);
                    hexValuesSize += 16; // This adds an extra row aka 16 elements (we call this after getting index to avoid needing to subtract 1)
                }
            }

            int skipAmount = StartPoint % 16; // The amount we need to skip before we reach our target byte
            int previousBytesize = 0;
            for (int i = 0; i < hexArray.GetLength(0); i++)
            {
                int newByteSize = Convert.ToInt32(sizeAndDesc[i, 0]);
                // I need to subtract StartPoint because I am using the offsets to gage the sizes 
                int previousOffset = (i > 0 ? (int.Parse(hexArray[i - 1, 0].Replace("0x", ""), NumberStyles.HexNumber)) - StartPoint : 0);
                int newOffset = previousBytesize + previousOffset;

                string newHexValues = string.Join(" ", hexValues.Skip(newOffset + skipAmount).Take(newByteSize));
                hexArray[i, 0] = "0x" + (i > 0 ? (newOffset + StartPoint).ToString("X") : (StartPoint).ToString("X"));
                hexArray[i, 1] = newHexValues;
                hexArray[i, 2] = sizeAndDesc[i, 1];

                long deciOffset = (i > 0 ? (newOffset + StartPoint) : StartPoint);
                string[] splitNewHexValues = newHexValues.Split(' ').Where(str => !string.IsNullOrWhiteSpace(str)).ToArray(); // Somehow a white space got in here or something...;
                deciArray[i, 0] = deciOffset.ToString();
                deciArray[i, 1] = string.Join(" ", Array.ConvertAll(splitNewHexValues, hex => long.Parse(hex, NumberStyles.HexNumber)));
                binaryArray[i, 0] = Convert.ToString(deciOffset, 2); // 2 specifies to use binary
                binaryArray[i, 1] = string.Join(" ", splitNewHexValues.Select(hex => Convert.ToString(long.Parse(hex, NumberStyles.HexNumber), 2)));
                previousBytesize = newByteSize;
            }
            // This will set the ending point
            int lastOffset = int.Parse((hexArray[RowSize - 1, 0]).Replace("0x", ""), NumberStyles.HexNumber) - StartPoint;
            int lastByteSize = Convert.ToInt32(sizeAndDesc[RowSize - 1, 0]);
            EndPoint = lastOffset + lastByteSize + StartPoint;
        }

        public string GetHex(int row, int column, bool doubleByte)
        {
            if (row > hexArray.GetLength(0) - 1 || column > hexArray.GetLength(1)) return "";
            if (!doubleByte || column == 0 || column == 2)
            {   
                if(column == 1 || column == 2 || processHandler.Offset == ProcessHandler.OffsetType.FILE_OFFSET) return hexArray[row, column]; // Returns the single byte data, offset, or the description
                return "0x" + GetRelativeOffsetDecimal(row).ToString("X");
            }
            // This will reverse the hex and switch it from little-endian to big-endian
            string[] values = hexArray[row, column].Split(' ');
            return GetBigEndian(values);
        }

        public string GetBinary(int row, int column, bool doubleByte)
        {
            if (row > binaryArray.GetLength(0) - 1 || column > hexArray.GetLength(1)) return "";
            if(column > 1) return hexArray[row, column]; // This is here because this array does not contain the desc
            if (column == 0 && processHandler.OffsetsInHex) return hexArray[row, 0]; // This means the offsets should be displayed in hex
            if (!doubleByte || column == 0)
            {   // This returns the binary data or the offsets if the offsets are set to file offsets
                if (column == 1 || processHandler.Offset == ProcessHandler.OffsetType.FILE_OFFSET) return binaryArray[row, column]; // Returns the single byte data or the offset
                return Convert.ToString(GetRelativeOffsetDecimal(row), 2);
            }
            // This will reverse the binary and switch it from little-endian to big-endian
            string[] values = hexArray[row, column].Split(' ');
            string bigEndianHex = GetBigEndian(values);
            return string.Join(" ", bigEndianHex.Split(' ').Select(hexPair => Convert.ToString(Convert.ToInt64(hexPair, 16), 2)));
        }

        public string GetDecimal(int row, int column, bool doubleByte)
        {
            if (row > deciArray.GetLength(0) - 1 || column > hexArray.GetLength(1)) return "";
            if (column > 1) return hexArray[row, column]; // This is here because this array does not contain the desc
            if (column == 0 && processHandler.OffsetsInHex) return hexArray[row, 0]; // This means the offsets should be displayed in hex
            if (!doubleByte || column == 0)
            {   // This returns the decimal data or the offsets if the offsets are set to file offsets
                if(column == 1 || processHandler.Offset == ProcessHandler.OffsetType.FILE_OFFSET) return deciArray[row, column]; // Returns the single byte data or the offset
                return GetRelativeOffsetDecimal(row).ToString();
            }
            // This will reverse the decimal and switch it from little-endian to big-endian (only for the data)
            string[] values = hexArray[row, column].Split(' ');
            string bigEndianHex = GetBigEndian(values);
            return string.Join(" ", bigEndianHex.Split(' ').Select(hexPair => ulong.Parse(hexPair, NumberStyles.HexNumber)));
        }

        private int GetRelativeOffsetDecimal(int row)
        {   // This is only for offsets which is why the column is set to a constant of 0
            int initialOffset = Convert.ToInt32(hexArray[0, 0], 16);
            int offset = Convert.ToInt32(hexArray[row, 0], 16);
            return offset - initialOffset;
        }

        private string GetBigEndian(string[] hexPairs)
        {
            if (ShrinkDataSection)
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

        protected string[] ReadCharacteristics(string hexString)
        {
            int characteristicsValue = Convert.ToInt32(hexString, 16);
            List<string> presentCharacteristics = new List<string>();

            foreach (var pair in characteristics)
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

        public static string GetSectionString(ProcessHandler.ProcessComponent type)
        {
            if (type == ProcessHandler.ProcessComponent.NULL_COMPONENT) return "";
            return "." + type.ToString().ToLower().Replace("_m", "$").Replace("_", " ");
        }


        protected class OptionsForm : Form
        {
            private readonly SuperHeader header;
            private readonly ComboBox comboBox;
            private readonly DateTimePicker dateTimePicker;
            private readonly CheckedListBox checkedListBox;
            private Button okButton;

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            public OptionsForm(SuperHeader header, string? bigEndianHex, string windowName, int selectedRow, string[] dropDownComboBoxArgs, string[] checkBoxComboBoxArgs, DateTime? initialDateTime)
            {
                this.header = header;

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
                    string selectedValue = bigEndianHex != null ? bigEndianHex : GetBigEndianValue(header.hexArray[selectedRow, 1]); // This returns the big endian hex
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
                else if (checkBoxComboBoxArgs != null && header.characteristics != null)
                {
                    checkedListBox = new CheckedListBox();
                    checkedListBox.Location = new Point(20, 20);

                    int maxWidth = checkBoxComboBoxArgs.Max(str => TextRenderer.MeasureText(str, checkedListBox.Font).Width);
                    checkedListBox.Width = Math.Max(maxWidth + 20, 180); // Set minimum width and account for padding

                    checkedListBox.Items.AddRange(checkBoxComboBoxArgs);

                    string selectedValue = bigEndianHex != null ? bigEndianHex : GetBigEndianValue(header.hexArray[selectedRow, 1]); // This returns the big endian hex
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
                    dateTimePicker.Location = new System.Drawing.Point(20, 20);
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
                Close();
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
