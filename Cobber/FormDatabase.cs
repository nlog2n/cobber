using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

using Cobber;
using Cobber.Core;

namespace Cobber
{
    public partial class FormDatabase : Form
    {
        public CobberDatabase db;

        public FormDatabase()
        {
            InitializeComponent();

            this.treeView1.NodeMouseClick += new TreeNodeMouseClickEventHandler(this.TreeView1_NodeMouseClick);
            this.listView1.ColumnClick += new ColumnClickEventHandler(this.listView1_ColumnClick);
            this.textBox2.TextChanged += new EventHandler(this.textBox2_TextChanged);
        }


        #region populate tree and list views

        string GetDbObjectName(object info)
        {
            if (info is CobberDatabase) return (info as CobberDatabase).Name;
            if (info is CobberDbModule) return (info as CobberDbModule).Name;
            if (info is CobberDbTable) return (info as CobberDbTable).Name;
            if (info is CobberDbEntry) return (info as CobberDbEntry).Name;

            return "";
        }

        List<object> GetDbChildren(object info)
        {
            List<object> children = new List<object>();
            if (info is CobberDatabase)
            {
                foreach (var x in (info as CobberDatabase).Values)
                {
                    children.Add(x);
                }
            }
            if (info is CobberDbModule)
            {
                foreach (var x in (info as CobberDbModule).Values)
                {
                    children.Add(x);
                }
            }
            if (info is CobberDbTable) return children;  // display its children in listView
            /*
            {
                foreach (var x in (info as DbTable).Entries)
                {
                    children.Add(x);
                }
            }
            */

            if (info is CobberDbEntry) return children; // no children

            return children;
        }

        private void AttachCobberObject(object info, TreeNode nodeToAddTo)
        {
            nodeToAddTo.Text = GetDbObjectName(info);
            nodeToAddTo.Tag = info;

            foreach (var child in GetDbChildren(info))
            {
                TreeNode aNode;
                aNode = new TreeNode();
                aNode.Tag = child;

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
            if (db == null) return;
            db.Name = Path.GetFileName(textBox1.Text);
            TreeNode rootNode = new TreeNode(db.Name);
            AttachCobberObject(db, rootNode);

            treeView1.Nodes.Add(rootNode);

            treeView1.SelectedNode = rootNode; 
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

                if (!(selectedNode.Tag is CobberDbTable)) return; // only display db entries

                CobberDbTable nodeInfo = (selectedNode.Tag as CobberDbTable);
                string keyword = textBox2.Text; // keyword

                listView1.Items.Clear();
                listView1.BeginUpdate(); // this can significantly faster the display!
                foreach (CobberDbEntry co in nodeInfo)
                {
                    if (Filter(co, keyword)) continue;

                    ListViewItem  item = new ListViewItem(co.Name); 
                    //item.UseItemStyleForSubItems = false; // color setting works only if ListViewItem.UseItemStyleForSubItems = false
                    item.Tag = co;
                    item.SubItems.Add(co.Value);

                    listView1.Items.Add(item);
                }
                listView1.EndUpdate();

                //listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            }
            catch (Exception)
            {
            }
        }

        // tree selected item changed on left-side tree view
        void TreeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            // By using TreeView.HideSelection = false, there will be shadow for selected folder.
            // If want to change folder icon too, need to set SelectedImageIndex for this node.
            //e.Node.SelectedImageIndex = 1;

            PopulateListView(e.Node);
        }

        public void UpdateTree()
        {
            PopulateTreeView();
            PopulateListView(treeView1.SelectedNode);
        }

        private bool Filter(CobberDbEntry entry, string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return false; // no filter

            return
             entry.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) == -1
             && entry.Value.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) == -1;
        }

        #endregion


        // choose database source
        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Cobber mapfile (*.map)|*.map|All Files (*.*)|*.*";
            if (ofd.ShowDialog() != DialogResult.OK) return;

            // read from file
            textBox1.Text = ofd.FileName;
            //textBox1.Text = "select a database logfile.";
            try
            {
                db = new CobberDatabase();
                using (BinaryReader rdr = new BinaryReader(File.OpenRead(textBox1.Text)))
                {
                    db.Deserialize(rdr);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Invalid database source!", "Cobber", MessageBoxButtons.OK, MessageBoxIcon.Error);
                db = null;
                return;
            }

            // display
            UpdateTree();
        }

        // filter
        void textBox2_TextChanged(object sender, EventArgs e)
        {
            PopulateListView(treeView1.SelectedNode);
        }

        // copy entries
        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string cptext = "";
                // get selected from listview
                for (int i = 0; i < listView1.SelectedItems.Count; i++)
                {
                    CobberDbEntry entry = (CobberDbEntry)listView1.SelectedItems[i].Tag;
                    cptext += (entry.Name + "\t" + entry.Value);
                    cptext += "\r\n";
                }

                Clipboard.SetText(cptext);
            }
            catch (Exception)
            { }
        }

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
                    // Compare the two items as a string.
                    string s1 = ((ListViewItem)x).SubItems[col].Text;
                    string s2 = ((ListViewItem)y).SubItems[col].Text;
                    returnVal = String.Compare(s1, s2);

                    // Determine whether the sort order is descending.
                    if (order == SortOrder.Descending)
                        returnVal *= -1; // Invert the value returned by String.Compare.

                    return returnVal;
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }
        #endregion
    }
}
