using System;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using ProcessExplorer.components.impl;
using ProcessExplorer.components;

namespace ProcessExplorer
{
    public partial class Form1 : Form
    {
        private readonly ContextMenuStrip fileContextMenu = new ContextMenuStrip();
        private readonly ContextMenuStrip settingsMenu = new ContextMenuStrip();
        private string selectedComponent = "null";
        private CharacterSet selectedCharacter = CharacterSet.ASCII;
        private ProcessHandler processHandler;

        public Form1()
        {
            InitializeComponent();
            Text = "Process Explorer";

            fileContextMenu.Items.Add("Open");
            fileContextMenu.Items.Add("Save");
            fileContextMenu.ItemClicked += new ToolStripItemClickedEventHandler(FileLabel_Click_ContextMenu);

            ToolStripMenuItem setting1 = new ToolStripMenuItem("Remove extra zeros");
            setting1.Click += Settings_Click;
            settingsMenu.Items.Add(setting1);

            ToolStripMenuItem setting2 = new ToolStripMenuItem("Display offsets in hex");
            setting2.Click += Settings_Click;
            settingsMenu.Items.Add(setting2);

            ToolStripMenuItem setting3 = new ToolStripMenuItem("Return to top");
            setting3.Click += Settings_Click;
            settingsMenu.Items.Add(setting3);

            ToolStripMenuItem setting4 = new ToolStripMenuItem("Treat null as '.'");
            setting4.Click += Settings_Click;
            settingsMenu.Items.Add(setting4);

            dataGridView.ColumnHeadersDefaultCellStyle.BackColor = SystemColors.ControlDark;
            dataGridView.AlternatingRowsDefaultCellStyle.BackColor = SystemColors.ControlDark;
            dataGridView.RowHeadersDefaultCellStyle.BackColor = SystemColors.ControlDark;
            dataGridView.CellFormatting += DataGridView_CellFormatting;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Console.WriteLine("Starting Process Explorer");
            splitContainer1.MaximumSize = new Size(ClientSize.Width, Height - 80);

            foreach (ToolStripItem t in toolStrip.Items)
            {   // Assign an event for every button
                t.Click += new System.EventHandler(this.ToolStripButton_Click);
            }

            hexButton.Checked = singleByteButton.Checked = fileOffsetButton.Checked = true;
            Resize += Form1_Resize;
            toolStrip.Renderer = new MyToolStripRenderer();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            // Adjust the SplitContainer's width and height as needed
            splitContainer1.MaximumSize = new Size(ClientSize.Width, Height - 80);//44
            splitContainer1.Width = Width; // Expands to the right
            splitContainer1.Height = Height - 80; // Expands downward 

            int newWidth = dataGridView.Width;

            const int defaultWidth = 727;
            const int column1StartingWidth = 85;
            int column2StartingWidth = 492; 
            int column3StartingWidth = 150;

            if(processHandler != null && selectedComponent != "null" && processHandler.GetComponentFromMap(selectedComponent).Size != null)
            {
                column2StartingWidth = column3StartingWidth;
                column3StartingWidth = 492;

                int difference = newWidth - defaultWidth;
                int column3Width = column3StartingWidth + (int)(difference * 0.8);
                int remainingWidth = (int)((difference * 0.1) / 2);

                dataGridView.Columns[0].Width = column1StartingWidth + remainingWidth; // Adjust column indices as needed
                dataGridView.Columns[1].Width = column2StartingWidth + remainingWidth;
                dataGridView.Columns[2].Width = column3Width; // This wont ever need to expand since there will always be 16 characters
            }
            else
            {
                // This makes the 2nd column expand faster than the others
                int difference = newWidth - defaultWidth;
                int column2Width = column2StartingWidth + (int)(difference * 0.9);
                int remainingWidth = (int)(difference * 0.1);

                dataGridView.Columns[0].Width = column1StartingWidth + remainingWidth; // Adjust column indices as needed
                dataGridView.Columns[1].Width = column2Width;
                dataGridView.Columns[2].Width = column3StartingWidth; // This wont ever need to expand since there will always be 16 characters
            }
        }

