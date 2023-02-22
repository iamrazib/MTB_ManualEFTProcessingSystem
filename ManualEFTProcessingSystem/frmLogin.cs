using ManualEFTProcessingSystem.DBUtility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ManualEFTProcessingSystem
{
    public partial class frmLogin : Form
    {
        static Manager mg = new Manager();

        public frmLogin()
        {
            InitializeComponent();
        }

        private void frmLogin_Load(object sender, EventArgs e)
        {
            comboBoxUser.Items.Clear();
            comboBoxUser.Items.Add("---- Select User ----");

            string usr = "";
            DataTable userlist = mg.GetPermittedUserList();

            for (int rw = 0; rw < userlist.Rows.Count; rw++)
            {
                usr = userlist.Rows[rw][0].ToString();
                comboBoxUser.Items.Add(usr);
            }

            comboBoxUser.SelectedIndex = 0;
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (comboBoxUser.SelectedIndex == 0)
            {
                MessageBox.Show("Please Select your user");
            }
            else
            {
                string userTypeV = "", isPwdChanged = "";

                string userIdName = comboBoxUser.SelectedItem.ToString();
                string pass = textBoxPass.Text;

                string userId = userIdName.Split('-')[0].Trim();
                bool passMatch = mg.isPasswordMatch(userId, pass, ref userTypeV, ref isPwdChanged);
                if (passMatch)
                {
                    Form1 frm1 = new Form1();
                    frm1.loggedUser = userId;
                    frm1.loggedUserIdAndName = userIdName;
                    frm1.userType = userTypeV;
                    frm1.isPassChanged = isPwdChanged;
                    frm1.Show();
                    this.Hide();
                }
                else
                {
                    MessageBox.Show("Password Do Not Match, Please Try Again !!!");
                }
            }
        }

        private void comboBoxUser_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                textBoxPass.Focus();
            }
        }

        private void textBoxPass_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnLogin_Click(sender, e);
            }
        }
    }
}
