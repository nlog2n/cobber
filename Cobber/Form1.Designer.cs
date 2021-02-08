namespace Cobber
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.toolStripMenuItem_File = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_Open = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_Save = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_Exit = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_Edit = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_Add = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_Remove = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_View = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_AssemblyInfo = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_DatabaseLog = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_StackTrace = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_StatusBar = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_Tools = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_Obfuscate = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_Options = new System.Windows.Forms.ToolStripMenuItem();
            this.obfuscationSettingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pluginsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_Help = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_About = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripButton_Add = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton_Remove = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton_AssemblyInfo = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton_Options = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton_ObfSetting = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton_Obfuscate = new System.Windows.Forms.ToolStripButton();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.listView1 = new System.Windows.Forms.ListView();
            this.colName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colType = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colObfuscation = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colApplyToMembers = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItem_Context_AssemblyInfo = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripMenuItem_Context_NoneSetting = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_Context_MinimumSetting = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_Context_NormalSetting = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_Context_AggressiveSetting = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_Context_MaximumSetting = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_Context_CustomizeSetting = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripMenuItem_Context_ApplyToMembers = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem_File,
            this.toolStripMenuItem_Edit,
            this.toolStripMenuItem_View,
            this.toolStripMenuItem_Tools,
            this.toolStripMenuItem_Help});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(695, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // toolStripMenuItem_File
            // 
            this.toolStripMenuItem_File.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem_Open,
            this.toolStripMenuItem_Save,
            this.toolStripMenuItem_Exit});
            this.toolStripMenuItem_File.Name = "toolStripMenuItem_File";
            this.toolStripMenuItem_File.Size = new System.Drawing.Size(37, 20);
            this.toolStripMenuItem_File.Text = "File";
            // 
            // toolStripMenuItem_Open
            // 
            this.toolStripMenuItem_Open.Name = "toolStripMenuItem_Open";
            this.toolStripMenuItem_Open.Size = new System.Drawing.Size(143, 22);
            this.toolStripMenuItem_Open.Text = "Open Project";
            this.toolStripMenuItem_Open.Click += new System.EventHandler(this.ToolStripMenuItem_Open_Click);
            // 
            // toolStripMenuItem_Save
            // 
            this.toolStripMenuItem_Save.Name = "toolStripMenuItem_Save";
            this.toolStripMenuItem_Save.Size = new System.Drawing.Size(143, 22);
            this.toolStripMenuItem_Save.Text = "Save Project";
            this.toolStripMenuItem_Save.Click += new System.EventHandler(this.ToolStripMenuItem_Save_Click);
            // 
            // toolStripMenuItem_Exit
            // 
            this.toolStripMenuItem_Exit.Name = "toolStripMenuItem_Exit";
            this.toolStripMenuItem_Exit.Size = new System.Drawing.Size(143, 22);
            this.toolStripMenuItem_Exit.Text = "Exit";
            this.toolStripMenuItem_Exit.Click += new System.EventHandler(this.ToolStripMenuItem_Exit_Click);
            // 
            // toolStripMenuItem_Edit
            // 
            this.toolStripMenuItem_Edit.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem_Add,
            this.toolStripMenuItem_Remove});
            this.toolStripMenuItem_Edit.Name = "toolStripMenuItem_Edit";
            this.toolStripMenuItem_Edit.Size = new System.Drawing.Size(39, 20);
            this.toolStripMenuItem_Edit.Text = "Edit";
            // 
            // toolStripMenuItem_Add
            // 
            this.toolStripMenuItem_Add.Image = global::Cobber.Properties.Resources.icon_add;
            this.toolStripMenuItem_Add.Name = "toolStripMenuItem_Add";
            this.toolStripMenuItem_Add.Size = new System.Drawing.Size(171, 22);
            this.toolStripMenuItem_Add.Text = "Add Assembly";
            this.toolStripMenuItem_Add.Click += new System.EventHandler(this.ToolStripMenuItem_Add_Click);
            // 
            // toolStripMenuItem_Remove
            // 
            this.toolStripMenuItem_Remove.Image = global::Cobber.Properties.Resources.icon_remove;
            this.toolStripMenuItem_Remove.Name = "toolStripMenuItem_Remove";
            this.toolStripMenuItem_Remove.Size = new System.Drawing.Size(171, 22);
            this.toolStripMenuItem_Remove.Text = "Remove Assembly";
            this.toolStripMenuItem_Remove.Click += new System.EventHandler(this.ToolStripMenuItem_Remove_Click);
            // 
            // toolStripMenuItem_View
            // 
            this.toolStripMenuItem_View.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem_AssemblyInfo,
            this.toolStripMenuItem_DatabaseLog,
            this.toolStripMenuItem_StackTrace,
            this.toolStripMenuItem_StatusBar});
            this.toolStripMenuItem_View.Name = "toolStripMenuItem_View";
            this.toolStripMenuItem_View.Size = new System.Drawing.Size(44, 20);
            this.toolStripMenuItem_View.Text = "View";
            // 
            // toolStripMenuItem_AssemblyInfo
            // 
            this.toolStripMenuItem_AssemblyInfo.Image = global::Cobber.Properties.Resources.icon_info;
            this.toolStripMenuItem_AssemblyInfo.Name = "toolStripMenuItem_AssemblyInfo";
            this.toolStripMenuItem_AssemblyInfo.Size = new System.Drawing.Size(152, 22);
            this.toolStripMenuItem_AssemblyInfo.Text = "Assembly Info";
            // 
            // toolStripMenuItem_DatabaseLog
            // 
            this.toolStripMenuItem_DatabaseLog.Name = "toolStripMenuItem_DatabaseLog";
            this.toolStripMenuItem_DatabaseLog.Size = new System.Drawing.Size(152, 22);
            this.toolStripMenuItem_DatabaseLog.Text = "Map Database";
            this.toolStripMenuItem_DatabaseLog.Click += new System.EventHandler(this.ToolStripMenuItem_DatabaseLog_Click);
            // 
            // toolStripMenuItem_StackTrace
            // 
            this.toolStripMenuItem_StackTrace.Name = "toolStripMenuItem_StackTrace";
            this.toolStripMenuItem_StackTrace.Size = new System.Drawing.Size(152, 22);
            this.toolStripMenuItem_StackTrace.Text = "Stack Trace";
            this.toolStripMenuItem_StackTrace.Click += new System.EventHandler(this.ToolStripMenuItem_StackTrace_Click);
            // 
            // toolStripMenuItem_StatusBar
            // 
            this.toolStripMenuItem_StatusBar.Checked = true;
            this.toolStripMenuItem_StatusBar.CheckOnClick = true;
            this.toolStripMenuItem_StatusBar.CheckState = System.Windows.Forms.CheckState.Checked;
            this.toolStripMenuItem_StatusBar.Name = "toolStripMenuItem_StatusBar";
            this.toolStripMenuItem_StatusBar.Size = new System.Drawing.Size(152, 22);
            this.toolStripMenuItem_StatusBar.Text = "Status Bar";
            this.toolStripMenuItem_StatusBar.Click += new System.EventHandler(this.ToolStripMenuItem_StatusBar_Click);
            // 
            // toolStripMenuItem_Tools
            // 
            this.toolStripMenuItem_Tools.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem_Obfuscate,
            this.toolStripMenuItem_Options,
            this.obfuscationSettingToolStripMenuItem,
            this.pluginsToolStripMenuItem});
            this.toolStripMenuItem_Tools.Name = "toolStripMenuItem_Tools";
            this.toolStripMenuItem_Tools.Size = new System.Drawing.Size(48, 20);
            this.toolStripMenuItem_Tools.Text = "Tools";
            // 
            // toolStripMenuItem_Obfuscate
            // 
            this.toolStripMenuItem_Obfuscate.Image = global::Cobber.Properties.Resources.icon_build;
            this.toolStripMenuItem_Obfuscate.Name = "toolStripMenuItem_Obfuscate";
            this.toolStripMenuItem_Obfuscate.Size = new System.Drawing.Size(179, 22);
            this.toolStripMenuItem_Obfuscate.Text = "Obfuscate!";
            this.toolStripMenuItem_Obfuscate.Click += new System.EventHandler(this.ToolStripMenuItem_Obfuscate_Click);
            // 
            // toolStripMenuItem_Options
            // 
            this.toolStripMenuItem_Options.Image = global::Cobber.Properties.Resources.icon_setting;
            this.toolStripMenuItem_Options.Name = "toolStripMenuItem_Options";
            this.toolStripMenuItem_Options.Size = new System.Drawing.Size(179, 22);
            this.toolStripMenuItem_Options.Text = "General Options";
            this.toolStripMenuItem_Options.Click += new System.EventHandler(this.ToolStripMenuItem_Options_Click);
            // 
            // obfuscationSettingToolStripMenuItem
            // 
            this.obfuscationSettingToolStripMenuItem.Image = global::Cobber.Properties.Resources.icon_obf;
            this.obfuscationSettingToolStripMenuItem.Name = "obfuscationSettingToolStripMenuItem";
            this.obfuscationSettingToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
            this.obfuscationSettingToolStripMenuItem.Text = "Obfuscation Setting";
            this.obfuscationSettingToolStripMenuItem.Click += new System.EventHandler(this.ToolStripMenuItem_ObfSetting_Click);
            // 
            // pluginsToolStripMenuItem
            // 
            this.pluginsToolStripMenuItem.Name = "pluginsToolStripMenuItem";
            this.pluginsToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
            this.pluginsToolStripMenuItem.Text = "Plugins";
            this.pluginsToolStripMenuItem.Click += new System.EventHandler(this.pluginsToolStripMenuItem_Click);
            // 
            // toolStripMenuItem_Help
            // 
            this.toolStripMenuItem_Help.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem_About});
            this.toolStripMenuItem_Help.Name = "toolStripMenuItem_Help";
            this.toolStripMenuItem_Help.Size = new System.Drawing.Size(44, 20);
            this.toolStripMenuItem_Help.Text = "Help";
            // 
            // toolStripMenuItem_About
            // 
            this.toolStripMenuItem_About.Name = "toolStripMenuItem_About";
            this.toolStripMenuItem_About.Size = new System.Drawing.Size(149, 22);
            this.toolStripMenuItem_About.Text = "About Cobber";
            this.toolStripMenuItem_About.Click += new System.EventHandler(this.ToolStripMenuItem_About_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 468);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(695, 22);
            this.statusStrip1.TabIndex = 1;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(60, 17);
            this.toolStripStatusLabel1.Text = "Welcome!";
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButton_Add,
            this.toolStripButton_Remove,
            this.toolStripButton_AssemblyInfo,
            this.toolStripButton_Options,
            this.toolStripButton_ObfSetting,
            this.toolStripButton_Obfuscate});
            this.toolStrip1.Location = new System.Drawing.Point(0, 24);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(695, 25);
            this.toolStrip1.TabIndex = 2;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripButton_Add
            // 
            this.toolStripButton_Add.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton_Add.Image = global::Cobber.Properties.Resources.icon_add;
            this.toolStripButton_Add.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton_Add.Name = "toolStripButton_Add";
            this.toolStripButton_Add.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton_Add.Text = "Add Assembly";
            this.toolStripButton_Add.Click += new System.EventHandler(this.ToolStripButton_Add_Click);
            // 
            // toolStripButton_Remove
            // 
            this.toolStripButton_Remove.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton_Remove.Image = global::Cobber.Properties.Resources.icon_remove;
            this.toolStripButton_Remove.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton_Remove.Name = "toolStripButton_Remove";
            this.toolStripButton_Remove.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton_Remove.Text = "Remove Assembly";
            this.toolStripButton_Remove.Click += new System.EventHandler(this.ToolStripButton_Remove_Click);
            // 
            // toolStripButton_AssemblyInfo
            // 
            this.toolStripButton_AssemblyInfo.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton_AssemblyInfo.Image = global::Cobber.Properties.Resources.icon_info;
            this.toolStripButton_AssemblyInfo.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton_AssemblyInfo.Name = "toolStripButton_AssemblyInfo";
            this.toolStripButton_AssemblyInfo.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton_AssemblyInfo.Text = "AssemblyInfo";
            this.toolStripButton_AssemblyInfo.Click += new System.EventHandler(this.ToolStripButton_AssemblyInfo_Click);
            // 
            // toolStripButton_Options
            // 
            this.toolStripButton_Options.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton_Options.Image = global::Cobber.Properties.Resources.icon_setting;
            this.toolStripButton_Options.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton_Options.Name = "toolStripButton_Options";
            this.toolStripButton_Options.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton_Options.Text = "Options";
            this.toolStripButton_Options.Click += new System.EventHandler(this.ToolStripButton_Options_Click);
            // 
            // toolStripButton_ObfSetting
            // 
            this.toolStripButton_ObfSetting.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton_ObfSetting.Image = global::Cobber.Properties.Resources.icon_obf;
            this.toolStripButton_ObfSetting.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton_ObfSetting.Name = "toolStripButton_ObfSetting";
            this.toolStripButton_ObfSetting.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton_ObfSetting.Text = "Obfuscations";
            this.toolStripButton_ObfSetting.Click += new System.EventHandler(this.ToolStripButton_ObfSetting_Click);
            // 
            // toolStripButton_Obfuscate
            // 
            this.toolStripButton_Obfuscate.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton_Obfuscate.Image = global::Cobber.Properties.Resources.icon_build;
            this.toolStripButton_Obfuscate.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton_Obfuscate.Name = "toolStripButton_Obfuscate";
            this.toolStripButton_Obfuscate.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton_Obfuscate.Text = "Obfuscate";
            this.toolStripButton_Obfuscate.ToolTipText = "Obfuscate";
            this.toolStripButton_Obfuscate.Click += new System.EventHandler(this.ToolStripButton_Obfuscate_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 49);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.treeView1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.listView1);
            this.splitContainer1.Size = new System.Drawing.Size(695, 419);
            this.splitContainer1.SplitterDistance = 231;
            this.splitContainer1.TabIndex = 3;
            // 
            // treeView1
            // 
            this.treeView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeView1.HideSelection = false;
            this.treeView1.ImageIndex = 0;
            this.treeView1.ImageList = this.imageList1;
            this.treeView1.Location = new System.Drawing.Point(0, 0);
            this.treeView1.Name = "treeView1";
            this.treeView1.SelectedImageIndex = 1;
            this.treeView1.Size = new System.Drawing.Size(231, 419);
            this.treeView1.TabIndex = 0;
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "assembly.png");
            this.imageList1.Images.SetKeyName(1, "module.png");
            this.imageList1.Images.SetKeyName(2, "namespace.png");
            this.imageList1.Images.SetKeyName(3, "type.png");
            this.imageList1.Images.SetKeyName(4, "event.png");
            this.imageList1.Images.SetKeyName(5, "field.png");
            this.imageList1.Images.SetKeyName(6, "method.png");
            this.imageList1.Images.SetKeyName(7, "property.png");
            this.imageList1.Images.SetKeyName(8, "unknown.png");
            this.imageList1.Images.SetKeyName(9, "assembly_main.png");
            this.imageList1.Images.SetKeyName(10, "interface.png");
            this.imageList1.Images.SetKeyName(11, "enum.png");
            this.imageList1.Images.SetKeyName(12, "valuetype.png");
            this.imageList1.Images.SetKeyName(13, "delegate.png");
            this.imageList1.Images.SetKeyName(14, "constant.png");
            this.imageList1.Images.SetKeyName(15, "constructor.png");
            this.imageList1.Images.SetKeyName(16, "omethod.png");
            this.imageList1.Images.SetKeyName(17, "propget.png");
            this.imageList1.Images.SetKeyName(18, "propset.png");
            this.imageList1.Images.SetKeyName(19, "famasm.png");
            this.imageList1.Images.SetKeyName(20, "internal.png");
            this.imageList1.Images.SetKeyName(21, "private.png");
            this.imageList1.Images.SetKeyName(22, "protected.png");
            this.imageList1.Images.SetKeyName(23, "static.png");
            // 
            // listView1
            // 
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colName,
            this.colType,
            this.colObfuscation,
            this.colApplyToMembers});
            this.listView1.ContextMenuStrip = this.contextMenuStrip1;
            this.listView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listView1.Location = new System.Drawing.Point(0, 0);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(460, 419);
            this.listView1.SmallImageList = this.imageList1;
            this.listView1.TabIndex = 0;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            // 
            // colName
            // 
            this.colName.Text = "Name";
            // 
            // colType
            // 
            this.colType.Text = "Type";
            // 
            // colObfuscation
            // 
            this.colObfuscation.Text = "Obfuscation";
            this.colObfuscation.Width = 79;
            // 
            // colApplyToMembers
            // 
            this.colApplyToMembers.Text = "ApplyToMembers";
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem_Context_AssemblyInfo,
            this.toolStripSeparator2,
            this.toolStripMenuItem_Context_NoneSetting,
            this.toolStripMenuItem_Context_MinimumSetting,
            this.toolStripMenuItem_Context_NormalSetting,
            this.toolStripMenuItem_Context_AggressiveSetting,
            this.toolStripMenuItem_Context_MaximumSetting,
            this.toolStripMenuItem_Context_CustomizeSetting,
            this.toolStripSeparator3,
            this.toolStripMenuItem_Context_ApplyToMembers});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(173, 192);
            // 
            // toolStripMenuItem_Context_AssemblyInfo
            // 
            this.toolStripMenuItem_Context_AssemblyInfo.Image = global::Cobber.Properties.Resources.icon_info;
            this.toolStripMenuItem_Context_AssemblyInfo.Name = "toolStripMenuItem_Context_AssemblyInfo";
            this.toolStripMenuItem_Context_AssemblyInfo.Size = new System.Drawing.Size(172, 22);
            this.toolStripMenuItem_Context_AssemblyInfo.Text = "Assembly Info";
            this.toolStripMenuItem_Context_AssemblyInfo.Click += new System.EventHandler(this.ToolStripMenuItem_Context_AssemblyInfo_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(169, 6);
            // 
            // toolStripMenuItem_Context_NoneSetting
            // 
            this.toolStripMenuItem_Context_NoneSetting.Name = "toolStripMenuItem_Context_NoneSetting";
            this.toolStripMenuItem_Context_NoneSetting.Size = new System.Drawing.Size(172, 22);
            this.toolStripMenuItem_Context_NoneSetting.Text = "None";
            this.toolStripMenuItem_Context_NoneSetting.Click += new System.EventHandler(this.ToolStripMenuItem_Context_NoneSetting_Click);
            // 
            // toolStripMenuItem_Context_MinimumSetting
            // 
            this.toolStripMenuItem_Context_MinimumSetting.Name = "toolStripMenuItem_Context_MinimumSetting";
            this.toolStripMenuItem_Context_MinimumSetting.Size = new System.Drawing.Size(172, 22);
            this.toolStripMenuItem_Context_MinimumSetting.Text = "Minimum";
            this.toolStripMenuItem_Context_MinimumSetting.Click += new System.EventHandler(this.ToolStripMenuItem_Context_MinimumSetting_Click);
            // 
            // toolStripMenuItem_Context_NormalSetting
            // 
            this.toolStripMenuItem_Context_NormalSetting.Name = "toolStripMenuItem_Context_NormalSetting";
            this.toolStripMenuItem_Context_NormalSetting.Size = new System.Drawing.Size(172, 22);
            this.toolStripMenuItem_Context_NormalSetting.Text = "Normal";
            this.toolStripMenuItem_Context_NormalSetting.Click += new System.EventHandler(this.ToolStripMenuItem_Context_NormalSetting_Click);
            // 
            // toolStripMenuItem_Context_AggressiveSetting
            // 
            this.toolStripMenuItem_Context_AggressiveSetting.Name = "toolStripMenuItem_Context_AggressiveSetting";
            this.toolStripMenuItem_Context_AggressiveSetting.Size = new System.Drawing.Size(172, 22);
            this.toolStripMenuItem_Context_AggressiveSetting.Text = "Aggressive";
            this.toolStripMenuItem_Context_AggressiveSetting.Click += new System.EventHandler(this.ToolStripMenuItem_Context_AggressiveSetting_Click);
            // 
            // toolStripMenuItem_Context_MaximumSetting
            // 
            this.toolStripMenuItem_Context_MaximumSetting.Name = "toolStripMenuItem_Context_MaximumSetting";
            this.toolStripMenuItem_Context_MaximumSetting.Size = new System.Drawing.Size(172, 22);
            this.toolStripMenuItem_Context_MaximumSetting.Text = "Maximum";
            this.toolStripMenuItem_Context_MaximumSetting.Click += new System.EventHandler(this.ToolStripMenuItem_Context_MaximumSetting_Click);
            // 
            // toolStripMenuItem_Context_CustomizeSetting
            // 
            this.toolStripMenuItem_Context_CustomizeSetting.Image = global::Cobber.Properties.Resources.icon_obf;
            this.toolStripMenuItem_Context_CustomizeSetting.Name = "toolStripMenuItem_Context_CustomizeSetting";
            this.toolStripMenuItem_Context_CustomizeSetting.Size = new System.Drawing.Size(172, 22);
            this.toolStripMenuItem_Context_CustomizeSetting.Text = "Customize";
            this.toolStripMenuItem_Context_CustomizeSetting.Click += new System.EventHandler(this.ToolStripMenuItem_Context_CustomizeSetting_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(169, 6);
            // 
            // toolStripMenuItem_Context_ApplyToMembers
            // 
            this.toolStripMenuItem_Context_ApplyToMembers.CheckOnClick = true;
            this.toolStripMenuItem_Context_ApplyToMembers.Name = "toolStripMenuItem_Context_ApplyToMembers";
            this.toolStripMenuItem_Context_ApplyToMembers.Size = new System.Drawing.Size(172, 22);
            this.toolStripMenuItem_Context_ApplyToMembers.Text = "Apply to Members";
            this.toolStripMenuItem_Context_ApplyToMembers.Click += new System.EventHandler(this.ToolStripMenuItem_Context_ApplyToMembers_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(695, 490);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Cobber";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_File;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Edit;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_View;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Tools;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Help;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton toolStripButton_Add;
        private System.Windows.Forms.ToolStripButton toolStripButton_Remove;
        private System.Windows.Forms.ToolStripButton toolStripButton_AssemblyInfo;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.ColumnHeader colType;
        private System.Windows.Forms.ColumnHeader colObfuscation;
        private System.Windows.Forms.ColumnHeader colApplyToMembers;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Open;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Save;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Options;
        private System.Windows.Forms.ToolStripButton toolStripButton_ObfSetting;
        private System.Windows.Forms.ToolStripButton toolStripButton_Options;
        private System.Windows.Forms.ToolStripButton toolStripButton_Obfuscate;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Add;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Remove;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Obfuscate;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_About;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_StatusBar;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_StackTrace;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_DatabaseLog;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Exit;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Context_AssemblyInfo;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Context_MinimumSetting;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Context_NormalSetting;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Context_AggressiveSetting;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Context_MaximumSetting;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Context_ApplyToMembers;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Context_CustomizeSetting;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Context_NoneSetting;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_AssemblyInfo;
        private System.Windows.Forms.ToolStripMenuItem obfuscationSettingToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pluginsToolStripMenuItem;
    }
}

