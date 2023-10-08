using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraPrinting.Native.WebClientUIControl;
using Newtonsoft.Json;

namespace SqlManagementStudioCustom
{
    public partial class ManagementStudio : Form
    {
        private Button currentButton;
        private Random random;
        private int tempIndex;
        private Form activeForm;
        public ManagementStudio()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized;
            random = new Random();
        }

        private Color SelectThemeColor()
        {
            int index = random.Next(ThemeColor.ColorList.Count);
            while (tempIndex == index)
            {
                index = random.Next(ThemeColor.ColorList.Count);
            }
            tempIndex = index;
            string color = ThemeColor.ColorList[index];
            return ColorTranslator.FromHtml(color);
        }

        private void ActivateButton(object btnSender)
        {
            if (btnSender != null)
            {
                if (currentButton != (Button)btnSender)
                {
                    DisableButton();
                    Color color = SelectThemeColor();
                    currentButton = (Button)btnSender;
                    currentButton.BackColor = color;
                    currentButton.ForeColor = Color.White;
                    currentButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                    panelTitleBar.BackColor = color;
                    panelLogo.BackColor = ThemeColor.ChangeColorBrightness(color, -0.3);
                    ThemeColor.PrimaryColor = color;
                    ThemeColor.SecondaryColor = ThemeColor.ChangeColorBrightness(color, -0.3);
                    //btnCloseChildForm.Visible = true;
                }
            }
        }

        private void OpenChildForm(Form childForm, object btnSender)
        {
            if (activeForm != null)
                activeForm.Close();
            ActivateButton(btnSender);
            activeForm = childForm;
            childForm.TopLevel = false;
            childForm.FormBorderStyle = FormBorderStyle.None;
            childForm.Dock = DockStyle.Fill;
            this.panelDesktopPane.Controls.Add(childForm);
            this.panelDesktopPane.Tag = childForm;
            childForm.BringToFront();
            childForm.Show();
            //lblTitle.Text = childForm.Text;

        }

        private void DisableButton()
        {
            foreach (Control previousBtn in panelMenu.Controls)
            {
                if (previousBtn.GetType() == typeof(Button))
                {
                    previousBtn.BackColor = Color.FromArgb(51, 51, 76);
                    previousBtn.ForeColor = Color.Gainsboro;
                    previousBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ActivateButton(sender);
        }

        private void btnExecute_Click(object sender, EventArgs e)
        {
            ActivateButton(sender);
        }

        private void textBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (lineNumberRTB1.RichTextBox.SelectedText.Length > 0)
            {
                Clipboard.SetText(lineNumberRTB1.RichTextBox.SelectedText);
            }
            else
            {

            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Check if Ctrl + F5 is pressed
            if (keyData == (Keys.Control | Keys.F5))
            {
                // Call the button click event handler
                button1_Click(this, EventArgs.Empty);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }


        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                dataGridView1.Rows.Clear();
                dataGridView1.Columns.Clear();

                TcpClient client = new TcpClient();
                client.Connect("127.0.0.1", 23456);

                NetworkStream stream = client.GetStream();

                string query = string.Empty;
                if (lineNumberRTB1.RichTextBox.SelectedText.Length == 0)
                {
                    query = lineNumberRTB1.Text.Trim();
                }
                else
                {
                    query = lineNumberRTB1.RichTextBox.SelectedText.Trim();
                }

                byte[] queryBytes = Encoding.UTF8.GetBytes(query);
                stream.Write(queryBytes, 0, queryBytes.Length);

                byte[] buffer = new byte[1000000];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string jsonResponse = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                ServerResponse response = JsonConvert.DeserializeObject<ServerResponse>(jsonResponse);

                lineNumberRTB1.Text = response.SuccessMessage;

                if (response.Data != null)
                {
                    dataGridView1.Columns.Clear();
                    dataGridView1.Columns.Clear();

                    if (response.Data.Count > 0)
                    {
                        // Create columns based on the first table's columns
                        foreach (var column in response.Data[0].Records)
                        {
                            dataGridView1.Columns.Add(column.ColumnName, column.ColumnName);
                        }

                        // Iterate through tables
                        foreach (var table in response.Data)
                        {
                            // Create a new row
                            DataGridViewRow dataGridViewRow = new DataGridViewRow();

                            // Populate row cells with values
                            foreach (var column in table.Records)
                            {
                                dataGridViewRow.Cells.Add(new DataGridViewTextBoxCell { Value = column.DataType });
                            }


                            dataGridView1.Rows.Add(dataGridViewRow);
                        }
                    }
                    else
                    {
                        lineNumberRTB1.Text = "No data found in the response.";
                    }
                }
                else if (!string.IsNullOrEmpty(response.SuccessMessage))
                {
                    lineNumberRTB1.Text = response.SuccessMessage;
                }
                else
                {
                    lineNumberRTB1.Text = "Unsupported query or an error occurred.";
                }



                client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        public class TableRecord
        {
            public string ColumnName { get; set; } = "NULL";
            public string DataType { get; set; } = "NULL";
            public int DataLength { get; set; }
        }

        public class Table
        {
            public string TableName { get; set; }
            public List<TableRecord> Records { get; set; }
        }
        public class ServerResponse
        {
            public string SuccessMessage { get; set; } // For INSERT and CREATE TABLE queries
            public List<Table> Data { get; set; }     // For SELECT queries
        }

        private void lineNumberRTB1_Load(object sender, EventArgs e)
        {

        }
    }
}