        private void Settings_Click(object sender, EventArgs e)
        {
            if (processHandler == null) return;

            ToolStripMenuItem settingItem = sender as ToolStripMenuItem;
            settingItem.Checked = !settingItem.Checked; // Toggle checked state
            
            switch(settingItem.Text)
            {   // This will also automatically update the settings file asynchronously in case this program gets randomly terminated
                case "Display offsets in hex": processHandler.dataStorage.Settings.OffsetsInHex = settingItem.Checked;
                    processHandler.UpdateSettingsFile(true);
                    TriggerRedraw();
                    break;
                case "Remove extra zeros": processHandler.dataStorage.Settings.RemoveZeros = settingItem.Checked;
                    processHandler.UpdateSettingsFile(true);
                    TriggerRedraw();
                    break;
                case "Return to top": processHandler.dataStorage.Settings.ReterunToTop = settingItem.Checked;
                    processHandler.UpdateSettingsFile(true);
                    TriggerRedraw();
                    break;
                case "Treat null as '.'": processHandler.dataStorage.Settings.TreatNullAsPeriod = settingItem.Checked;
                    processHandler.UpdateSettingsFile(true);
                    break;
            }
        }

        private void FileLabel_Click_ContextMenu(object sender, ToolStripItemClickedEventArgs e)
        {
            string text = e.ClickedItem.Text;

            if(text == "Open")
            {
                fileContextMenu.Close();
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "All Files (*.*)|*.*"; //"Executable Files (*.exe;*.dll)|*.exe;*.dll|All Files (*.*)|*.*"; //
                    openFileDialog.RestoreDirectory = true;

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string selectedFilePath = openFileDialog.FileName;
                        try
                        {
                            treeView.Nodes.Clear();
                            using FileStream fileStream = new FileStream(selectedFilePath, FileMode.Open, FileAccess.Read);
                            processHandler = new ProcessHandler(fileStream);
                            Setup();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            else if(text == "Save")
            {
                fileContextMenu.Close();
                if (processHandler == null || processHandler.FileName == null) return;

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "All Files (*.*)|*.*",
                    Title = "Save File As",
                    FileName = processHandler.FileName,
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };

                // Show the SaveFileDialog and check if the user clicked "Save"
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // Get the selected file name
                    string filePath = saveFileDialog.FileName;
                    processHandler.SaveFile(filePath);

                    // Process the selected file path (e.g., save data to the file)
                    Console.WriteLine("Selected File: " + filePath);
                }
            }
        }

