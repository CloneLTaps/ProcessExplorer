using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

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
            fileContextMenu.ItemClicked += new ToolStripItemClickedEventHandler(fileLabel_Click_ContextMenu);

            ToolStripMenuItem setting1 = new ToolStripMenuItem("Remove extra 0's");
            setting1.Click += Settings_Click;
            settingsMenu.Items.Add(setting1);

            ToolStripMenuItem setting2 = new ToolStripMenuItem("Display offsets in hex");
            setting2.Click += Settings_Click;
            settingsMenu.Items.Add(setting2);
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            Console.WriteLine("Starting HexEditor");
            splitContainer1.MaximumSize = new Size(ClientSize.Width, Height - 80);

            foreach (ToolStripItem t in toolStrip.Items)
            {   // Assign an event for every button
                t.Click += new System.EventHandler(this.toolStripButton_Click);
            }

            hexButton.Checked = singleByteButton.Checked = fileOffsetButton.Checked = true;
            Resize += Form1_Resize;
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
            const int column2StartingWidth = 492;
            const int column3StartingWidth = 150;

            // This makes the 2nd column expand faster than the others
            int difference = newWidth - defaultWidth;
            int column2Width = column2StartingWidth + (int) (difference * 0.9);
            int remainingWidth = (int) (difference * 0.1);

            dataGridView.Columns[0].Width = column1StartingWidth + remainingWidth; // Adjust column indices as needed
            dataGridView.Columns[1].Width = column2Width;
            dataGridView.Columns[2].Width = column3StartingWidth; // This wont ever need to expand since there will always be 16 characters
        }

        private void Settings_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem settingItem = sender as ToolStripMenuItem;
            settingItem.Checked = !settingItem.Checked; // Toggle checked state
            Console.WriteLine("Text: " + settingItem.Text);
            
            switch(settingItem.Text)
            {
                case "Display offsets in hex" : processHandler.OffsetsInHex = settingItem.Checked;
                    triggerRedraw();
                    break;
                case "Remove extra 0's": processHandler.RemoveZeros = settingItem.Checked;
                    triggerRedraw();
                    break;
            }
        }

        private void fileLabel_Click_ContextMenu(object sender, ToolStripItemClickedEventArgs e)
        {
            string text = e.ClickedItem.Text;

            if(text == "Open")
            {
                fileContextMenu.Close();
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "Executable Files (*.exe;*.dll)|*.exe;*.dll|All Files (*.*)|*.*"; //"All Files (*.*)|*.*"; 
                    openFileDialog.RestoreDirectory = true;

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string selectedFilePath = openFileDialog.FileName;
                        try
                        {
                            using FileStream fileStream = new FileStream(selectedFilePath, FileMode.Open, FileAccess.Read);
                            processHandler = new ProcessHandler(fileStream);
                            setup();
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

            }
        }

        private void setup()
        {
            TreeNode rootNode = new TreeNode(processHandler.FileName);

            TreeNode dosHeader = new TreeNode("DOS Header");
            TreeNode dosStub = new TreeNode("DOS Stub");
            TreeNode peHeader = new TreeNode("PE Header");

            TreeNode optionalPeHeader = new TreeNode("Optional PE Header");
            peHeader.Nodes.Add(optionalPeHeader);

            rootNode.Nodes.Add(dosHeader);
            rootNode.Nodes.Add(dosStub);
            rootNode.Nodes.Add(peHeader);
            //Next I need to check for sections and add them to the root node
            treeView.Nodes.Add(rootNode);

            // This will auto check the following settings on startup
            foreach (ToolStripItem item in settingsMenu.Items)
            {
                if (item is ToolStripMenuItem menuItem && menuItem.Text == "Remove extra 0's")
                {   
                    menuItem.Checked = processHandler.RemoveZeros = true;
                }
            }

            // This will trigger the data to be dislayed
            selectedComponent = ProcessHandler.ProcessComponent.EVERYTHING;
            dataGridView.RowCount = processHandler.filesHex.GetLength(0);
            dataGridView.CellValueNeeded += dataGridView_CellValueNeeded;
            triggerRedraw();
        }

        private void toolStripButton_Click(object sender, EventArgs e)
        {
            // Get the button that was clicked
            ToolStripButton t = sender as ToolStripButton;
            if (t == null) return;

            string text = t.Text;
            Console.WriteLine("ButtonClick:" + text + " HexChecked:" + hexButton.Checked + " BinaryChecked:" + binaryButton.Checked +
                " DecimalChecked:" + decimalButton.Checked);
            if (text == "Hex")
            {   // This ensures the user cant uncheck the only option
                if (hexButton.Checked)
                {
                    binaryButton.Checked = decimalButton.Checked = false;
                    triggerRedraw();
                }
                hexButton.Checked = true;
            }
            else if (text == "Dec")
            {
                if (decimalButton.Checked)
                {
                    binaryButton.Checked = hexButton.Checked = false;
                    triggerRedraw();
                }
                decimalButton.Checked = true;
            }
            else if (text == "Bin")
            {
                if (binaryButton.Checked)
                {
                    decimalButton.Checked = hexButton.Checked = false;
                    triggerRedraw();
                }
                binaryButton.Checked = true;
            }
            else if (text == "FO")
            {
                if(fileOffsetButton.Checked)
                {
                    relativeOffsetButton.Checked = false;
                    // RelativeOffset and FileOffsets are the same if you are viewing everything or DOS Headers
                    if (selectedComponent != ProcessHandler.ProcessComponent.EVERYTHING && selectedComponent != ProcessHandler.ProcessComponent.DOS_HEADER)
                        triggerRedraw();
                }
                fileOffsetButton.Checked = true;
            }
            else if (text == "RO")
            {
                if(relativeOffsetButton.Checked)
                {
                    fileOffsetButton.Checked = false;
                    // RelativeOffset and FileOffsets are the same if you are viewing everything or DOS Headers
                    if (selectedComponent != ProcessHandler.ProcessComponent.EVERYTHING && selectedComponent != ProcessHandler.ProcessComponent.DOS_HEADER)
                        triggerRedraw();
                }
                relativeOffsetButton.Checked = true;
            }
            else if(text == "B")
            {
                if(singleByteButton.Checked)
                {
                    doubleByteButton.Checked = false;
                    triggerRedraw();
                }
                singleByteButton.Checked = true;
            }
            else if(text == "BB")
            {
                if(doubleByteButton.Checked)
                {
                    singleByteButton.Checked = false;
                    triggerRedraw();
                }
                doubleByteButton.Checked = true;
            }

        }

        /* This will trigger the cells the user is currently looking at to be redrawn since they just changed the format */
        private void triggerRedraw()
        {
            int firstVisibleRowIndex = dataGridView.FirstDisplayedScrollingRowIndex;
            int lastVisibleRowIndex = firstVisibleRowIndex + dataGridView.DisplayedRowCount(true);

            for (int rowIndex = firstVisibleRowIndex; rowIndex < lastVisibleRowIndex; rowIndex++)
            {
                dataGridView.InvalidateRow(rowIndex);
            }
        }

        private void treeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            // Check if the click is within the bounds of the node (excluding expand/collapse area)
            if (e.Node.Bounds.Contains(e.Location) && processHandler != null)
            {
                TreeNode clickedNode = e.Node;
                string nodeText = clickedNode.Text;
                if (nodeText.Contains(".exe") || nodeText.Contains(".dll")) nodeText = "everything";
                ProcessHandler.ProcessComponent selected = ProcessHandler.getProcessComponent(nodeText);
                //dataGridView.RowCount = processHandler.filesHex.GetLength(0);
                if (selected == selectedComponent) return;
                selectedComponent = selected;

                Console.WriteLine("Selected:" + selectedComponent + " Rows:" + processHandler.filesHex.GetLength(0));
                dataGridView.CellValueNeeded += dataGridView_CellValueNeeded;
                triggerRedraw();
            }
        }
        
        private void dataGridView_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex >= processHandler.filesHex.GetLength(0) || e.ColumnIndex >= processHandler.filesHex.GetLength(1)) return;

            // This will ensure the data is displayed in the correct format
            if (decimalButton.Checked)
            {
                switch (selectedComponent)
                {
                    case ProcessHandler.ProcessComponent.EVERYTHING:
                        // decimal and binary's array both dont contain the ASCI characters
                        if (e.ColumnIndex < 2) e.Value = processHandler.getDecimal(e.RowIndex, e.ColumnIndex, doubleByteButton.Checked);
                        else e.Value = processHandler.filesHex[e.RowIndex, e.ColumnIndex];
                        break;
                    case ProcessHandler.ProcessComponent.DOS_HEADER:
                       // dataGridView.RowCount = processHandler.dosHeader.getRowCount();
                        e.Value = processHandler.dosHeader.getDecimal(e.RowIndex, e.ColumnIndex, doubleByteButton.Checked);
                        break;
                }
            }
            else if (binaryButton.Checked)
            {   
                switch (selectedComponent)
                {
                    case ProcessHandler.ProcessComponent.EVERYTHING:
                        if (e.ColumnIndex < 2) e.Value = processHandler.getBinary(e.RowIndex, e.ColumnIndex, doubleByteButton.Checked);
                        else e.Value = processHandler.filesHex[e.RowIndex, e.ColumnIndex];
                        break;
                    case ProcessHandler.ProcessComponent.DOS_HEADER:
                        e.Value = processHandler.dosHeader.getBinary(e.RowIndex, e.ColumnIndex, doubleByteButton.Checked);
                        break;
                }
            }
            else
            {
                switch (selectedComponent)
                {
                    case ProcessHandler.ProcessComponent.EVERYTHING : e.Value = processHandler.getHex(e.RowIndex, e.ColumnIndex, doubleByteButton.Checked);
                        break;
                    case ProcessHandler.ProcessComponent.DOS_HEADER: // Starting here the total rows of EVERYTHING will excede the size of the bellow arrays
                        e.Value = processHandler.dosHeader.getHex(e.RowIndex, e.ColumnIndex, doubleByteButton.Checked);
                        break;

                }
            }
        }

        private void dataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
/*            if (processHandler != null && e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                DataGridViewCell cell = dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex];
                if (cell != null && cell.Style != null)
                {
                    // Set the desired font size here
                    cell.Style.Font = new Font(cell.Style.Font.FontFamily, 12); // Change 12 to your desired font size
                }
            }
            Console.WriteLine("Change text");*/
        }

        private void fileLabel_Click(object sender, EventArgs e)
        {
            fileContextMenu.Show(fileLabel, new Point(fileLabel.Location.X, fileLabel.Location.Y + fileLabel.Size.Height));
        }

        private void fileLabel_MouseHover(object sender, EventArgs e)
        {
            fileLabel.BackColor = SystemColors.GradientInactiveCaption;
        }

        private void fileLabel_MouseLeave(object sender, EventArgs e)
        {
            // Change label appearance when mouse leaves
            fileLabel.BackColor = SystemColors.Control;
            fileLabel.ForeColor = SystemColors.ControlText;
        }

        private void settingsLabel_Click(object sender, EventArgs e)
        {
            settingsMenu.Show(settingsLabel, new Point(settingsLabel.Location.X - settingsLabel.Size.Width + fileLabel.Size.Width, settingsLabel.Location.Y + settingsLabel.Size.Height));
        }
    }
}
