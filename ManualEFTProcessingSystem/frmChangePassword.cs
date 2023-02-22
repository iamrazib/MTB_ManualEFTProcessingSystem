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
    public partial class frmChangePassword : Form
    {
        static Manager mg = new Manager();
        public string loggedUserChPass = "";
        public string loggedUserIdAndNameChPass = "";

        public frmChangePassword()
        {
            InitializeComponent();
        }

        private void frmChangePassword_Load(object sender, EventArgs e)
        {
            lblUserIdName.Text = loggedUserIdAndNameChPass;
        }

        private void btnChangePass_Click(object sender, EventArgs e)
        {
            string currPass = txtCurrPass.Text.Trim();
            string newPass = txtNewPass.Text.Trim();

            if (!currPass.Equals("") && !newPass.Equals(""))
            {
                bool status = mg.ChangeUserPassword(loggedUserChPass, currPass, newPass);
                if (status)
                {
                    MessageBox.Show("Password Changed Successfully");
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Error in Password Changed !!!");
                }
            }
            else
            {
                MessageBox.Show("Error In Input ....");
            }
        }

        private void frmChangePassword_FormClosing(object sender, FormClosingEventArgs e)
        {
            //MessageBox.Show("111");
            //Application.Exit();
        }
    }
}
