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
using Office = Microsoft.Office.Core;
using Excel = Microsoft.Office.Interop.Excel;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Globalization;
using ManualEFTProcessingSystem.MTBRemittanceService;
//using ManualEFTProcessingSystem.UATRemitServiceReference;
using System.Xml;

/*
    Changes::
 *  24-Oct-2021 :  Inside ValidateTxn() EFT account number length check added
 *                 Constant value added at top 
 *  08-Nov-2021 : Modify Manager class, Received & Processed txn will check for uploading   
 *  05-Dec-2021 : Wrong MTB account number deleted/modified option added
 *  09-Dec-2021 : 'Failed Txn screen' Txn status update portion added at lower part
 *  12-Dec-2021 : User Management screen added to confiugure user role, activity, email and new user creation
 *  05-Jul-2022 : Copy Error list added
 *  31-Aug-2022 : Hold & Cancel tab added
 *  11-Oct-2022 : Email issue resolved, default IP set. Teller Total amount displayed
 */

namespace ManualEFTProcessingSystem
{
    public partial class Form1 : Form
    {
        static Manager mg = new Manager();
        public string loggedUser = "";
        public string loggedUserIdAndName = "";

        public string userType = "";
        public string isPassChanged = "";
        DataTable tellerScreenExhFileData = new DataTable();
        DataTable exhFileDataToAuthorize = new DataTable();
        DataTable reportData = new DataTable();
        string batchIds;

        static int EFT_ACCOUNT_NUMBER_MAX_LENGTH = 17;
        static int EFT_ROUTING_NUMBER_MAX_LENGTH = 9;

        //API LIVE
        static RemitServiceSoapClient remitServiceClient = new RemitServiceSoapClient();

        //API UAT
        //static RemitServiceSoapClient remitServiceClient = new RemitServiceSoapClient();


        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (isPassChanged.Equals("N"))
            {
                frmChangePassword fcp = new frmChangePassword();
                fcp.loggedUserChPass = this.loggedUser;
                fcp.loggedUserIdAndNameChPass = this.loggedUserIdAndName;
                fcp.ShowDialog();
            }

            LoadExhouseList();
            lblRowCount.Text = "";
            this.Text = "Version-1.02 => Logged in as: " + loggedUserIdAndName + " ; [ Server: " + mg.nrbworkConnectionString.Split(';')[0].Split('=')[1] + " - Database: " + mg.nrbworkConnectionString.Split(';')[1].Split('=')[1] + " ]";

            lblReportRowCount.Text = "";
            lblRptDataSaveProgress.Text = "";

            btnDownloadReport.Enabled = false;
            btnDownloadFailedReport.Enabled = false;

            //string batchId = "637694677028980381";
            //string uploadedUserId = "C2781";
            //SendTransactionAuthStatusMailToTeller(batchId, uploadedUserId);

            btnUpdateAccountNumber.Enabled = false;
            lblRowCountPending.Text = "";
            lblRowCountFailedTxn.Text = "";
            lblFileTxnLoadingMsg.Text = "";
        }

        private void LoadExhouseList()
        {
            DataTable dtExchs = mg.LoadExhouseList();
            cbExh.Items.Clear();
            cbExh.Items.Add("--Select--");

            cmbExhAuthList.Items.Clear();
            cmbExhAuthList.Items.Add("--Select--");

            for (int rows = 0; rows < dtExchs.Rows.Count; rows++)
            {
                cbExh.Items.Add(dtExchs.Rows[rows][0]);
                cmbExhAuthList.Items.Add(dtExchs.Rows[rows][0]);
            }

            cbExh.SelectedIndex = 0;
            cmbExhAuthList.SelectedIndex = 0;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Process[] AllProcesses = Process.GetProcessesByName("excel");
            //foreach (Process ExcelProcess in AllProcesses)
            //{
            //    {
            //        ExcelProcess.Kill();
            //    }
            //}

            Application.Exit();
        }

