using System;
using System.Collections.Generic;
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
    public partial class FormOption : Form
    {
        public bool Done = false;

        public FormOption()
        {
            InitializeComponent();
        }

        // Cancel
        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }

        // OK
        private void button3_Click(object sender, EventArgs e)
        {
            Done = true;
            Close();
        }

        // Choose a strong name key file
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "Strong name key file (*.snk; *.pfx)|*.snk;*.pfx|All Files (*.*)|*.*";
                ofd.Title = "Select a strong name key file";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    this.textBox2.Text = ofd.FileName;
                }
            }
            catch (Exception)
            { }
        }

        // Choose an output directory
        private void button4_Click(object sender, EventArgs e)
        {
            // Show the folder browser.
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "Choose an output direcotry:";
            fbd.RootFolder = Environment.SpecialFolder.Desktop;

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                this.textBox3.Text = fbd.SelectedPath;
            }
        }

        public void PopulateFromProject(CobberProject proj)
        {
            textBox1.Text = proj.Seed;
            textBox2.Text = proj.SNKeyPath;
            textBox4.Text = proj.SNKeyPassword; // snk password

            // combobox
            foreach (ObfuscationSetting setting in ObfuscationSetting.GetSettings())
            {
                if (setting.IsPreset()) // only add preset settings
                {
                    comboBox1.Items.Add(setting.Name);
                }
            }
            comboBox1.Text = proj.ObfSettingName;  // preset

            // packer list
            foreach (string packer in Cobber.Core.Cobber.Packers.Keys)
            {
                comboBox2.Items.Add(packer);
            }
            comboBox2.Text = proj.PackerID;  // packer

            checkBox1.Checked = proj.Debug;  // generate debug symbols
            checkBox2.Checked = proj.RecognizeObfuscationAttributes;
            textBox3.Text = proj.OutputPath;
        }

        // update the configuration
        public bool UpdateProject(CobberProject proj)
        {
            bool isModified = false;
            {
                if (proj.Seed != textBox1.Text)
                {
                    isModified = true;
                    proj.Seed = textBox1.Text;
                }
                if (proj.SNKeyPath != textBox2.Text)
                {
                    isModified = true;
                    proj.SNKeyPath = textBox2.Text;
                }
                if (proj.SNKeyPassword != textBox4.Text)
                {
                    isModified = true;
                    proj.SNKeyPassword = textBox4.Text;
                }
                if (proj.ObfSettingName != comboBox1.Text)
                {
                    isModified = true;
                    proj.ObfSettingName = comboBox1.Text;
                }
                if (proj.PackerID != comboBox2.Text)
                {
                    isModified = true;
                    proj.PackerID = comboBox2.Text;
                }
                if (proj.Debug != checkBox1.Checked)
                {
                    isModified = true;
                    proj.Debug = checkBox1.Checked;
                }
                if (proj.RecognizeObfuscationAttributes != checkBox2.Checked)
                {
                    isModified = true;
                    proj.RecognizeObfuscationAttributes = checkBox2.Checked;
                }
                if (proj.OutputPath != textBox3.Text)
                {
                    isModified = true;
                    proj.OutputPath = textBox3.Text;
                }
            }

            return isModified;
        }

    }
}