        private void Setup()
        {
            TreeNode rootNode = new TreeNode(processHandler.FileName);

            TreeNode dosHeaderNode = new TreeNode("DOS Header");
            TreeNode dosStubNode = new TreeNode("DOS Stub");
            TreeNode peHeaderNode = new TreeNode("PE Header");
            TreeNode optionalPeHeaderNode = new TreeNode("Optional PE Header");

            TreeNode certifcationTable = new TreeNode("Certificate Table");
            bool addCertifcationTable = false;

            PluginInterface.SuperHeader optionalPeHeader = processHandler.GetComponentFromMap(optionalPeHeaderNode.Text.ToLower());
            if (optionalPeHeader != null && !optionalPeHeader.FailedToInitlize) peHeaderNode.Nodes.Add(optionalPeHeaderNode);

            TreeNode optionalPeHeader64Node = new TreeNode("Optional PE Header 64");
            PluginInterface.SuperHeader optionalPeHeader64 = processHandler.GetComponentFromMap(optionalPeHeader64Node.Text.ToLower());
            if (optionalPeHeader64 != null && !optionalPeHeader64.FailedToInitlize && ((OptionalPeHeader)optionalPeHeader).peThirtyTwoPlus)
            {
                peHeaderNode.Nodes.Add(optionalPeHeader64Node);
                TreeNode optionalPeHeaderDataDirectories = new TreeNode("Optional PE Header Data Directories");
                peHeaderNode.Nodes.Add(optionalPeHeaderDataDirectories);

                OptionalPeHeaderDataDirectories dataDirectories = (OptionalPeHeaderDataDirectories) processHandler.GetComponentFromMap(optionalPeHeaderDataDirectories.Text.ToLower());
                if (dataDirectories != null && !dataDirectories.FailedToInitlize && dataDirectories.CertificateTablePointer > 0 && dataDirectories.CertificateTableSize > 0)
                    addCertifcationTable = true;
            }

            TreeNode optionalPeHeader32Node = new TreeNode("Optional PE Header 32");
            PluginInterface.SuperHeader optionalPeHeader32 = processHandler.GetComponentFromMap(optionalPeHeader32Node.Text.ToLower());
            if (optionalPeHeader32 != null && !optionalPeHeader32.FailedToInitlize && !((OptionalPeHeader)optionalPeHeader).peThirtyTwoPlus)
            {
                peHeaderNode.Nodes.Add(optionalPeHeader32Node);
                TreeNode optionalPeHeaderDataDirectories = new TreeNode("Optional PE Header Data Directories");
                peHeaderNode.Nodes.Add(optionalPeHeaderDataDirectories);

                OptionalPeHeaderDataDirectories dataDirectories = (OptionalPeHeaderDataDirectories)processHandler.GetComponentFromMap(optionalPeHeaderDataDirectories.Text.ToLower());
                if (dataDirectories != null && !dataDirectories.FailedToInitlize && dataDirectories.CertificateTablePointer > 0 && dataDirectories.CertificateTableSize > 0)
                    addCertifcationTable = true;
            }

            TreeNode mainSectionNode = new TreeNode("Sections");

            foreach (var map in processHandler.componentMap)
            {
                PluginInterface.SuperHeader header = map.Value;
                string compString = header.Component.ToString();
                if (!compString.Contains("section header")) continue;

                TreeNode sectionNode = new TreeNode("");
                sectionNode.Text = header.Component;

                foreach (var innerMap in processHandler.componentMap)
                {
                    PluginInterface.SuperHeader body = innerMap.Value;
                    string newCompString = body.Component.ToString();
                   
                    if (!newCompString.Contains("section body") || compString.Replace("section header", "") != newCompString.Replace("section body", "")) continue;

                    TreeNode sectionBodyNode = new TreeNode("")
                    {
                        Text = body.Component
                    };
                    sectionNode.Nodes.Add(sectionBodyNode);
                    break;
                }
                mainSectionNode.Nodes.Add(sectionNode); // Add the main section node for each section to our parent node
            }

            PluginInterface.SuperHeader dosHeader = processHandler.GetComponentFromMap("dos header");
            PluginInterface.SuperHeader dosStub = processHandler.GetComponentFromMap("dos stub");
            PluginInterface.SuperHeader peHeader = processHandler.GetComponentFromMap("pe header");
            if (dosHeader != null && !dosHeader.FailedToInitlize) rootNode.Nodes.Add(dosHeaderNode);
            if (dosStub != null && !dosStub.FailedToInitlize) rootNode.Nodes.Add(dosStubNode);
            if (peHeader != null && !peHeader.FailedToInitlize) rootNode.Nodes.Add(peHeaderNode);

            if(mainSectionNode.Nodes.Count > 0) rootNode.Nodes.Add(mainSectionNode);
            if(addCertifcationTable) rootNode.Nodes.Add(certifcationTable);

            // Add all of the nodes to the tree
            treeView.Nodes.Add(rootNode);

            // This will auto check the following settings on startup
            foreach (ToolStripItem item in settingsMenu.Items)
            {
                if (item is ToolStripMenuItem menuItem)
                {
                    switch(menuItem.Text)
                    {
                        case "Remove extra zeros": menuItem.Checked = processHandler.dataStorage.Settings.RemoveZeros;
                            break;
                        case "Return to top": menuItem.Checked = processHandler.dataStorage.Settings.ReterunToTop;
                            break;
                        case "Treat null as '.'": menuItem.Checked = processHandler.dataStorage.Settings.TreatNullAsPeriod;
                            break;
                        case "Display offsets in hex": menuItem.Checked = processHandler.dataStorage.Settings.OffsetsInHex;
                            break;
                    }
                }
            }

            // This will trigger the data to be dislayed
            selectedComponent = "everything";
            dataGridView.RowCount = processHandler.dataStorage.GetFilesRows(); //processHandler.GetComponentFromMap(selectedComponent). GetFilesRows();
            dataGridView.CellValueNeeded += DataGridView_CellValueNeeded;
            TriggerRedraw();
        }

