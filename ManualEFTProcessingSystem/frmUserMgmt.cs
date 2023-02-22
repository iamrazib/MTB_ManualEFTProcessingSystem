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
    public partial class frmUserMgmt : Form
    {
        public string loggedUser = "";
        public string loggedUserIdAndName = "";
        public string userType = "";

        static string[] USER_ALLOWED_TO_CONFIG = new string[] { "admin", "superadmin", "authorizer" };

        static Manager mg = new Manager();

        public frmUserMgmt()
        {
            InitializeComponent();
        }

        private void frmUserMgmt_Load(object sender, EventArgs e)
        {
            //string uid = this.loggedUser;
            //string uTyp = this.userType;
            //string uNm = this.loggedUserIdAndName;

            lblLoggedUserInfo.Text = this.loggedUserIdAndName;
            lblLoggedUserRole.Text = this.userType;

            LoadAllUserInfo();

            LoadNewUserUserType(lblLoggedUserRole.Text);
            LoadExistingUsers();
            LoadUserType();
            LoadUserActivity();

            EnableDisableButtonBasedOnUserRole(lblLoggedUserRole.Text);

        }

        private void EnableDisableButtonBasedOnUserRole(string loggedUserRole)
        {
            foreach (string userRole in USER_ALLOWED_TO_CONFIG)
            {
                if (loggedUserRole.ToLower().Equals(userRole))
                {
                    btnSaveNewUser.Enabled = true;
                    btnUpdateUserType.Enabled = true;
                    btnUpdateUserActivity.Enabled = true;
                    btnUpdateUserEmail.Enabled = true;
                    break;
                }
                else
                {
                    btnSaveNewUser.Enabled = false;
                    btnUpdateUserType.Enabled = false;
                    btnUpdateUserActivity.Enabled = false;
                    btnUpdateUserEmail.Enabled = false;
                }
            }
        }

        private void LoadAllUserInfo()
        {
            DataTable dtUsrs = mg.GetAllUsersInfo();

            dataGridViewUsersInfo.DataSource = null;
            dataGridViewUsersInfo.DataSource = dtUsrs;

            dataGridViewUsersInfo.Columns["Sl"].Width = 40;
            dataGridViewUsersInfo.Columns["UserId"].Width = 70;
            dataGridViewUsersInfo.Columns["UserName"].Width = 150;
            dataGridViewUsersInfo.Columns["isActive"].Width = 50;
            dataGridViewUsersInfo.Columns["UserEmail"].Width = 200;
        }

        private void LoadUserActivity()
        {
            cmbUpdateUserActivity.Items.Clear();

            cmbUpdateUserActivity.Items.Add("--- SELECT ---");
            cmbUpdateUserActivity.Items.Add("1-Active");
            cmbUpdateUserActivity.Items.Add("0-Inactive");

            cmbUpdateUserActivity.SelectedIndex = 0;
        }

        private void LoadUserType()
        {
            cmbUpdateUserType.Items.Clear();

            cmbUpdateUserType.Items.Add("--- SELECT ---");
            cmbUpdateUserType.Items.Add("SuperAdmin");
            cmbUpdateUserType.Items.Add("Admin");
            cmbUpdateUserType.Items.Add("Authorizer");
            cmbUpdateUserType.Items.Add("Teller");

            cmbUpdateUserType.SelectedIndex = 0;
        }

        private void LoadExistingUsers()
        {
            cmbUpdateUserName.Items.Clear();

            DataTable dtUsrs = mg.GetAllUserList();
            cmbUpdateUserName.Items.Add("--- SELECT ---");

            for (int rw = 0; rw < dtUsrs.Rows.Count; rw++)
            {
                cmbUpdateUserName.Items.Add(dtUsrs.Rows[rw][0].ToString());
            }

            cmbUpdateUserName.SelectedIndex = 0;
        }

        private void LoadNewUserUserType(string loggedUserRole)
        {
            foreach (string userRole in USER_ALLOWED_TO_CONFIG)
            {
                if (loggedUserRole.ToLower().Equals(userRole))
                {
                    cmbNewUserType.Items.Clear();
                    cmbNewUserType.Items.Add("--- SELECT ---");
                    cmbNewUserType.Items.Add("SuperAdmin");
                    cmbNewUserType.Items.Add("Admin");
                    cmbNewUserType.Items.Add("Authorizer");
                    cmbNewUserType.Items.Add("Teller");
                    cmbNewUserType.SelectedIndex = 0;
                }
            }

            //if( loggedUserRole.ToLower().Equals("admin") || loggedUserRole.ToLower().Equals("superadmin"))
            //{
            //    cmbNewUserType.Items.Clear();
            //    cmbNewUserType.Items.Add("--- SELECT ---");
            //    cmbNewUserType.Items.Add("SuperAdmin");
            //    cmbNewUserType.Items.Add("Admin");
            //    cmbNewUserType.Items.Add("Authorizer");
            //    cmbNewUserType.Items.Add("Teller");
            //    cmbNewUserType.SelectedIndex = 0;
            //}
        }

        private void btnSaveNewUser_Click(object sender, EventArgs e)
        {
            string uId = "", uName = "", uPass = "", uType = "", uMail = "";
            int uTypeIndx = 0;

            uId = txtNewUserId.Text.Trim();
            uName = txtNewUserName.Text.Trim();
            uPass = txtNewUserPass.Text.Trim();
            uType = cmbNewUserType.Text;
            uTypeIndx = cmbNewUserType.SelectedIndex;
            uMail = txtNewUserEmail.Text.Trim();


            if (!uId.Equals(""))
            {
                if (!uName.Equals(""))
                {
                    if (!uPass.Equals(""))
                    {
                        if (uTypeIndx != 0)
                        {
                            if (!uMail.Equals(""))
                            {
                                if (!mg.IsThisUserAlreadyExist(uId))
                                {
                                    bool saveStat = mg.SaveNewUserInfo(uId, uName, uPass, uType, uMail);
                                    if(saveStat)
                                    {
                                        MessageBox.Show("User '" + uId + "' Saved Successfully", "Confirmation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        ClearNewUserFields();
                                    }
                                    else
                                    {
                                        MessageBox.Show("Error in User Saving !!! Please Try Later...", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                }
                                else
                                {
                                    MessageBox.Show("User '" + uId + "' Already Exists in the System !!!", "Error In Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                            else
                            {
                                MessageBox.Show("Email address cannot be empty !!!", "Error In Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        else
                        {
                            MessageBox.Show("Please Select User Type !!!", "Error In Selection", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Please Provide Password !!!", "Error In Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Please Provide UserName !!!", "Error In Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Please Provide UserId !!!", "Error In Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void ClearNewUserFields()
        {
            txtNewUserId.Text = "";
            txtNewUserName.Text = "";
            txtNewUserPass.Text = "";
            cmbNewUserType.SelectedIndex = 0;
            txtNewUserEmail.Text = "";
        }

        private void btnReloadUpdateUserName_Click(object sender, EventArgs e)
        {
            LoadExistingUsers();
            LoadAllUserInfo();
        }

        private void btnUpdateUserType_Click(object sender, EventArgs e)
        {
            if (cmbUpdateUserName.SelectedIndex != 0)
            {
                if(cmbUpdateUserType.SelectedIndex != 0)
                {
                    string userId = Convert.ToString(cmbUpdateUserName.Text.Split('-')[0]).Trim();
                    string userType = cmbUpdateUserType.SelectedItem.ToString();

                    bool stat = mg.UpdateUserRoleType(userId, userType);
                    if(stat)
                    {
                        MessageBox.Show("User '" + userId + "' User Role Updated Successfully", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        btnReloadUpdateUserName_Click(sender, e);
                    }
                    else
                    {
                        MessageBox.Show("User Role Update ERROR !!!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Please Select User Type", "Selection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Please Select User", "Selection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnUpdateUserActivity_Click(object sender, EventArgs e)
        {
            if (cmbUpdateUserName.SelectedIndex != 0)
            {
                if (cmbUpdateUserActivity.SelectedIndex != 0)
                {
                    string userId = Convert.ToString(cmbUpdateUserName.Text.Split('-')[0]).Trim();
                    int userActivity = Convert.ToInt32(cmbUpdateUserActivity.Text.Split('-')[0]);

                    bool stat = mg.UpdateUserActivity(userId, userActivity);
                    if (stat)
                    {
                        MessageBox.Show("User '" + userId + "' Activity Updated Successfully", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        btnReloadUpdateUserName_Click(sender, e);
                    }
                    else
                    {
                        MessageBox.Show("User Activity Update ERROR !!!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Please Select User Activity", "Selection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Please Select User", "Selection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnUpdateUserEmail_Click(object sender, EventArgs e)
        {
            if (cmbUpdateUserName.SelectedIndex != 0)
            {
                if (txtUpdateUserEmail.Text.Trim().Length > 0)
                {
                    string userId = Convert.ToString(cmbUpdateUserName.Text.Split('-')[0]).Trim();
                    string userEmail = txtUpdateUserEmail.Text.Trim();

                    bool stat = mg.UpdateUserEmail(userId, userEmail);
                    if (stat)
                    {
                        MessageBox.Show("User '" + userId + "' Email Updated Successfully", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        btnReloadUpdateUserName_Click(sender, e);
                    }
                    else
                    {
                        MessageBox.Show("User Email Update ERROR !!!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Please Input User Email", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Please Select User", "Selection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        
    }
}
