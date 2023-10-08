using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SqlManagementStudioCustom
{
    public partial class LoginManager : Form
    {
        public LoginManager()
        {
            InitializeComponent();
            textBox2.PasswordChar = '*';
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void pictureBox5_Click(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void passwordTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                button1.PerformClick();
                e.Handled = true;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Length == 0)
            {
                MessageBox.Show("You must fill the username");
            }

            if (textBox2.Text.Length == 0)
            {
                MessageBox.Show("You must fill the password");
            }

            try
            {
                var canAuthenticate = AuthenticateUser(textBox1.Text, textBox2.Text);

                if(!canAuthenticate)
                {
                    MessageBox.Show("User or password is incorrect");
                    return;
                }

                var managementStudio = new ManagementStudio();
                managementStudio.Show();
                this.Hide();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void SaveCredentials(string username, string password)
        {
            SHA256 sha256 = SHA256.Create();

            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }

            string hashedPassword = builder.ToString();
            using (StreamWriter sw = new StreamWriter(@"C:\credentials.txt", true))
            {
                sw.WriteLine("{0},{1}", username, hashedPassword);
            }
        }

        public bool AuthenticateUser(string username, string password)
        {
            SHA256 sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            string hashedPassword = builder.ToString();

            using (StreamReader sr = new StreamReader(@"C:\credentials.txt"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] parts = line.Split(',');
                    if (parts.Length == 2)
                    {
                        if (parts[0].ToLower() == username && parts[1] == hashedPassword)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
