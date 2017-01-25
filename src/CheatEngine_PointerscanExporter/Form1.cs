using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using CheatEngine_PointerscanExporter.Exporters;

namespace CheatEngine_PointerscanExporter
{
    public partial class Form1 : Form
    {
        private PointerscanresultReader Reader;
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = openFileDialog1.FileName;
                Process(openFileDialog1.FileName);
            }
        }

        private void Process(string fileName)
        {
            button2.Enabled = false;

            Reader = new PointerscanresultReader();

            try
            {
                Reader.ParseFile(fileName);
            }
            catch (Exception e)
            {
                MessageBox.Show("Error while parsing: " + e.Message);
                return;
            }

            label2.Text = "Results: " + Reader.TableResults.Count;

            listBox1.Items.Clear();
            listBox2.Items.Clear();

            foreach (var module in Reader.Modules)
                listBox1.Items.Add(module);

            foreach (var file in Reader.LinkedFiles)
                listBox2.Items.Add(file);

            button2.Enabled = true;

            if (checkBox1.Checked)
            {

                dataGridView1.Rows.Clear();


                foreach (var record in Reader.TableResults)
                {
                    dataGridView1.Rows.Add();
                }


                var colCount = dataGridView1.ColumnCount;
                for (int i= colCount;i < Reader.MaxOffsetCount + 1; i++)
                {
                    dataGridView1.Columns.Add("Column_" + i, "Offset " + i);
                    dataGridView1.Columns[i].Width = 50;
                }

                int rowCounter = 0;
                foreach (var record in Reader.TableResults)
                {
                    dataGridView1.Rows[rowCounter].Cells[0].Value = Reader.Modules[record.modulenr] + "+" + record.moduleoffset.ToString("X");

                    for (int i = 0; i < record.offsets.Length; i++)
                    {
                        dataGridView1.Rows[rowCounter].Cells[1 + i].Value = record.offsets[(record.offsets.Length - 1) - i].ToString("X");
                    }
                    rowCounter++;

                    if (checkBox2.Checked)
                    {
                        if (rowCounter >= numericUpDown1.Value)
                            break;
                    }
                }
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox1.Checked)
            {
                checkBox2.Enabled = true;
                numericUpDown1.Enabled = true;
                groupBox1.Visible = true;
            }
            else
            {
                checkBox2.Enabled = false;
                numericUpDown1.Enabled = false;
                groupBox1.Visible = false;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(radioButton1.Checked)
            {
                var result = ExportTo_TXT.Convert(Reader);

                saveFileDialog1.Filter = "Text Files|*.txt";
                saveFileDialog1.ShowDialog();

                if(saveFileDialog1.FileName != "")
                {
                    File.WriteAllText(saveFileDialog1.FileName, result);
                }
            }
            else if (radioButton2.Checked)
            {
                saveFileDialog1.Filter = "Excel Files|*.xlsx";
                saveFileDialog1.ShowDialog();

                if (saveFileDialog1.FileName != "")
                {
                    ExportTo_Excel.SaveXml(Reader, saveFileDialog1.FileName);
                }
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            numericUpDown1.Enabled = checkBox2.Checked;
        }
    }
}