        private void btnBrowseFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                Title = "Browse Files",
                RestoreDirectory = true,
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBoxExhFile.Text = openFileDialog1.FileName;
                btnLoadExcelFile.Enabled = true;
            }
        }

        private void btnLoadExcelFile_Click(object sender, EventArgs e)
        {
            if (!textBoxExhFile.Text.Trim().Equals(""))
            {
                Cursor.Current = Cursors.WaitCursor;

                if (cbExh.SelectedIndex != 0)
                {
                    btnLoadExcelFile.Enabled = false;
                    string exhName = cbExh.Text.Split('-')[1].Trim();

                    tellerScreenExhFileData = GetExchangeDataFromExcel(textBoxExhFile.Text, exhName);

                    dataGridViewExhDataTellerScreen.DataSource = null;
                    dataGridViewExhDataTellerScreen.DataSource = tellerScreenExhFileData;

                    double totalAmount = 0;
                    foreach (DataGridViewRow eftrow in dataGridViewExhDataTellerScreen.Rows)
                    {
                        totalAmount += Math.Round(Convert.ToDouble(eftrow.Cells["Amount"].Value), 2);
                    }


                    btnLoadExcelFile.Enabled = true;
                    lblFileTxnLoadingMsg.Text = tellerScreenExhFileData.Rows.Count + " Txn Loaded, TotalAmount: " + totalAmount;

                    string rtNum = "";
                    foreach (DataGridViewRow eftrow in dataGridViewExhDataTellerScreen.Rows)
                    {
                        rtNum = eftrow.Cells["RoutingNo"].Value.ToString().Trim();

                        if (rtNum.Equals(""))
                        {
                            eftrow.DefaultCellStyle.BackColor = Color.Yellow;
                        }
                        else if (rtNum.Length != 9)
                        {
                            eftrow.DefaultCellStyle.BackColor = Color.Yellow;
                        }
                    }

                }
                else
                {
                    MessageBox.Show("Please Select Exchange House...", "Selection Problem", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                Cursor.Current = Cursors.Default;
            }
        }

        private DataTable GetExchangeDataFromExcel(string filePath, string exhName)
        {
            DataRow drow;
            DataTable dtFile = CreateDataTableCommon();
            listBoxTellerProgress.Items.Clear();

            try
            {
                Excel.Application xlAppDataFile = null;
                Excel.Workbooks xlWorkBooksDataFile = null;
                Excel.Workbook xlWorkBookDataFile = null;
                Excel.Sheets _xlSheetsDataFile = null;
                Excel.Worksheet xlWorkSheetDataFile = null;
                Excel.Range rangeDataFile = null;

                xlAppDataFile = new Excel.Application();
                xlWorkBooksDataFile = xlAppDataFile.Workbooks;
                xlWorkBookDataFile = xlWorkBooksDataFile.Open(filePath, 0, true, 5, "", "", true, Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);
                _xlSheetsDataFile = xlWorkBookDataFile.Worksheets;
                xlWorkSheetDataFile = _xlSheetsDataFile.get_Item(1);

                rangeDataFile = xlWorkSheetDataFile.UsedRange;

                int rw = rangeDataFile.Rows.Count;
                int cl = rangeDataFile.Columns.Count;
                int rCnt;
                string pinno, bankNm = "", accountNo = "";

                if (exhName.Contains("GHURAIR"))
                {
                    for (rCnt = 2; rCnt <= rw; rCnt++)
                    {
                        pinno = Convert.ToString(rangeDataFile.Cells[rCnt, 2].Value2);

                        if (pinno != null)
                        {
                            drow = dtFile.NewRow();

                            drow["RefNo"] = Convert.ToString(rangeDataFile.Cells[rCnt, 2].Value2);
                            drow["BeneficiaryName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 3].Value2);
                            accountNo = Convert.ToString(rangeDataFile.Cells[rCnt, 4].Value2);
                            accountNo = (accountNo == null || accountNo.Equals("")) ? "" : accountNo;
                            drow["AccountNo"] = accountNo;
                            drow["BankName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 5].Value2);
                            drow["BranchName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 6].Value2);
                            drow["RoutingNo"] = Convert.ToString(rangeDataFile.Cells[rCnt, 11].Value2);
                            drow["Amount"] = Math.Round(Convert.ToDouble(rangeDataFile.Cells[rCnt, 7].Value2), 2);
                            drow["BeneficiaryAddress"] = "";
                            drow["RemitterName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 8].Value2);
                            drow["RemitterAddress"] = Convert.ToString(rangeDataFile.Cells[rCnt, 9].Value2);
                            drow["BeneficiaryContactNo"] = Convert.ToString(rangeDataFile.Cells[rCnt, 10].Value2);
                            drow["Purpose"] = "";

                            bankNm = Convert.ToString(rangeDataFile.Cells[rCnt, 5].Value2);

                            if (accountNo.Trim().Equals("") || accountNo.Trim().ToLower().Contains("coc") || accountNo.Trim().ToLower().Contains("cash"))
                            {
                                drow["PayMode"] = "CASH";
                            }
                            else if (!accountNo.Trim().Equals("") && (bankNm.ToUpper().Contains("MUTUAL") || bankNm.ToUpper().Contains("MTB")))
                            {
                                drow["PayMode"] = "MTB";
                            }
                            else
                            {
                                drow["PayMode"] = "EFT";
                            }

                            dtFile.Rows.Add(drow);
                            lblFileTxnLoadingMsg.Text = "Adding Txn " + drow["RefNo"].ToString() + " [ remain " + (rw - rCnt) + " ]";
                            listBoxTellerProgress.Items.Insert(0, drow["RefNo"].ToString().Trim() + " - Added into list - " + DateTime.Now);
                        }
                        Application.DoEvents();
                    }
                }
                else if (exhName.Contains("GULF"))
                {
                    for (rCnt = 2; rCnt <= rw; rCnt++)
                    {
                        pinno = Convert.ToString(rangeDataFile.Cells[rCnt, 1].Value2);

                        if (pinno != null)
                        {
                            drow = dtFile.NewRow();

                            drow["RefNo"] = Convert.ToString(rangeDataFile.Cells[rCnt, 1].Value2);
                            drow["BeneficiaryName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 4].Value2);
                            accountNo = Convert.ToString(rangeDataFile.Cells[rCnt, 6].Value2);
                            accountNo = (accountNo == null || accountNo.Equals("")) ? "" : accountNo;
                            drow["AccountNo"] = accountNo;
                            drow["BankName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 8].Value2);
                            drow["BranchName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 9].Value2);
                            drow["RoutingNo"] = Convert.ToString(rangeDataFile.Cells[rCnt, 11].Value2);
                            drow["Amount"] = Math.Round(Convert.ToDouble(rangeDataFile.Cells[rCnt, 10].Value2), 2);
                            drow["BeneficiaryAddress"] = Convert.ToString(rangeDataFile.Cells[rCnt, 5].Value2);
                            drow["RemitterName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 2].Value2);
                            drow["RemitterAddress"] = Convert.ToString(rangeDataFile.Cells[rCnt, 3].Value2);
                            drow["Purpose"] = Convert.ToString(rangeDataFile.Cells[rCnt, 13].Value2);
                            drow["BeneficiaryContactNo"] = "";

                            bankNm = Convert.ToString(rangeDataFile.Cells[rCnt, 8].Value2);
                            if (accountNo.Trim().Equals("") || accountNo.Trim().ToLower().Contains("coc") || accountNo.Trim().ToLower().Contains("cash"))
                            {
                                drow["PayMode"] = "CASH";
                            }
                            else if (!accountNo.Trim().Equals("") && (bankNm.ToUpper().Contains("MUTUAL") || bankNm.ToUpper().Contains("MTB")))
                            {
                                drow["PayMode"] = "MTB";
                            }
                            else
                            {
                                drow["PayMode"] = "EFT";
                            }

                            dtFile.Rows.Add(drow);
                            lblFileTxnLoadingMsg.Text = "Adding Txn " + drow["RefNo"].ToString() + " [ remain " + (rw - rCnt) + " ]";
                            listBoxTellerProgress.Items.Insert(0, drow["RefNo"].ToString().Trim() + " - Added into list - " + DateTime.Now);
                        }
                        Application.DoEvents();
                    }
                }
                else if (exhName.Contains("ISLAMIC"))
                {
                    for (rCnt = 2; rCnt <= rw; rCnt++)
                    {
                        pinno = Convert.ToString(rangeDataFile.Cells[rCnt, 1].Value2);

                        if (pinno != null)
                        {
                            drow = dtFile.NewRow();

                            drow["RefNo"] = Convert.ToString(rangeDataFile.Cells[rCnt, 1].Value2);
                            drow["BeneficiaryName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 4].Value2);
                            accountNo = Convert.ToString(rangeDataFile.Cells[rCnt, 6].Value2);
                            accountNo = (accountNo == null || accountNo.Equals("")) ? "" : accountNo;
                            drow["AccountNo"] = accountNo;
                            drow["BankName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 8].Value2);
                            drow["BranchName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 9].Value2);
                            drow["RoutingNo"] = Convert.ToString(rangeDataFile.Cells[rCnt, 10].Value2);
                            drow["Amount"] = Math.Round(Convert.ToDouble(rangeDataFile.Cells[rCnt, 11].Value2), 2);
                            drow["BeneficiaryAddress"] = Convert.ToString(rangeDataFile.Cells[rCnt, 5].Value2);
                            drow["RemitterName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 2].Value2);
                            drow["RemitterAddress"] = Convert.ToString(rangeDataFile.Cells[rCnt, 3].Value2);
                            drow["Purpose"] = Convert.ToString(rangeDataFile.Cells[rCnt, 12].Value2);
                            drow["BeneficiaryContactNo"] = "";

                            bankNm = Convert.ToString(rangeDataFile.Cells[rCnt, 8].Value2);
                            if (accountNo.Trim().Equals("") || accountNo.Trim().ToLower().Contains("coc") || accountNo.Trim().ToLower().Contains("cash"))
                            {
                                drow["PayMode"] = "CASH";
                            }
                            else if (!accountNo.Trim().Equals("") && (bankNm.ToUpper().Contains("MUTUAL") || bankNm.ToUpper().Contains("MTB")))
                            {
                                drow["PayMode"] = "MTB";
                            }
                            else
                            {
                                drow["PayMode"] = "EFT";
                            }

                            dtFile.Rows.Add(drow);
                            lblFileTxnLoadingMsg.Text = "Adding Txn " + drow["RefNo"].ToString() + " [ remain " + (rw - rCnt) + " ]";
                            listBoxTellerProgress.Items.Insert(0, drow["RefNo"].ToString().Trim() + " - Added into list - " + DateTime.Now);
                        }
                        Application.DoEvents();
                    }
                }
                else if (exhName.Contains("UAE EXCHANGE"))
                {
                    for (rCnt = 2; rCnt <= rw; rCnt++)
                    {
                        pinno = Convert.ToString(rangeDataFile.Cells[rCnt, 1].Value2);

                        if (pinno != null)
                        {
                            drow = dtFile.NewRow();

                            drow["RefNo"] = Convert.ToString(rangeDataFile.Cells[rCnt, 1].Value2);
                            drow["BeneficiaryName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 5].Value2);
                            accountNo = Convert.ToString(rangeDataFile.Cells[rCnt, 6].Value2);
                            accountNo = (accountNo == null || accountNo.Equals("")) ? "" : accountNo;
                            drow["AccountNo"] = accountNo;
                            drow["BankName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 7].Value2);
                            drow["BranchName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 8].Value2);
                            drow["RoutingNo"] = Convert.ToString(rangeDataFile.Cells[rCnt, 9].Value2);
                            drow["Amount"] = Math.Round(Convert.ToDouble(rangeDataFile.Cells[rCnt, 11].Value2), 2);
                            drow["BeneficiaryAddress"] = Convert.ToString(rangeDataFile.Cells[rCnt, 3].Value2);
                            drow["RemitterName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 2].Value2);
                            drow["RemitterAddress"] = Convert.ToString(rangeDataFile.Cells[rCnt, 4].Value2);
                            drow["Purpose"] = "Family Maintenance";
                            drow["BeneficiaryContactNo"] = "";

                            bankNm = Convert.ToString(rangeDataFile.Cells[rCnt, 7].Value2);
                            if (accountNo.Trim().Equals("") || accountNo.Trim().ToLower().Contains("coc") || accountNo.Trim().ToLower().Contains("cash"))
                            {
                                drow["PayMode"] = "CASH";
                            }
                            else if (!accountNo.Trim().Equals("") && (bankNm.ToUpper().Contains("MUTUAL") || bankNm.ToUpper().Contains("MTB")))
                            {
                                drow["PayMode"] = "MTB";
                            }
                            else
                            {
                                drow["PayMode"] = "EFT";
                            }

                            dtFile.Rows.Add(drow);

                            lblFileTxnLoadingMsg.Text = "Adding Txn " + drow["RefNo"].ToString() + " [ remain " + (rw - rCnt) + " ]";
                            listBoxTellerProgress.Items.Insert(0, drow["RefNo"].ToString().Trim() + " - Added into list - " + DateTime.Now);
                        }
                        Application.DoEvents();
                    }
                }
                else if (exhName.Contains("DOHA"))
                {
                    for (rCnt = 6; rCnt <= rw; rCnt++)
                    {
                        pinno = Convert.ToString(rangeDataFile.Cells[rCnt, 1].Value2);

                        if (pinno != null)
                        {
                            drow = dtFile.NewRow();

                            drow["RefNo"] = Convert.ToString(rangeDataFile.Cells[rCnt, 1].Value2);
                            drow["BeneficiaryName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 6].Value2);
                            accountNo = Convert.ToString(rangeDataFile.Cells[rCnt, 7].Value2);
                            accountNo = (accountNo == null || accountNo.Equals("")) ? "" : accountNo;
                            drow["AccountNo"] = accountNo;
                            drow["BankName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 8].Value2);
                            drow["BranchName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 9].Value2);
                            drow["RoutingNo"] = Convert.ToString(rangeDataFile.Cells[rCnt, 10].Value2);
                            drow["Amount"] = Math.Round(Convert.ToDouble(rangeDataFile.Cells[rCnt, 12].Value2), 2);
                            drow["BeneficiaryAddress"] = Convert.ToString(rangeDataFile.Cells[rCnt, 3].Value2);
                            drow["RemitterName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 2].Value2);
                            drow["RemitterAddress"] = Convert.ToString(rangeDataFile.Cells[rCnt, 4].Value2);
                            drow["Purpose"] = "Family Maintenance";
                            drow["BeneficiaryContactNo"] = "";

                            bankNm = Convert.ToString(rangeDataFile.Cells[rCnt, 8].Value2);
                            if (accountNo.Trim().Equals("") || accountNo.Trim().ToLower().Contains("coc") || accountNo.Trim().ToLower().Contains("cash"))
                            {
                                drow["PayMode"] = "CASH";
                            }
                            else if (!accountNo.Trim().Equals("") && (bankNm.ToUpper().Contains("MUTUAL") || bankNm.ToUpper().Contains("MTB")))
                            {
                                drow["PayMode"] = "MTB";
                            }
                            else
                            {
                                drow["PayMode"] = "EFT";
                            }

                            dtFile.Rows.Add(drow);

                            lblFileTxnLoadingMsg.Text = "Adding Txn " + drow["RefNo"].ToString() + " [ remain " + (rw - rCnt) + " ]";
                            listBoxTellerProgress.Items.Insert(0, drow["RefNo"].ToString().Trim() + " - Added into list - " + DateTime.Now);
                        }
                        Application.DoEvents();
                    }
                }
                else if (exhName.Contains("ZAMAN"))
                {
                    for (rCnt = 2; rCnt <= rw; rCnt++)
                    {
                        pinno = Convert.ToString(rangeDataFile.Cells[rCnt, 2].Value2);

                        if (pinno != null)
                        {
                            drow = dtFile.NewRow();

                            drow["RefNo"] = Convert.ToString(rangeDataFile.Cells[rCnt, 2].Value2);
                            drow["BeneficiaryName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 3].Value2);
                            accountNo = Convert.ToString(rangeDataFile.Cells[rCnt, 4].Value2);
                            accountNo = (accountNo == null || accountNo.Equals("")) ? "" : accountNo;
                            drow["AccountNo"] = accountNo;
                            drow["BankName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 5].Value2);
                            drow["BranchName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 6].Value2);
                            drow["RoutingNo"] = Convert.ToString(rangeDataFile.Cells[rCnt, 11].Value2);
                            drow["Amount"] = Math.Round(Convert.ToDouble(rangeDataFile.Cells[rCnt, 7].Value2), 2);
                            drow["BeneficiaryAddress"] = Convert.ToString(rangeDataFile.Cells[rCnt, 9].Value2);
                            drow["RemitterName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 8].Value2);
                            drow["RemitterAddress"] = Convert.ToString(rangeDataFile.Cells[rCnt, 9].Value2);
                            drow["Purpose"] = "Family Maintenance";
                            drow["BeneficiaryContactNo"] = Convert.ToString(rangeDataFile.Cells[rCnt, 10].Value2);

                            bankNm = Convert.ToString(rangeDataFile.Cells[rCnt, 5].Value2);
                            if (accountNo.Trim().Equals("") || accountNo.Trim().ToLower().Contains("coc") || accountNo.Trim().ToLower().Contains("cash"))
                            {
                                drow["PayMode"] = "CASH";
                            }
                            else if (!accountNo.Trim().Equals("") && (bankNm.ToUpper().Contains("MUTUAL") || bankNm.ToUpper().Contains("MTB")))
                            {
                                drow["PayMode"] = "MTB";
                            }
                            else
                            {
                                drow["PayMode"] = "EFT";
                            }

                            dtFile.Rows.Add(drow);

                            lblFileTxnLoadingMsg.Text = "Adding Txn " + drow["RefNo"].ToString() + " [ remain " + (rw - rCnt) + " ]";
                            listBoxTellerProgress.Items.Insert(0, drow["RefNo"].ToString().Trim() + " - Added into list - " + DateTime.Now);
                        }
                        Application.DoEvents();
                    }
                }
                else if (exhName.Contains("UNIVERSAL"))
                {
                    for (rCnt = 2; rCnt <= rw; rCnt++)
                    {
                        pinno = Convert.ToString(rangeDataFile.Cells[rCnt, 1].Value2);

                        if (pinno != null)
                        {
                            drow = dtFile.NewRow();

                            drow["RefNo"] = Convert.ToString(rangeDataFile.Cells[rCnt, 1].Value2);
                            drow["BeneficiaryName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 4].Value2);
                            accountNo = Convert.ToString(rangeDataFile.Cells[rCnt, 6].Value2);
                            accountNo = (accountNo == null || accountNo.Equals("")) ? "" : accountNo;
                            drow["AccountNo"] = accountNo;
                            drow["BankName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 8].Value2);
                            drow["BranchName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 9].Value2);
                            drow["RoutingNo"] = Convert.ToString(rangeDataFile.Cells[rCnt, 10].Value2);
                            drow["Amount"] = Math.Round(Convert.ToDouble(rangeDataFile.Cells[rCnt, 11].Value2), 2);
                            drow["BeneficiaryAddress"] = Convert.ToString(rangeDataFile.Cells[rCnt, 5].Value2);
                            drow["RemitterName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 2].Value2);
                            drow["RemitterAddress"] = Convert.ToString(rangeDataFile.Cells[rCnt, 3].Value2);
                            drow["Purpose"] = "Family Maintenance";
                            drow["BeneficiaryContactNo"] = "";

                            bankNm = Convert.ToString(rangeDataFile.Cells[rCnt, 8].Value2);
                            if (accountNo.Trim().Equals("") || accountNo.Trim().ToLower().Contains("coc") || accountNo.Trim().ToLower().Contains("cash"))
                            {
                                drow["PayMode"] = "CASH";
                            }
                            else if (!accountNo.Trim().Equals("") && (bankNm.ToUpper().Contains("MUTUAL") || bankNm.ToUpper().Contains("MTB")))
                            {
                                drow["PayMode"] = "MTB";
                            }
                            else
                            {
                                drow["PayMode"] = "EFT";
                            }

                            dtFile.Rows.Add(drow);

                            lblFileTxnLoadingMsg.Text = "Adding Txn " + drow["RefNo"].ToString() + " [ remain " + (rw - rCnt) + " ]";
                            listBoxTellerProgress.Items.Insert(0, drow["RefNo"].ToString().Trim() + " - Added into list - " + DateTime.Now);
                        }
                        Application.DoEvents();
                    }
                }
                if (exhName.Contains("INSTANT CASH"))
                {
                    for (rCnt = 6; rCnt <= rw; rCnt++)
                    {
                        pinno = Convert.ToString(rangeDataFile.Cells[rCnt, 1].Value2);
                        int pinLength = pinno != null ? pinno.Length : 0;

                        if (pinLength > 0) //(pinno != null || pinno.Length>0)
                        {
                            drow = dtFile.NewRow();

                            drow["RefNo"] = Convert.ToString(rangeDataFile.Cells[rCnt, 1].Value2);
                            drow["BeneficiaryName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 4].Value2);
                            accountNo = Convert.ToString(rangeDataFile.Cells[rCnt, 6].Value2);
                            accountNo = (accountNo == null || accountNo.Equals("")) ? "" : accountNo;
                            drow["AccountNo"] = accountNo;
                            drow["BankName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 8].Value2);
                            drow["BranchName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 9].Value2);
                            drow["RoutingNo"] = Convert.ToString(rangeDataFile.Cells[rCnt, 10].Value2);
                            drow["Amount"] = Math.Round(Convert.ToDouble(rangeDataFile.Cells[rCnt, 11].Value2), 2);
                            drow["BeneficiaryAddress"] = Convert.ToString(rangeDataFile.Cells[rCnt, 5].Value2);
                            drow["RemitterName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 2].Value2);
                            drow["RemitterAddress"] = Convert.ToString(rangeDataFile.Cells[rCnt, 3].Value2);
                            drow["Purpose"] = "Family Maintenance";
                            drow["BeneficiaryContactNo"] = Convert.ToString(rangeDataFile.Cells[rCnt, 14].Value2);

                            bankNm = Convert.ToString(rangeDataFile.Cells[rCnt, 8].Value2);
                            if (accountNo.Trim().Equals("") || accountNo.Trim().ToLower().Contains("coc") || accountNo.Trim().ToLower().Contains("cash"))
                            {
                                drow["PayMode"] = "CASH";
                            }
                            else if (!accountNo.Trim().Equals("") && (bankNm.ToUpper().Contains("MUTUAL") || bankNm.ToUpper().Contains("MTB")))
                            {
                                drow["PayMode"] = "MTB";
                            }
                            else
                            {
                                drow["PayMode"] = "EFT";
                            }
                            dtFile.Rows.Add(drow);

                            lblFileTxnLoadingMsg.Text = "Adding Txn " + drow["RefNo"].ToString() + " [ remain " + (rw - rCnt) + " ]";
                            listBoxTellerProgress.Items.Insert(0, drow["RefNo"].ToString().Trim() + " - Added into list - " + DateTime.Now);
                        }
                        Application.DoEvents();
                    }
                }

                if (exhName.Contains("AHALIA"))
                {
                    for (rCnt = 11; rCnt <= rw; rCnt++)
                    {
                        pinno = Convert.ToString(rangeDataFile.Cells[rCnt, 2].Value2);
                        int pinLength = pinno != null ? pinno.Length : 0;

                        if (pinLength > 0) //(pinno != null || pinno.Length>0)
                        {
                            drow = dtFile.NewRow();

                            drow["RefNo"] = Convert.ToString(rangeDataFile.Cells[rCnt, 2].Value2);
                            drow["BeneficiaryName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 5].Value2);
                            accountNo = Convert.ToString(rangeDataFile.Cells[rCnt, 6].Value2);
                            accountNo = (accountNo == null || accountNo.Equals("")) ? "" : accountNo;
                            drow["AccountNo"] = accountNo;
                            drow["BankName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 7].Value2);
                            drow["BranchName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 8].Value2);
                            drow["RoutingNo"] = Convert.ToString(rangeDataFile.Cells[rCnt, 10].Value2);
                            drow["Amount"] = Math.Round(Convert.ToDouble(rangeDataFile.Cells[rCnt, 4].Value2), 2);
                            drow["BeneficiaryAddress"] = "";
                            drow["RemitterName"] = Convert.ToString(rangeDataFile.Cells[rCnt, 11].Value2);
                            drow["RemitterAddress"] = "";
                            drow["Purpose"] = "Family Maintenance";
                            drow["BeneficiaryContactNo"] = "";

                            bankNm = Convert.ToString(rangeDataFile.Cells[rCnt, 7].Value2);
                            if (accountNo.Trim().Equals("") || accountNo.Trim().ToLower().Contains("coc") || accountNo.Trim().ToLower().Contains("cash"))
                            {
                                drow["PayMode"] = "CASH";
                            }
                            else if (!accountNo.Trim().Equals("") && (bankNm.ToUpper().Contains("MUTUAL") || bankNm.ToUpper().Contains("MTB")))
                            {
                                drow["PayMode"] = "MTB";
                            }
                            else
                            {
                                drow["PayMode"] = "EFT";
                            }
                            dtFile.Rows.Add(drow);

                            lblFileTxnLoadingMsg.Text = "Adding Txn " + drow["RefNo"].ToString() + " [ remain " + (rw - rCnt) + " ]";
                            listBoxTellerProgress.Items.Insert(0, drow["RefNo"].ToString().Trim() + " - Added into list - " + DateTime.Now);
                        }
                        Application.DoEvents();
                    }
                }




                try
                {
                    xlWorkBookDataFile.Close(true, null, null);
                    xlAppDataFile.Quit();
                }
                finally
                {
                    if (xlWorkSheetDataFile != null)
                    {
                        Marshal.FinalReleaseComObject(xlWorkSheetDataFile);
                        xlWorkSheetDataFile = null;
                    }
                    if (xlWorkBookDataFile != null)
                    {
                        Marshal.FinalReleaseComObject(xlWorkBookDataFile);
                        xlWorkBookDataFile = null;
                    }
                    if (xlAppDataFile != null)
                    {
                        Marshal.FinalReleaseComObject(xlAppDataFile);
                        xlAppDataFile = null;
                    }
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ec)
            {
                MessageBox.Show(ec.ToString());
            }
            return dtFile;
        }

        private DataTable CreateDataTableCommon()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("RefNo");//0
            dt.Columns.Add("PayMode");//1
            dt.Columns.Add("BeneficiaryName");//2
            dt.Columns.Add("AccountNo");//3
            dt.Columns.Add("BankName");//4
            dt.Columns.Add("BranchName");//5
            dt.Columns.Add("RoutingNo");//6
            dt.Columns.Add("Amount");//7
            dt.Columns.Add("BeneficiaryAddress");//8
            dt.Columns.Add("RemitterName");//9
            dt.Columns.Add("RemitterAddress");//10
            dt.Columns.Add("Purpose");//11            
            dt.Columns.Add("BeneficiaryContactNo");//12
            return dt;
        }

        private void buttonUploadDataFromTeller_Click(object sender, EventArgs e)
        {
            if (this.userType.ToLower().Equals("teller") || this.userType.ToLower().Equals("admin") || this.userType.ToLower().Equals("superadmin"))
            {
                DialogResult result = MessageBox.Show("Are You Sure to Upload ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    Cursor.Current = Cursors.WaitCursor;

                    buttonUploadDataFromTeller.Enabled = false;

                    //this will validate EFT 9 digit routing number
                    bool isSuccess = ValidateTxn(tellerScreenExhFileData);

                    if (isSuccess)
                    {
                        bool isExistsRecord = false;
                        bool isSaved = false;

                        if (tellerScreenExhFileData.Rows.Count > 0)
                        {
                            DataTable dtAuthAndSAEmailList = mg.GetAuthorizerAndSuperAdminEmailList();
                            DataTable dtLoggedUserInfo = mg.GetLoggedUserInfoEmail(loggedUser);

                            int exhId = Convert.ToInt32(cbExh.Text.Split('-')[0]);
                            string exhName = Convert.ToString(cbExh.Text.Split('-')[1]).Trim();
                            int recordCount = tellerScreenExhFileData.Rows.Count;
                            string ticks = Convert.ToString(DateTime.Now.Ticks);

                            for (int ii = 0; ii < tellerScreenExhFileData.Rows.Count; ii++)
                            {
                                isExistsRecord = mg.IsThisTransactionExistBefore(exhId, Convert.ToString(tellerScreenExhFileData.Rows[ii][0]));
                                if (!isExistsRecord)
                                {
                                    isSaved = mg.SaveExhData(exhId, tellerScreenExhFileData.Rows[ii], loggedUser, ticks);
                                }
                                else
                                {
                                    listBoxError.Items.Add("PIN -> " + tellerScreenExhFileData.Rows[ii][0].ToString().Trim() + " - Already Exist.");
                                    //textBoxErrorList.Text += "PIN -> " + tellerScreenExhFileData.Rows[ii][0].ToString().Trim() + " - Already Exist." + "\n";
                                }

                                Application.DoEvents();

                            } //for end 

                            if (isSaved)
                            {
                                mg.ChangeStatusFromUploadedToReceived(ticks);
                                MessageBox.Show("Upload Successfully for Authorization !!!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                tellerScreenExhFileData = new DataTable();
                                dataGridViewExhDataTellerScreen.DataSource = null;

                                DataTable dtSummaryUploadedRecord = mg.GetSummaryTellerUploadedRecord(ticks);
                                SendMailToAuthorizer(exhName, recordCount, dtAuthAndSAEmailList, dtLoggedUserInfo, dtSummaryUploadedRecord);
                            }
                            else
                            {
                                MessageBox.Show("Error Occured when uploading data !!!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        } //if end
                    }
                    else
                    {
                        MessageBox.Show("One or more transaction has Invalid Routing number/Data. Please Fix it !!!", "Error In Data", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    Cursor.Current = Cursors.Default;
                    buttonUploadDataFromTeller.Enabled = true;
                    btnLoadExcelFile.Enabled = true;

                }//if dialog end
            }// if end
            else
            {
                MessageBox.Show("Only Teller can upload Transactions !!!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SendMailToAuthorizer(string exhName, int recordCount, DataTable dtAuthAndSAEmailList, DataTable dtLoggedUserInfo, DataTable dtSummaryUploadedRecord)
        {
            MailManager mailManager = new MailManager();
            string tomail = "", ccmail = "", bccmail = "", frommail = "";

            frommail = dtLoggedUserInfo.Rows[0]["UserEmail"].ToString().Trim();
            string tellerUserName = dtLoggedUserInfo.Rows[0]["UserName"].ToString().Trim();

            //tomail = "razibul.islam@mutualtrustbank.com";

            for (int jj = 0; jj < dtAuthAndSAEmailList.Rows.Count; jj++)
            {
                if (dtAuthAndSAEmailList.Rows[jj]["UserType"].ToString().Trim().Equals("Authorizer"))
                {
                    if (tomail.Equals(""))
                    {
                        tomail += dtAuthAndSAEmailList.Rows[jj]["UserEmail"].ToString().Trim();
                    }
                    else
                    {
                        tomail += "; " + dtAuthAndSAEmailList.Rows[jj]["UserEmail"].ToString().Trim();
                    }
                }
            }

            for (int jj = 0; jj < dtAuthAndSAEmailList.Rows.Count; jj++)
            {
                if (dtAuthAndSAEmailList.Rows[jj]["UserType"].ToString().Trim().Equals("SuperAdmin") 
                    || dtAuthAndSAEmailList.Rows[jj]["UserType"].ToString().Trim().Equals("Admin"))
                {
                    if (ccmail.Equals(""))
                    {
                        ccmail += dtAuthAndSAEmailList.Rows[jj]["UserEmail"].ToString().Trim();
                    }
                    else
                    {
                        ccmail += "; " + dtAuthAndSAEmailList.Rows[jj]["UserEmail"].ToString().Trim();
                    }
                }
            }
            
            //ccmail += "; mtbremittance@mutualtrustbank.com";

            if (!ccmail.Equals(""))
            {
                ccmail += "; " + frommail;
            }
            else
            {
                ccmail += frommail;
            }
            
            bccmail = "";

            string subject = "Manual EFT Transaction To Authorize: Exh- " + exhName + " , Record:" + recordCount;
            string emailbody = "Dear Sir/Madam,";
            emailbody += "<br><br>Please authorize File based transactions.";
            emailbody += "<br><br>Exchange House: " + exhName + " , Total Record:" + recordCount;

            emailbody += "<br><br>";

            emailbody += "<style type=\"text/css\"> "
                + " .tg  {border-collapse:collapse;border-spacing:0;margin:0px auto;} "
                + " .tg td{border-color:black;border-style:solid;border-width:1px;font-family:Arial, sans-serif;font-size:14px; overflow:hidden;padding:10px 5px;word-break:normal;} "
                + " .tg th{border-color:black;border-style:solid;border-width:1px;font-family:Arial, sans-serif;font-size:14px; "
                + " font-weight:normal;overflow:hidden;padding:10px 5px;word-break:normal;} "
                + " .tg .tg-c3ow{border-color:inherit;text-align:center;vertical-align:top} "
                + " .tg .tg-fymr{border-color:inherit;font-weight:bold;text-align:left;vertical-align:top} "
                + " .tg .tg-7btt{border-color:inherit;font-weight:bold;text-align:center;vertical-align:top} "
                + " .tg .tg-0pky{border-color:inherit;text-align:left;vertical-align:top} "
                + " </style> "
                + " <table class=\"tg\"> "
                + " <thead> "
                + " <tr> "
                + " <th class=\"tg-fymr\"><span style=\"color:#002060\">Payment Mode</span></th> "
                + " <th class=\"tg-7btt\"><span style=\"color:#002060\">No of Txn</span></th> "
                + " <th class=\"tg-7btt\"><span style=\"color:#002060\">Total Amount</span></th> "
                + " </tr> "
                + " </thead> "
                + " <tbody> ";

            for (int rw = 0; rw < dtSummaryUploadedRecord.Rows.Count; rw++)
            {
                emailbody += " <tr> "
                    + " <td class=\"tg-0pky\">" + dtSummaryUploadedRecord.Rows[rw][0].ToString() + "</td>"
                    + " <td class=\"tg-c3ow\">" + dtSummaryUploadedRecord.Rows[rw][1].ToString() + "</td>"
                    + " <td class=\"tg-c3ow\">" + dtSummaryUploadedRecord.Rows[rw][2].ToString() + "</td>"
                    + "</tr>";
            }

            emailbody += "</tbody></table>";


            emailbody += "<br><br><br>Thanks & Regards";
            emailbody += "<br>" + tellerUserName;

            bool mailstatus = mailManager.SendMail(frommail, tomail, ccmail, bccmail, subject, emailbody);

        }

        private bool ValidateTxn(DataTable tellerScreenExhFileData)
        {
            string paymode = "", routingNo = "", accountNumber = "";
            bool isValid = true;
            listBoxError.Items.Clear();
            //textBoxErrorList.Text = "";

            for (int ii = 0; ii < tellerScreenExhFileData.Rows.Count; ii++)
            {
                paymode = tellerScreenExhFileData.Rows[ii]["PayMode"].ToString().Trim();
                routingNo = tellerScreenExhFileData.Rows[ii]["RoutingNo"].ToString().Trim();
                accountNumber = tellerScreenExhFileData.Rows[ii]["AccountNo"].ToString().Trim();

                if (paymode.Equals("EFT"))
                {
                    if (routingNo.Length != EFT_ROUTING_NUMBER_MAX_LENGTH)
                    {
                        isValid = false; //break;
                        listBoxError.Items.Add("PIN -> " + tellerScreenExhFileData.Rows[ii]["RefNo"].ToString().Trim() + " - " + routingNo);
                        //textBoxErrorList.Text += "PIN -> " + tellerScreenExhFileData.Rows[ii]["RefNo"].ToString().Trim() + " - " + routingNo + "\n";
                    }
                    else if (mg.GetBranchNameByRoutingCode(routingNo).Equals(""))
                    {
                        isValid = false; //break;
                        listBoxError.Items.Add("PIN -> " + tellerScreenExhFileData.Rows[ii]["RefNo"].ToString().Trim() + " - " + routingNo + " NOT FOUND IN OUR SYSTEM");
                        //textBoxErrorList.Text += "PIN -> " + tellerScreenExhFileData.Rows[ii]["RefNo"].ToString().Trim() + " - " + routingNo + " NOT FOUND IN OUR SYSTEM" + "\n";
                    }

                    if (accountNumber.Length > EFT_ACCOUNT_NUMBER_MAX_LENGTH)
                    {
                        isValid = false; //break;
                        listBoxError.Items.Add("PIN -> " + tellerScreenExhFileData.Rows[ii]["RefNo"].ToString().Trim() + " - " + accountNumber + " : INVALID Account Length");
                        //textBoxErrorList.Text += "PIN -> " + tellerScreenExhFileData.Rows[ii]["RefNo"].ToString().Trim() + " - " + accountNumber + " : INVALID Account Length" + "\n";
                    }
                }
                Application.DoEvents();
            }
            return isValid;
        }



        private void btnSearchUnAuthorizedTxn_Click(object sender, EventArgs e)
        {
            if (this.userType.ToLower().Equals("authorizer") || this.userType.ToLower().Equals("admin") || this.userType.ToLower().Equals("superadmin"))
            {
                listBoxAuthOutput.Items.Clear();
                dGridViewFindRecord.DataSource = null;
                txtSearchRefNo.Text = "";

                Cursor.Current = Cursors.WaitCursor;

                DateTime dateTime1 = DateTime.ParseExact(dtpickerFrom.Text, "dd-MMM-yyyy", CultureInfo.InvariantCulture);
                DateTime dateTime2 = DateTime.ParseExact(dtpickerTo.Text, "dd-MMM-yyyy", CultureInfo.InvariantCulture);

                string dtValue1 = dateTime1.ToString("yyyy-MM-dd");
                string dtValue2 = dateTime2.ToString("yyyy-MM-dd");

                int exhId = 0;

                if (cmbExhAuthList.SelectedIndex == 0)
                {
                    MessageBox.Show("Please Select Exchange House From Dropdown List !!!", "Selection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    if (cmbExhAuthList.SelectedIndex != 0)
                    {
                        exhId = Convert.ToInt32(cmbExhAuthList.Text.Split('-')[0].Trim());
                    }

                    batchIds = mg.GetDistinctBatchIdForThisExchangeHouseUnAuthorizeData(dtValue1, dtValue2, exhId);
                    exhFileDataToAuthorize = mg.GetExhFileUnAuthorizeData(dtValue1, dtValue2, exhId);

                    dataGridViewTxnToAuthorize.DataSource = null;
                    dataGridViewTxnToAuthorize.DataSource = exhFileDataToAuthorize;

                    dataGridViewTxnToAuthorize.Columns["Sl"].Width = 50;
                    dataGridViewTxnToAuthorize.Columns["Mode"].Width = 50;
                    dataGridViewTxnToAuthorize.Columns["Amount"].Width = 60;
                    dataGridViewTxnToAuthorize.Columns["RoutingNo"].Width = 70;
                    dataGridViewTxnToAuthorize.Columns["UplodeBy"].Width = 80;
                    dataGridViewTxnToAuthorize.Columns["UploadTime"].Width = 110;
                    dataGridViewTxnToAuthorize.Columns["IsSuccess"].Width = 70;
                    dataGridViewTxnToAuthorize.Columns["PartyId"].Width = 50;

                    lblRowCount.Text = "Total Records: " + exhFileDataToAuthorize.Rows.Count;

                } // Exh selection ok

                Cursor.Current = Cursors.Default;
                btnAuthFileTxn.Enabled = true;

            }// if end
            else
            {
                MessageBox.Show("Only Authorizer can Search Transactions to Authorize !!!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void btnAuthFileTxn_Click(object sender, EventArgs e)
        {
            listBoxAuthOutput.Items.Clear();

            if (this.userType.ToLower().Equals("authorizer") || this.userType.ToLower().Equals("admin") || this.userType.ToLower().Equals("superadmin"))
            {
                if (exhFileDataToAuthorize.Rows.Count > 0)
                {                    
                    //string batchId = exhFileDataToAuthorize.Rows[0]["BatchId"].ToString().Trim();
                    string uploadedUserId = exhFileDataToAuthorize.Rows[0]["UplodeByUserId"].ToString().Trim();
                    
                    DialogResult result = MessageBox.Show("Are You Sure to Authorize ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        Cursor.Current = Cursors.WaitCursor;
                        btnAuthFileTxn.Enabled = false;
                        string paymode = "";

                        for (int row = 0; row < exhFileDataToAuthorize.Rows.Count; row++)
                        {
                            paymode = exhFileDataToAuthorize.Rows[row]["Mode"].ToString().Trim();

                            if (paymode.Equals("EFT"))
                            {
                                UploadBEFTNTxnIntoSystem(exhFileDataToAuthorize.Rows[row]);
                            }
                            else if (paymode.Equals("MTB"))
                            {
                                ProcessMTBAccountCreditTxn(exhFileDataToAuthorize.Rows[row]);
                            }
                            else // CASH
                            {
                                UploadCashTxnDataIntoTable(exhFileDataToAuthorize.Rows[row]);
                            }

                            Application.DoEvents();

                        }//for end

                        btnAuthFileTxn.Enabled = true;

                        SendTransactionAuthStatusMailToTeller(batchIds, uploadedUserId);

                        Cursor.Current = Cursors.Default;
                        MessageBox.Show("Process Complete ....", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        exhFileDataToAuthorize = new DataTable();
                        //btnSearchUnAuthorizedTxn_Click(sender, e);

                        dataGridViewTxnToAuthorize.DataSource = null;
                        dGridViewFindRecord.DataSource = null;

                        btnAuthFileTxn.Enabled = true;

                    }// if dialog end
                } // if row count end
                else
                {
                    MessageBox.Show("Nothing to Authorize. Its not a Fun !!!", "No Data", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }// if end
            else
            {
                MessageBox.Show("Only Authorizer can Authorize Transactions !!!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SendTransactionAuthStatusMailToTeller(string batchId, string uploadedUserId)
        {
            try
            {
                MailManager mailManager = new MailManager();
                string tomail = "", ccmail = "", bccmail = "", frommail = "";

                DataTable dtUploadedUserInfo = mg.GetUploadUserInfo(uploadedUserId);
                tomail = dtUploadedUserInfo.Rows[0]["UserEmail"].ToString();
                frommail = mg.GetUploadUserInfo(loggedUser).Rows[0]["UserEmail"].ToString();
                string fromUser = mg.GetUploadUserInfo(loggedUser).Rows[0]["UserName"].ToString();

                DataTable dtAuthorizedTxnList = mg.GetAuthorizedTxnList(batchId);
                DataTable dtAuthorizedTxnSummaryList = mg.GetAuthorizedTxnSummaryList(batchId);
                DataTable dtAuthAndSAEmailList = mg.GetAuthorizerAndSuperAdminEmailListExcludingMe(loggedUser);

                for (int jj = 0; jj < dtAuthAndSAEmailList.Rows.Count; jj++)
                {
                    if (ccmail.Equals(""))
                    {
                        ccmail += dtAuthAndSAEmailList.Rows[jj]["UserEmail"].ToString().Trim();
                    }
                    else
                    {
                        ccmail += "; " + dtAuthAndSAEmailList.Rows[jj]["UserEmail"].ToString().Trim();
                    }
                }
                //ccmail += "; mtbremittance@mutualtrustbank.com";
                ccmail += "; " + frommail;

                string exhName = Convert.ToString(dtAuthorizedTxnList.Rows[0]["ExchangeHouse"]);
                int recordCount = dtAuthorizedTxnList.Rows.Count;

                string subject = "Manual EFT Transaction Authorized: Exh- " + exhName + " , Record:" + recordCount;
                string emailbody = "Dear Sir/Madam,";
                
                emailbody += "<br><br>Exchange House: " + exhName + " , Total Record:" + recordCount;
                emailbody += "<br><br>";

                emailbody += GenerateSummaryPart(dtAuthorizedTxnSummaryList);
                emailbody += "<br><br>";

                emailbody += "<style type=\"text/css\"> "
                    + " .tg  {border-collapse:collapse;border-spacing:0;margin:0px auto;} "
                    + " .tg td{border-color:black;border-style:solid;border-width:1px;font-family:Arial, sans-serif;font-size:14px; overflow:hidden;padding:10px 5px;word-break:normal;} "
                    + " .tg th{border-color:black;border-style:solid;border-width:1px;font-family:Arial, sans-serif;font-size:14px; "
                    + " font-weight:normal;overflow:hidden;padding:10px 5px;word-break:normal;} "
                    + " .tg .tg-c3ow{border-color:inherit;text-align:center;vertical-align:top} "
                    + " .tg .tg-fymr{border-color:inherit;font-weight:bold;text-align:left;vertical-align:top} "
                    + " .tg .tg-7btt{border-color:inherit;font-weight:bold;text-align:center;vertical-align:top} "
                    + " .tg .tg-0pky{border-color:inherit;text-align:left;vertical-align:top} "
                    + " </style> "
                    + " <table class=\"tg\"> "
                    + " <thead> "
                    + " <tr> "
                    + " <th class=\"tg-fymr\"><span style=\"color:#002060\">Exchange House</span></th> "
                    + " <th class=\"tg-7btt\"><span style=\"color:#002060\">RefNo</span></th> "
                    + " <th class=\"tg-7btt\"><span style=\"color:#002060\">Mode</span></th> "
                    + " <th class=\"tg-fymr\"><span style=\"color:#002060\">Status</span></th> "
                    + " <th class=\"tg-7btt\"><span style=\"color:#002060\">Remarks</span></th> "
                    + " <th class=\"tg-7btt\"><span style=\"color:#002060\">BankName</span></th> "
                    + " <th class=\"tg-fymr\"><span style=\"color:#002060\">Amount</span></th> "
                    + " <th class=\"tg-7btt\"><span style=\"color:#002060\">UplodeBy</span></th> "
                    + " <th class=\"tg-7btt\"><span style=\"color:#002060\">UploadTime</span></th> "
                    + " <th class=\"tg-fymr\"><span style=\"color:#002060\">ProcessBy</span></th> "
                    + " <th class=\"tg-7btt\"><span style=\"color:#002060\">ProcessTime</span></th> "
                    + " </tr> "
                    + " </thead> "
                    + " <tbody> ";

                for (int rw = 0; rw < dtAuthorizedTxnList.Rows.Count; rw++)
                {
                    emailbody += " <tr> "
                        + " <td class=\"tg-0pky\">" + dtAuthorizedTxnList.Rows[rw][0].ToString() + "</td>"
                        + " <td class=\"tg-c3ow\">" + dtAuthorizedTxnList.Rows[rw][1].ToString() + "</td>"
                        + " <td class=\"tg-c3ow\">" + dtAuthorizedTxnList.Rows[rw][2].ToString() + "</td>"
                        + " <td class=\"tg-0pky\">" + dtAuthorizedTxnList.Rows[rw][3].ToString() + "</td>"
                        + " <td class=\"tg-c3ow\">" + dtAuthorizedTxnList.Rows[rw][4].ToString() + "</td>"
                        + " <td class=\"tg-c3ow\">" + dtAuthorizedTxnList.Rows[rw][5].ToString() + "</td>"
                        + " <td class=\"tg-0pky\">" + dtAuthorizedTxnList.Rows[rw][6].ToString() + "</td>"
                        + " <td class=\"tg-c3ow\">" + dtAuthorizedTxnList.Rows[rw][7].ToString() + "</td>"
                        + " <td class=\"tg-c3ow\">" + dtAuthorizedTxnList.Rows[rw][8].ToString() + "</td>"
                        + " <td class=\"tg-0pky\">" + dtAuthorizedTxnList.Rows[rw][9].ToString() + "</td>"
                        + " <td class=\"tg-c3ow\">" + dtAuthorizedTxnList.Rows[rw][10].ToString() + "</td>"
                        + "</tr>";
                }

                emailbody += "</tbody></table>";

                emailbody += "<br><br><br>Thanks & Regards";
                emailbody += "<br>" + fromUser;

                bool mailstatus = mailManager.SendMail(frommail, tomail, ccmail, bccmail, subject, emailbody);

            }
            catch (Exception exc)
            { }

        }

        private string GenerateSummaryPart(DataTable dtAuthorizedTxnSummaryList)
        {
            string mailText = "";
            mailText += "<style type=\"text/css\"> "
                + ".tgAuthSumr  {border-collapse:collapse;border-spacing:0;margin:0px auto;} "
                + ".tgAuthSumr td{border-color:black;border-style:solid;border-width:1px;font-family:Arial, sans-serif;font-size:14px; overflow:hidden;padding:10px 5px;word-break:normal;} "
                + ".tgAuthSumr th{border-color:black;border-style:solid;border-width:1px;font-family:Arial, sans-serif;font-size:14px; font-weight:normal;overflow:hidden;padding:10px 5px;word-break:normal;} "
                + ".tgAuthSumr .tgAuthSumr-c3ow{border-color:inherit;text-align:center;vertical-align:top} "
                + ".tgAuthSumr .tgAuthSumr-fymr{border-color:inherit;font-weight:bold;text-align:left;vertical-align:top} "
                + ".tgAuthSumr .tgAuthSumr-7btt{border-color:inherit;font-weight:bold;text-align:center;vertical-align:top} "
                + ".tgAuthSumr .tgAuthSumr-0pky{border-color:inherit;text-align:left;vertical-align:top} "
                + "</style> "
                + "<table class=\"tgAuthSumr\"> "
                + "<thead> "
                + "  <tr> "
                    + " <th class=\"tgAuthSumr-fymr\"><span style=\"color:#002060\">Mode</span></th> "
                    + " <th class=\"tgAuthSumr-7btt\"><span style=\"color:#002060\">Status</span></th> "
                    + "<th class=\"tgAuthSumr-7btt\"><span style=\"color:#002060\">No of Txn</span></th> "
                    + " </tr> "
                + " </thead> "
                + "<tbody>";

            for (int rw = 0; rw < dtAuthorizedTxnSummaryList.Rows.Count; rw++)
            {
                mailText += " <tr> "
                        + " <td class=\"tgAuthSumr-0pky\">" + dtAuthorizedTxnSummaryList.Rows[rw][0].ToString() + "</td>"
                        + " <td class=\"tgAuthSumr-c3ow\">" + dtAuthorizedTxnSummaryList.Rows[rw][1].ToString() + "</td>"
                        + " <td class=\"tgAuthSumr-c3ow\">" + dtAuthorizedTxnSummaryList.Rows[rw][2].ToString() + "</td>"
                        + "</tr>";
            }
            mailText += "</tbody></table>";

            return mailText;
        }

        private void UploadCashTxnDataIntoTable(DataRow dataRow)
        {
            // Sl, ExchangeHouse, RefNo, Mode, BeneficiaryName, AccountNo, BankName, RoutingNo, Amount, UplodeBy, UploadTime, IsSuccess
            //BranchName, PartyId, BeneficiaryAddress, SenderName, SenderAddress, Purpose, Status, BeneficiaryContactNo 

            string refno, senderName, benfName, bank, branch, exhouseId, SenderAddress, senderPhoneNo = "", BeneficiaryAddress, BeneficiaryContactNo;
            string batchId = DateTime.Now.ToString("yyyyMMddHHmmss");
            decimal receivingAmount;

            refno = dataRow["RefNo"].ToString();
            senderName = dataRow["SenderName"].ToString();
            benfName = dataRow["BeneficiaryName"].ToString();
            receivingAmount = decimal.Round(Convert.ToDecimal(dataRow["Amount"].ToString()), 2);
            bank = dataRow["BankName"].ToString();
            branch = dataRow["BranchName"].ToString();
            exhouseId = dataRow["PartyId"].ToString();
            SenderAddress = dataRow["SenderAddress"].ToString();
            BeneficiaryAddress = dataRow["BeneficiaryAddress"].ToString();
            BeneficiaryContactNo = dataRow["BeneficiaryContactNo"].ToString();

            if (BeneficiaryAddress.Equals(""))
            {
                BeneficiaryAddress = "Bangladesh";
            }

            if (BeneficiaryContactNo.Trim().Equals(""))
            {
                BeneficiaryContactNo = BeneficiaryAddress;
            }

            senderPhoneNo = "123456";
            if (SenderAddress.Trim().Equals(""))
            {
                SenderAddress = "ABC";
            }

            // Save to NRBWork DB
            //bool status = mg.saveCashDataIntoNRBWorkDbFileBasedCashTxnDataTable(refno, senderName, benfName, receivingAmount, bank, branch, exhouseId, batchId, loggedUser);


            string exhUserId, exhAccountNo, passwd = "", msgCodeValue;
            string autoID, txnStatus = "", refNo;
            int partyId;

            XmlDocument xDoc = new XmlDocument();
            XmlNodeList msgCode;
            XmlNodeList msgVal;
            string msgValue = "";

            txnStatus = dataRow["Status"].ToString();

            if (!txnStatus.Equals("") && txnStatus.Equals("RECEIVED"))
            {
                refNo = dataRow["RefNo"].ToString();
                autoID = dataRow["Sl"].ToString();
                partyId = Convert.ToInt32(dataRow["PartyId"].ToString());

                DataTable dtExchAccInfo = mg.GetExchangeHouseAccountInfo(partyId);
                exhUserId = dtExchAccInfo.Rows[0]["UserId"].ToString();
                exhAccountNo = dtExchAccInfo.Rows[0]["NRTAccount"].ToString();
                passwd = dtExchAccInfo.Rows[0]["Password"].ToString();

                try
                {
                    string paymntResp = "";
                    msgCodeValue = "";

                    try
                    {
                        var nodeOTCPaymentRequest = remitServiceClient.OTCPayment(partyId, exhUserId, passwd, refNo, senderName, senderPhoneNo, SenderAddress,
                                                                                    benfName, BeneficiaryAddress, BeneficiaryContactNo, receivingAmount);
                        
                        /*
                        paymntResp = nodeOTCPaymentRequest.InnerXml;
                        if (!paymntResp.Contains("MessageCode"))  // backup plan
                        {
                            paymntResp = nodeOTCPaymentRequest.ToString();
                        }
                        */

                        paymntResp = nodeOTCPaymentRequest.ToString();
                        if (!paymntResp.Contains("OTCPaymentresponse"))
                        {
                            paymntResp = "<OTCPaymentresponse>" + paymntResp + "</OTCPaymentresponse>";
                        }

                        //xDoc.LoadXml(nodePaymentRequest.ToString());
                        xDoc.LoadXml(paymntResp);

                        msgCode = xDoc.GetElementsByTagName("MessageCode");
                        msgCodeValue = msgCode[0].InnerText;
                        msgVal = xDoc.GetElementsByTagName("Message");
                        msgValue = msgVal[0].InnerText;
                    }
                    catch (Exception ex)
                    {

                    }

                    if (!msgCodeValue.Equals("") && msgCodeValue.Equals("1022"))    // Fund Transfer Success
                    {
                        try
                        {
                            bool stat = mg.UpdateEFTFileDataStatusTxnTable(autoID, refNo, partyId, loggedUser, "SUCCESS", msgValue);

                            if (stat)
                            {
                                //listBoxAuthOutput.Items.Add("PIN -> " + refNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> " + msgValue);
                                listBoxAuthOutput.Items.Insert(0, "PIN -> " + refNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> " + msgValue + "-" + DateTime.Now);
                            }
                            else
                            {
                                //listBoxAuthOutput.Items.Add("PIN -> " + refNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> stat=" + stat);
                                listBoxAuthOutput.Items.Insert(0, "PIN -> " + refNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> stat=" + stat + "-" + DateTime.Now);
                            }
                        }
                        catch (Exception ex)
                        {
                            //listBoxAuthOutput.Items.Add("PIN -> " + refNo + " - Update in SqlDb Error -> " + ex.ToString());
                            listBoxAuthOutput.Items.Insert(0, "PIN -> " + refNo + " - Update in SqlDb Error -> " + ex.ToString() + "-" + DateTime.Now);
                        }
                    }
                    else
                    {
                        try
                        {
                            bool stat = mg.UpdateEFTFileDataStatusTxnTable(autoID, refNo, partyId, loggedUser, "FAILED", msgValue);
                            if (stat)
                            {
                                //listBoxAuthOutput.Items.Add("PIN -> " + refNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> " + msgValue);
                                listBoxAuthOutput.Items.Insert(0, "PIN -> " + refNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> " + msgValue + "-" + DateTime.Now);
                            }
                            else
                            {
                                //listBoxAuthOutput.Items.Add("PIN -> " + refNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> stat=" + stat);
                                listBoxAuthOutput.Items.Insert(0, "PIN -> " + refNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> stat=" + stat + "-" + DateTime.Now);
                            }
                        }
                        catch (Exception ex)
                        {
                            //listBoxAuthOutput.Items.Add("PIN -> " + refNo + " - Update in SqlDb Error -> " + ex.ToString());
                            listBoxAuthOutput.Items.Insert(0, "PIN -> " + refNo + " - Update in SqlDb Error -> " + ex.ToString() + "-" + DateTime.Now);
                        }
                    }

                }
                catch (Exception ex)
                {
                    //listBoxAuthOutput.Items.Add("PIN -> " + refNo + " - Error -> " + ex.ToString());
                    listBoxAuthOutput.Items.Insert(0, "PIN -> " + refNo + " - Error -> " + ex.ToString() + "-" + DateTime.Now);
                }
            }

        }

        private void ProcessMTBAccountCreditTxn(DataRow dataRow)
        {
            // Sl, ExchangeHouse, RefNo, Mode, BeneficiaryName, AccountNo, BankName, RoutingNo, Amount, UplodeBy, UploadTime, IsSuccess
            //BranchName, PartyId, BeneficiaryAddress, SenderName, SenderAddress, Purpose, Status 

            string exhUserId, exhAccountNo, beneficiaryAccountNo, beneficiaryName, msgCodeValue, refrnNo;
            string autoID, txnStatus = "", refNo, SenderName, senderPhoneNo = "", senderAddress, senderCountry, bankId, branchId, transferCurrency, msgToBenfcry;
            int partyId;
            string remitPaymentStatus = "", passwd = "";
            XmlDocument xDoc = new XmlDocument();
            XmlNodeList msgCode;
            XmlNodeList msgVal;
            string msgValue = "";

            txnStatus = dataRow["Status"].ToString();

            if (!txnStatus.Equals("") && txnStatus.Equals("RECEIVED"))
            {
                refrnNo = dataRow["RefNo"].ToString();
                DataTable dtRemitFundTransferInfo = mg.GetOwnAccountRemitTransferInfo(refrnNo);

                if (dtRemitFundTransferInfo.Rows.Count > 0)
                {
                    remitPaymentStatus = dtRemitFundTransferInfo.Rows[0]["PaymentStatus"].ToString();

                    if (!remitPaymentStatus.Equals("") && remitPaymentStatus.Equals("5"))
                    {
                        autoID = dataRow["Sl"].ToString();
                        partyId = Convert.ToInt32(dtRemitFundTransferInfo.Rows[0]["PartyId"].ToString());
                        bool stat = mg.UpdateEFTFileDataStatusTxnTable(autoID, refrnNo, partyId, loggedUser, "SUCCESS", "");

                        if (stat)
                        {
                            //listBoxAuthOutput.Items.Add("PIN -> " + refrnNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> SUCCESS. ");
                            listBoxAuthOutput.Items.Insert(0, "PIN -> " + refrnNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> SUCCESS. " + "-" + DateTime.Now);
                        }
                        else
                        {
                            //listBoxAuthOutput.Items.Add("PIN -> " + refrnNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> stat=" + stat);
                            listBoxAuthOutput.Items.Insert(0, "PIN -> " + refrnNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> stat=" + stat + "-" + DateTime.Now);
                        }
                    }
                    else
                    {
                        autoID = dataRow["Sl"].ToString();
                        partyId = Convert.ToInt32(dtRemitFundTransferInfo.Rows[0]["PartyId"].ToString());
                        bool stat = mg.UpdateEFTFileDataStatusTxnTable(autoID, refrnNo, partyId, loggedUser, "FAILED", "");

                        if (stat)
                        {
                            //listBoxAuthOutput.Items.Add("PIN -> " + refrnNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> SUCCESS. ");
                            listBoxAuthOutput.Items.Insert(0, "PIN -> " + refrnNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> SUCCESS. " + "-" + DateTime.Now);
                        }
                        else
                        {
                            //listBoxAuthOutput.Items.Add("PIN -> " + refrnNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> stat=" + stat);
                            listBoxAuthOutput.Items.Insert(0, "PIN -> " + refrnNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> stat=" + stat + "-" + DateTime.Now);
                        }
                    }
                }
                else
                {
                    autoID = dataRow["Sl"].ToString();
                    partyId = Convert.ToInt32(dataRow["PartyId"].ToString());

                    DataTable dtExchAccInfo = mg.GetExchangeHouseAccountInfo(partyId);
                    exhUserId = dtExchAccInfo.Rows[0]["UserId"].ToString();
                    exhAccountNo = dtExchAccInfo.Rows[0]["NRTAccount"].ToString();
                    passwd = dtExchAccInfo.Rows[0]["Password"].ToString();

                    beneficiaryAccountNo = dataRow["AccountNo"].ToString();
                    beneficiaryName = dataRow["BeneficiaryName"].ToString();

                    try
                    {
                        refNo = dataRow["RefNo"].ToString();
                        decimal receivingAmount = decimal.Round(Convert.ToDecimal(dataRow["Amount"].ToString()), 2);

                        SenderName = dataRow["SenderName"].ToString();
                        senderAddress = dataRow["SenderAddress"].ToString();
                        senderCountry = dataRow["SenderAddress"].ToString();
                        bankId = "001";
                        branchId = "";
                        DateTime paymentDate = DateTime.Now;
                        transferCurrency = "053";
                        msgToBenfcry = dataRow["Purpose"].ToString();

                        string paymntResp = "";
                        msgCodeValue = "";

                        try
                        {
                            var nodePaymentRequest = remitServiceClient.Payment("1", partyId, exhUserId, passwd, refNo, beneficiaryAccountNo, beneficiaryName, SenderName,
                                senderPhoneNo, senderAddress, senderCountry, bankId, branchId, paymentDate, transferCurrency, receivingAmount, "", msgToBenfcry, "");

                            /*
                            paymntResp = nodePaymentRequest.InnerXml;
                            if (!paymntResp.Contains("MessageCode"))  // backup plan
                            {
                                paymntResp = nodePaymentRequest.ToString();
                            }
                            */

                            paymntResp = nodePaymentRequest.ToString();
                            if (!paymntResp.Contains("PaymentResponse"))
                            {
                                paymntResp = "<PaymentResponse>" + paymntResp + "</PaymentResponse>";
                            }

                            //xDoc.LoadXml(nodePaymentRequest.ToString());
                            xDoc.LoadXml(paymntResp);

                            msgCode = xDoc.GetElementsByTagName("MessageCode");
                            msgCodeValue = msgCode[0].InnerText;
                            msgVal = xDoc.GetElementsByTagName("Message");
                            msgValue = msgVal[0].InnerText;
                        }
                        catch (Exception ex)
                        {

                        }

                        if (!msgCodeValue.Equals("") && msgCodeValue.Equals("1009"))    // Fund Transfer Success
                        {
                            try
                            {
                                bool stat = mg.UpdateEFTFileDataStatusTxnTable(autoID, refNo, partyId, loggedUser, "SUCCESS", msgValue);

                                if (stat)
                                {
                                    //listBoxAuthOutput.Items.Add("PIN -> " + refNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> " + msgValue);
                                    listBoxAuthOutput.Items.Insert(0, "PIN -> " + refNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> " + msgValue + " - " + DateTime.Now);
                                }
                                else
                                {
                                    //listBoxAuthOutput.Items.Add("PIN -> " + refNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> stat=" + stat);
                                    listBoxAuthOutput.Items.Insert(0, "PIN -> " + refNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> stat=" + stat + " - " + DateTime.Now);
                                }
                            }
                            catch (Exception ex)
                            {
                                //listBoxAuthOutput.Items.Add("PIN -> " + refNo + " - Update in SqlDb Error -> " + ex.ToString());
                                listBoxAuthOutput.Items.Insert(0, "PIN -> " + refNo + " - Update in SqlDb Error -> " + ex.ToString() + " - " + DateTime.Now);
                            }
                        }
                        else // Fund Transfer Failed
                        {
                            try
                            {
                                bool stat = mg.UpdateEFTFileDataStatusTxnTable(autoID, refNo, partyId, loggedUser, "FAILED", msgValue);
                                if (stat)
                                {
                                    //listBoxAuthOutput.Items.Add("PIN -> " + refNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> " + msgValue);
                                    listBoxAuthOutput.Items.Insert(0, "PIN -> " + refNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> " + msgValue + " - " + DateTime.Now);
                                }
                                else
                                {
                                    //listBoxAuthOutput.Items.Add("PIN -> " + refNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> stat=" + stat);
                                    listBoxAuthOutput.Items.Insert(0, "PIN -> " + refNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> stat=" + stat + " - " + DateTime.Now);
                                }
                            }
                            catch (Exception ex)
                            {
                                //listBoxAuthOutput.Items.Add("PIN -> " + refNo + " - Update in SqlDb Error -> " + ex.ToString());
                                listBoxAuthOutput.Items.Insert(0, "PIN -> " + refNo + " - Update in SqlDb Error -> " + ex.ToString() + " - " + DateTime.Now);
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        //listBoxAuthOutput.Items.Add("PIN -> " + refrnNo + " - Error -> " + ex.ToString());
                        listBoxAuthOutput.Items.Insert(0, "PIN -> " + refrnNo + " - Error -> " + ex.ToString() + " - " + DateTime.Now);
                    }

                }// else end

            }// outer if end
        }

        private void UploadBEFTNTxnIntoSystem(DataRow dataRow)
        {
            // Sl, ExchangeHouse, RefNo, Mode, BeneficiaryName, AccountNo, BankName, RoutingNo, Amount, UplodeBy, UploadTime, IsSuccess
            //BranchName, PartyId, BeneficiaryAddress, SenderName, SenderAddress, Purpose, Status 

            string refNo, autoID, msgCodeValue = "", txnStatus = "", passwd = "";
            string exhUserId, beneficiaryAccountNo, beneficiaryName, bankName, branchName, routingNumber, beneficiaryAddress, senderName, senderAddress, transferCurrency, paymentDescription;
            decimal receivingAmount;
            int partyId;

            XmlDocument xDoc = new XmlDocument();
            XmlNodeList msgCode;
            XmlNodeList msgVal;
            string msgValue = "";

            txnStatus = dataRow["Status"].ToString();

            if (!txnStatus.Equals("") && txnStatus.Equals("RECEIVED"))
            {
                refNo = dataRow["RefNo"].ToString();
                autoID = dataRow["Sl"].ToString();
                partyId = Convert.ToInt32(dataRow["PartyId"].ToString());

                DataTable dtExchAccInfo = mg.GetExchangeHouseAccountInfo(partyId);
                exhUserId = dtExchAccInfo.Rows[0]["UserId"].ToString();
                passwd = dtExchAccInfo.Rows[0]["Password"].ToString();

                beneficiaryAccountNo = dataRow["AccountNo"].ToString();
                beneficiaryName = dataRow["BeneficiaryName"].ToString();
                bankName = dataRow["BankName"].ToString();
                routingNumber = dataRow["RoutingNo"].ToString();
                branchName = mg.GetBranchNameByRoutingCode(routingNumber);

                if (!branchName.Equals(""))
                {
                    beneficiaryAddress = dataRow["BeneficiaryAddress"].ToString();
                    senderName = dataRow["SenderName"].ToString();
                    senderAddress = dataRow["SenderAddress"].ToString();
                    transferCurrency = "053";
                    receivingAmount = decimal.Round(Convert.ToDecimal(dataRow["Amount"].ToString()), 2);
                    paymentDescription = dataRow["Purpose"].ToString();
                    string paymntResp = "";

                    try
                    {
                        var nodeBeftnPaymentRequest = remitServiceClient.BEFTNPayment(partyId, exhUserId, passwd, refNo, beneficiaryAccountNo, "SB", beneficiaryName, bankName,
                            branchName, routingNumber, beneficiaryAddress, senderName, senderAddress, transferCurrency, receivingAmount, paymentDescription);

                        /*
                        paymntResp = nodeBeftnPaymentRequest.InnerXml;
                        if (!paymntResp.Contains("MessageCode"))  // backup plan
                        {
                            paymntResp = nodeBeftnPaymentRequest.ToString();
                        }
                        */

                        paymntResp = nodeBeftnPaymentRequest.ToString();
                        if (!paymntResp.Contains("BEFTNPayment"))
                        {
                            paymntResp = "<BEFTNPaymentResponse>" + paymntResp + "</BEFTNPaymentResponse>";
                        }

                        xDoc.LoadXml(paymntResp);
                        //xDoc.LoadXml(nodeBeftnPaymentRequest.ToString());

                        msgCode = xDoc.GetElementsByTagName("MessageCode");
                        msgCodeValue = msgCode[0].InnerText;
                        msgVal = xDoc.GetElementsByTagName("Message");
                        msgValue = msgVal[0].InnerText;
                    }
                    catch (Exception ex)
                    {
                        //listBoxAuthOutput.Items.Add("PIN -> " + refNo + " - Error -> " + ex.ToString());
                        listBoxAuthOutput.Items.Insert(0, "PIN -> " + refNo + " - Error -> " + ex.ToString() + " - " + DateTime.Now);
                    }

                    if (!msgCodeValue.Equals("") && msgCodeValue.Equals("1020"))    //BEFTN success
                    {
                        try
                        {
                            bool stat = mg.UpdateEFTFileDataStatusTxnTable(autoID, refNo, partyId, loggedUser, "SUCCESS", msgValue);

                            if (stat)
                            {
                                //listBoxAuthOutput.Items.Add("PIN -> " + refNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> " + msgValue);
                                listBoxAuthOutput.Items.Insert(0, "PIN -> " + refNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> " + msgValue + " - " + DateTime.Now);
                            }
                            else
                            {
                                //listBoxAuthOutput.Items.Add("PIN -> " + refNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> stat=" + stat);
                                listBoxAuthOutput.Items.Insert(0, "PIN -> " + refNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> stat=" + stat + " - " + DateTime.Now);
                            }
                        }
                        catch (Exception ex)
                        {
                            //listBoxAuthOutput.Items.Add("PIN -> " + refNo + " - Update in SqlDb Error -> " + ex.ToString());
                            listBoxAuthOutput.Items.Insert(0, "PIN -> " + refNo + " - Update in SqlDb Error -> " + ex.ToString() + " - " + DateTime.Now);
                        }
                    }
                    else
                    {
                        try
                        {
                            bool stat = mg.UpdateEFTFileDataStatusTxnTable(autoID, refNo, partyId, loggedUser, "FAILED", msgValue);
                            if (stat)
                            {
                                //listBoxAuthOutput.Items.Add("PIN -> " + refNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> " + msgValue);
                                listBoxAuthOutput.Items.Insert(0, "PIN -> " + refNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> " + msgValue + " - " + DateTime.Now);
                            }
                            else
                            {
                                //listBoxAuthOutput.Items.Add("PIN -> " + refNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> stat=" + stat);
                                listBoxAuthOutput.Items.Insert(0, "PIN -> " + refNo + " - PaymentMode -> " + dataRow["Mode"].ToString() + " -> stat=" + stat + " - " + DateTime.Now);
                            }
                        }
                        catch (Exception ex)
                        {
                            //listBoxAuthOutput.Items.Add("PIN -> " + refNo + " - Update in SqlDb Error -> " + ex.ToString());
                            listBoxAuthOutput.Items.Insert(0, "PIN -> " + refNo + " - Update in SqlDb Error -> " + ex.ToString() + " - " + DateTime.Now);
                        }
                    }
                }
                else
                {
                    //listBoxAuthOutput.Items.Add("PIN -> " + refNo + " - Invalid RoutingNo -> " + routingNumber);
                    listBoxAuthOutput.Items.Insert(0, "PIN -> " + refNo + " - Invalid RoutingNo -> " + routingNumber + " - " + DateTime.Now);
                }
            }

        }

        private void changePasswordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmChangePassword fcp = new frmChangePassword();
            fcp.loggedUserChPass = this.loggedUser;
            fcp.loggedUserIdAndNameChPass = this.loggedUserIdAndName;
            fcp.ShowDialog();
        }



        private void btnSearchReportData_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;

            int eftCount = 0, mtbCount = 0, cashCount = 0;
            DateTime dateTime1 = DateTime.ParseExact(dtPickerFromRpt.Text, "dd-MMM-yyyy", CultureInfo.InvariantCulture);
            DateTime dateTime2 = DateTime.ParseExact(dtPickerToRpt.Text, "dd-MMM-yyyy", CultureInfo.InvariantCulture);

            string dtValue1 = dateTime1.ToString("yyyy-MM-dd");
            string dtValue2 = dateTime2.ToString("yyyy-MM-dd");

            reportData = mg.GetSuccessfulReportData(dtValue1, dtValue2, ref eftCount, ref mtbCount, ref cashCount);
            dataGridViewReport.DataSource = null;
            dataGridViewReport.DataSource = reportData;

            lblReportRowCount.Text = "Total Records: " + reportData.Rows.Count + " , EFT: " + eftCount + " , MTB: " + mtbCount + " , CASH:" + cashCount;

            if (reportData.Rows.Count > 0)
            {
                btnDownloadReport.Enabled = true;
            }
            else
            {
                btnDownloadReport.Enabled = false;
            }

            Cursor.Current = Cursors.Default;
        }

        private void btnDownloadReport_Click(object sender, EventArgs e)
        {
            var folderBrowserDialog1 = new FolderBrowserDialog();

            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                Cursor.Current = Cursors.WaitCursor;

                string folderName = folderBrowserDialog1.SelectedPath;
                //MessageBox.Show(folderName);

                DateTime dateTime1 = DateTime.ParseExact(dtPickerFromRpt.Text, "dd-MMM-yyyy", CultureInfo.InvariantCulture);
                DateTime dateTime2 = DateTime.ParseExact(dtPickerToRpt.Text, "dd-MMM-yyyy", CultureInfo.InvariantCulture);

                string dtValue1 = dateTime1.ToString("yyyy-MM-dd");
                string dtValue2 = dateTime2.ToString("yyyy-MM-dd");

                DataTable dtEFTTransactions = mg.GetSuccessReportData(dtValue1, dtValue2, "EFT");
                DataTable dtMTBTransactions = mg.GetSuccessReportData(dtValue1, dtValue2, "MTB");
                string today = DateTime.Now.ToString("dd.MM.yyyy");


                if (dtEFTTransactions.Rows.Count > 0)
                {
                    string fileName = "BEFTN TXN " + today + ".xls";
                    string fileLocationTemp = folderName + "\\" + fileName;

                    try
                    {
                        Microsoft.Office.Interop.Excel.Application _excelApp = new Microsoft.Office.Interop.Excel.Application();
                        Microsoft.Office.Interop.Excel.Workbooks _workbooks = _excelApp.Workbooks;
                        Microsoft.Office.Interop.Excel.Workbook _workbook = _workbooks.Add();
                        Microsoft.Office.Interop.Excel.Worksheet _worksheet = _workbook.Worksheets[1];
                        _worksheet.Name = "Sheet1";
                        Microsoft.Office.Interop.Excel.Range _workSheetRange = _worksheet.get_Range("A1", "N1");


                        TOPHeader(_worksheet, _workSheetRange, today);

                        int row_num = 5;
                        ADD_HEADER_ROW_EFT(row_num, _worksheet, _workSheetRange);

                        row_num++;
                        int firstTimeEmptyRow = 0;

                        object pinno, remitterName, district, remitterCountry, remitterAcNo, beneName, accountNo, bank, branch, routing, currCode, exchangeName, remarks;
                        double amount;

                        for (int rCnt = 0; rCnt < dtEFTTransactions.Rows.Count; rCnt++)
                        {
                            if (firstTimeEmptyRow == 0)
                            {
                                addDataMain(row_num, 1, "", "A" + row_num, "A" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 2, "", "B" + row_num, "B" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 3, "", "C" + row_num, "C" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 4, "", "D" + row_num, "D" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 5, "", "E" + row_num, "E" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 6, "", "F" + row_num, "F" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 7, "", "G" + row_num, "G" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 8, "", "H" + row_num, "H" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 9, "", "I" + row_num, "I" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 10, "", "J" + row_num, "J" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 11, "", "K" + row_num, "K" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 12, "", "L" + row_num, "L" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 13, "", "M" + row_num, "M" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 14, "", "N" + row_num, "N" + row_num, "@", _worksheet, _workSheetRange);

                                row_num++;
                                firstTimeEmptyRow = 1;
                            }

                            /*
                             ExchangeHouse,RefNo,PaymentMode, BeneficiaryName, BeneficiaryAddress, BeneficiaryAccountNo,BeneficiaryContactNo,BankName,BranchName,"
                                RoutingNo,Amount,SenderName,SenderAddress,Purpose, UplodeBy, UploadTime, ProcessBy, ProcessTime,Remarks, NRTAccount 
                             */

                            pinno = dtEFTTransactions.Rows[rCnt]["RefNo"];
                            remitterName = dtEFTTransactions.Rows[rCnt]["SenderName"];
                            district = "";
                            remitterCountry = "";
                            remitterAcNo = dtEFTTransactions.Rows[rCnt]["NRTAccount"];
                            beneName = dtEFTTransactions.Rows[rCnt]["BeneficiaryName"];
                            accountNo = dtEFTTransactions.Rows[rCnt]["BeneficiaryAccountNo"];
                            bank = dtEFTTransactions.Rows[rCnt]["BankName"];
                            branch = dtEFTTransactions.Rows[rCnt]["BranchName"];
                            routing = dtEFTTransactions.Rows[rCnt]["RoutingNo"];
                            currCode = "BDT";
                            amount = Convert.ToDouble(dtEFTTransactions.Rows[rCnt]["Amount"]);
                            exchangeName = dtEFTTransactions.Rows[rCnt]["ExchangeHouse"];
                            remarks = pinno + " EFT " + beneName;

                            addDataMain(row_num, 1, pinno, "A" + row_num, "A" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 2, remitterName, "B" + row_num, "B" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 3, district, "C" + row_num, "C" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 4, remitterCountry, "D" + row_num, "D" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 5, remitterAcNo, "E" + row_num, "E" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 6, beneName, "F" + row_num, "F" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 7, accountNo, "G" + row_num, "G" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 8, bank, "H" + row_num, "H" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 9, branch, "I" + row_num, "I" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 10, routing, "J" + row_num, "J" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 11, currCode, "K" + row_num, "K" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 12, String.Format("{0:0.00}", amount), "L" + row_num, "L" + row_num, "###.##", _worksheet, _workSheetRange);
                            addDataMain(row_num, 13, exchangeName, "M" + row_num, "M" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 14, remarks, "N" + row_num, "N" + row_num, "@", _worksheet, _workSheetRange);

                            row_num++;

                            lblRptDataSaveProgress.Text = "Saving " + pinno + " -( remaining " + (dtEFTTransactions.Rows.Count - rCnt) + ")";

                        }//for end

                        _excelApp.ActiveWorkbook.SaveCopyAs(fileLocationTemp);
                        _excelApp.ActiveWorkbook.Saved = true;

                        //---------------- remove extra empty row -----------------------
                        RemoveExtraEmptyRowFromSheetForMainData(fileLocationTemp, "A6:N6");
                        //---------------------------------------------------------------

                        try
                        {
                            _workbook.Close(true, null, null); _excelApp.Quit();
                        }
                        finally
                        {
                            if (_worksheet != null) { Marshal.FinalReleaseComObject(_worksheet); _worksheet = null; }
                            if (_workbook != null) { Marshal.FinalReleaseComObject(_workbook); _workbook = null; }
                            if (_excelApp != null) { Marshal.FinalReleaseComObject(_excelApp); _excelApp = null; }
                        }

                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();
                        GC.WaitForPendingFinalizers();

                    }
                    catch (Exception exc)
                    {
                        string err = exc.ToString();
                        MessageBox.Show(exc.ToString());
                    }

                } //if (dtEFTTransactions.Rows.Count > 0)

                if (dtMTBTransactions.Rows.Count > 0)
                {
                    string fileName = "Daliy AC CREDIT payment " + today + ".xls";
                    string fileLocationTemp = folderName + "\\" + fileName;

                    try
                    {
                        Microsoft.Office.Interop.Excel.Application _excelApp = new Microsoft.Office.Interop.Excel.Application();
                        Microsoft.Office.Interop.Excel.Workbooks _workbooks = _excelApp.Workbooks;
                        Microsoft.Office.Interop.Excel.Workbook _workbook = _workbooks.Add();
                        Microsoft.Office.Interop.Excel.Worksheet _worksheet = _workbook.Worksheets[1];
                        _worksheet.Name = "Sheet1";
                        Microsoft.Office.Interop.Excel.Range _workSheetRange = _worksheet.get_Range("A1", "I1");

                        //TOPHeader(_worksheet, _workSheetRange, today);
                        int row_num = 1;
                        ADD_HEADER_ROW_MTB(row_num, _worksheet, _workSheetRange);
                        row_num++;
                        int firstTimeEmptyRow = 0;

                        object pinno, remitterName, district, remitterCountry, beneName, accountNo, branch, exchangeName;
                        double amount;

                        for (int rCnt = 0; rCnt < dtMTBTransactions.Rows.Count; rCnt++)
                        {
                            if (firstTimeEmptyRow == 0)
                            {
                                addDataMain(row_num, 1, "", "A" + row_num, "A" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 2, "", "B" + row_num, "B" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 3, "", "C" + row_num, "C" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 4, "", "D" + row_num, "D" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 5, "", "E" + row_num, "E" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 6, "", "F" + row_num, "F" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 7, "", "G" + row_num, "G" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 8, "", "H" + row_num, "H" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 9, "", "I" + row_num, "I" + row_num, "@", _worksheet, _workSheetRange);
                                row_num++;
                                firstTimeEmptyRow = 1;
                            }

                            /*
                             ExchangeHouse,RefNo,PaymentMode, BeneficiaryName, BeneficiaryAddress, BeneficiaryAccountNo,BeneficiaryContactNo,BankName,BranchName,"
                                RoutingNo,Amount,SenderName,SenderAddress,Purpose, UplodeBy, UploadTime, ProcessBy, ProcessTime,Remarks, NRTAccount 
                             */

                            pinno = dtMTBTransactions.Rows[rCnt]["RefNo"];
                            remitterName = dtMTBTransactions.Rows[rCnt]["SenderName"];
                            district = "";
                            remitterCountry = "";
                            amount = Convert.ToDouble(dtMTBTransactions.Rows[rCnt]["Amount"]);
                            beneName = dtMTBTransactions.Rows[rCnt]["BeneficiaryName"];
                            accountNo = dtMTBTransactions.Rows[rCnt]["BeneficiaryAccountNo"];
                            branch = dtMTBTransactions.Rows[rCnt]["BranchName"];
                            exchangeName = dtMTBTransactions.Rows[rCnt]["ExchangeHouse"];

                            addDataMain(row_num, 1, pinno, "A" + row_num, "A" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 2, remitterName, "B" + row_num, "B" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 3, district, "C" + row_num, "C" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 4, remitterCountry, "D" + row_num, "D" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 5, String.Format("{0:0.00}", amount), "E" + row_num, "E" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 6, beneName, "F" + row_num, "F" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 7, accountNo, "G" + row_num, "G" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 8, branch, "H" + row_num, "H" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 9, exchangeName, "I" + row_num, "I" + row_num, "@", _worksheet, _workSheetRange);
                            row_num++;

                            lblRptDataSaveProgress.Text = "Saving " + pinno + " -( remaining " + (dtMTBTransactions.Rows.Count - rCnt) + ")";

                        }//for end

                        _excelApp.ActiveWorkbook.SaveCopyAs(fileLocationTemp);
                        _excelApp.ActiveWorkbook.Saved = true;

                        //---------------- remove extra empty row -----------------------
                        RemoveExtraEmptyRowFromSheetForMainData(fileLocationTemp, "A2:I2");
                        //---------------------------------------------------------------

                        try
                        {
                            _workbook.Close(true, null, null); _excelApp.Quit();
                        }
                        finally
                        {
                            if (_worksheet != null) { Marshal.FinalReleaseComObject(_worksheet); _worksheet = null; }
                            if (_workbook != null) { Marshal.FinalReleaseComObject(_workbook); _workbook = null; }
                            if (_excelApp != null) { Marshal.FinalReleaseComObject(_excelApp); _excelApp = null; }
                        }

                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();
                        GC.WaitForPendingFinalizers();

                    }
                    catch (Exception exc)
                    {
                        string err = exc.ToString();
                        MessageBox.Show(exc.ToString());
                    }

                } //if (dtMTBTransactions.Rows.Count > 0)

                if (dtEFTTransactions.Rows.Count > 0 || dtMTBTransactions.Rows.Count > 0)
                {
                    MessageBox.Show("File saved at: " + folderName);
                    lblRptDataSaveProgress.Text = "";
                }

                Cursor.Current = Cursors.Default;
            }

        }

        private void ADD_HEADER_ROW_MTB(int rownum, Excel.Worksheet _worksheet, Excel.Range _workSheetRange)
        {
            createHeadersData(rownum, 1, "Ref/Pin No", "A" + rownum, "A" + rownum, 0, true, 15, _worksheet, _workSheetRange);
            createHeadersData(rownum, 2, "Remitter Name", "B" + rownum, "B" + rownum, 0, true, 12, _worksheet, _workSheetRange);
            createHeadersData(rownum, 3, "District", "C" + rownum, "C" + rownum, 0, true, 10, _worksheet, _workSheetRange);
            createHeadersData(rownum, 4, "Remitter Country", "D" + rownum, "D" + rownum, 0, true, 10, _worksheet, _workSheetRange);
            createHeadersData(rownum, 5, "Amount [BDT]", "E" + rownum, "E" + rownum, 0, true, 10, _worksheet, _workSheetRange);
            createHeadersData(rownum, 6, "Beneficiary Name ", "F" + rownum, "F" + rownum, 0, true, 15, _worksheet, _workSheetRange);
            createHeadersData(rownum, 7, "AC NUMBER", "G" + rownum, "G" + rownum, 0, true, 15, _worksheet, _workSheetRange);
            createHeadersData(rownum, 8, "BRANCH NAME", "H" + rownum, "H" + rownum, 0, true, 15, _worksheet, _workSheetRange);
            createHeadersData(rownum, 9, "Name Of Exchange House", "I" + rownum, "I" + rownum, 0, true, 15, _worksheet, _workSheetRange);
        }

        private void RemoveExtraEmptyRowFromSheetForMainData(string fileLocationTemp, string rowToRemove)
        {
            Excel.Application _app = new Excel.Application();
            Excel.Workbooks _books;
            Excel.Workbook _book = null;
            Excel.Sheets _sheets;
            Excel.Worksheet _sheet;

            _books = _app.Workbooks;
            _book = _books.Open(fileLocationTemp, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
            _sheets = _book.Worksheets;

            _sheet = (Excel.Worksheet)_sheets[1];
            _sheet.Select(Type.Missing);
            Excel.Range rangeDD = _sheet.get_Range(rowToRemove, Type.Missing);
            rangeDD.Delete(Excel.XlDeleteShiftDirection.xlShiftUp);

            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(rangeDD);
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(_sheet);
            _book.Save();
            _book.Close(false, Type.Missing, Type.Missing);

            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(_book);
            _app.Quit();
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(_app);

            Marshal.FinalReleaseComObject(_sheet);
            Marshal.FinalReleaseComObject(_book);
            Marshal.FinalReleaseComObject(_app);
            GC.Collect();
        }

        private void addDataMain(int row, int col, object data, string cell1, string cell2, string format,
            Excel.Worksheet _worksheet, Excel.Range _workSheetRange)
        {
            _worksheet.Cells[row, col] = data;
            _workSheetRange = _worksheet.get_Range(cell1, cell2);
            _workSheetRange.Borders.Color = System.Drawing.Color.Black.ToArgb();
            _workSheetRange.EntireColumn.NumberFormat = format;

            if (col == 12)
            {
                _workSheetRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignRight;
            }
        }

        private void ADD_HEADER_ROW_EFT(int rownum, Excel.Worksheet _worksheet, Excel.Range _workSheetRange)
        {
            createHeadersData(rownum, 1, "TT/Pin No.", "A" + rownum, "A" + rownum, 0, true, 15, _worksheet, _workSheetRange);
            createHeadersData(rownum, 2, "Remitter Full Name", "B" + rownum, "B" + rownum, 0, true, 12, _worksheet, _workSheetRange);
            createHeadersData(rownum, 3, "District", "C" + rownum, "C" + rownum, 0, true, 10, _worksheet, _workSheetRange);
            createHeadersData(rownum, 4, "Remitter Country Name", "D" + rownum, "D" + rownum, 0, true, 10, _worksheet, _workSheetRange);
            createHeadersData(rownum, 5, "Remitter A/C No", "E" + rownum, "E" + rownum, 0, true, 15, _worksheet, _workSheetRange);
            createHeadersData(rownum, 6, "BeneName ", "F" + rownum, "F" + rownum, 0, true, 15, _worksheet, _workSheetRange);
            createHeadersData(rownum, 7, "Account No", "G" + rownum, "G" + rownum, 0, true, 15, _worksheet, _workSheetRange);
            createHeadersData(rownum, 8, "Ben bank name", "H" + rownum, "H" + rownum, 0, true, 18, _worksheet, _workSheetRange);
            createHeadersData(rownum, 9, "Ben bank branch", "I" + rownum, "I" + rownum, 0, true, 15, _worksheet, _workSheetRange);
            createHeadersData(rownum, 10, "ROUTING NO", "J" + rownum, "J" + rownum, 0, true, 10, _worksheet, _workSheetRange);
            createHeadersData(rownum, 11, "Currency Code", "K" + rownum, "K" + rownum, 0, true, 10, _worksheet, _workSheetRange);
            createHeadersData(rownum, 12, "Amount", "L" + rownum, "L" + rownum, 0, true, 10, _worksheet, _workSheetRange);
            createHeadersData(rownum, 13, "Name Of Exchange House", "M" + rownum, "M" + rownum, 0, true, 15, _worksheet, _workSheetRange);
            createHeadersData(rownum, 14, "REMARKS", "N" + rownum, "N" + rownum, 0, true, 20, _worksheet, _workSheetRange);
        }

        private void createHeadersData(int row, int col, string htext, string cell1, string cell2, int mergeColumns, bool fontBold, int columnSize,
            Excel.Worksheet _worksheet, Excel.Range _workSheetRange)
        {
            _worksheet.Cells[row, col] = htext;
            _workSheetRange = _worksheet.get_Range(cell1, cell2);
            _workSheetRange.Merge(mergeColumns);
            _workSheetRange.Interior.Color = System.Drawing.Color.Gainsboro.ToArgb();

            _workSheetRange.Borders.Color = System.Drawing.Color.Black.ToArgb();
            _workSheetRange.Font.Bold = fontBold;
            _workSheetRange.ColumnWidth = columnSize;
            _workSheetRange.Font.Color = System.Drawing.Color.Black.ToArgb();
            _workSheetRange.Font.Size = 10;
        }

        private void TOPHeader(Excel.Worksheet _worksheet, Excel.Range _workSheetRange, string today)
        {
            _worksheet.Cells[1, 14] = "Mutual Trust Bank";
            _workSheetRange = _worksheet.get_Range("A1", "N1");
            _workSheetRange.Merge(0);
            //_workSheetRange.Interior.Color = Color.Gainsboro.ToArgb();
            //_workSheetRange.Borders.Color = Color.Black.ToArgb();
            //_workSheetRange.Font.Bold = true;
            _workSheetRange.Font.Color = Color.Black.ToArgb();
            //_workSheetRange.Font.Name = "Century Gothic";
            _worksheet.get_Range("A1", "N1").Cells.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            //_workSheetRange.Font.Size = 12;

            _worksheet.Cells[2, 14] = "NRB Division";
            _workSheetRange = _worksheet.get_Range("A2", "N2");
            _workSheetRange.Merge(0);
            //_workSheetRange.Interior.Color = Color.Gainsboro.ToArgb();
            //_workSheetRange.Borders.Color = Color.Black.ToArgb();
            //_workSheetRange.Font.Bold = true;
            _workSheetRange.Font.Color = Color.Black.ToArgb();
            _worksheet.get_Range("A2", "N2").Cells.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;


            _worksheet.Cells[3, 14] = "Txn list for BEFTN";
            _workSheetRange = _worksheet.get_Range("A3", "N3");
            _workSheetRange.Merge(0);
            //_workSheetRange.Interior.Color = Color.Gainsboro.ToArgb();
            //_workSheetRange.Borders.Color = Color.Black.ToArgb();
            //_workSheetRange.Font.Bold = true;
            _workSheetRange.Font.Color = Color.Black.ToArgb();
            _worksheet.get_Range("A3", "N3").Cells.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;

            _worksheet.Cells[4, 14] = "Date:" + today;
            _workSheetRange = _worksheet.get_Range("A4", "N4");
            _workSheetRange.Merge(0);
            //_workSheetRange.Interior.Color = Color.Gainsboro.ToArgb();
            //_workSheetRange.Borders.Color = Color.Black.ToArgb();
            //_workSheetRange.Font.Bold = true;
            _workSheetRange.Font.Color = Color.Black.ToArgb();
            _worksheet.get_Range("A4", "N4").Cells.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
        }



        private void btnSearchFailedTxn_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;

            DateTime dateTime1 = DateTime.ParseExact(dTPickerFailedFrom.Text, "dd-MMM-yyyy", CultureInfo.InvariantCulture);
            DateTime dateTime2 = DateTime.ParseExact(dTPickerFailedTo.Text, "dd-MMM-yyyy", CultureInfo.InvariantCulture);

            string dtValue1 = dateTime1.ToString("yyyy-MM-dd");
            string dtValue2 = dateTime2.ToString("yyyy-MM-dd");

            DataTable reportDataFailTxn = mg.GetFailedReportData(dtValue1, dtValue2);
            dataGridViewFailedTxn.DataSource = null;
            dataGridViewFailedTxn.DataSource = reportDataFailTxn;

            dataGridViewFailedTxn.Columns["Mode"].Width = 45;
            dataGridViewFailedTxn.Columns["CurrentStatus"].Width = 75;
            dataGridViewFailedTxn.Columns["Amount"].Width = 65;
            dataGridViewFailedTxn.Columns["RoutingNo"].Width = 65;

            if (reportDataFailTxn.Rows.Count > 0)
            {
                btnDownloadFailedReport.Enabled = true;
            }
            else
            {
                btnDownloadFailedReport.Enabled = false;
            }

            lblRowCountFailedTxn.Text = "Total Rows: " + reportDataFailTxn.Rows.Count;
            Cursor.Current = Cursors.Default;
        }

        private void btnDownloadFailedReport_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;

            string dtValue1 = DateTime.ParseExact(dTPickerFailedFrom.Text, "dd-MMM-yyyy", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");
            string dtValue2 = DateTime.ParseExact(dTPickerFailedTo.Text, "dd-MMM-yyyy", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");

            DataTable reportDataFailTxn = mg.GetFailedReportData(dtValue1, dtValue2);

            if (reportDataFailTxn.Rows.Count > 0)
            {
                string folderName = "";
                var folderBrowserDialog1 = new FolderBrowserDialog();

                DialogResult result = folderBrowserDialog1.ShowDialog();
                if (result == DialogResult.OK)
                {
                    Cursor.Current = Cursors.WaitCursor;

                    folderName = folderBrowserDialog1.SelectedPath;
                    string today = DateTime.Now.ToString("dd.MM.yyyy");

                    string fileName = "Failed TXN List " + today + ".xls";
                    string fileLocationTemp = folderName + "\\" + fileName;

                    try
                    {
                        Microsoft.Office.Interop.Excel.Application _excelApp = new Microsoft.Office.Interop.Excel.Application();
                        Microsoft.Office.Interop.Excel.Workbooks _workbooks = _excelApp.Workbooks;
                        Microsoft.Office.Interop.Excel.Workbook _workbook = _workbooks.Add();
                        Microsoft.Office.Interop.Excel.Worksheet _worksheet = _workbook.Worksheets[1];
                        _worksheet.Name = "Sheet1";
                        Microsoft.Office.Interop.Excel.Range _workSheetRange = _worksheet.get_Range("A1", "T1");

                        int row_num = 1;
                        ADD_HEADER_ROW_FAIL(row_num, _worksheet, _workSheetRange);

                        row_num++;
                        int firstTimeEmptyRow = 0;
                        
                        for (int rCnt = 0; rCnt < reportDataFailTxn.Rows.Count; rCnt++)
                        {
                            if (firstTimeEmptyRow == 0)
                            {
                                addDataMain(row_num, 1, "", "A" + row_num, "A" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 2, "", "B" + row_num, "B" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 3, "", "C" + row_num, "C" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 4, "", "D" + row_num, "D" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 5, "", "E" + row_num, "E" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 6, "", "F" + row_num, "F" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 7, "", "G" + row_num, "G" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 8, "", "H" + row_num, "H" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 9, "", "I" + row_num, "I" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 10, "", "J" + row_num, "J" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 11, "", "K" + row_num, "K" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 12, "", "L" + row_num, "L" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 13, "", "M" + row_num, "M" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 14, "", "N" + row_num, "N" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 15, "", "O" + row_num, "O" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 16, "", "P" + row_num, "P" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 17, "", "Q" + row_num, "Q" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 18, "", "R" + row_num, "R" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 19, "", "S" + row_num, "S" + row_num, "@", _worksheet, _workSheetRange);
                                addDataMain(row_num, 20, "", "T" + row_num, "T" + row_num, "@", _worksheet, _workSheetRange);
                               
                                row_num++;
                                firstTimeEmptyRow = 1;
                            }                                                       

                            addDataMain(row_num, 1, reportDataFailTxn.Rows[rCnt]["ExchangeHouse"], "A" + row_num, "A" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 2, reportDataFailTxn.Rows[rCnt]["RefNo"], "B" + row_num, "B" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 3, reportDataFailTxn.Rows[rCnt]["Mode"], "C" + row_num, "C" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 4, reportDataFailTxn.Rows[rCnt]["Status"], "D" + row_num, "D" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 5, reportDataFailTxn.Rows[rCnt]["ProcessTime"], "E" + row_num, "E" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 6, reportDataFailTxn.Rows[rCnt]["Remarks"], "F" + row_num, "F" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 7, reportDataFailTxn.Rows[rCnt]["CurrentStatus"], "G" + row_num, "G" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 8, reportDataFailTxn.Rows[rCnt]["LastProcessTime"], "H" + row_num, "H" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 9, reportDataFailTxn.Rows[rCnt]["BeneficiaryName"], "I" + row_num, "I" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 10, reportDataFailTxn.Rows[rCnt]["BeneficiaryAccountNo"], "J" + row_num, "J" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 11, reportDataFailTxn.Rows[rCnt]["BankName"], "K" + row_num, "K" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 12, reportDataFailTxn.Rows[rCnt]["BranchName"], "L" + row_num, "L" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 13, reportDataFailTxn.Rows[rCnt]["RoutingNo"], "M" + row_num, "M" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 14, String.Format("{0:0.00}", reportDataFailTxn.Rows[rCnt]["Amount"]), "N" + row_num, "N" + row_num, "###.##", _worksheet, _workSheetRange);
                            addDataMain(row_num, 15, reportDataFailTxn.Rows[rCnt]["SenderName"], "O" + row_num, "O" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 16, reportDataFailTxn.Rows[rCnt]["Purpose"], "P" + row_num, "P" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 17, reportDataFailTxn.Rows[rCnt]["UplodeBy"], "Q" + row_num, "Q" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 18, reportDataFailTxn.Rows[rCnt]["UploadTime"], "R" + row_num, "R" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 19, reportDataFailTxn.Rows[rCnt]["ProcessBy"], "S" + row_num, "S" + row_num, "@", _worksheet, _workSheetRange);
                            addDataMain(row_num, 20, reportDataFailTxn.Rows[rCnt]["NRTAccount"], "T" + row_num, "T" + row_num, "@", _worksheet, _workSheetRange);
                            
                            row_num++;

                            //lblRptDataSaveProgress.Text = "Saving " + pinno + " -( remaining " + (reportDataFailTxn.Rows.Count - rCnt) + ")";

                        }//for end

                        _excelApp.ActiveWorkbook.SaveCopyAs(fileLocationTemp);
                        _excelApp.ActiveWorkbook.Saved = true;

                        //---------------- remove extra empty row -----------------------
                        RemoveExtraEmptyRowFromSheetForMainData(fileLocationTemp, "A2:T2");
                        //---------------------------------------------------------------

                        try
                        {
                            _workbook.Close(true, null, null); _excelApp.Quit();
                        }
                        finally
                        {
                            if (_worksheet != null) { Marshal.FinalReleaseComObject(_worksheet); _worksheet = null; }
                            if (_workbook != null) { Marshal.FinalReleaseComObject(_workbook); _workbook = null; }
                            if (_excelApp != null) { Marshal.FinalReleaseComObject(_excelApp); _excelApp = null; }
                        }

                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();
                        GC.WaitForPendingFinalizers();

                    }
                    catch (Exception exc)
                    {
                        string err = exc.ToString();
                        MessageBox.Show(exc.ToString());
                    }

                }
                
                MessageBox.Show("File saved at: " + folderName);
                lblRptDataSaveProgress.Text = "";
            }

            Cursor.Current = Cursors.Default;
        }

        private void ADD_HEADER_ROW_FAIL(int rownum, Excel.Worksheet _worksheet, Excel.Range _workSheetRange)
        {
            createHeadersData(rownum, 1, "Exchange House", "A" + rownum, "A" + rownum, 0, true, 15, _worksheet, _workSheetRange);
            createHeadersData(rownum, 2, "Pin No.", "B" + rownum, "B" + rownum, 0, true, 15, _worksheet, _workSheetRange);
            createHeadersData(rownum, 3, "PaymentMode", "C" + rownum, "C" + rownum, 0, true, 10, _worksheet, _workSheetRange);
            createHeadersData(rownum, 4, "Status ", "D" + rownum, "D" + rownum, 0, true, 10, _worksheet, _workSheetRange);
            createHeadersData(rownum, 5, "ProcessTime ", "E" + rownum, "E" + rownum, 0, true, 10, _worksheet, _workSheetRange);
            createHeadersData(rownum, 6, "Remarks", "F" + rownum, "F" + rownum, 0, true, 20, _worksheet, _workSheetRange);
            createHeadersData(rownum, 7, "CurrentStatus", "G" + rownum, "G" + rownum, 0, true, 10, _worksheet, _workSheetRange);
            createHeadersData(rownum, 8, "LastProcessTime", "H" + rownum, "H" + rownum, 0, true, 15, _worksheet, _workSheetRange);
            createHeadersData(rownum, 9, "BeneficiaryName", "I" + rownum, "I" + rownum, 0, true, 15, _worksheet, _workSheetRange);
            createHeadersData(rownum, 10, "AccountNo", "J" + rownum, "J" + rownum, 0, true, 10, _worksheet, _workSheetRange);
            createHeadersData(rownum, 11, "BankName", "K" + rownum, "K" + rownum, 0, true, 10, _worksheet, _workSheetRange);
            createHeadersData(rownum, 12, "BranchName", "L" + rownum, "L" + rownum, 0, true, 12, _worksheet, _workSheetRange);
            createHeadersData(rownum, 13, "RoutingNo", "M" + rownum, "M" + rownum, 0, true, 12, _worksheet, _workSheetRange);
            createHeadersData(rownum, 14, "Amount", "N" + rownum, "N" + rownum, 0, true, 10, _worksheet, _workSheetRange);
            createHeadersData(rownum, 15, "SenderName", "O" + rownum, "O" + rownum, 0, true, 10, _worksheet, _workSheetRange);
            createHeadersData(rownum, 16, "Purpose", "P" + rownum, "P" + rownum, 0, true, 10, _worksheet, _workSheetRange);
            createHeadersData(rownum, 17, "UplodeBy", "Q" + rownum, "Q" + rownum, 0, true, 10, _worksheet, _workSheetRange);
            createHeadersData(rownum, 18, "UploadTime", "R" + rownum, "R" + rownum, 0, true, 10, _worksheet, _workSheetRange);
            createHeadersData(rownum, 19, "ProcessBy", "S" + rownum, "S" + rownum, 0, true, 10, _worksheet, _workSheetRange);
            createHeadersData(rownum, 20, "NRT Account", "T" + rownum, "T" + rownum, 0, true, 15, _worksheet, _workSheetRange);
        }

        private void btnFindByRefNo_Click(object sender, EventArgs e)
        {
            if(!txtSearchRefNo.Text.Trim().Equals(""))
            {
                string refno = txtSearchRefNo.Text.Trim();

                DataTable dtSearch = mg.GetUnAuthorizeDataByRefNo(refno);

                dGridViewFindRecord.DataSource = null;
                dGridViewFindRecord.DataSource = dtSearch;

                dGridViewFindRecord.Columns["Sl"].Width = 50;
                dGridViewFindRecord.Columns["Mode"].Width = 50;
                dGridViewFindRecord.Columns["Amount"].Width = 60;
                dGridViewFindRecord.Columns["RoutingNo"].Width = 70;
                dGridViewFindRecord.Columns["UplodeBy"].Width = 80;
                dGridViewFindRecord.Columns["UploadTime"].Width = 110;
                dGridViewFindRecord.Columns["IsSuccess"].Width = 70;
                dGridViewFindRecord.Columns["PartyId"].Width = 50;
            }
            else
            { 
                dGridViewFindRecord.DataSource = null; 
            }
        }

        private void btnSearchPendingTxnToAuth_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;

            DateTime dateTime1 = DateTime.ParseExact(dTPickerPendingFrom.Text, "dd-MMM-yyyy", CultureInfo.InvariantCulture);
            DateTime dateTime2 = DateTime.ParseExact(dTPickerPendingTo.Text, "dd-MMM-yyyy", CultureInfo.InvariantCulture);

            string dtValue1 = dateTime1.ToString("yyyy-MM-dd");
            string dtValue2 = dateTime2.ToString("yyyy-MM-dd");

            DataTable reportDataPendingTxn = mg.GetPendingTxnToAuthorizeReportData(dtValue1, dtValue2);
            dataGridViewPendingTxn.DataSource = null;
            dataGridViewPendingTxn.DataSource = reportDataPendingTxn;

            dataGridViewPendingTxn.Columns["Mode"].Width = 45;
            dataGridViewPendingTxn.Columns["Status"].Width = 70;

            lblRowCountPending.Text = "Total Rows: " + reportDataPendingTxn.Rows.Count;
            Cursor.Current = Cursors.Default;
        }

        private void userManagementToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmUserMgmt um = new frmUserMgmt();
            um.loggedUser = this.loggedUser;
            um.loggedUserIdAndName = this.loggedUserIdAndName;
            um.userType = this.userType;
            um.ShowDialog();
        }

        private void btnRemoveFailedMtbTxnFromSystem_Click(object sender, EventArgs e)
        {
            string pinNumToRemove = txtFailedTxnPINNumber.Text.Trim();
            if (!pinNumToRemove.Equals(""))
            {
                DialogResult result = MessageBox.Show("Are You Sure to Remove ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    DataTable dtRec = mg.GetFailedDataByRefNo(pinNumToRemove);
                    if (dtRec.Rows.Count > 0)
                    {
                        string paymode = dtRec.Rows[0]["PaymentMode"].ToString();
                        string stats = dtRec.Rows[0]["Status"].ToString();

                        if(paymode.Equals("MTB") && stats.Equals("FAILED"))
                        {
                            DataTable dtMtb = mg.GetOwnAccountRemitTransferInfo(pinNumToRemove);
                            if (dtMtb.Rows.Count > 0)
                            {
                                string paystat = dtMtb.Rows[0]["PaymentStatus"].ToString();
                                string isScs = dtMtb.Rows[0]["IsSuccess"].ToString();

                                if(paystat.Equals("1") && isScs.ToLower().Equals("false"))
                                {
                                    bool statFTManualDataDel = mg.DeleteFromManualEFTDataTable(pinNumToRemove, stats);
                                    bool statFTDataDel = mg.DeleteFromFundTransferDataTable(pinNumToRemove);
                                    MessageBox.Show("Transaction Removed From System", "Confirmation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                            }
                            else
                            {
                                bool statFTManualDataDel = mg.DeleteFromManualEFTDataTable(pinNumToRemove, stats);
                                MessageBox.Show("Transaction Removed From System", "Confirmation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                        else if(paymode.Equals("EFT") && stats.Equals("FAILED"))
                        {
                            DataTable dtEft = mg.GetEFTDataInfoFromBEFTNRequestTable(pinNumToRemove);
                            if (dtEft.Rows.Count > 0)
                            {
                                string paystat = dtEft.Rows[0]["PaymentStatus"].ToString();
                                string uploadby = dtEft.Rows[0]["UplodedBy"].ToString();

                                if (paystat.Equals("1") && (uploadby==null || uploadby.Equals("")) )
                                {
                                    bool statEFTDataDel = mg.DeleteFromBEFTNRequestTable(pinNumToRemove);
                                }
                            }

                            bool statEftDataDel = mg.DeleteFromManualEFTDataTable(pinNumToRemove, stats);
                            MessageBox.Show("Transaction Removed From System", "Confirmation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else if(paymode.Equals("CASH") && stats.Equals("FAILED"))
                        {
                            bool statManualDataDel = mg.DeleteFromManualEFTDataTable(pinNumToRemove, stats);
                            MessageBox.Show("Transaction Removed From System", "Confirmation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }

                    }//

                    btnSearchFailedTxn_Click(sender, e);
                    txtFailedTxnPINNumber.Text = "";

                } // if dialog end

            }//if end
            else
            {
                MessageBox.Show("No Input Given", "Empty Value", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

        }//

        private void btnModifyAcNumSearchByPin_Click(object sender, EventArgs e)
        {
            string pinNumToUpdate = txtPinNumberAccModify.Text.Trim();
            if (!pinNumToUpdate.Equals(""))
            {
                DataTable dtRec = mg.GetFailedDataByRefNo(pinNumToUpdate);
                if (dtRec.Rows.Count > 0)
                {
                    txtAccountNumberAccModify.Text = dtRec.Rows[0]["BeneficiaryAccountNo"].ToString().Trim();
                    btnUpdateAccountNumber.Enabled = true;
                }
                else
                {
                    btnUpdateAccountNumber.Enabled = false;
                }
            }
            else
            {
                MessageBox.Show("No Input Given", "Empty Value", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnUpdateAccountNumber_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are You Sure to Update ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                string ticks = Convert.ToString(DateTime.Now.Ticks);
                bool statEftTableModify = mg.ModifyAccountNumberAtEFTTable(txtPinNumberAccModify.Text.Trim(), txtAccountNumberAccModify.Text.Trim(), ticks);
                bool statFTDataDel = mg.DeleteFromFundTransferDataTable(txtPinNumberAccModify.Text.Trim());

                btnSearchFailedTxn_Click(sender, e);
                MessageBox.Show("Account Number Update Successfully.", "Confirmation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtPinNumberAccModify.Text = "";
                txtAccountNumberAccModify.Text = "";
            }
        }

        private void btnUpdateTxnStatusToReceived_Click(object sender, EventArgs e)
        {

            string pinNumToUpdate = txtPinNumberTxnStatModify.Text.Trim();
            if (!pinNumToUpdate.Equals(""))
            {
                DataTable dtRec = mg.GetDataByRefNo(pinNumToUpdate);
                if (dtRec.Rows.Count > 0 && dtRec.Rows[0]["Status"].ToString().Trim().Equals("FAILED"))
                {
                    DialogResult result = MessageBox.Show("Are You Sure to Update Transaction Status ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        string ticks = Convert.ToString(DateTime.Now.Ticks);
                        bool statEftTableModify = mg.UpdateTxnStatusToReceivedAtInputDataTable(pinNumToUpdate, ticks);
                        bool statFTDataDel = mg.DeleteFromFundTransferDataTable(pinNumToUpdate);

                        btnSearchFailedTxn_Click(sender, e);
                        MessageBox.Show("Transaction Status Updates Successfully.", "Confirmation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        txtPinNumberTxnStatModify.Text = "";
                    }
                }
                else
                {
                    MessageBox.Show("Transaction Not Found / Not in a state to Update Status", "Insufficient ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show("No Input Given", "Empty Value", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnEnableFailedTxnToProcessingStatusAsBULK_Click(object sender, EventArgs e)
        {
            DateTime dateTime1 = DateTime.ParseExact(dTPickerFailedFrom.Text, "dd-MMM-yyyy", CultureInfo.InvariantCulture);
            DateTime dateTime2 = DateTime.ParseExact(dTPickerFailedTo.Text, "dd-MMM-yyyy", CultureInfo.InvariantCulture);

            string dtValue1 = dateTime1.ToString("yyyy-MM-dd");
            string dtValue2 = dateTime2.ToString("yyyy-MM-dd");

            DataTable reportDataFailTxn = mg.GetFailedReportData(dtValue1, dtValue2);

            if (reportDataFailTxn.Rows.Count > 0)
            { 
                DialogResult result = MessageBox.Show("Are You Sure to Update Status ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    string ticks = Convert.ToString(DateTime.Now.Ticks);
                    string pinNumToUpdate = "";
                    bool statInputTableModify, statFTDataDel;
                    int updateRecCount = 0;

                    for (int ii = 0; ii < reportDataFailTxn.Rows.Count; ii++)
                    {
                        pinNumToUpdate = Convert.ToString(reportDataFailTxn.Rows[ii]["RefNo"]);
                        statInputTableModify = mg.UpdateTxnStatusToReceivedAtInputDataTable(pinNumToUpdate, ticks);
                        statFTDataDel = mg.DeleteFromFundTransferDataTable(pinNumToUpdate);

                        if (statInputTableModify)
                        {
                            updateRecCount++;
                        }
                    }

                    MessageBox.Show("Record Update: " + updateRecCount);
                }
            }

        }

        private void btnCopyToClipboard_Click(object sender, EventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                
                foreach (object row in listBoxError.Items)
                {
                    sb.Append(row.ToString());
                    sb.AppendLine();
                }
                sb.Remove(sb.Length - 1, 1); // Just to avoid copying last empty row
                Clipboard.SetData(System.Windows.Forms.DataFormats.Text, sb.ToString());
                MessageBox.Show("Data copied, Paste now");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }



        private void btnSearchCancelTxn_Click(object sender, EventArgs e)
        {
            if (!txtPinNoCancelTxnPage.Text.Trim().Equals(""))
            {
                string refno = txtPinNoCancelTxnPage.Text.Trim();
                DataTable dtSearch = mg.GetUnAuthorizeDataByRefNo(refno);

                dataGridViewCancelTxnDetail.DataSource = null;
                dataGridViewCancelTxnDetail.DataSource = dtSearch;

                dataGridViewCancelTxnDetail.Columns["Sl"].Width = 50;
                dataGridViewCancelTxnDetail.Columns["Mode"].Width = 50;
                dataGridViewCancelTxnDetail.Columns["Amount"].Width = 60;
                dataGridViewCancelTxnDetail.Columns["RoutingNo"].Width = 70;
                dataGridViewCancelTxnDetail.Columns["UplodeBy"].Width = 80;
                dataGridViewCancelTxnDetail.Columns["UploadTime"].Width = 110;
                dataGridViewCancelTxnDetail.Columns["IsSuccess"].Width = 70;
                dataGridViewCancelTxnDetail.Columns["PartyId"].Width = 50;
            }
            else
            {
                dataGridViewCancelTxnDetail.DataSource = null;
            }
        }

        //private void btnDeleteNotProcessedTxnFromSystem_Click(object sender, EventArgs e)
        //{
        //    string pinNumToRemove = txtPinNoCancelTxnPage.Text.Trim();
        //    if (!pinNumToRemove.Equals(""))
        //    {
        //        DialogResult result = MessageBox.Show("Are You Sure to Remove ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        //        if (result == DialogResult.Yes)
        //        {
        //            DataTable dtRec = mg.GetUnAuthorizeDataByRefNo(pinNumToRemove);
        //            if (dtRec.Rows.Count > 0)
        //            {
        //                string stats = dtRec.Rows[0]["Status"].ToString();
        //                if(stats.Equals("RECEIVED"))
        //                {
        //                    bool statFTManualDataDel = mg.DeleteFromManualEFTDataTable(pinNumToRemove, stats);
        //                    MessageBox.Show("Transaction Removed From System", "Confirmation", MessageBoxButtons.OK, MessageBoxIcon.Information);
        //                    dataGridViewCancelTxnDetail.DataSource = null;
        //                }
        //            }
        //        }
        //    }//if end
        //    else
        //    {
        //        MessageBox.Show("No Input Given", "Empty Value", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //    }
        //}

        private void btnCancelNotProcessedTxnIntoSystem_Click(object sender, EventArgs e)
        {
            string pinNumToCancel = txtPinNoCancelTxnPage.Text.Trim();
            if (!pinNumToCancel.Equals(""))
            {
                DialogResult result = MessageBox.Show("Are You Sure to CANCEL ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    DataTable dtRec = mg.GetUnAuthorizeDataByRefNo(pinNumToCancel);
                    if (dtRec.Rows.Count > 0)
                    {
                        string stats = dtRec.Rows[0]["Status"].ToString();
                        if (stats.Equals("RECEIVED"))
                        {
                            bool statFTManualDataCancel = mg.CancelTxnManualEFTDataTable(pinNumToCancel, stats);
                            MessageBox.Show("Transaction CANCELLED From System", "Confirmation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            dataGridViewCancelTxnDetail.DataSource = null;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Nothing to Cancel", "Empty Value", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }//if end
            else
            {
                MessageBox.Show("No Input Given", "Empty Value", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }


        private void btnSearchHoldTxn_Click(object sender, EventArgs e)
        {
            if (!txtPinNoHoldTxnPage.Text.Trim().Equals(""))
            {
                string refno = txtPinNoHoldTxnPage.Text.Trim();
                DataTable dtSearch = mg.GetUnAuthorizeDataByRefNo(refno);

                dataGridViewHoldTxnDetail.DataSource = null;
                dataGridViewHoldTxnDetail.DataSource = dtSearch;

                dataGridViewHoldTxnDetail.Columns["Sl"].Width = 50;
                dataGridViewHoldTxnDetail.Columns["Mode"].Width = 50;
                dataGridViewHoldTxnDetail.Columns["Amount"].Width = 60;
                dataGridViewHoldTxnDetail.Columns["RoutingNo"].Width = 70;
                dataGridViewHoldTxnDetail.Columns["UplodeBy"].Width = 80;
                dataGridViewHoldTxnDetail.Columns["UploadTime"].Width = 110;
                dataGridViewHoldTxnDetail.Columns["IsSuccess"].Width = 70;
                dataGridViewHoldTxnDetail.Columns["PartyId"].Width = 50;
            }
            else
            {
                dataGridViewHoldTxnDetail.DataSource = null;
            }
        }

        private void btnHoldNotProcessedTxnFromSystem_Click(object sender, EventArgs e)
        {
            string pinNumToHold = txtPinNoHoldTxnPage.Text.Trim();
            if (!pinNumToHold.Equals(""))
            {
                DialogResult result = MessageBox.Show("Are You Sure to HOLD ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    DataTable dtRec = mg.GetUnAuthorizeDataByRefNo(pinNumToHold);
                    if (dtRec.Rows.Count > 0)
                    {
                        string stats = dtRec.Rows[0]["Status"].ToString();
                        if (stats.Equals("RECEIVED"))
                        {
                            bool statFTManualDataHold = mg.HoldTxnManualEFTDataTable(pinNumToHold, stats);
                            MessageBox.Show("Transaction HOLD at System", "Confirmation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            dataGridViewHoldTxnDetail.DataSource = null;

                            dtPickerFromHoldTxn.Value = DateTime.Now;
                            dtPickerToHoldTxn.Value = DateTime.Now;
                            btnSearchHoldTxnList_Click(sender, e);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Nothing to HOLD", "Empty Value", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }//if end
            else
            {
                MessageBox.Show("No Input Given", "Empty Value", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnSearchCancelledList_Click(object sender, EventArgs e)
        {
            DateTime dateTime1 = DateTime.ParseExact(dtPickerFromCancelList.Text, "dd-MMM-yyyy", CultureInfo.InvariantCulture);
            DateTime dateTime2 = DateTime.ParseExact(dtPickerToCancelList.Text, "dd-MMM-yyyy", CultureInfo.InvariantCulture);

            string dtValue1 = dateTime1.ToString("yyyy-MM-dd");
            string dtValue2 = dateTime2.ToString("yyyy-MM-dd");

            DataTable dtCancelledTxn = mg.GetCancelledTxnDataByDateRange(dtValue1, dtValue2);
            dataGridViewCancelledTxnList.DataSource = null;
            dataGridViewCancelledTxnList.DataSource = dtCancelledTxn;

            dataGridViewCancelledTxnList.Columns["Sl"].Width = 40;
            dataGridViewCancelledTxnList.Columns["Mode"].Width = 50;
            dataGridViewCancelledTxnList.Columns["Amount"].Width = 60;
            dataGridViewCancelledTxnList.Columns["Status"].Width = 80;
            dataGridViewCancelledTxnList.Columns["UploderId"].Width = 70;
            
        }

        private void btnSearchHoldTxnList_Click(object sender, EventArgs e)
        {
            DateTime dateTime1 = DateTime.ParseExact(dtPickerFromHoldTxn.Text, "dd-MMM-yyyy", CultureInfo.InvariantCulture);
            DateTime dateTime2 = DateTime.ParseExact(dtPickerToHoldTxn.Text, "dd-MMM-yyyy", CultureInfo.InvariantCulture);

            string dtValue1 = dateTime1.ToString("yyyy-MM-dd");
            string dtValue2 = dateTime2.ToString("yyyy-MM-dd");

            DataTable dtHoldTxn = mg.GetHoldTxnDataByDateRange(dtValue1, dtValue2);
            dataGridViewHoldTxnList.DataSource = null;
            dataGridViewHoldTxnList.DataSource = dtHoldTxn;

            dataGridViewHoldTxnList.Columns["Sl"].Width = 40;
            dataGridViewHoldTxnList.Columns["Mode"].Width = 50;
            dataGridViewHoldTxnList.Columns["Amount"].Width = 60;
            dataGridViewHoldTxnList.Columns["Status"].Width = 60;
            dataGridViewHoldTxnList.Columns["HoldDate"].Width = 110;
            dataGridViewHoldTxnList.Columns["UploderId"].Width = 70;
        }

        private void btnUnHoldTxn_Click(object sender, EventArgs e)
        {
            string pinNumToUnHold = txtPinNoUnHoldTxn.Text.Trim();
            if (!pinNumToUnHold.Equals(""))
            {
                DialogResult result = MessageBox.Show("Are You Sure to UnHold ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    DataTable dtRec = mg.GetHoldDataByRefNo(pinNumToUnHold);
                    if (dtRec.Rows.Count > 0)
                    {
                        string stats = dtRec.Rows[0]["Status"].ToString();
                        if (stats.Equals("HOLD"))
                        {
                            string ticks = Convert.ToString(DateTime.Now.Ticks);
                            bool statFTManualDataUnHold = mg.UnHoldTxnManualEFTDataTable(pinNumToUnHold, stats, ticks);
                            MessageBox.Show("Transaction UNHOLD From System", "Confirmation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            
                            txtPinNoUnHoldTxn.Text = "";
                            dtPickerFromHoldTxn.Value = DateTime.Now;
                            dtPickerToHoldTxn.Value = DateTime.Now;
                            btnSearchHoldTxnList_Click(sender, e);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Nothing to UNHOLD", "Empty Value", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }//if end
            else
            {
                MessageBox.Show("No Input Given", "Empty Value", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        


    }
}
