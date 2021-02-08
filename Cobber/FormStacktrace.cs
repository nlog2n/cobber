using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;

using Cobber;
using Cobber.Core;

namespace Cobber
{
    public partial class FormStacktrace : Form
    {
        public CobberDatabase db;

        public FormStacktrace()
        {
            InitializeComponent();
        }

        // choose database log file
        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Cobber mapfile (*.map)|*.map|All Files (*.*)|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = ofd.FileName;
            }
        }

        // translate
        private void button2_Click(object sender, EventArgs e)
        {
            if ( db == null) // read from file
            {
                db = new CobberDatabase();
                try
                {
                    using (BinaryReader rdr = new BinaryReader(File.OpenRead(textBox1.Text)))
                    {
                        db.Deserialize(rdr);
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Invalid database source!", "Cobber", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            string stacktrace = textBox2.Text; // input
            var entries = new Dictionary<string, string>(); // reverse dictionary
            foreach (var mod in db.Values)
            {
                foreach (var tbl in mod.Values)
                {
                    if (tbl.Name == "Rename")  // only in this section: renamed
                    {
                        foreach (var entry in tbl)
                        {
                            int index = stacktrace.IndexOf(entry.Value);
                            if (index != -1 && !entries.ContainsKey(entry.Value))
                                entries.Add(entry.Value, entry.Name);
                        }
                    }
                }
            }

            //output
            if (entries.Count == 0)
            {
                textBox2.Text = stacktrace;
            }
            else
            {
                string regex = "(" + string.Join("|", entries.Keys.Select(_ => Regex.Escape(_)).ToArray()) + ")";
                textBox2.Text = Regex.Replace(stacktrace, regex, m => entries[m.Value]);
            }
        }
    }
}
