using System;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.IO;
using ProcessExplorer.components.impl;
using ProcessExplorer.components;

namespace ProcessExplorer
{
    public partial class Form1 : Form
    {
        private readonly ContextMenuStrip fileContextMenu = new ContextMenuStrip();
        private readonly ContextMenuStrip settingsMenu = new ContextMenuStrip();
        private ProcessHandler.ProcessComponent selectedComponent = ProcessHandler.ProcessComponent.NULL_COMPONENT;
        private CharacterSet selectedCharacter = CharacterSet.ASCII;
        private ProcessHandler processHandler;

            //
            // replaceAllCheckBox
            //
/*            this.replaceAllCheckBox.AutoSize = false;
            this.replaceAllCheckBox.Name = "replaceAllCheckBox";
            this.replaceAllCheckBox.Text = "Replace All";
            this.replaceAllCheckBox.Size = new System.Drawing.Size(90, 21);
            this.replaceAllCheckBox.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);*/
        public Form1()
        {
            //replaceAllCheckBox = new ToolStripControlHost(new CheckBox());
            InitializeComponent();

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

            if(processHandler != null && selectedComponent != ProcessHandler.ProcessComponent.NULL_COMPONENT && processHandler.GetComponentFromMap(selectedComponent).ShrinkDataSection)
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
            ToolStripMenuItem settingItem = sender as ToolStripMenuItem;
            settingItem.Checked = !settingItem.Checked; // Toggle checked state
            
            switch(settingItem.Text)
            {
                case "Display offsets in hex": processHandler.OffsetsInHex = settingItem.Checked;
                    TriggerRedraw();
                    break;
                case "Remove extra zeros": processHandler.RemoveZeros = settingItem.Checked;
                    TriggerRedraw();
                    break;
                case "Return to top": processHandler.ReterunToTop = settingItem.Checked;
                    TriggerRedraw();
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

            SuperHeader optionalPeHeader = processHandler.GetComponentFromMap(ProcessHandler.ProcessComponent.OPITIONAL_PE_HEADER);
            if (optionalPeHeader != null && !optionalPeHeader.FailedToInitlize) peHeaderNode.Nodes.Add(optionalPeHeaderNode);

            TreeNode optionalPeHeader64Node = new TreeNode("Optional PE Header 64");
            SuperHeader optionalPeHeader64 = processHandler.GetComponentFromMap(ProcessHandler.ProcessComponent.OPITIONAL_PE_HEADER_64);
            if (optionalPeHeader64 != null && !optionalPeHeader64.FailedToInitlize && ((OptionalPeHeader)optionalPeHeader).peThirtyTwoPlus)
            {
                peHeaderNode.Nodes.Add(optionalPeHeader64Node);
                TreeNode optionalPeHeaderDataDirectories = new TreeNode("Optional PE Header Data Directories");
                peHeaderNode.Nodes.Add(optionalPeHeaderDataDirectories);
            }

            TreeNode optionalPeHeader32Node = new TreeNode("Optional PE Header 32");
            SuperHeader optionalPeHeader32 = processHandler.GetComponentFromMap(ProcessHandler.ProcessComponent.OPITIONAL_PE_HEADER_32);
            if (optionalPeHeader32 != null && !optionalPeHeader32.FailedToInitlize && !((OptionalPeHeader)optionalPeHeader).peThirtyTwoPlus)
            {
                peHeaderNode.Nodes.Add(optionalPeHeader32Node);
                TreeNode optionalPeHeaderDataDirectories = new TreeNode("Optional PE Header Data Directories");
                peHeaderNode.Nodes.Add(optionalPeHeaderDataDirectories);
            }

            TreeNode mainSectionNode = new TreeNode("Sections");

            foreach (var map in processHandler.componentMap)
            {
                SuperHeader header = map.Value;
                string compString = header.Component.ToString();
                if (!compString.Contains("SECTION_HEADER")) continue;

                TreeNode sectionNode = new TreeNode("");
                string sectionNodeText = SuperHeader.GetSectionString(header.Component);
                sectionNode.Text = sectionNodeText;

                foreach (var innerMap in processHandler.componentMap)
                {
                    SuperHeader body = innerMap.Value;
                    string newCompString = body.Component.ToString();
                   
                    if (!newCompString.Contains("SECTION_BODY") || compString.Replace("SECTION_HEADER", "") != newCompString.Replace("SECTION_BODY", "")) continue;

                    TreeNode sectionBodyNode = new TreeNode("")
                    {
                        Text = SuperHeader.GetSectionString(body.Component)
                    };
                    sectionNode.Nodes.Add(sectionBodyNode);
                    break;
                }
                mainSectionNode.Nodes.Add(sectionNode); // Add the main section node for each section to our parent node
                Console.WriteLine(" ");
            }

            SuperHeader dosHeader = processHandler.GetComponentFromMap(ProcessHandler.ProcessComponent.DOS_HEADER);
            SuperHeader dosStub = processHandler.GetComponentFromMap(ProcessHandler.ProcessComponent.DOS_STUB);
            SuperHeader peHeader = processHandler.GetComponentFromMap(ProcessHandler.ProcessComponent.PE_HEADER);
            if (dosHeader != null && !dosHeader.FailedToInitlize) rootNode.Nodes.Add(dosHeaderNode);
            if (dosStub != null && !dosStub.FailedToInitlize) rootNode.Nodes.Add(dosStubNode);
            if (peHeader != null && !peHeader.FailedToInitlize) rootNode.Nodes.Add(peHeaderNode);

            if(mainSectionNode.Nodes.Count > 0) rootNode.Nodes.Add(mainSectionNode);
            //Next I need to check for sections and add them to the root node
            treeView.Nodes.Add(rootNode);

            // This will auto check the following settings on startup
            foreach (ToolStripItem item in settingsMenu.Items)
            {
                if (item is ToolStripMenuItem menuItem)
                {
                    switch(menuItem.Text)
                    {
                        case "Remove extra zeros": menuItem.Checked = processHandler.RemoveZeros = true;
                            break;
                        case "Return to top":
                            menuItem.Checked = processHandler.ReterunToTop = true;
                            break;
                    }
                }
            }

            // This will trigger the data to be dislayed
            selectedComponent = ProcessHandler.ProcessComponent.EVERYTHING;
            dataGridView.RowCount = processHandler.GetComponentFromMap(ProcessHandler.ProcessComponent.EVERYTHING).hexArray.GetLength(0);
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
                    processHandler.Offset = ProcessHandler.OffsetType.FILE_OFFSET;
                    // RelativeOffset and FileOffsets are the same if you are viewing everything 
                    if (selectedComponent != ProcessHandler.ProcessComponent.EVERYTHING)
                        TriggerRedraw();
                }
                fileOffsetButton.Checked = true;
            }
            else if (text == "RO")
            {
                if(relativeOffsetButton.Checked)
                {
                    fileOffsetButton.Checked = false;
                    processHandler.Offset = ProcessHandler.OffsetType.RELATIVE_OFFSET;
                    // RelativeOffset and FileOffsets are the same if you are viewing everything 
                    if (selectedComponent != ProcessHandler.ProcessComponent.EVERYTHING)
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
                ProcessHandler.ProcessComponent selected = ProcessHandler.GetProcessComponent(nodeText);
                int newRowCount = processHandler.GetComponentsRowIndexCount(selected);
  
                if (selected == selectedComponent) return;
                selectedComponent = selected;

                if(processHandler.ReterunToTop || newRowCount > previousRowCount) dataGridView.FirstDisplayedScrollingRowIndex = 0;

                TriggerRedraw();
                Form1_Resize(this, EventArgs.Empty);
            }
        }

