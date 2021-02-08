using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Forms;

using Cobber;
using Cobber.Core;
using Cobber.Core.Project;


namespace Cobber
{
    public partial class Form1 : Form
    {
        #region Initialization

        Core.Cobber cobber = new Cobber.Core.Cobber(); // also for generating static obfuscators!
        CobberProject Project;
        bool IsModified = false;

        public Form1()
        {
            InitializeComponent();

            this.FormClosing += On_FormClosing;

            // tree and list view
            this.treeView1.NodeMouseClick += new TreeNodeMouseClickEventHandler(this.TreeView1_NodeMouseClick);
            this.listView1.MouseDoubleClick += new MouseEventHandler(this.ListView1_MouseDoubleClick);
            this.listView1.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.listView1_ColumnClick);
            this.listView1.MouseUp += new MouseEventHandler(this.listView1_MouseUp); // for adaptive context menu show only

            // Enable drag-and-drop operations and add handlers for DragEnter and DragDrop.
            this.AllowDrop = true;
            this.DragDrop += new DragEventHandler(this.Form1_DragDrop);
            this.DragEnter += new DragEventHandler(this.Form1_DragEnter);


            this.Project = new CobberProject();

            PopulateTreeView();
            PopulateListView(treeView1.SelectedNode);
        }
        #endregion

        #region Icon and display text
        private string CheckImageIcon(string key, CobberIconVisible visible, CobberIconOverlay overlay)
        {
            if (!this.imageList1.Images.ContainsKey(key)) return null;

            string newKey = key;
            if (overlay != CobberIconOverlay.None)
            {
                newKey += "/" + overlay.ToString().ToLower();
            }
            if (visible != CobberIconVisible.Public)
            {
                newKey += "/" + visible.ToString().ToLower();
            }

            // add a new overlayed image to the list
            if (!this.imageList1.Images.ContainsKey(newKey))
            {
                // get current icon image
                Bitmap bmp = new Bitmap(this.imageList1.Images[key]);
                Graphics g = Graphics.FromImage(bmp);

                // overlay 
                //g.DrawImageUnscaled(Properties.Resources.StaticIcon, 0, 0);
                if (overlay != CobberIconOverlay.None)
                {
                    Image static_icon = this.imageList1.Images[overlay.ToString().ToLower() + ".png"];
                    g.DrawImageUnscaled(static_icon, 0, 0);
                }

                if (visible != CobberIconVisible.Public)
                {
                    Image static_icon = this.imageList1.Images[visible.ToString().ToLower() + ".png"];
                    g.DrawImageUnscaled(static_icon, 0, 0);
                }

                this.imageList1.Images.Add(newKey, bmp);
            }

            return newKey;
        }

        private string GetCobberObjectImageKey(CobberObject info)
        {
            CobberIcon icon = info.XgetIcon();
            CobberIconVisible visible = info.XgetIconVisible();
            CobberIconOverlay overlay = info.XgetIconStatic();

            string key;
            switch (icon)
            {
                case CobberIcon.Project: key = "unknown.png"; break;
                case CobberIcon.Assembly: key = "assembly.png"; break;
                case CobberIcon.Main: key = "assembly_main.png"; break;

                case CobberIcon.Module:
                case CobberIcon.Namespace:

                case CobberIcon.Type:
                case CobberIcon.Interface:
                case CobberIcon.Enum:
                case CobberIcon.Valuetype:
                case CobberIcon.Delegate:
                case CobberIcon.Field:
                case CobberIcon.Constant:
                case CobberIcon.Method:
                case CobberIcon.Constructor:
                case CobberIcon.Omethod:
                case CobberIcon.Property:
                case CobberIcon.Propget:
                case CobberIcon.Propset:
                case CobberIcon.Event:
                    key = icon.ToString().ToLower() + ".png"; break;

                case CobberIcon.None:
                case CobberIcon.Resource:
                default: key = "unknown.png"; break;
            }

            return CheckImageIcon(key, visible, overlay); 
        }

