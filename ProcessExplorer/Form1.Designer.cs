﻿using PluginInterface;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using static PluginInterface.Enums;

namespace ProcessExplorer
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
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

        private void DownloadButton_Click(object sender, EventArgs e)
        {
            if (selectedComponent == null || !selectedComponent.Contains("resource body")) return;

            SuperHeader body = processHandler.GetComponentFromMap(selectedComponent);
            if (body == null) return;

            byte[] bytes = new byte[body.EndPoint - body.StartPoint];
            int byteIndex = 0;

            for (int i=0; i<body.RowSize; i++)
            {
                if (byteIndex >= bytes.Length) break;

                string[] hexValues = body.GetData(i, 1, DataType.HEX, 1, true, processHandler.dataStorage).Split(' ');
                foreach (string hex in hexValues)
                {
                    if (!string.IsNullOrEmpty(hex)) 
                    {
                        bytes[byteIndex++] = byte.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        if (byteIndex >= bytes.Length) break;
                    }
                }
            }

            if (bytes.Length == 0) return;

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = "Resource"; 

            DialogResult result = saveFileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                string filePath = saveFileDialog.FileName;

                try
                {
                    File.WriteAllBytes(saveFileDialog.FileName, bytes);
                    Console.WriteLine("File saved successfully!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error writing the file: " + ex.Message);
                }
            }
        }

        private void TreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            // Check if the click is within the bounds of the node (excluding expand/collapse area)
            if (e.Node.Bounds.Contains(e.Location) && processHandler != null)
            {
                TreeNode clickedNode = e.Node;
                string nodeText = clickedNode.Text;

                if (nodeText == "Sections" || nodeText == "Chunks") return;
                if (nodeText.Contains(processHandler.dataStorage.FileName)) nodeText = "everything";

                int previousRowCount = processHandler.GetComponentsRowIndexCount(selectedComponent);
                string selected = nodeText.ToLower();
                int newRowCount = processHandler.GetComponentsRowIndexCount(selected);

                if (selected == selectedComponent) return;
                selectedComponent = selected;

                downloadButton.Visible = nodeText.Contains("resource body") && !nodeText.Contains("resource body header");

                dataGridView.SuspendLayout();
                dataGridView.Rows.Clear();
                if (processHandler.dataStorage.Settings.ReterunToTop || newRowCount > previousRowCount) dataGridView.FirstDisplayedScrollingRowIndex = 0;
                dataGridView.RowCount = newRowCount + 1;
                dataGridView.Invalidate();
                dataGridView.Refresh();
                dataGridView.ResumeLayout();

                Form1_Resize(this, EventArgs.Empty);
            }
        }


        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.fileLabel = new System.Windows.Forms.Label();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.treeView = new System.Windows.Forms.TreeView();
            this.dataGridView = new System.Windows.Forms.DataGridView();
            this.Offset = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Data = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ASCII = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.hexLabel = new System.Windows.Forms.Label();
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.hexButton = new System.Windows.Forms.ToolStripButton();
            this.decimalButton = new System.Windows.Forms.ToolStripButton();
            this.binaryButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.fileOffsetButton = new System.Windows.Forms.ToolStripButton();
            this.relativeOffsetButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.singleByteButton = new System.Windows.Forms.ToolStripButton();
            this.doubleByteButton = new System.Windows.Forms.ToolStripButton();
            this.fourByteButton = new System.Windows.Forms.ToolStripButton();
            this.eightByteButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.replaceButton = new System.Windows.Forms.ToolStripButton();
            this.characterSet = new System.Windows.Forms.ToolStripDropDownButton();
            this.aSCIIToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.searchTextBox = new System.Windows.Forms.ToolStripTextBox();
            this.replaceWithTextBox = new System.Windows.Forms.ToolStripTextBox();
            this.settingsLabel = new System.Windows.Forms.Label();
            this.blankTopLabel = new System.Windows.Forms.Label();
            this.pluginsLabel = new System.Windows.Forms.Label();
            this.downloadButton = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).BeginInit();
            this.toolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // fileLabel
            // 
            this.fileLabel.BackColor = System.Drawing.Color.White;
            this.fileLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.fileLabel.Location = new System.Drawing.Point(0, 0);
            this.fileLabel.Margin = new System.Windows.Forms.Padding(3);
            this.fileLabel.Name = "fileLabel";
            this.fileLabel.Size = new System.Drawing.Size(29, 19);
            this.fileLabel.TabIndex = 0;
            this.fileLabel.Text = "File";
            this.fileLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.fileLabel.Click += new System.EventHandler(this.FileLabel_Click);
            this.fileLabel.MouseLeave += new System.EventHandler(this.FileLabel_MouseLeave);
            this.fileLabel.MouseHover += new System.EventHandler(this.FileLabel_MouseHover);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Cursor = System.Windows.Forms.Cursors.VSplit;
            this.splitContainer1.Location = new System.Drawing.Point(0, 44);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.treeView);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.dataGridView);
            this.splitContainer1.Size = new System.Drawing.Size(984, 517);
            this.splitContainer1.SplitterDistance = 253;
            this.splitContainer1.TabIndex = 1;
            // 
            // treeView
            // 
            this.treeView.BackColor = System.Drawing.Color.LightGray;
            this.treeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeView.Location = new System.Drawing.Point(0, 0);
            this.treeView.Name = "treeView";
            this.treeView.Size = new System.Drawing.Size(253, 517);
            this.treeView.TabIndex = 0;
            this.treeView.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.TreeView_NodeMouseClick);
            // 
            // dataGridView
            // 
            dataGridViewCellStyle5.BackColor = System.Drawing.SystemColors.ControlDark;
            this.dataGridView.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle5;
            this.dataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridView.BackgroundColor = System.Drawing.SystemColors.ControlDarkDark;
            dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.BackColor = System.Drawing.SystemColors.ControlDark;
            dataGridViewCellStyle6.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle6.ForeColor = System.Drawing.SystemColors.ControlDark;
            dataGridViewCellStyle6.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle6.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle6;
            this.dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView.ColumnHeadersVisible = false;
            this.dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Offset,
            this.Data,
            this.ASCII});
            dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle7.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle7.Font = new System.Drawing.Font("Courier New", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle7.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle7.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle7.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle7.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dataGridView.DefaultCellStyle = dataGridViewCellStyle7;
            this.dataGridView.GridColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.dataGridView.Location = new System.Drawing.Point(0, 0);
            this.dataGridView.Name = "dataGridView";
            dataGridViewCellStyle8.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle8.BackColor = System.Drawing.SystemColors.ControlDark;
            dataGridViewCellStyle8.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle8.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle8.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle8.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle8.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle8;
            this.dataGridView.RowHeadersVisible = false;
            this.dataGridView.RowTemplate.Height = 25;
            this.dataGridView.Size = new System.Drawing.Size(727, 517);
            this.dataGridView.TabIndex = 0;
            this.dataGridView.VirtualMode = true;
            this.dataGridView.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.DataGridView_CellContentClick);
            // 
            // Offset
            // 
            this.Offset.DataPropertyName = "Offset";
            this.Offset.FillWeight = 1F;
            this.Offset.HeaderText = "";
            this.Offset.MaxInputLength = 30;
            this.Offset.MinimumWidth = 85;
            this.Offset.Name = "Offset";
            this.Offset.ReadOnly = true;
            // 
            // Data
            // 
            this.Data.DataPropertyName = "Data";
            this.Data.FillWeight = 3F;
            this.Data.HeaderText = "";
            this.Data.MinimumWidth = 150;
            this.Data.Name = "Data";
            // 
            // ASCII
            // 
            this.ASCII.DataPropertyName = "ASCII";
            this.ASCII.FillWeight = 1F;
            this.ASCII.HeaderText = "";
            this.ASCII.MaxInputLength = 16;
            this.ASCII.MinimumWidth = 150;
            this.ASCII.Name = "ASCII";
            // 
            // hexLabel
            // 
            this.hexLabel.BackColor = System.Drawing.Color.DimGray;
            this.hexLabel.Location = new System.Drawing.Point(0, 19);
            this.hexLabel.Name = "hexLabel";
            this.hexLabel.Size = new System.Drawing.Size(38, 25);
            this.hexLabel.TabIndex = 2;
            this.hexLabel.Text = "Hex";
            this.hexLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // toolStrip
            // 
            this.toolStrip.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.toolStrip.AutoSize = false;
            this.toolStrip.Dock = System.Windows.Forms.DockStyle.None;
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.hexButton,
            this.decimalButton,
            this.binaryButton,
            this.toolStripSeparator1,
            this.fileOffsetButton,
            this.relativeOffsetButton,
            this.toolStripSeparator2,
            this.singleByteButton,
            this.doubleByteButton,
            this.fourByteButton,
            this.eightByteButton,
            this.toolStripSeparator3,
            this.replaceButton,
            this.characterSet,
            this.searchTextBox,
            this.replaceWithTextBox,
            this.downloadButton});
            this.toolStrip.Location = new System.Drawing.Point(0, 19);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Padding = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this.toolStrip.Size = new System.Drawing.Size(990, 25);
            this.toolStrip.TabIndex = 3;
            this.toolStrip.Text = "toolStrip";
            // 
            // hexButton
            // 
            this.hexButton.AutoSize = false;
            this.hexButton.CheckOnClick = true;
            this.hexButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.hexButton.Image = ((System.Drawing.Image)(resources.GetObject("hexButton.Image")));
            this.hexButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.hexButton.Name = "hexButton";
            this.hexButton.Size = new System.Drawing.Size(30, 21);
            this.hexButton.Text = "Hex";
            this.hexButton.ToolTipText = "Displays data in hexadecimal";
            // 
            // decimalButton
            // 
            this.decimalButton.AutoSize = false;
            this.decimalButton.CheckOnClick = true;
            this.decimalButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.decimalButton.Image = ((System.Drawing.Image)(resources.GetObject("decimalButton.Image")));
            this.decimalButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.decimalButton.Name = "decimalButton";
            this.decimalButton.Size = new System.Drawing.Size(31, 21);
            this.decimalButton.Text = "Dec";
            this.decimalButton.ToolTipText = "Displays data in decimal";
            // 
            // binaryButton
            // 
            this.binaryButton.AutoSize = false;
            this.binaryButton.CheckOnClick = true;
            this.binaryButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.binaryButton.Image = ((System.Drawing.Image)(resources.GetObject("binaryButton.Image")));
            this.binaryButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.binaryButton.Name = "binaryButton";
            this.binaryButton.Size = new System.Drawing.Size(30, 21);
            this.binaryButton.Text = "Bin";
            this.binaryButton.ToolTipText = "Displays data in binary";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // fileOffsetButton
            // 
            this.fileOffsetButton.AutoSize = false;
            this.fileOffsetButton.CheckOnClick = true;
            this.fileOffsetButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.fileOffsetButton.Image = ((System.Drawing.Image)(resources.GetObject("fileOffsetButton.Image")));
            this.fileOffsetButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.fileOffsetButton.Name = "fileOffsetButton";
            this.fileOffsetButton.Size = new System.Drawing.Size(30, 21);
            this.fileOffsetButton.Text = "FO";
            this.fileOffsetButton.ToolTipText = "File Offset";
            // 
            // relativeOffsetButton
            // 
            this.relativeOffsetButton.AutoSize = false;
            this.relativeOffsetButton.CheckOnClick = true;
            this.relativeOffsetButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.relativeOffsetButton.Image = ((System.Drawing.Image)(resources.GetObject("relativeOffsetButton.Image")));
            this.relativeOffsetButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.relativeOffsetButton.Name = "relativeOffsetButton";
            this.relativeOffsetButton.Size = new System.Drawing.Size(30, 21);
            this.relativeOffsetButton.Text = "RO";
            this.relativeOffsetButton.ToolTipText = "Relative Offset";
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // singleByteButton
            // 
            this.singleByteButton.AutoSize = false;
            this.singleByteButton.CheckOnClick = true;
            this.singleByteButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.singleByteButton.Image = ((System.Drawing.Image)(resources.GetObject("singleByteButton.Image")));
            this.singleByteButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.singleByteButton.Name = "singleByteButton";
            this.singleByteButton.Size = new System.Drawing.Size(30, 21);
            this.singleByteButton.Text = "B";
            this.singleByteButton.ToolTipText = "Data will be displayed as individual bytes in little-endian";
            // 
            // doubleByteButton
            // 
            this.doubleByteButton.AutoSize = false;
            this.doubleByteButton.CheckOnClick = true;
            this.doubleByteButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.doubleByteButton.Image = ((System.Drawing.Image)(resources.GetObject("doubleByteButton.Image")));
            this.doubleByteButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.doubleByteButton.Name = "doubleByteButton";
            this.doubleByteButton.Size = new System.Drawing.Size(30, 21);
            this.doubleByteButton.Text = "BB";
            this.doubleByteButton.ToolTipText = "Data will be displayed as sets of two bytes. Data in all headers and\r\nsections wi" +
    "ll be converted to their big-endian value. Please note\r\nthat ASCII characters sh" +
    "ould be read in their single byte form.";
            // 
            // fourByteButton
            // 
            this.fourByteButton.AutoSize = false;
            this.fourByteButton.CheckOnClick = true;
            this.fourByteButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.fourByteButton.Image = ((System.Drawing.Image)(resources.GetObject("fourByteButton.Image")));
            this.fourByteButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.fourByteButton.Name = "fourByteButton";
            this.fourByteButton.Size = new System.Drawing.Size(24, 22);
            this.fourByteButton.Text = "B4";
            // 
            // eightByteButton
            // 
            this.eightByteButton.AutoSize = false;
            this.eightByteButton.CheckOnClick = true;
            this.eightByteButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.eightByteButton.Image = ((System.Drawing.Image)(resources.GetObject("eightByteButton.Image")));
            this.eightByteButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.eightByteButton.Name = "eightByteButton";
            this.eightByteButton.Size = new System.Drawing.Size(24, 22);
            this.eightByteButton.Text = "B8";
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
            // 
            // replaceButton
            // 
            this.replaceButton.AutoSize = false;
            this.replaceButton.CheckOnClick = true;
            this.replaceButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.replaceButton.DoubleClickEnabled = true;
            this.replaceButton.Image = ((System.Drawing.Image)(resources.GetObject("replaceButton.Image")));
            this.replaceButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.replaceButton.Name = "replaceButton";
            this.replaceButton.Size = new System.Drawing.Size(52, 21);
            this.replaceButton.Text = "Replace";
            this.replaceButton.Click += new System.EventHandler(this.ReplaceButton_Click);
            // 
            // characterSet
            // 
            this.characterSet.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.characterSet.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aSCIIToolStripMenuItem});
            this.characterSet.Image = ((System.Drawing.Image)(resources.GetObject("characterSet.Image")));
            this.characterSet.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.characterSet.Name = "characterSet";
            this.characterSet.Size = new System.Drawing.Size(90, 22);
            this.characterSet.Text = "Character Set";
            this.characterSet.ToolTipText = "Character Set";
            // 
            // aSCIIToolStripMenuItem
            // 
            this.aSCIIToolStripMenuItem.Name = "aSCIIToolStripMenuItem";
            this.aSCIIToolStripMenuItem.Size = new System.Drawing.Size(102, 22);
            this.aSCIIToolStripMenuItem.Text = "ASCII";
            this.aSCIIToolStripMenuItem.Click += new System.EventHandler(this.ASCIIToolStripMenuItem_Click);
            // 
            // searchTextBox
            // 
            this.searchTextBox.BackColor = System.Drawing.Color.DarkGray;
            this.searchTextBox.Name = "searchTextBox";
            this.searchTextBox.Size = new System.Drawing.Size(150, 25);
            this.searchTextBox.ToolTipText = "Data you want to search for and replace";
            // 
            // replaceWithTextBox
            // 
            this.replaceWithTextBox.BackColor = System.Drawing.Color.DarkGray;
            this.replaceWithTextBox.Name = "replaceWithTextBox";
            this.replaceWithTextBox.Size = new System.Drawing.Size(150, 25);
            this.replaceWithTextBox.ToolTipText = "Data you want replace with";
            // 
            // settingsLabel
            // 
            this.settingsLabel.BackColor = System.Drawing.Color.White;
            this.settingsLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.settingsLabel.Location = new System.Drawing.Point(29, 0);
            this.settingsLabel.Margin = new System.Windows.Forms.Padding(3);
            this.settingsLabel.Name = "settingsLabel";
            this.settingsLabel.Size = new System.Drawing.Size(58, 19);
            this.settingsLabel.TabIndex = 4;
            this.settingsLabel.Text = "Settings";
            this.settingsLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.settingsLabel.Click += new System.EventHandler(this.SettingsLabel_Click);
            this.settingsLabel.MouseLeave += new System.EventHandler(this.SettingsLabel_MouseLeave);
            this.settingsLabel.MouseHover += new System.EventHandler(this.SettingsLabel_MouseHover);
            // 
            // blankTopLabel
            // 
            this.blankTopLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.blankTopLabel.BackColor = System.Drawing.Color.White;
            this.blankTopLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.blankTopLabel.Location = new System.Drawing.Point(87, 0);
            this.blankTopLabel.Margin = new System.Windows.Forms.Padding(3);
            this.blankTopLabel.Name = "blankTopLabel";
            this.blankTopLabel.Size = new System.Drawing.Size(900, 19);
            this.blankTopLabel.TabIndex = 5;
            this.blankTopLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // pluginsLabel
            // 
            this.pluginsLabel.BackColor = System.Drawing.Color.White;
            this.pluginsLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.pluginsLabel.Location = new System.Drawing.Point(87, 0);
            this.pluginsLabel.Margin = new System.Windows.Forms.Padding(3);
            this.pluginsLabel.Name = "pluginsLabel";
            this.pluginsLabel.Size = new System.Drawing.Size(58, 19);
            this.pluginsLabel.TabIndex = 6;
            this.pluginsLabel.Text = "Plugins";
            this.pluginsLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.pluginsLabel.Click += new System.EventHandler(this.PluginsLabel_Click);
            this.pluginsLabel.MouseLeave += new System.EventHandler(this.PluginsLabel_MouseLeave);
            this.pluginsLabel.MouseHover += new System.EventHandler(this.PluginsLabel_MouseHover);
            // 
            // downloadButton
            // 
            this.downloadButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.downloadButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.downloadButton.Image = ((System.Drawing.Image)(resources.GetObject("downloadButton.Image")));
            this.downloadButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.downloadButton.Name = "downloadButton";
            this.downloadButton.Size = new System.Drawing.Size(65, 22);
            this.downloadButton.Text = "Download";
            this.downloadButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.downloadButton.Click += new System.EventHandler(this.DownloadButton_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.LightGray;
            this.ClientSize = new System.Drawing.Size(984, 561);
            this.Controls.Add(this.pluginsLabel);
            this.Controls.Add(this.blankTopLabel);
            this.Controls.Add(this.settingsLabel);
            this.Controls.Add(this.toolStrip);
            this.Controls.Add(this.hexLabel);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.fileLabel);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).EndInit();
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label fileLabel;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TreeView treeView;
        private System.Windows.Forms.DataGridView dataGridView;
        private Label hexLabel;
        private ToolStrip toolStrip;
        private ToolStripButton hexButton;
        private ToolStripButton decimalButton;
        private ToolStripButton binaryButton;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripButton fileOffsetButton;
        private ToolStripButton relativeOffsetButton;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripButton singleByteButton;
        private ToolStripButton doubleByteButton;
        private Label settingsLabel;
        private ToolStripSeparator toolStripSeparator3;
        private DataGridViewTextBoxColumn Offset;
        private DataGridViewTextBoxColumn Data;
        private DataGridViewTextBoxColumn ASCII;
        private Label blankTopLabel;
        private ToolStripButton replaceButton;
        private ToolStripControlHost replaceAllCheckBox = new ToolStripControlHost(new CheckBox());
        private ToolStripTextBox searchTextBox;
        private ToolStripTextBox replaceWithTextBox;
        private ToolStripDropDownButton characterSet;
        private ToolStripMenuItem aSCIIToolStripMenuItem;
        private Label pluginsLabel;
        private ToolStripButton fourByteButton;
        private ToolStripButton eightByteButton;
        private ToolStripButton downloadButton;
    }
}