        private void ToolStripButton_Click(object sender, EventArgs e)
        {
            // Get the button that was clicked
            ToolStripButton t = sender as ToolStripButton;
            if (t == null) return;

            string text = t.Text;
            if (text == "Hex")
            {   // This ensures the user cant uncheck the only option
                if (hexButton.Checked)
                {
                    binaryButton.Checked = decimalButton.Checked = false;
                    TriggerRedraw();
                }
                hexButton.Checked = true;
            }
            else if (text == "Dec")
            {
                if (decimalButton.Checked)
                {
                    binaryButton.Checked = hexButton.Checked = false;
                    TriggerRedraw();
                }
                decimalButton.Checked = true; 
            }
            else if (text == "Bin")
            {
                if (binaryButton.Checked)
                {
                    decimalButton.Checked = hexButton.Checked = false;
                    TriggerRedraw();
                }
                binaryButton.Checked = true; 
            }
            else if (text == "FO")
            {
                if(fileOffsetButton.Checked)
                {
                    relativeOffsetButton.Checked = false;
                    processHandler.Offset = PluginInterface.Enums.OffsetType.FILE_OFFSET;
                    // RelativeOffset and FileOffsets are the same if you are viewing everything 
                    if (selectedComponent != "everything")
                        TriggerRedraw();
                }
                fileOffsetButton.Checked = true;
            }
            else if (text == "RO")
            {
                if(relativeOffsetButton.Checked)
                {
                    fileOffsetButton.Checked = false;
                    processHandler.Offset = PluginInterface.Enums.OffsetType.RELATIVE_OFFSET;
                    // RelativeOffset and FileOffsets are the same if you are viewing everything 
                    if (selectedComponent != "everything")
                        TriggerRedraw();
                }
                relativeOffsetButton.Checked = true;
            }
            else if(text == "B")
            {
                if(singleByteButton.Checked)
                {
                    doubleByteButton.Checked = false;
                    TriggerRedraw();
                }
                singleByteButton.Checked = true;
            }
            else if(text == "BB")
            {
                if(doubleByteButton.Checked)
                {
                    singleByteButton.Checked = false;
                    TriggerRedraw();
                }
                doubleByteButton.Checked = true;
            }

        }

        /* This will trigger the cells the user is currently looking at to be redrawn since they just changed the format */
        private void TriggerRedraw()
        {
            int firstVisibleRowIndex = dataGridView.FirstDisplayedScrollingRowIndex;
            int lastVisibleRowIndex = firstVisibleRowIndex + dataGridView.DisplayedRowCount(true);

            for (int rowIndex = firstVisibleRowIndex; rowIndex < lastVisibleRowIndex; rowIndex++)
            {
                dataGridView.InvalidateRow(rowIndex);
            }
        }

        private void TreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            // Check if the click is within the bounds of the node (excluding expand/collapse area)
            if (e.Node.Bounds.Contains(e.Location) && processHandler != null)
            {
                TreeNode clickedNode = e.Node;
                string nodeText = clickedNode.Text;
                if (nodeText == "Sections") return;
                if (nodeText.Contains(processHandler.FileName)) nodeText = "everything";

                int previousRowCount = processHandler.GetComponentsRowIndexCount(selectedComponent);
                string selected = nodeText.ToLower();
                int newRowCount = processHandler.GetComponentsRowIndexCount(selected);
  
                if (selected == selectedComponent) return;
                selectedComponent = selected;

                if(processHandler.dataStorage.Settings.ReterunToTop || newRowCount > previousRowCount) dataGridView.FirstDisplayedScrollingRowIndex = 0;

                TriggerRedraw();
                Form1_Resize(this, EventArgs.Empty);
            }
        }