        private string GetCobberObjectTypeInfo(CobberObject info)
        {
            CobberIcon ret = info.XgetIcon();
            if (ret == CobberIcon.None) return "";
            if (ret == CobberIcon.Type) return "Class";
            return ret.ToString();
        }
        #endregion

        #region populate tree and list views
        private void AttachCobberObject(CobberObject info, TreeNode nodeToAddTo)
        {
            nodeToAddTo.Text = info.Name;
            nodeToAddTo.Tag = info;
            nodeToAddTo.ImageKey = GetCobberObjectImageKey(info); //.ImageIndex; 
            nodeToAddTo.SelectedImageKey = nodeToAddTo.ImageKey; 

            info.Resolve();
            foreach (CobberObject child in info.GetChildren())
            {
                TreeNode aNode;
                aNode = new TreeNode(child.Name);
                aNode.Tag = child;
                aNode.ImageKey = GetCobberObjectImageKey(child);

                AttachCobberObject(child, aNode);

                nodeToAddTo.Nodes.Add(aNode);
            }
        }


        /// <summary>
        /// populate tree view on the left window
        /// </summary>
        private void PopulateTreeView()
        {
            treeView1.Nodes.Clear();

            // create a root node for cobber project
            TreeNode rootNode = new TreeNode("Drag your assemblies here."); 
            AttachCobberObject(this.Project, rootNode);
            rootNode.Text = Path.GetFileName(this.Project.Name) + " [" + this.Project.ObfSettingName + "]"; 
            
            treeView1.Nodes.Add(rootNode);

            treeView1.SelectedNode = rootNode; // treeView1.Nodes[0];

            /*
            {
                foreach (var asm in this.Project.Assemblies)
                {
                    // create a root node for cobber object
                    TreeNode rootNode = new TreeNode();
                    AttachCobberObject(asm, rootNode);
                    treeView1.Nodes.Add(rootNode);
                }

                treeView1.SelectedNode = treeView1.Nodes[0];
            }
            */
        }

        /// <summary>
        /// populate list view on the right window
        /// </summary>
        /// <param name="selectedNode"></param>
        private void PopulateListView(TreeNode selectedNode)
        {
            try
            {
                if (selectedNode == null) return;
                CobberObject nodeInfo = (CobberObject)selectedNode.Tag;

                listView1.Items.Clear();
                listView1.BeginUpdate(); // this can significantly faster the display!
                foreach (CobberObject co in nodeInfo.GetChildren())
                {
                    ListViewItem item = new ListViewItem(co.Name);
                    item.Tag = co;
                    item.ImageKey = GetCobberObjectImageKey(co);
                    item.UseItemStyleForSubItems = false; // color setting works only if ListViewItem.UseItemStyleForSubItems = false


                    string colType = GetCobberObjectTypeInfo(co);
                    string colObf = ""; Color color;
                    string colApplyToMembers = (co.ApplyToMembers ? "Y" : "");
                    if (co.Inherited)
                    {
                        colObf = "inherited"; color = Color.Green;
                    }
                    else
                    {
                        colObf = (co.ObfSettingName == "none" ? "" : co.ObfSettingName);
                        color = Color.Red;
                    }

                    ListViewItem.ListViewSubItem[] subItems = new ListViewItem.ListViewSubItem[]
                        {
                            new ListViewItem.ListViewSubItem(item, colType),
                            new ListViewItem.ListViewSubItem(item, colObf){ForeColor = color, Font = new Font(DefaultFont, FontStyle.Bold)},
                            new ListViewItem.ListViewSubItem(item, colApplyToMembers) 
                            //new ListViewItem.ListViewSubItem(item, colInherited)  
                        };
                    item.SubItems.AddRange(subItems);
                    listView1.Items.Add(item);
                }
                listView1.EndUpdate();

                listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            }
            catch (Exception)
            {
            }
        }

        // click the node on left-side tree
        void TreeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            // By using TreeView.HideSelection = false, there will be shadow for selected folder.
            // If want to change folder icon too, need to set SelectedImageIndex for this node.
            //e.Node.SelectedImageIndex = 1;

