using System;
using System.Linq;
using System.Globalization;
using System.Drawing;
using System.Windows.Forms;
using PluginInterface;

namespace ProcessExplorer.components
{
    public class OptionsForm : Form
    {
        public readonly ComboBox comboBox;
        public readonly DateTimePicker dateTimePicker;
        public readonly CheckedListBox checkedListBox;
        private Button okButton;

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        public OptionsForm(SuperHeader header, string? bigEndianHex, string windowName, int selectedRow, string[] dropDownComboBoxArgs, string[] checkBoxComboBoxArgs,
            DateTime? initialDateTime, DataStorage dataStorage)
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

                string selectedValue = bigEndianHex != null ? bigEndianHex : GetBigEndianValue(header.GetData(selectedRow, 1, PluginInterface.Enums.DataType.HEX, 1, false, dataStorage)); // This returns the big endian hex

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

                string selectedValue = bigEndianHex != null ? bigEndianHex : GetBigEndianValue(header.GetData(selectedRow, 1, Enums.DataType.HEX, 1, false, dataStorage)); // This returns the big endian hex
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
            else if (initialDateTime != null)
            {
                dateTimePicker = new DateTimePicker();
                dateTimePicker.Location = new Point(20, 20);
                dateTimePicker.Width = 200;
                dateTimePicker.Format = DateTimePickerFormat.Custom;
                dateTimePicker.CustomFormat = "MM/dd/yyyy HH:mm:ss";

                dateTimePicker.Value = (DateTime)initialDateTime;

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

        public string GetUpdatedComboBoxValue()
        {
            if (comboBox.SelectedItem == null) return null;
            string result = (string)comboBox.SelectedItem;
            string hex = result.Substring(0, result.IndexOf(' ')).Replace("0x", ""); // Big endian form

            // Add a leading '0' if the string length is odd
            if (hex.Length % 2 != 0) hex = "0" + hex;

            string[] bytes = Enumerable.Range(0, hex.Length / 2).Select(i => hex.Substring(i * 2, 2)).ToArray();
            Array.Reverse(bytes);
            return string.Join(" ", bytes);
        }

        public string GetUpdatedCharacterisitcs()
        {
            ushort characteristicsValue = 0;

            foreach (var item in checkedListBox.CheckedItems)
            {
                // Split the item's text to get the hex value part
                string[] parts = item.ToString().Split('-');
                if (parts.Length == 2)
                {
                    // Trim and parse the hex value part
                    string hexValue = parts[0].Trim();
                    if (hexValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    {
                        // Parse the hex value as an integer
                        int value = int.Parse(hexValue.Substring(2), NumberStyles.HexNumber);

                        // Swap the bytes to convert from big-endian to little-endian
                        characteristicsValue |= (ushort)((value >> 8) | (value << 8));
                    }
                }
            }

            string hexString = characteristicsValue.ToString("X4"); // Convert the characteristicsValue to a hexadecimal string with two bytes

            // Ensure that the string has two bytes, adding leading zeros if necessary
            if (hexString.Length < 4) hexString = hexString.PadLeft(4, '0');

            return hexString.Insert(2, " "); // Insert a space between the bytes
        }

        public string GetUpdatedDateAndTime()
        {
            // Assuming you have a DateTimePicker control named dateTimePicker1
            DateTime selectedDateTime = dateTimePicker.Value;

            // Define a reference date (e.g., January 1, 1970, as a Unix timestamp)
            DateTime referenceDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // Calculate the difference between selectedDateTime and referenceDate
            TimeSpan difference = selectedDateTime.ToUniversalTime() - referenceDate;

            // Convert the difference to seconds (Unix timestamp)
            long unixTimestamp = (long)difference.TotalSeconds;

            // Convert the timestamp to a little-endian hex string with four bytes
            string hexString = unixTimestamp.ToString("X8");

            // Add spaces between every two characters (bytes)
            return string.Join(" ", Enumerable.Range(0, hexString.Length / 2).Select(i => hexString.Substring(i * 2, 2)));
        }

        /// <summary>
        ///  This changes little-endian data into big-endian while preserving "usless" zeros which is required for making
        ///  OptionsForm work correctly. On the other hand GetData() has a BigEndian option but this removes "usless" zeros
        ///  which is mainly used for displaying data to the user in DataGridView.
        /// </summary>
        public static string GetBigEndianValue(string littleEndian)
        {
            string[] values = littleEndian.Split(' ');
            Array.Reverse(values);
            string bigEndian = string.Concat(values).Replace(" ", "");
            return bigEndian;
        }
    }

}
