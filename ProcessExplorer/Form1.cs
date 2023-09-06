using System;
using System.Collections.Generic;
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
        private ProcessHandler.ProcessComponent selectedComponent = ProcessHandler.ProcessComponent.NULL_COMPONENT;
        private ProcessHandler processHandler;

        public Form1()
        {
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

            if(processHandler != null && selectedComponent != ProcessHandler.ProcessComponent.NULL_COMPONENT && processHandler.GetSuperHeader(selectedComponent).ShrinkDataSection)
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

            TreeNode dosHeader = new TreeNode("DOS Header");
            TreeNode dosStub = new TreeNode("DOS Stub");
            TreeNode peHeader = new TreeNode("PE Header");

            TreeNode optionalPeHeader = new TreeNode("Optional PE Header");
            if(processHandler.optionalPeHeader != null && !processHandler.optionalPeHeader.FailedToInitlize) peHeader.Nodes.Add(optionalPeHeader);

            TreeNode optionalPeHeader64 = new TreeNode("Optional PE Header 64");
            if (processHandler.optionalPeHeader64 != null && ((OptionalPeHeader)processHandler.optionalPeHeader).peThirtyTwoPlus && !processHandler.optionalPeHeader64.FailedToInitlize)
            {
                peHeader.Nodes.Add(optionalPeHeader64);
                TreeNode optionalPeHeaderDataDirectories = new TreeNode("Optional PE Header Data Directories");
                peHeader.Nodes.Add(optionalPeHeaderDataDirectories);
            }

            TreeNode optionalPeHeader32 = new TreeNode("Optional PE Header 32");
            if (processHandler.optionalPeHeader32 != null && !((OptionalPeHeader)processHandler.optionalPeHeader).peThirtyTwoPlus && !processHandler.optionalPeHeader32.FailedToInitlize)
            {
                peHeader.Nodes.Add(optionalPeHeader32);
                TreeNode optionalPeHeaderDataDirectories = new TreeNode("Optional PE Header Data Directories");
                peHeader.Nodes.Add(optionalPeHeaderDataDirectories);
            }

            TreeNode mainSectionNode = new TreeNode("Sections");

            foreach (var map in processHandler.sectionHeaders)
            {
                SuperHeader.SectionTypes type = map.Key;
                List<SuperHeader> headerList = map.Value;

                TreeNode sectionNode = new TreeNode("");

                int count = 0;
                foreach (SuperHeader section in headerList)
                {
                    string sectionNodeText = SuperHeader.GetSectionString(((ISection)section).GetSectionType()) + " Section ";
                    Console.WriteLine("Adding a Node count:" + count + " Text:" + sectionNodeText + "Length:" + headerList.Count);
                    if (count++ == 0) sectionNode.Text = sectionNodeText + "Header";
                    else
                    {   // This will add the body nodes
                        TreeNode sectionBodyNode = new TreeNode("")
                        {
                            Text = sectionNodeText + "Body"
                        };
                        sectionNode.Nodes.Add(sectionBodyNode);
                        Console.WriteLine("Added a Node Body");
                    }
                }

                mainSectionNode.Nodes.Add(sectionNode); // Add the main section node for each section to our parent node
            }

            if (processHandler.dosHeader != null && !processHandler.dosHeader.FailedToInitlize) rootNode.Nodes.Add(dosHeader);
            if (processHandler.dosStub != null && !processHandler.dosStub.FailedToInitlize) rootNode.Nodes.Add(dosStub);
            if (processHandler.peHeader!= null && !processHandler.peHeader.FailedToInitlize) rootNode.Nodes.Add(peHeader);

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
            dataGridView.RowCount = processHandler.everything.hexArray.GetLength(0);
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

                dataGridView.CellValueNeeded += DataGridView_CellValueNeeded;
                TriggerRedraw();
                Form1_Resize(this, EventArgs.Empty);
            }
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
    }
}
