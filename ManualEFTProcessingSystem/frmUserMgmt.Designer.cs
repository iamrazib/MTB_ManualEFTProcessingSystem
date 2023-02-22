namespace ManualEFTProcessingSystem
{
    partial class frmUserMgmt
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            this.label1 = new System.Windows.Forms.Label();
            this.lblLoggedUserInfo = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lblLoggedUserRole = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label12 = new System.Windows.Forms.Label();
            this.btnSaveNewUser = new System.Windows.Forms.Button();
            this.cmbNewUserType = new System.Windows.Forms.ComboBox();
            this.txtNewUserId = new System.Windows.Forms.TextBox();
            this.txtNewUserName = new System.Windows.Forms.TextBox();
            this.txtNewUserEmail = new System.Windows.Forms.TextBox();
            this.txtNewUserPass = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.btnReloadUpdateUserName = new System.Windows.Forms.Button();
            this.btnUpdateUserEmail = new System.Windows.Forms.Button();
            this.txtUpdateUserEmail = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.btnUpdateUserActivity = new System.Windows.Forms.Button();
            this.cmbUpdateUserActivity = new System.Windows.Forms.ComboBox();
            this.label10 = new System.Windows.Forms.Label();
            this.btnUpdateUserType = new System.Windows.Forms.Button();
            this.cmbUpdateUserType = new System.Windows.Forms.ComboBox();
            this.label9 = new System.Windows.Forms.Label();
            this.cmbUpdateUserName = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.dataGridViewUsersInfo = new System.Windows.Forms.DataGridView();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewUsersInfo)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(42, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(75, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Logged in As :";
            // 
            // lblLoggedUserInfo
            // 
            this.lblLoggedUserInfo.AutoSize = true;
            this.lblLoggedUserInfo.Location = new System.Drawing.Point(120, 18);
            this.lblLoggedUserInfo.Name = "lblLoggedUserInfo";
            this.lblLoggedUserInfo.Size = new System.Drawing.Size(93, 13);
            this.lblLoggedUserInfo.TabIndex = 1;
            this.lblLoggedUserInfo.Text = "lblLoggedUserInfo";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(42, 43);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Role :";
            // 
            // lblLoggedUserRole
            // 
            this.lblLoggedUserRole.AutoSize = true;
            this.lblLoggedUserRole.Location = new System.Drawing.Point(120, 43);
            this.lblLoggedUserRole.Name = "lblLoggedUserRole";
            this.lblLoggedUserRole.Size = new System.Drawing.Size(97, 13);
            this.lblLoggedUserRole.TabIndex = 3;
            this.lblLoggedUserRole.Text = "lblLoggedUserRole";
            // 
            // groupBox1
            // 
            this.groupBox1.BackColor = System.Drawing.Color.PapayaWhip;
            this.groupBox1.Controls.Add(this.label12);
            this.groupBox1.Controls.Add(this.btnSaveNewUser);
            this.groupBox1.Controls.Add(this.cmbNewUserType);
            this.groupBox1.Controls.Add(this.txtNewUserId);
            this.groupBox1.Controls.Add(this.txtNewUserName);
            this.groupBox1.Controls.Add(this.txtNewUserEmail);
            this.groupBox1.Controls.Add(this.txtNewUserPass);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Location = new System.Drawing.Point(17, 83);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(405, 195);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "New User ";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(272, 24);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(68, 13);
            this.label12.TabIndex = 11;
            this.label12.Text = "( Ex. C1234 )";
            // 
            // btnSaveNewUser
            // 
            this.btnSaveNewUser.Location = new System.Drawing.Point(114, 155);
            this.btnSaveNewUser.Name = "btnSaveNewUser";
            this.btnSaveNewUser.Size = new System.Drawing.Size(143, 28);
            this.btnSaveNewUser.TabIndex = 5;
            this.btnSaveNewUser.Text = "Save";
            this.btnSaveNewUser.UseVisualStyleBackColor = true;
            this.btnSaveNewUser.Click += new System.EventHandler(this.btnSaveNewUser_Click);
            // 
            // cmbNewUserType
            // 
            this.cmbNewUserType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbNewUserType.FormattingEnabled = true;
            this.cmbNewUserType.Location = new System.Drawing.Point(114, 98);
            this.cmbNewUserType.Name = "cmbNewUserType";
            this.cmbNewUserType.Size = new System.Drawing.Size(152, 21);
            this.cmbNewUserType.TabIndex = 3;
            // 
            // txtNewUserId
            // 
            this.txtNewUserId.Location = new System.Drawing.Point(114, 21);
            this.txtNewUserId.Name = "txtNewUserId";
            this.txtNewUserId.Size = new System.Drawing.Size(152, 20);
            this.txtNewUserId.TabIndex = 0;
            // 
            // txtNewUserName
            // 
            this.txtNewUserName.Location = new System.Drawing.Point(114, 47);
            this.txtNewUserName.Name = "txtNewUserName";
            this.txtNewUserName.Size = new System.Drawing.Size(250, 20);
            this.txtNewUserName.TabIndex = 1;
            // 
            // txtNewUserEmail
            // 
            this.txtNewUserEmail.Location = new System.Drawing.Point(114, 124);
            this.txtNewUserEmail.Name = "txtNewUserEmail";
            this.txtNewUserEmail.Size = new System.Drawing.Size(250, 20);
            this.txtNewUserEmail.TabIndex = 4;
            // 
            // txtNewUserPass
            // 
            this.txtNewUserPass.Location = new System.Drawing.Point(114, 73);
            this.txtNewUserPass.Name = "txtNewUserPass";
            this.txtNewUserPass.Size = new System.Drawing.Size(152, 20);
            this.txtNewUserPass.TabIndex = 2;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(24, 127);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(63, 13);
            this.label7.TabIndex = 4;
            this.label7.Text = "User Email :";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(24, 101);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(62, 13);
            this.label6.TabIndex = 3;
            this.label6.Text = "User Type :";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(24, 76);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(84, 13);
            this.label5.TabIndex = 2;
            this.label5.Text = "User Password :";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(24, 49);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(66, 13);
            this.label4.TabIndex = 1;
            this.label4.Text = "User Name :";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(24, 24);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(47, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "User Id :";
            // 
            // groupBox2
            // 
            this.groupBox2.BackColor = System.Drawing.Color.Beige;
            this.groupBox2.Controls.Add(this.btnReloadUpdateUserName);
            this.groupBox2.Controls.Add(this.btnUpdateUserEmail);
            this.groupBox2.Controls.Add(this.txtUpdateUserEmail);
            this.groupBox2.Controls.Add(this.label11);
            this.groupBox2.Controls.Add(this.btnUpdateUserActivity);
            this.groupBox2.Controls.Add(this.cmbUpdateUserActivity);
            this.groupBox2.Controls.Add(this.label10);
            this.groupBox2.Controls.Add(this.btnUpdateUserType);
            this.groupBox2.Controls.Add(this.cmbUpdateUserType);
            this.groupBox2.Controls.Add(this.label9);
            this.groupBox2.Controls.Add(this.cmbUpdateUserName);
            this.groupBox2.Controls.Add(this.label8);
            this.groupBox2.Location = new System.Drawing.Point(444, 83);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(471, 195);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Update User";
            // 
            // btnReloadUpdateUserName
            // 
            this.btnReloadUpdateUserName.Location = new System.Drawing.Point(303, 23);
            this.btnReloadUpdateUserName.Name = "btnReloadUpdateUserName";
            this.btnReloadUpdateUserName.Size = new System.Drawing.Size(70, 23);
            this.btnReloadUpdateUserName.TabIndex = 20;
            this.btnReloadUpdateUserName.Text = "Reload";
            this.btnReloadUpdateUserName.UseVisualStyleBackColor = true;
            this.btnReloadUpdateUserName.Click += new System.EventHandler(this.btnReloadUpdateUserName_Click);
            // 
            // btnUpdateUserEmail
            // 
            this.btnUpdateUserEmail.Location = new System.Drawing.Point(343, 109);
            this.btnUpdateUserEmail.Name = "btnUpdateUserEmail";
            this.btnUpdateUserEmail.Size = new System.Drawing.Size(115, 23);
            this.btnUpdateUserEmail.TabIndex = 6;
            this.btnUpdateUserEmail.Text = "Update Email";
            this.btnUpdateUserEmail.UseVisualStyleBackColor = true;
            this.btnUpdateUserEmail.Click += new System.EventHandler(this.btnUpdateUserEmail_Click);
            // 
            // txtUpdateUserEmail
            // 
            this.txtUpdateUserEmail.Location = new System.Drawing.Point(84, 111);
            this.txtUpdateUserEmail.Name = "txtUpdateUserEmail";
            this.txtUpdateUserEmail.Size = new System.Drawing.Size(250, 20);
            this.txtUpdateUserEmail.TabIndex = 5;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(16, 114);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(63, 13);
            this.label11.TabIndex = 19;
            this.label11.Text = "User Email :";
            // 
            // btnUpdateUserActivity
            // 
            this.btnUpdateUserActivity.Location = new System.Drawing.Point(260, 81);
            this.btnUpdateUserActivity.Name = "btnUpdateUserActivity";
            this.btnUpdateUserActivity.Size = new System.Drawing.Size(132, 23);
            this.btnUpdateUserActivity.TabIndex = 4;
            this.btnUpdateUserActivity.Text = "Update Activity";
            this.btnUpdateUserActivity.UseVisualStyleBackColor = true;
            this.btnUpdateUserActivity.Click += new System.EventHandler(this.btnUpdateUserActivity_Click);
            // 
            // cmbUpdateUserActivity
            // 
            this.cmbUpdateUserActivity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbUpdateUserActivity.FormattingEnabled = true;
            this.cmbUpdateUserActivity.Location = new System.Drawing.Point(84, 81);
            this.cmbUpdateUserActivity.Name = "cmbUpdateUserActivity";
            this.cmbUpdateUserActivity.Size = new System.Drawing.Size(152, 21);
            this.cmbUpdateUserActivity.TabIndex = 3;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(16, 84);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(54, 13);
            this.label10.TabIndex = 16;
            this.label10.Text = "Is Active :";
            // 
            // btnUpdateUserType
            // 
            this.btnUpdateUserType.Location = new System.Drawing.Point(260, 54);
            this.btnUpdateUserType.Name = "btnUpdateUserType";
            this.btnUpdateUserType.Size = new System.Drawing.Size(132, 23);
            this.btnUpdateUserType.TabIndex = 2;
            this.btnUpdateUserType.Text = "Update Type";
            this.btnUpdateUserType.UseVisualStyleBackColor = true;
            this.btnUpdateUserType.Click += new System.EventHandler(this.btnUpdateUserType_Click);
            // 
            // cmbUpdateUserType
            // 
            this.cmbUpdateUserType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbUpdateUserType.FormattingEnabled = true;
            this.cmbUpdateUserType.Location = new System.Drawing.Point(84, 54);
            this.cmbUpdateUserType.Name = "cmbUpdateUserType";
            this.cmbUpdateUserType.Size = new System.Drawing.Size(152, 21);
            this.cmbUpdateUserType.TabIndex = 1;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(16, 57);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(62, 13);
            this.label9.TabIndex = 13;
            this.label9.Text = "User Type :";
            // 
            // cmbUpdateUserName
            // 
            this.cmbUpdateUserName.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbUpdateUserName.FormattingEnabled = true;
            this.cmbUpdateUserName.Location = new System.Drawing.Point(60, 24);
            this.cmbUpdateUserName.Name = "cmbUpdateUserName";
            this.cmbUpdateUserName.Size = new System.Drawing.Size(236, 21);
            this.cmbUpdateUserName.TabIndex = 0;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(16, 27);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(35, 13);
            this.label8.TabIndex = 10;
            this.label8.Text = "User :";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.dataGridViewUsersInfo);
            this.groupBox3.Location = new System.Drawing.Point(17, 284);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(898, 211);
            this.groupBox3.TabIndex = 4;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Users Info";
            // 
            // dataGridViewUsersInfo
            // 
            this.dataGridViewUsersInfo.AllowUserToAddRows = false;
            this.dataGridViewUsersInfo.AllowUserToDeleteRows = false;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridViewUsersInfo.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dataGridViewUsersInfo.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dataGridViewUsersInfo.DefaultCellStyle = dataGridViewCellStyle2;
            this.dataGridViewUsersInfo.Location = new System.Drawing.Point(10, 19);
            this.dataGridViewUsersInfo.Name = "dataGridViewUsersInfo";
            this.dataGridViewUsersInfo.ReadOnly = true;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridViewUsersInfo.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.dataGridViewUsersInfo.Size = new System.Drawing.Size(879, 186);
            this.dataGridViewUsersInfo.TabIndex = 6;
            // 
            // frmUserMgmt
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(953, 507);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.lblLoggedUserRole);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lblLoggedUserInfo);
            this.Controls.Add(this.label1);
            this.MaximizeBox = false;
            this.Name = "frmUserMgmt";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "User Management";
            this.Load += new System.EventHandler(this.frmUserMgmt_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewUsersInfo)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblLoggedUserInfo;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblLoggedUserRole;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox cmbNewUserType;
        private System.Windows.Forms.TextBox txtNewUserId;
        private System.Windows.Forms.TextBox txtNewUserName;
        private System.Windows.Forms.TextBox txtNewUserEmail;
        private System.Windows.Forms.TextBox txtNewUserPass;
        private System.Windows.Forms.Button btnSaveNewUser;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.ComboBox cmbUpdateUserName;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ComboBox cmbUpdateUserType;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Button btnUpdateUserActivity;
        private System.Windows.Forms.ComboBox cmbUpdateUserActivity;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Button btnUpdateUserType;
        private System.Windows.Forms.TextBox txtUpdateUserEmail;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Button btnUpdateUserEmail;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Button btnReloadUpdateUserName;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.DataGridView dataGridViewUsersInfo;
    }
}