            PopulateListView(e.Node);
        }
        #endregion

        #region Open assemblies or project file, Save, Add Plugin and Exit
        // Menu-File-Open for choosing assemblies or cobber project file or xap file
        private void ToolStripMenuItem_Open_Click(object sender, EventArgs e)
        {
            if (this.IsModified)
            {
                switch (MessageBox.Show(
                    "You have unsaved changes in current project!\r\nDo you want to save them?",
                    "Cobber", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question))
                {
                    case DialogResult.Yes:
                        ToolStripMenuItem_Save_Click(sender, e);
                        break;
                    case DialogResult.No:
                        break;
                    case DialogResult.Cancel:
                        return;
                }
            }


            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Cobber Project (*.cobber)|*.cobber|Windows Phone App (*.xap)|*.xap|All Files (*.*)|*.*";
            if (ofd.ShowDialog() != DialogResult.Cancel)
            {
                try
                {
                    if (Path.GetExtension(ofd.FileName) == ".cobber")
                    {
                        this.Project = new CobberProject();
                        this.Project.Name = ofd.FileName;
                        this.Project.Load(ofd.FileName);
                    }
                    else if (Path.GetExtension(ofd.FileName) == ".xap")
                    {
                        // TODO: 
                    }
                    else  // single assembly file, added to current project
                    {
                        bool modified = this.Project.AddAssembly(ofd.FileName);
                        this.IsModified = this.IsModified || modified;
                    }

                    this.Project.Mark(this.Project.ObfSettingName); // for refreshing the tree

                    PopulateTreeView();
                    PopulateListView(treeView1.SelectedNode);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format(
@"Invalid project file!
Message : {0}
Stack Trace : {1}", ex.Message, ex.StackTrace), "Cobber", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Menu-File-Save for save project file
        private void ToolStripMenuItem_Save_Click(object sender, EventArgs e)
        {
            if (!this.IsModified)
            {
                toolStripStatusLabel1.Text = "no project changes.";
                return;
            }

            bool projNameChanged = false;
            if (this.Project.Name == "Untitled.cobber")
            {
                SaveFileDialog sfd = new SaveFileDialog();
                {
                    sfd.Filter = "Cobber Project (*.cobber)|*.cobber|All Files (*.*)|*.*";
                    sfd.FileName = this.Project.Name;
                }
                if (sfd.ShowDialog() != DialogResult.Cancel)
                {
                    this.Project.Name = sfd.FileName; // change project name
                    projNameChanged = true;
                    if (string.IsNullOrEmpty(this.Project.Name))
                        return;
                }
            }

            this.Project.Save(this.Project.Name);
            this.IsModified = false;
            toolStripStatusLabel1.Text = "project saved.";

            if (projNameChanged) // refresh root node name
            {
                PopulateTreeView();
                PopulateListView(treeView1.SelectedNode);
            }
        }

        // Menu-File-Exit
        private void ToolStripMenuItem_Exit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        // form closing event handler
        private void On_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.IsModified)
            {
                switch (MessageBox.Show(
                    "You have unsaved changes in this project!\r\nDo you want to save them?",
                    "Cobber", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question))
                {
                    case DialogResult.Yes:
                        ToolStripMenuItem_Save_Click(sender, e);
                        break;
                    case DialogResult.No:
                        break;
                    case DialogResult.Cancel:
                        e.Cancel = true;  // cancel this event
                        return;
                    //break;
                }
            }
        }

        // plugin click
        private void pluginsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Plugins (*.dll)|*.dll|All Files (*.*)|*.*";
            if (ofd.ShowDialog() != DialogResult.Cancel)
            {
                List<string> result = Cobber.Core.Cobber.LoadAssembly(System.Reflection.Assembly.LoadFile(ofd.FileName));
                if (result.Count > 0)
                {
                    string sss = "";
                    foreach (string s in result) { sss += "\r\n" + s; }
                    MessageBox.Show( "Added:\r\n" + sss, "Cobber", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show( "No plugins found!", "Cobber", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        #endregion

        #region File Drag and Drop
        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            // Handle FileDrop data.
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // get file names into a string array
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                try
                {
                    foreach (string filename in files)
                    {
                        bool modified = this.Project.AddAssembly(filename);
                        this.IsModified = this.IsModified || modified;
                    }

                    if (IsModified)
                    {
                        this.Project.Mark(this.Project.ObfSettingName); // for refreshing the tree

                        PopulateTreeView();
                        PopulateListView(treeView1.SelectedNode);
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Invalid assembly file!", "Cobber", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            // If the data is a file or a bitmap, display the copy cursor.
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
        #endregion

        #region Add Assembly
        // click ToolStrip button "add assembly"
        private void ToolStripButton_Add_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "All Files (*.*)|*.*|Cobber Project (*.cobber)|*.cobber|Windows Phone App (*.xap)|*.xap";
                ofd.Title = "Add Assemblies";
                ofd.Multiselect = true;
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    foreach (string filename in ofd.FileNames)
                    {
                        bool modified = this.Project.AddAssembly(filename);
                        this.IsModified = this.IsModified || modified;
                    }

                    if (IsModified)
                    {
                        this.Project.Mark(this.Project.ObfSettingName); // for refreshing the tree

                        PopulateTreeView();
                        PopulateListView(treeView1.SelectedNode);
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Invalid assembly file!", "Cobber", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Menu-Edit-AddAssembly
        private void ToolStripMenuItem_Add_Click(object sender, EventArgs e)
        {
            ToolStripButton_Add_Click(sender, e);
        }

        #endregion

        #region Remove Assembly

        // click ToolStrip button "remove assembly"
        private void ToolStripButton_Remove_Click(object sender, EventArgs e)
        {
            try
            {
                // get selected assembly from treeView
                if (treeView1.SelectedNode != null)
                {
                    CobberObject node = (CobberObject)treeView1.SelectedNode.Tag;
                    if (node is CobberAssembly)
                    {
                        TreeNode parentNode = treeView1.SelectedNode.Parent;
                        treeView1.SelectedNode = parentNode;

                        this.Project.Assemblies.Remove(node as CobberAssembly);
                        this.IsModified = true;
                    }
                }

                // get selected assemblies from listView
                for (int i = 0; i < listView1.SelectedItems.Count; i++)
                {
                    CobberObject node = (CobberObject)listView1.SelectedItems[i].Tag;
                    if (node is CobberAssembly)
                    {
                        this.Project.Assemblies.Remove(node as CobberAssembly);
                        this.IsModified = true;
                    }
                }

                if (IsModified)
                {
                    PopulateTreeView();
                    PopulateListView(treeView1.SelectedNode);
                }
            }
            catch (Exception)
            { }
        }

        // Menu-Edit-RemoveAssembly
        private void ToolStripMenuItem_Remove_Click(object sender, EventArgs e)
        {
            ToolStripButton_Remove_Click(sender, e);
        }
        #endregion

        #region Assembly Info

        // click ToolStrip button "assembly information"
        private void ToolStripButton_AssemblyInfo_Click(object sender, EventArgs e)
        {
            try
            {
                // get selected from listview
                if (listView1.SelectedItems.Count > 1)
                {
                    toolStripStatusLabel1.Text = "please choose one object from the list view.";
                    return; // must select only one
                }

                CobberObject nodeInfo = null;
                if (listView1.SelectedItems.Count == 1)
                {
                    nodeInfo = (CobberObject)listView1.SelectedItems[0].Tag;
                }
                else
                {
                    nodeInfo = (CobberObject)treeView1.SelectedNode.Tag;
                }

                string typeInfo = GetCobberObjectTypeInfo(nodeInfo);
                CobberIconVisible visible = nodeInfo.XgetIconVisible();
                CobberIconOverlay overlay = nodeInfo.XgetIconStatic();

                MessageBox.Show(nodeInfo.Name + ":\n\t" + typeInfo + "\n\t" + visible.ToString() + "\n\t" + (overlay == CobberIconOverlay.Static ? "static": "") , "Information");
            }
            catch (Exception)
            { }
        }

        private void ToolStripMenuItem_AssemblyInfo_Click(object sender, EventArgs e)
        {
            ToolStripButton_AssemblyInfo_Click(sender, e);
        }

        // ListView - Context Menu - AssemblyInfo
        private void ToolStripMenuItem_Context_AssemblyInfo_Click(object sender, EventArgs e)
        {
            ToolStripButton_AssemblyInfo_Click(sender, e);
        }

        #endregion

        #region Global Options
        // click ToolStrip button "Options"
        private void ToolStripButton_Options_Click(object sender, EventArgs e)
        {
            try
            {
                // init the window from saved project
                FormOption optionDlg = new FormOption();
                optionDlg.PopulateFromProject(this.Project);

                // upon setting OK, make a change
                //if (setting.ShowDialog(this) == DialogResult.OK)
                optionDlg.ShowDialog();
                if (!optionDlg.Done)  return;
                
                // update the configuration
                bool optionChanged = optionDlg.UpdateProject(this.Project);

                IsModified = IsModified || optionChanged;
                if (IsModified)
                {
                    PopulateTreeView();
                    PopulateListView(treeView1.SelectedNode);

                    toolStripStatusLabel1.Text = "options changed.";
                }
            }
            catch (Exception)
            { }
        }

        // Menu-Options for global options
        private void ToolStripMenuItem_Options_Click(object sender, EventArgs e)
        {
            ToolStripButton_Options_Click(sender, e);
        }
        #endregion

        #region Double-click the file to open
        // Double-click list item to enter or display information
        private void ListView1_MouseDoubleClick(object sender, EventArgs e)
        {
            try
            {
                if (listView1.SelectedItems.Count != 1)
                    return;

                CobberObject nodeParent = (CobberObject)treeView1.SelectedNode.Tag;
                CobberObject node = (CobberObject)listView1.SelectedItems[0].Tag;

                // determine whether the node has children
                if ( node.GetChildren().Count > 0 )
                {
                    foreach (TreeNode tn in treeView1.SelectedNode.Nodes)
                    {
                        if ( node.Name == tn.Text)
                        {
                            treeView1.SelectedNode = tn;
                            //treeView1.SelectedNode.SelectedImageIndex = 1; 
                            PopulateListView(treeView1.SelectedNode);
                            return;
                        }
                    }
                }
                else // assume it is pure member
                {
                    //
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        #endregion

        #region View statusbar
        // Menu-View-StatusBar
        private void ToolStripMenuItem_StatusBar_Click(object sender, EventArgs e)
        {
            statusStrip1.Visible = !statusStrip1.Visible;
        }
        #endregion

        #region View database log and stack trace
        // Menu-View-StackTrace
        private void ToolStripMenuItem_StackTrace_Click(object sender, EventArgs e)
        {
            FormStacktrace formStackTrace = new FormStacktrace();
            formStackTrace.db = this.cobber.Database;
            
            // upon setting OK, make a change
            //if (formStackTrace.ShowDialog(this) == DialogResult.OK)
            formStackTrace.ShowDialog();
            //if (!formSetting.Done) return;
        }

        // Menu-View-DatabaseLog
        private void ToolStripMenuItem_DatabaseLog_Click(object sender, EventArgs e)
        {
            FormDatabase formDb = new FormDatabase();
            formDb.db = this.cobber.Database;
            formDb.UpdateTree();

            formDb.ShowDialog();
        }

        #endregion

        #region About Cobber
        // Menu-Help-AboutCobber
        private void ToolStripMenuItem_About_Click(object sender, EventArgs e)
        {
            try
            {
                string text =
                   "Product:       Cobber c2012\n"
                 + "Version:       0.9.0\n"
                 + "Contact:       cipherbox@outlook.com\n"
                 + "\n\n"
                 + "Cobber is a powerful .NET program obfuscator for Windows Phone applications.\n";

                string VerStr = "Cobber v" + typeof(Core.Cobber).Assembly.GetName().Version.ToString();

                //MessageBox.Show(text, "About Cobber");
                // consider to customize a message box with an http link
                Form msgbox = new Form();
                msgbox.Width = 420;
                msgbox.Height = 220;
                //msgbox.AutoSize = true;
                msgbox.Text = "About Cobber";  // caption
                msgbox.StartPosition = FormStartPosition.CenterParent;

                Label label = new Label() { Left = 20, Top = 20, AutoSize = true, Text = text };

                LinkLabel link = new LinkLabel() { Left = 20, Top = 130, AutoSize = true, Text = "http://www.Nlog2N.com/" };
                link.Links.Add(0, link.Text.Length, link.Text);
                link.LinkClicked += (linksender, linke) =>
                {
                    string http = linke.Link.LinkData as string;
                    if (!string.IsNullOrEmpty(http)) System.Diagnostics.Process.Start(http);
                    msgbox.Close();
                };

                Button button = new Button() { Left = 120, Top = 150, AutoSize = true, Text = "OK" };
                button.Click += (bsender, be) => { msgbox.Close(); };

                msgbox.Controls.Add(label);
                msgbox.Controls.Add(link);
                msgbox.Controls.Add(button);
                msgbox.ShowDialog(this);
            }
            catch (Exception)
            { }
        }
       
        #endregion

        #region Sorting Listview Columns
        private int sortColumn = -1;
        private void listView1_ColumnClick(object sender, System.Windows.Forms.ColumnClickEventArgs e)
        {
            try
            {
                // Determine whether the column is the same as the last column clicked.
                if (e.Column != sortColumn)
                {
                    // Set the sort column to the new column.
                    sortColumn = e.Column;
                    // Set the sort order to ascending by default.
                    listView1.Sorting = SortOrder.Ascending;
                }
                else
                {
                    // Determine what the last sort order was and change it.
                    if (listView1.Sorting == SortOrder.Ascending)
                        listView1.Sorting = SortOrder.Descending;
                    else
                        listView1.Sorting = SortOrder.Ascending;
                }

                // Set the ListViewItemSorter property to a new ListViewItemComparer object.
                this.listView1.ListViewItemSorter = new ListViewItemComparer(e.Column, listView1.Sorting);
                // Call the sort method to manually sort.
                listView1.Sort();

                listView1.Sorting = SortOrder.None; // no sort afterwards to speed up display
            }
            catch (Exception)
            { }
        }

        // Implements the manual sorting of items by columns.
        class ListViewItemComparer : IComparer
        {
            private int col;
            private SortOrder order;
            public ListViewItemComparer()
            {
                col = 0;
                order = SortOrder.Ascending;
            }
            public ListViewItemComparer(int column, SortOrder order)
            {
                col = column;
                this.order = order;
            }
            public int Compare(object x, object y)
            {
                int returnVal;
                try
                {
                    // folder is always put ahead
                    if (((ListViewItem)x).SubItems[2].Text == "Folder" && ((ListViewItem)y).SubItems[2].Text != "Folder")
                        returnVal = -1;
                    else if (((ListViewItem)x).SubItems[2].Text != "Folder" && ((ListViewItem)y).SubItems[2].Text == "Folder")
                        returnVal = 1;
                    else
                    {
                        string s1 = ((ListViewItem)x).SubItems[col].Text;
                        string s2 = ((ListViewItem)y).SubItems[col].Text;

                        /* fanghui: date comparison kinda slow. so i choose date string to be formated
                         * as "2013/08/02 14:00:00"
                         */
                        // Determine whether the type being compared is a date type ??
                        // Parse the two objects passed as a parameter as a DateTime.
                        /*
                        System.DateTime firstDate = DateTime.Parse(s1);
                        System.DateTime secondDate = DateTime.Parse(s2);
                        returnVal = DateTime.Compare(firstDate, secondDate); // Compare the two dates.
                        */

                        if (col == 3 && ((ListViewItem)x).SubItems[2].Text != "Folder") // size
                        {
                            // Compare the two items as integers
                            int firstSize = 0; int.TryParse(s1, out firstSize);
                            int secondSize = 0; int.TryParse(s2, out secondSize);
                            returnVal = (firstSize < secondSize ? (-1) : (firstSize == secondSize ? 0 : 1));
                        }
                        else
                        {
                            // Compare the two items as a string.
                            returnVal = String.Compare(s1, s2);
                        }
                        // Determine whether the sort order is descending.
                        if (order == SortOrder.Descending)
                            returnVal *= -1; // Invert the value returned by String.Compare.
                    }
                    return returnVal;
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }
        #endregion

        #region Display different context menu for file types

        // when user right-clicks a file from list view, program will show different context menu strip
        private ContextMenuStrip GetAdaptiveContextMenuStrip()
        {
            // default setting
            ContextMenuStrip cms = new ContextMenuStrip(this.components);
            cms.Name = "contextMenuStrip1";
            //cms.Size = new System.Drawing.Size(173, 236);

            cms.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
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
            //cms.Items.Add(tsm);

            //if (treeView1.SelectedNode == null) return null;
            //CobberObject nodeDirInfo = (CobberObject)treeView1.SelectedNode.Tag;

            for (int i = 0; i < listView1.SelectedItems.Count; i++)
            {
                CobberObject node = (CobberObject)listView1.SelectedItems[i].Tag;
                /*
                string filename = listView1.SelectedItems[i].Text;
                string filepath = Path.Combine(nodeDirInfo.FullName, filename);
                */
                this.toolStripMenuItem_Context_ApplyToMembers.Checked = node.ApplyToMembers;
            }
            return cms;
        }

        void listView1_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.Button == MouseButtons.Right)
                {
                    if (listView1.SelectedItems.Count > 0)
                    {
                        // update menu
                        ContextMenuStrip cms = GetAdaptiveContextMenuStrip();
                        if (cms == null) return;
                        this.listView1.ContextMenuStrip = cms;
                        this.contextMenuStrip1 = cms;

                        this.contextMenuStrip1.Show(this.listView1, new Point(e.X, e.Y));
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        #endregion

        #region Apply to members in context menu

        private void ToolStripMenuItem_Context_ApplyToMembers_Click(object sender, EventArgs e)
        {
            try
            {
                // get selected from listview
                if (listView1.SelectedItems.Count == 0)
                {
                    toolStripStatusLabel1.Text = "select assembly object(s) please.";
                    return;
                }

                // remove password for all selected files
                //CobberObject nodeInfo = (CobberObject)treeView1.SelectedNode.Tag;
                for (int i = 0; i < listView1.SelectedItems.Count; i++)
                {
                    CobberObject node = (CobberObject)listView1.SelectedItems[i].Tag;
                    node.ApplyToMembers = !(node.ApplyToMembers); // changed
                }

                PopulateListView(treeView1.SelectedNode);

                IsModified = true;

                //toolStripStatusLabel1.Text = "add PDF watermark done.";

                return;
            }
            catch (Exception)
            { }
        }
       
        #endregion

        #region ContextMenu - Obfuscation Setting

        private void ChangeObfuscationSetting(object sender, EventArgs e, string setting)
        {
            try
            {
                //CobberObject parent = (CobberObject)treeView1.SelectedNode.Tag;

                for (int i = 0; i < listView1.SelectedItems.Count; i++)
                {
                    CobberObject node = (CobberObject)listView1.SelectedItems[i].Tag;
                    if (node.ObfSettingName != setting)
                    {
                        node.ObfSettingName = setting;
                        node.Inherited = false;

                        this.IsModified = true;
                    }
                }

                if (IsModified)
                {
                    PopulateListView(treeView1.SelectedNode);

                    toolStripStatusLabel1.Text = "changed to " + setting + " obfuscation.";
                }
            }
            catch (Exception)
            { }
        }

        // ListView - Context Menu - NoneSetting
        private void ToolStripMenuItem_Context_NoneSetting_Click(object sender, EventArgs e)
        {
            ChangeObfuscationSetting(sender, e, "none");
        }

        // ListView - Context Menu - MinimumSetting
        private void ToolStripMenuItem_Context_MinimumSetting_Click(object sender, EventArgs e)
        {
            ChangeObfuscationSetting(sender, e, "minimum");
        }

        // ListView - ContextMenu - NormalSetting
        private void ToolStripMenuItem_Context_NormalSetting_Click(object sender, EventArgs e)
        {
            ChangeObfuscationSetting(sender, e, "normal");
        }

        // ListView - ContextMenu - AggressiveSetting
        private void ToolStripMenuItem_Context_AggressiveSetting_Click(object sender, EventArgs e)
        {
            ChangeObfuscationSetting(sender, e, "aggressive");
        }

        // ListView - ContextMenu - MaximumSetting
        private void ToolStripMenuItem_Context_MaximumSetting_Click(object sender, EventArgs e)
        {
            ChangeObfuscationSetting(sender, e, "maximum");
        }
        #endregion

        #region Modify obfuscation settings

        // ListView - ContextMenu - CustomizeSetting
        private void ToolStripMenuItem_Context_CustomizeSetting_Click(object sender, EventArgs e)
        {
            try
            {
                // get selected object from listview
                //CobberObject nodeInfo = (CobberObject)treeView1.SelectedNode.Tag;

                if (listView1.SelectedItems.Count == 0) return;
                string selected_setting_name = null;
                for (int i = 0; i < listView1.SelectedItems.Count; i++)
                {
                    CobberObject one = (CobberObject)listView1.SelectedItems[i].Tag;
                    selected_setting_name = one.ObfSettingName; // simply choose any one
                }

                // show this setting, and get a new setting from dialog

                // init a data grid view
                FormSetting formSetting = new FormSetting(this.Project, selected_setting_name);

                // commit a change only upon OK 
                //if (setting.ShowDialog(this) == DialogResult.OK)
                formSetting.ShowDialog();
                if (!formSetting.Done) return; // cancelled

                // any modification on settings?
                bool settingChanged = formSetting.Done && formSetting.isModified;
                IsModified = IsModified || settingChanged;

                // any change on current setting?
                string new_setting_name = formSetting.selectedSetting;
                for (int i = 0; i < listView1.SelectedItems.Count; i++)
                {
                    CobberObject node = (CobberObject)listView1.SelectedItems[i].Tag;
                    if (node.ObfSettingName != new_setting_name)
                    {
                        node.ObfSettingName = new_setting_name;
                        node.Inherited = false;

                        this.IsModified = true;
                    }
                }

                if (IsModified)
                {
                    //PopulateTreeView();
                    PopulateListView(treeView1.SelectedNode);
                    toolStripStatusLabel1.Text = "changed to " + new_setting_name + " obfuscation.";
                }
            }
            catch (Exception)
            { }
        }


        // click ToolStrip button "Obfuscation Settings"
        private void ToolStripButton_ObfSetting_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem_ObfSetting_Click(sender, e);
        }


        // Menu-Tools-ObfSetting
        private void ToolStripMenuItem_ObfSetting_Click(object sender, EventArgs e)
        {
            // init a data grid view
            FormSetting formSetting = new FormSetting(this.Project, null);

            // commit a change only upon OK 
            //if (setting.ShowDialog(this) == DialogResult.OK)
            formSetting.ShowDialog();

            // any modification?
            bool settingChanged = formSetting.Done && formSetting.isModified; 
            IsModified = IsModified ||  settingChanged;
            if (settingChanged)
            {
                //PopulateTreeView();
                PopulateListView(treeView1.SelectedNode);

                toolStripStatusLabel1.Text = "obfuscation settings modified.";
            }
        }
        #endregion

        #region Do Obfuscation

        // click ToolStrip button "Obfuscate"
        private void ToolStripButton_Obfuscate_Click(object sender, EventArgs e)
        {
            try
            {
                FormProgress formProgress = new FormProgress();
                formProgress.button1.Text = "Cancel";
                formProgress.ProcessAsync(this.cobber, this.Project); 
                formProgress.ShowDialog();
            }
            catch (Exception)
            { }
        }


        // Menu-Tools-Obfuscate
        private void ToolStripMenuItem_Obfuscate_Click(object sender, EventArgs e)
        {
            ToolStripButton_Obfuscate_Click(sender, e);
        }

        #endregion
    }
}
