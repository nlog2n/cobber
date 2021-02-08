using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Cobber.Core;
using Cobber.Core.Project;

namespace Cobber
{
    public partial class FormSetting : Form
    {
        public bool Done = false; // for commit
        public bool isModified = false;  // any modification on settings
        public string selectedSetting = null; // output new selected setting from list

        private CobberProject Project = null;

        public FormSetting(CobberProject proj, string inputSetting)
        {
            InitializeComponent();

            this.Project = proj;
            this.selectedSetting = inputSetting;

            this.listView1.SelectedIndexChanged += new EventHandler(listView1_SelectedIndexChanged);
            this.listView1.MouseDoubleClick += new MouseEventHandler(listView1_MouseDoubleClick);
            this.listView1.AfterLabelEdit += new LabelEditEventHandler(listView1_AfterLabelEdit);

            this.dataGridView1.CellValueChanged += new DataGridViewCellEventHandler(dataGridView1_CellValueChanged);
            this.dataGridView1.CurrentCellDirtyStateChanged += new EventHandler(dataGridView1_CurrentCellDirtyStateChanged);

            PopulateListView();
            PopulateGridView();
        }

        // Cancel
        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }

        // OK
        private void button2_Click(object sender, EventArgs e)
        {
            UpdateProject();
            Done = true;
            Close();
        }

        // init listView and data grid view on the right side
        private void PopulateListView()
        {
            // set a vertical scrollable listView by doing:
            //    Listview1.Scrollable = true;
            //    Listview1.View = View.Details
            //    ListView.HeaderStyle = ColumnHeaderStyle.None;
            // This will only work correctly if you have added some columns in your Listview1, 
            // So add a dummy column. like, 
            ColumnHeader header = new ColumnHeader();
            header.Text = "";
            header.Name = "col1";
            listView1.Columns.Add(header);

            // populate each obfuscation setting into listView
            foreach ( ObfuscationSetting setting in ObfuscationSetting.GetSettings())
            {
                // clone for future modifications
                ObfuscationSetting s = ObfuscationSetting.Clone(setting); // new ObfuscationSetting(setting);

                // create a listView item
                ListViewItem item = new ListViewItem(s.Name);
                item.Tag = s; // save for future modification

                listView1.Items.Add(item); // add

                // set the selected item
                if (s.Name == this.selectedSetting)
                {
                    item.Selected = true;
                    listView1.Select();
                }
            }
        }

        // refresh the right-side grid view for selected setting
        private void PopulateGridView()
        {
            // Note: in order to autosize datagridview, need to set its AntoSizeColumn = fill, Dock = top
            //       also, set DataGridView.EditMode = EditOnEnter to open combobox in single click.
            this.dataGridView1.Rows.Clear();

            // get selected setting
            ObfuscationSetting selected = GetSelectedSetting();
            bool ispreset = (selected != null && selected.IsPreset());

            // match this setting to all known obfuscators
            // each row contains:   column1=obf name, column2=param, column3=value, column4=checked
            //List<string> obfs = Cobber.Core.Cobber.Obfuscators.Keys.ToList();
            //obfs.Sort();
            foreach(IObfuscation obf in Cobber.Core.Cobber.Obfuscators.Values)
            {
                // add row values for this obfuscator directly
                //string[] row = new string[] { row0_obf, row1_param, row2_value, row3_checked }; // row values
                //int rindex = this.dataGridView1.Rows.Add(row);

                // more datagridview methods, refer to
                // http://blog.sina.com.cn/s/blog_52476c1d01018dy1.html

                int rindex = this.dataGridView1.Rows.Add();
                DataGridViewRow row = dataGridView1.Rows[rindex];

                // column 1
                row.Cells[0].Value = obf.ID;
                row.Cells[0].ToolTipText = obf.Description;

                // column 2
                DataGridViewComboBoxCell param_combox = (DataGridViewComboBoxCell)row.Cells[1];
                foreach (string param_key in obf.Parameters.Keys)
                {
                    param_combox.Items.Add(param_key);
                }
                param_combox.Value = ""; 

                // column 3
                DataGridViewComboBoxCell value_combox = (DataGridViewComboBoxCell)row.Cells[2];
                value_combox.Value = "";
                value_combox.ReadOnly = ispreset;

                // column 4
                DataGridViewCheckBoxCell checkbox = (DataGridViewCheckBoxCell)row.Cells[3];
                string obf_checked = "false";
                if (selected != null && selected.ObfParameters.ContainsKey(obf.ID))
                {
                    // if current setting exists, and it contains this obf
                    NameValueCollection param = selected.ObfParameters[obf.ID];
                    obf_checked = "true"; // Note: in view, set checkboxcell.TrueValue = "true", FalseValue = "false"
                }
                checkbox.Value = obf_checked;
                if (obf_checked == "false")
                {
                    param_combox.ReadOnly = true;
                    value_combox.ReadOnly = true;
                }
                checkbox.ReadOnly = ispreset;
            }

            // disable whole editing for presettings, which is improper here.
            // so I disable editing only on value and checkbox for presettings.
            //this.dataGridView1.Enabled = !current_setting.IsPreset();
        }