        private void ReplaceButton_Click(object sender, EventArgs e)
        {
            string search = searchTextBox.Text;
            string replace = replaceWithTextBox.Text;
            replaceButton.Checked = false;

            if (search == "" || replace == "" || selectedComponent == "null") return;
            bool replaceAll = false;

            if (replaceAllCheckBox.Control is CheckBox checkBox)
            {   // I need to cast replaceAllCheckBox to CheckBox
                if (checkBox.Checked) replaceAll = true;
            }

            PluginInterface.SuperHeader header = replaceAll ? processHandler.GetComponentFromMap("everything") : processHandler.GetComponentFromMap(selectedComponent);
            int firstRow = (int)Math.Floor(header.StartPoint / 16.0);

            int count = 0;
            for (int row = firstRow; row < header.RowSize; row++)
            {   // This will loop through every row in our selected component
                string[] hexBytes = processHandler.dataStorage.FilesHex[row, 1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                string asciiString = "";
                foreach (string hexByte in hexBytes)
                {   // This will loop through each row's bytes
                    byte asciiByte;
                    if (ParseAscii(hexByte, out asciiByte))
                    {
                        if (asciiByte >= 32 && asciiByte <= 128)
                        {
                            asciiString += (char)asciiByte;

                            if (search == asciiString)
                            {   // This means we reached a target string that we need to replace
                                string originalRow = processHandler.dataStorage.FilesHex[row, 1];
                                string[,] values = processHandler.GetValueVariations(originalRow.Replace(GetHexFromAscii(search), GetHexFromAscii(replace)), true, decimalButton.Checked); // This is always in hex
                                processHandler.dataStorage.FilesHex[row, 1] = values[0, 0];
                                processHandler.dataStorage.FilesDecimal[row, 1] = values[0, 1];
                                processHandler.dataStorage.FilesBinary[row, 1] = values[0, 2];
                                processHandler.dataStorage.UpdateASCII(values[0, 0], row);
                                TriggerRedraw();
                                ++count;

                                asciiString = "";
                                continue;
                            }

                            if (!search.Contains(asciiString))
                            {   // This means this string
                                asciiString = "";
                                continue;
                            }
                        }
                        else asciiString = "";
                    }
                    else asciiString = "";
                }
            }
        }

        private bool ParseAscii(string hexByte, out byte asciiByte)
        {
            if (processHandler.dataStorage.Settings.TreatNullAsPeriod && hexByte == "00")
            {
                asciiByte = (byte)'.';
                return true;
            }
            return byte.TryParse(hexByte, System.Globalization.NumberStyles.HexNumber, null, out asciiByte);
        }

        private string GetHexFromAscii(string ascii)
        {
            StringBuilder hexString = new StringBuilder();

            foreach (char c in ascii)
            {
                hexString.AppendFormat("{0:X2} ", processHandler.dataStorage.Settings.TreatNullAsPeriod ? c == 46 ? 0 : (int)c : (int) c);
            }
            return hexString.ToString().Trim();
        }


        private void DataGridView_CellValuePushed(object sender, DataGridViewCellValueEventArgs e)
        {
            int column = e.ColumnIndex;
            if (column != 1) return;

            int row = e.RowIndex;
            string newValue = (string)e.Value;

            PluginInterface.SuperHeader selectedHeader = processHandler.GetComponentFromMap(selectedComponent);
            if (selectedHeader == null || row >= selectedHeader.RowSize || doubleByteButton.Checked) return;
           
            if (selectedComponent == "everything")
            {   // if this is true then that means I need to update the other data source which first requies me to customize the data to the fit the structure
                string[,] values = processHandler.GetValueVariations(newValue, hexButton.Checked, decimalButton.Checked);

                int offset = int.Parse(selectedHeader.GetData(row, 0, PluginInterface.Enums.DataType.DECIMAL, false, false, processHandler.dataStorage)); // This gets the file offset in decimal form

                processHandler.dataStorage.FilesHex[row, column] = values[0, 0];
                processHandler.dataStorage.FilesDecimal[row, column] = values[0, 1];
                processHandler.dataStorage.FilesBinary[row, column] = values[0, 2];

                processHandler.dataStorage.UpdateASCII(values[0, 0], row);

                foreach (var map in processHandler.componentMap)
                {
                    PluginInterface.SuperHeader comp = map.Value;
                    if (comp.Component != "everything" && comp.StartPoint <= offset && comp.EndPoint >= offset)
                    {
                        //processHandler.RecalculateHeaders(comp);
                        TriggerRedraw();
                        return;
                    }
                }
            }
            else
            {   // else I need to update the data source inside everything
                selectedHeader.UpdateData(row, newValue, hexButton.Checked, decimalButton.Checked, processHandler.dataStorage);
                TriggerRedraw();
            }
            Console.WriteLine(" ");
        }

        private void DataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex % 2 == 0) e.CellStyle.BackColor = SystemColors.ControlDark;
            else e.CellStyle.BackColor = Color.LightGray;
        }

        private void DataGridView_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            PluginInterface.Enums.DataType type = hexButton.Checked ? PluginInterface.Enums.DataType.HEX : decimalButton.Checked ? PluginInterface.Enums.DataType.DECIMAL : PluginInterface.Enums.DataType.BINARY;
            e.Value = processHandler.GetValue(e.RowIndex, e.ColumnIndex, doubleByteButton.Checked, selectedComponent, type);
        }

        /* This will open a custom form for selected cells that */
        private void DataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 2) processHandler.OpenDescrptionForm(selectedComponent, e.RowIndex);
        }

        private void FileLabel_Click(object sender, EventArgs e)
        {
            fileContextMenu.Show(fileLabel, new Point(fileLabel.Location.X, fileLabel.Location.Y + fileLabel.Size.Height));
        }

        private void FileLabel_MouseHover(object sender, EventArgs e)
        {
            fileLabel.BackColor = SystemColors.GradientInactiveCaption;
        }

        private void FileLabel_MouseLeave(object sender, EventArgs e)
        {
            // Change label appearance when mouse leaves
            fileLabel.BackColor = SystemColors.Control;
            fileLabel.ForeColor = SystemColors.ControlText;
        }

        private void SettingsLabel_Click(object sender, EventArgs e)
        {
            settingsMenu.Show(settingsLabel, new Point(settingsLabel.Location.X - settingsLabel.Size.Width + fileLabel.Size.Width, settingsLabel.Location.Y + settingsLabel.Size.Height));
        }

        private void SettingsLabel_MouseLeave(object sender, EventArgs e)
        {
            // Change label appearance when mouse leaves
            settingsLabel.BackColor = SystemColors.Control;
            settingsLabel.ForeColor = SystemColors.ControlText;
        }

        private void SettingsLabel_MouseHover(object sender, EventArgs e)
        {
            settingsLabel.BackColor = SystemColors.GradientInactiveCaption;
        }

        private class MyToolStripRenderer : ToolStripProfessionalRenderer
        {
            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                // Set the background color here
                e.Graphics.FillRectangle(new SolidBrush(Color.LightGray), e.AffectedBounds);
            }
        }

        private void ASCIIToolStripMenuItem_Click(object sender, EventArgs e)
        {
            characterSet.Text = aSCIIToolStripMenuItem.Text;
            characterSet.ToolTipText = "Character Set";
            selectedCharacter = CharacterSet.ASCII;
        }

        public enum CharacterSet
        {
            ASCII
        }

    }
}