        private void UpdateASCII(string hexString, int row)
        {
            string[] hexBytes = hexString.Split(' '); // Split by spaces

            string asciiString = "";
            foreach (string hexByte in hexBytes)
            {
                if (byte.TryParse(hexByte, System.Globalization.NumberStyles.HexNumber, null, out byte asciiByte))
                {
                    if (asciiByte >= 32 && asciiByte <= 128) asciiString += (char)asciiByte;
                    else asciiString += ".";
                }
                else asciiString += ".";
            }
            processHandler.GetComponentFromMap(ProcessHandler.ProcessComponent.EVERYTHING).hexArray[row, 2] = asciiString;
        }

        private void ReplaceButton_Click(object sender, EventArgs e)
        {
            string search = searchTextBox.Text;
            string replace = replaceWithTextBox.Text;
            replaceButton.Checked = false;

            if (search == "" || replace == "" || selectedComponent == ProcessHandler.ProcessComponent.NULL_COMPONENT) return;
            Console.WriteLine("SearchTextBox:" + searchTextBox.Text + " ReplaceAllCheckBox:" + replaceWithTextBox.Text);

            SuperHeader header = processHandler.GetComponentFromMap(selectedComponent);
            int count = 0;
            for (int row=0; row<header.hexArray.GetLength(0); row++)
            {   // This will loop through every row in our selected component
                string[] hexBytes = header.hexArray[row, 1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                int offset = int.Parse(processHandler.GetComponentFromMap(selectedComponent).deciArray[row, 0]); // Offset of where this row starts

                string asciiString = "";
                foreach (string hexByte in hexBytes)
                {   // This will loop through each row's bytes
                    if (byte.TryParse(hexByte, System.Globalization.NumberStyles.HexNumber, null, out byte asciiByte))
                    {
                        if (asciiByte >= 32 && asciiByte <= 128)
                        {
                            asciiString += (char)asciiByte;

                            if(search == asciiString)
                            {   // This means we reached a target string that we need to replace
                                string[,] values;

                                if (selectedComponent == ProcessHandler.ProcessComponent.EVERYTHING)
                                {   // if this is true then that means I need to update the other data source
                                    foreach (var map in processHandler.componentMap)
                                    {   // Loop through every component and check for a similar address space
                                        SuperHeader comp = map.Value;
                                        
                                        if (comp.Component != ProcessHandler.ProcessComponent.EVERYTHING && comp.StartPoint <= offset && comp.EndPoint > offset)
                                        {   // This means the following section or header falls within the changed memory space
                                            Console.WriteLine("HEADER StartPoint:" + comp.StartPoint + " EndPoint:" + comp.EndPoint + " Offset:" + offset + " Type:" + comp.Component.ToString());
                                            string originalRow = processHandler.GetComponentFromMap(selectedComponent).hexArray[row, 1];
                                            values = GetValueVariations(originalRow.Replace(GetHexFromAscii(search), GetHexFromAscii(replace)), true); // This is always in hex
                                            UpdateASCII(values[0, 0], row);

                                            Console.WriteLine("Comp:" + comp.Component.ToString() + " Original:" + string.Join(" ", originalRow) + "   Value:" 
                                                + string.Join(" ", values[0,0]) );

                                            processHandler.GetComponentFromMap(selectedComponent).hexArray[row, 1] = values[0, 0];
                                            processHandler.GetComponentFromMap(selectedComponent).deciArray[row, 1] = values[0, 1];
                                            processHandler.GetComponentFromMap(selectedComponent).binaryArray[row, 1] = values[0, 2]; 
                                            processHandler.RecalculateHeaders(comp);
                                            TriggerRedraw();
                                            ++count;
                                            break;
                                        }
                                    }
                                }
                                else
                                {   // else I need to update the data source inside everything
                                    const ProcessHandler.ProcessComponent everyEnum = ProcessHandler.ProcessComponent.EVERYTHING;
                                    string originalRow = processHandler.GetComponentFromMap(selectedComponent).hexArray[row, 1];
                                    values = GetValueVariations(originalRow.Replace(GetHexFromAscii(search), GetHexFromAscii(replace)), true); //GetValueVariations(GetHexFromAscii(replace), true); // This is always in hex
                                    processHandler.GetComponentFromMap(selectedComponent).hexArray[row, 1] = values[0, 0];
                                    processHandler.GetComponentFromMap(selectedComponent).deciArray[row, 1] = values[0, 1];
                                    processHandler.GetComponentFromMap(selectedComponent).binaryArray[row, 1] = values[0, 2];

                                    int everythingRow = (int)Math.Floor(offset / 16.0);
                                    int everythingOffset = int.Parse(processHandler.GetComponentFromMap(everyEnum).deciArray[everythingRow, 0]);
                                    int dataByteLength = (processHandler.GetComponentFromMap(selectedComponent).hexArray[row, 1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)).Length;
                                    int difference = offset - everythingOffset;

                                    Console.WriteLine("Comp:" + selectedComponent.ToString() + "  Hex:" + string.Join(" ", values[0, 0]) + "  EverythingRowNum:" +
                                        everythingRow + " EverythingOffset:" + everythingOffset + " DataByteLength:" + dataByteLength + " Difference:" + difference +
                                        "  OriginalRow:" + processHandler.GetComponentFromMap(everyEnum).hexArray[everythingRow, 1]);

                                    processHandler.GetComponentFromMap(everyEnum).hexArray[everythingRow, 1] = ReplaceData(difference, dataByteLength,
                                         processHandler.GetComponentFromMap(everyEnum).hexArray[everythingRow, 1], values[0, 0], 0, everyEnum);
                                    processHandler.GetComponentFromMap(everyEnum).deciArray[everythingRow, 1] = ReplaceData(difference, dataByteLength,
                                        processHandler.GetComponentFromMap(everyEnum).deciArray[everythingRow, 1], values[0, 1], 0, everyEnum);
                                    processHandler.GetComponentFromMap(everyEnum).binaryArray[everythingRow, 1] = ReplaceData(difference, dataByteLength,
                                        processHandler.GetComponentFromMap(everyEnum).binaryArray[everythingRow, 1], values[0, 2], 0, everyEnum);
                                    UpdateASCII(processHandler.GetComponentFromMap(everyEnum).hexArray[everythingRow, 1], everythingRow);
                                    Console.WriteLine("UpdatedRow:" + processHandler.GetComponentFromMap(everyEnum).hexArray[everythingRow, 1]);

                                    if(selectedComponent == ProcessHandler.ProcessComponent.DOS_STUB || selectedComponent.ToString().Contains("SECTION_BODY")) {
                                        values = GetValueVariations(processHandler.GetComponentFromMap(everyEnum).hexArray[everythingRow, 1], true); // This is always in hex
                                        processHandler.GetComponentFromMap(selectedComponent).hexArray[row, 1] = values[0, 0];
                                        processHandler.GetComponentFromMap(selectedComponent).deciArray[row, 1] = values[0, 1];
                                        processHandler.GetComponentFromMap(selectedComponent).binaryArray[row, 1] = values[0, 2];
                                    }

                                    Console.WriteLine("NewHex: " + processHandler.GetComponentFromMap(everyEnum).hexArray[everythingRow, 1]);
                                    TriggerRedraw();
                                    ++count;
                                    break;
                                }

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
            MessageBox.Show("Replaced \"" + search + "\" with " + "\"" + replace + "\" " + count.ToString() + " times!");
            Console.WriteLine(" ");
        }

        private string GetHexFromAscii(string ascii)
        {
            StringBuilder hexString = new StringBuilder();

            foreach (char c in ascii)
            {
                hexString.AppendFormat("{0:X2} ", (int)c);
            }
            return hexString.ToString().Trim();
        }


        private void DataGridView_CellValuePushed(object sender, DataGridViewCellValueEventArgs e)
        {
            int column = e.ColumnIndex;
            if (column != 1) return;

            int row = e.RowIndex; 
            string newValue = (string) e.Value;

            if (processHandler.GetComponentFromMap(selectedComponent) == null || row >= processHandler.GetComponentFromMap(selectedComponent).hexArray.GetLength(0)) return;

            int orignalLength = (processHandler.GetComponentFromMap(selectedComponent).hexArray[row, 1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)).Length;
            string[,] values = GetValueVariations(newValue, false);
            processHandler.GetComponentFromMap(selectedComponent).hexArray[row, column] = values[0, 0]; 
            processHandler.GetComponentFromMap(selectedComponent).deciArray[row, column] = values[0, 1];
            processHandler.GetComponentFromMap(selectedComponent).binaryArray[row, column] = values[0, 2];
            Console.WriteLine("EVERYTHING NewValue:" + newValue + " Row:" + e.RowIndex + " Column:" + e.ColumnIndex + " Component:" + selectedComponent.ToString() );

            int offset = int.Parse(processHandler.GetComponentFromMap(selectedComponent).deciArray[row, 0]); // This gets the file offset in decimal form
            if (selectedComponent == ProcessHandler.ProcessComponent.EVERYTHING)
            {   // if this is true then that means I need to update the other data source which first requies me to customize the data to the fit the structure
                UpdateASCII(values[0, 0], row);

                foreach (var map in processHandler.componentMap)
                {
                    SuperHeader comp = map.Value;
                    Console.WriteLine("HEADER StartPoint:" + comp.StartPoint + " EndPoint:" + comp.EndPoint + " Offset:" + offset + " Type:" + comp.Component.ToString());
                    if(comp.Component != ProcessHandler.ProcessComponent.EVERYTHING && comp.StartPoint <= offset && comp.EndPoint >= offset)
                    {
                        processHandler.RecalculateHeaders(comp);
                        Console.WriteLine("Update from Everything for Normal Header");
                        TriggerRedraw();
                        return;
                    }
                }
            }
            else
            {   // else I need to update the data source inside everything
                int everythingRow = (int) Math.Floor(offset / 16.0);
                int everythingOffset = int.Parse(processHandler.GetComponentFromMap(ProcessHandler.ProcessComponent.EVERYTHING).deciArray[everythingRow, 0]);
                int dataByteLength = (processHandler.GetComponentFromMap(selectedComponent).hexArray[row, 1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)).Length;
                int difference = offset - everythingOffset;
                string[] hex = processHandler.GetComponentFromMap(ProcessHandler.ProcessComponent.EVERYTHING).hexArray[everythingRow, column]
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); // This ensures extra white space array indexes get removed


                Console.WriteLine("EverythingRow:" + everythingRow + " EverythingOffset:" + everythingOffset + " Offset:" + offset + " ByteLength:" + dataByteLength +
                    " Difference:" + difference + " CompsHex:" + processHandler.GetComponentFromMap(selectedComponent).hexArray[row, 1]);

                Console.WriteLine("OTHER SOURCE NewValue:" + newValue + " Row:" + e.RowIndex + " Column:" + e.ColumnIndex);
                Console.WriteLine("OriginalHex: " + string.Join(" ", hex));
                processHandler.GetComponentFromMap(ProcessHandler.ProcessComponent.EVERYTHING).hexArray[everythingRow, column] = ReplaceData(difference, dataByteLength,
                    processHandler.GetComponentFromMap(ProcessHandler.ProcessComponent.EVERYTHING).hexArray[everythingRow, column], values[0, 0], orignalLength, selectedComponent);
                processHandler.GetComponentFromMap(ProcessHandler.ProcessComponent.EVERYTHING).deciArray[everythingRow, column] = ReplaceData(difference, dataByteLength,
                    processHandler.GetComponentFromMap(ProcessHandler.ProcessComponent.EVERYTHING).deciArray[everythingRow, column], values[0, 1], orignalLength, selectedComponent);
                processHandler.GetComponentFromMap(ProcessHandler.ProcessComponent.EVERYTHING).binaryArray[everythingRow, column] = ReplaceData(difference, dataByteLength,
                    processHandler.GetComponentFromMap(ProcessHandler.ProcessComponent.EVERYTHING).binaryArray[everythingRow, column], values[0, 2], orignalLength, selectedComponent);
                UpdateASCII(processHandler.GetComponentFromMap(ProcessHandler.ProcessComponent.EVERYTHING).hexArray[everythingRow, 1], everythingRow);
                Console.WriteLine("NewHex: " + processHandler.GetComponentFromMap(ProcessHandler.ProcessComponent.EVERYTHING).hexArray[everythingRow, column]);
                TriggerRedraw();
            }
            Console.WriteLine(" ");
        }

        private string ReplaceData(int difference, int dataByteLength, string data, string replacment, int originalLength, ProcessHandler.ProcessComponent type)
        {
            string[] originalBytes = data.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string[] replacementBytes = replacment.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (type == ProcessHandler.ProcessComponent.DOS_STUB || type.ToString().Contains("SECTION_BODY"))
            {   // This means I just need to replace the data instead of switching out a few bytes
                Console.WriteLine("ReplacementLength:" + replacementBytes.Length + " originalLength:" + originalLength);
                if(replacementBytes.Length < originalLength)
                {   // This means the user is trying to reduce the data sections size for some reason
                    return string.Join(" ", replacementBytes);
                }

                for(int i=0; i<replacementBytes.Length; i++)
                {
                    if (originalBytes.Length - 1 >= i) originalBytes[i] = replacementBytes[i];
                }
                return string.Join(" ", originalBytes);
            }

            if (difference >= 0 && dataByteLength > 0 && difference + (dataByteLength * 3) <= data.Length)
            {
                int byteLength = originalBytes.Length;
                if (difference >= 0 && dataByteLength > 0 && difference + dataByteLength <= byteLength)
                {
                    // Calculate the start and end indexes of the section to replace
                    int startIndex = difference;
                    int endIndex = startIndex + dataByteLength;

                    // Copy the original data
                    string[] modifiedBytes = new string[byteLength];
                    Array.Copy(originalBytes, modifiedBytes, byteLength);

                    // Replace the specified section with the replacement data
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        modifiedBytes[i] = replacementBytes[i - startIndex];
                    }

                    // Combine the modified bytes into a single string
                    return string.Join(" ", modifiedBytes);
                }
            }
            return data;
        }


        /* Here index 0 is hex, index 1 is decimal, and index 2 is binary */
        private string[,] GetValueVariations(string newValue, bool isHex)
        {
            string[,] values = new string[1, 3];
            string[] bytes = newValue.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if(hexButton.Checked || isHex)
            {
                values[0, 0] = newValue; // Hex
                values[0, 1] = string.Join(" ", Array.ConvertAll(bytes, hex => long.Parse(hex, System.Globalization.NumberStyles.HexNumber))); //string.Join(" ", bytes.Select(hex => byte.Parse(hex, System.Globalization.NumberStyles.HexNumber))); // Decimal
                values[0, 2] = string.Join(" ", bytes.Select(hexByte => Convert.ToString(byte.Parse(hexByte, System.Globalization.NumberStyles.HexNumber), 2).PadLeft(8, '0'))); // Binary
            }
            else if(decimalButton.Checked)
            {
                var decimalNumbers = bytes.Select(number => long.Parse(number)).ToList();
                values[0, 0] = string.Join(" ", decimalNumbers.Select(decimalValue => decimalValue.ToString("X"))); // Hex
                values[0, 1] = newValue; // Decimal
                values[0, 2] = string.Join(" ", decimalNumbers.Select(decimalValue => Convert.ToString(decimalValue, 2).PadLeft(8, '0'))); // Binary
            }
            else
            {
                var binaryBytes = bytes.Select(binary => Convert.ToByte(binary, 2)).ToArray(); 
                values[0, 0] = BitConverter.ToString(binaryBytes).Replace("-", " "); // Hex
                values[0, 1] = string.Join(" ", binaryBytes.Select(byteValue => byteValue.ToString())); // Decimal
                values[0, 2] = newValue; // Binary
            }
            Console.WriteLine("Hex:" + string.Join(" ", values[0, 0]) ); //+ "  Decimal:" + string.Join(" ", values[0, 1]) + "  Binary:" + string.Join(" ", values[0, 2])
            return values;
        }

        private void DataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex % 2 == 0) e.CellStyle.BackColor = SystemColors.ControlDark;
            else e.CellStyle.BackColor = Color.LightGray;
        }

        private void DataGridView_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            ProcessHandler.DataType type = hexButton.Checked ? ProcessHandler.DataType.HEX : decimalButton.Checked ? ProcessHandler.DataType.DECIMAL : ProcessHandler.DataType.BINARY;
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