        void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridView1.CurrentCell == null) return;

            ObfuscationSetting selected = GetSelectedSetting();
            if (selected == null) return;

            DataGridViewRow row = dataGridView1.Rows[e.RowIndex]; // which row got change
            int colIndex = e.ColumnIndex; // which column got change
            string obf_id = row.Cells[0].Value as string;

            if (colIndex == 1) // change in param list
            {
                // show new optional values only, no substantial change on internal data
                string param = row.Cells[colIndex].Value as string;
                if (!string.IsNullOrEmpty(param))
                {
                    List<string> values = Cobber.Core.Cobber.Obfuscators[obf_id].Parameters[param];
                    DataGridViewComboBoxCell value_combox = (DataGridViewComboBoxCell)row.Cells[2];
                    value_combox.Items.Clear();
                    foreach (string v in values) { value_combox.Items.Add(v); }

                    value_combox.Value = "";
                    if (selected.ObfParameters.ContainsKey(obf_id))
                    {
                        NameValueCollection nvc = selected.ObfParameters[obf_id];
                        value_combox.Value = nvc[param] ?? "";
                    }
                    if (selected.IsPreset())
                    {
                        value_combox.ReadOnly = true;
                    }
                }
            }
            else if (colIndex == 2) // change in param value
            {
                string param = row.Cells[1].Value as string;
                string value_new = row.Cells[colIndex].Value as string;
                if (!string.IsNullOrEmpty(param) && !string.IsNullOrEmpty(value_new))
                {
                    if (selected.ObfParameters.ContainsKey(obf_id))
                    {
                        NameValueCollection nvc = selected.ObfParameters[obf_id];
                        string value_old = nvc[param] ?? "";
                        if (value_new != value_old)
                        {
                            if (selected.IsPreset())
                            {
                                row.Cells[colIndex].Value = value_old;
                            }
                            else
                            {
                                nvc[param] = value_new;
                                this.isModified = true;
                            }
                        }
                    }
                }
            }
            else if (colIndex == 3) // change in checkbox
            {
                DataGridViewCheckBoxCell chk = (DataGridViewCheckBoxCell)row.Cells[colIndex];
                if (chk.Value == chk.FalseValue || chk.Value == null)
                {
                    // remove this obfuscator from setting
                    if (selected.ObfParameters.ContainsKey(obf_id))
                    {
                        selected.ObfParameters.Remove(obf_id);
                        this.isModified = true;
                    }

                    // also lock the param and value boxes
                    row.Cells[1].ReadOnly = true;
                    row.Cells[2].ReadOnly = true;
                }
                else
                {
                    // add this obfuscator to setting, with empty/default parameters
                    if (!selected.ObfParameters.ContainsKey(obf_id))
                    {
                        selected.ObfParameters.Add(obf_id, new NameValueCollection());
                        this.isModified = true;
                    }

                    // also unlock the param and value boxes
                    row.Cells[1].ReadOnly = false;
                    row.Cells[2].ReadOnly = false;
                }
            }
        }

        // CellValueChanged won't fire until the cell has lost focus.
        // But the CurrentCellDirtyStateChanged event commits the changes immediately when the cell is clicked. 
        // So I manually raise the CellValueChanged event inside by calling the CommitEdit method.
        // Refer to: http://stackoverflow.com/questions/17275166/checkboxes-in-datagridview-not-firing-cellvaluechanged-event
        void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dataGridView1.IsCurrentCellDirty)
            {
                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        // update when click OK button
        private void UpdateProject()
        {
            Dictionary<string, ObfuscationSetting> modified = new Dictionary<string, ObfuscationSetting>();
            foreach (ListViewItem item in listView1.Items)
            {
                ObfuscationSetting s = item.Tag as ObfuscationSetting;
                modified[s.Name] = s;
            }

            ObfuscationSetting.ObfSettings = modified; // replaced. need a better way to do this??
        }

        void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ObfuscationSetting selected = GetSelectedSetting();
            this.selectedSetting = (selected == null ? null : selected.Name);

            PopulateGridView();
        }

        // selected setting in list view
        private ObfuscationSetting GetSelectedSetting()
        {
            ObfuscationSetting selected_setting = null;

            if (listView1.SelectedItems.Count == 1)
            {
                selected_setting = listView1.SelectedItems[0].Tag as ObfuscationSetting;
            }

            return selected_setting;
        }


        private static void CheckSettingUsage(CobberObject root, string setting, List<CobberObject> result)
        {
            if (root.ObfSettingName == setting) { result.Add(root); }

            foreach (var c in root.GetChildren())
            {
                CheckSettingUsage(c, setting, result);
            }
        }


        #region Add or remove settings
        //add new setting
        private void button3_Click(object sender, EventArgs e)
        {
            ObfuscationSetting selected = GetSelectedSetting();
            ObfuscationSetting sample = new ObfuscationSetting(selected);

            ListViewItem item = new ListViewItem(sample.Name);
            item.Tag = sample; 

            // Add a new item to the ListView
            listView1.Items.Add(item);

            // Place the newly-added item into edit mode immediately
            item.BeginEdit();

            PopulateGridView();

            this.isModified = true;
        }

        // start change setting name
        void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // Listview has a LabelEdit property; when you set it "true", 
            // then in an event handler you can call Listview.Items[x].BeginEdit(), and edit an item. 
            // As an example, you can handle ListView.DoubleClick event and call BeginEdit right there

            if (listView1.SelectedItems.Count == 1)
            {
                ObfuscationSetting selected_setting = listView1.SelectedItems[0].Tag as ObfuscationSetting;
                if (selected_setting.IsPreset()) return; // do not change name for presets

                this.listView1.SelectedItems[0].BeginEdit();
            }
        }

        // change setting name done
        void listView1_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            ListViewItem item = this.listView1.Items[e.Item];
            ObfuscationSetting setting = item.Tag as ObfuscationSetting;
            if (setting.IsPreset())
            {
                e.CancelEdit = true;
                return;
            }

            string setting_name_old = item.Text;
            string setting_name_new = e.Label;
            if (string.IsNullOrEmpty(setting_name_new) || setting_name_new == setting_name_old)
            {
                e.CancelEdit = true;
                return;
            }

            // check if this new name already exists
            foreach (ListViewItem exist in listView1.Items)
            {
                if (exist.Text == setting_name_new)
                {
                    e.CancelEdit = true;
                    return;
                }
            }

            // let it go

            // check if this name was used in cobber project.
            // in such case need to replace all used ones.
            List<CobberObject> result = new List<CobberObject>();
            CheckSettingUsage(this.Project, setting_name_old, result);
            foreach (CobberObject obj in result)
            {
                obj.ObfSettingName = setting_name_new;
            }

            // change internal save for this setting
            setting.Name = setting_name_new;

            // later label name will get changed also.

            // focus on this item
            item.Selected = true;
            listView1.Select();

            this.isModified = true;
        }


        // remove setting
        private void button4_Click(object sender, EventArgs e)
        {
            ObfuscationSetting set = GetSelectedSetting();
            if (set == null) return;
            if (set.IsPreset()) return;

            // check if this setting was used in cobber project.
            //       in such case do NOT allow user to remove it for now.
            List<CobberObject> result = new List<CobberObject>();
            CheckSettingUsage(this.Project, set.Name, result);
            if (result.Count > 0)
            {
                MessageBox.Show("Setting [" + set.Name + "] was used in project yet!", "Cobber", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // actually only one removed,cause we disable multiple select.
            ListViewItem item = listView1.SelectedItems[0];
            int next = item.Index;
            listView1.Items.Remove(item);

            // let next item selected
            if (listView1.Items.Count > 0)
            {
                if (next >= listView1.Items.Count)
                {
                    next = listView1.Items.Count - 1;
                }

                listView1.Items[next].Selected = true;
                listView1.Select();
            }

            this.isModified = true;

            PopulateGridView();
        }

        #endregion
    }
}
