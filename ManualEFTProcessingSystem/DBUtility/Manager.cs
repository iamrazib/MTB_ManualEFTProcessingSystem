using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManualEFTProcessingSystem.DBUtility
{
    class Manager
    {        
        static ConnectionInfo connInfo = new ConnectionInfo();

        public string nrbworkConnectionString = connInfo.getNrbWorkConnString();
        public string drConnectionString = connInfo.getConnStringDR();
        public string remittanceDbLvConnectionString = connInfo.getConnStringRemitDbLv();

        //public string nrbworkConnectionString = Utility.DecryptString(connectionStringNrbWork);
        //public string drConnectionString = Utility.DecryptString(connectionStringDR);
        //public string remittanceDbLvConnectionString = Utility.DecryptString(connectionStringRemitDbLv);


        MTBDBManager dbManager = null;

        internal DataTable GetPermittedUserList()
        {
            DataTable dt = new DataTable();
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();
                string query = "SELECT [UserId]+' - '+[UserName]+'  ['+[UserType]+']' FROM [NRBWork].[dbo].[ManualEFTProcessUserCredential] WHERE [isActive]=1";
                dt = dbManager.GetDataTable(query.Trim());
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return dt;
        }

        internal DataTable GetAllUserList()
        {
            DataTable dt = new DataTable();
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();
                string query = "SELECT [UserId]+' - '+[UserName] FROM [NRBWork].[dbo].[ManualEFTProcessUserCredential] ORDER BY [AutoId]";
                dt = dbManager.GetDataTable(query.Trim());
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return dt;
        }

        internal bool isPasswordMatch(string userId, string pass, ref string userType, ref string isPwdChanged)
        {
            bool isFound = false;
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();

                string queryDateCheck = "SELECT * FROM [NRBWork].[dbo].[ManualEFTProcessUserCredential]  WHERE [UserId]='" + userId + "' AND [UserPassword]='" + pass + "'";
                DataTable dt = dbManager.GetDataTable(queryDateCheck);
                if (dt.Rows.Count > 0)
                {
                    isFound = true;
                    userType = dt.Rows[0]["UserType"].ToString();
                    isPwdChanged = dt.Rows[0]["isPasswordChanged"].ToString();
                }
                else
                    isFound = false;
            }
            catch (Exception ex)
            {
                isFound = false;
            }
            finally
            {
                dbManager.CloseDatabaseConnection();
            }
            return isFound;
        }



        internal bool ChangeUserPassword(string loggedUserChPass, string currPass, string newPass)
        {
            SqlConnection _sqlConnection = null;
            SqlCommand cmdUpdateData = new SqlCommand();
            bool isSuccess = false;

            try
            {
                _sqlConnection = new SqlConnection(nrbworkConnectionString);
                if (_sqlConnection.State.Equals(ConnectionState.Closed)) { _sqlConnection.Open(); }

                string queryUpdate = "UPDATE [NRBWork].[dbo].[ManualEFTProcessUserCredential] SET [isPasswordChanged]='Y', [UserPassword]='" + newPass + "' WHERE [UserId]='" + loggedUserChPass + "' AND [UserPassword]='" + currPass + "'";

                cmdUpdateData.CommandText = queryUpdate;
                cmdUpdateData.Connection = _sqlConnection;

                try
                {
                    int updateOk = cmdUpdateData.ExecuteNonQuery();
                    if (updateOk == 0)
                        isSuccess = false;
                    else
                        isSuccess = true;
                }
                catch (Exception ec) { throw ec; }
            }
            catch (Exception ex) { isSuccess = false; }
            finally
            {
                try { if (_sqlConnection != null && _sqlConnection.State == ConnectionState.Open) { _sqlConnection.Close(); } }
                catch (SqlException sqlException) { throw sqlException; }
            }
            return isSuccess;
        }

        internal DataTable LoadExhouseList()
        {
            DataTable dt = new DataTable();
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();
                string query = " SELECT ltrim(rtrim(STR([PartyId])))+' - '+[ExchangeHouseName] FROM [NRBWork].[dbo].[ManualEFTBasedExchangeHouseInfo] WHERE [isActive]=1";
                dt = dbManager.GetDataTable(query.Trim());
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return dt;
        }

        internal string GetBranchNameByRoutingCode(string routingNo)
        {
            DataTable dt = new DataTable();
            string branchName = "";
            string sqlQuery = string.Empty;
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, drConnectionString);
                dbManager.OpenDatabaseConnection();

                sqlQuery = "SELECT [MTB Sl No],[MTB Code],[Bank Code],[Agent Name],[Branch Name],[City Name],[District],[Routing Number],[Country] "
                    + " FROM [RemittanceDB].[dbo].[BANK_BRANCH] WHERE [Routing Number]='" + routingNo + "'";
                dt = dbManager.GetDataTable(sqlQuery.Trim());

                branchName = dt.Rows[0]["Branch Name"].ToString();
            }
            catch (Exception exception)
            {
                //InsertAutoFetchLog(userId, "GetBranchNameByRoutingCode", "Error ! GetBranchNameByRoutingCode fetch Error. " + ", " + exception.ToString());
            }
            finally
            {
                dbManager.CloseDatabaseConnection();
            }
            return branchName;
        }

        /*
        internal bool SaveExhData(int exhId, DataTable tellerScreenExhFileData, string loggedUser)
        {
            string refNo, BeneficiaryName, AccountNo, BankName, BranchName, RoutingNo, BeneficiaryAddress, RemitterName, RemitterAddress, Purpose, PayMode;
            Double Amount;
            string saveData = "";
            SqlConnection openCon = new SqlConnection(nrbworkConnectionString);
            SqlCommand cmdSaveData = new SqlCommand();

            if (openCon.State.Equals(ConnectionState.Closed))
            {
                openCon.Open();
            }

            for (int ii = 0; ii < tellerScreenExhFileData.Rows.Count; ii++)
            {

                refNo = Convert.ToString(tellerScreenExhFileData.Rows[ii][0]);
                BeneficiaryName = Convert.ToString(tellerScreenExhFileData.Rows[ii][1]);
                AccountNo = Convert.ToString(tellerScreenExhFileData.Rows[ii][2]);
                BankName = Convert.ToString(tellerScreenExhFileData.Rows[ii][3]);
                BranchName = Convert.ToString(tellerScreenExhFileData.Rows[ii][4]);
                RoutingNo = Convert.ToString(tellerScreenExhFileData.Rows[ii][5]);
                Amount = Math.Round(Convert.ToDouble(tellerScreenExhFileData.Rows[ii][6]), 2);
                BeneficiaryAddress = Convert.ToString(tellerScreenExhFileData.Rows[ii][7]);
                RemitterName = Convert.ToString(tellerScreenExhFileData.Rows[ii][8]);
                RemitterAddress = Convert.ToString(tellerScreenExhFileData.Rows[ii][9]);
                Purpose = Convert.ToString(tellerScreenExhFileData.Rows[ii][10]);
                PayMode = Convert.ToString(tellerScreenExhFileData.Rows[ii][11]);

                saveData = "INSERT INTO [dbo].[ManualEFTFileData]([PartyId],[RefNo],[PaymentMode],[BeneficiaryName],[BeneficiaryAddress],[BeneficiaryAccountNo],[BankName],"
                    + " [BranchName],[RoutingNo],[Amount],[SenderName],[SenderAddress],[Purpose],[UplodeBy],[UploadTime],[IsSuccess],[Status])"
                    + " VALUES(@PartyId,@RefNo,@PaymentMode,@BeneficiaryName,@BeneficiaryAddress,@BeneficiaryAccountNo,@BankName,@BranchName,@RoutingNo,@Amount,"
                    + " @SenderName,@SenderAddress,@Purpose,@UplodeBy,@UploadTime,@IsSuccess,@Status)";

                cmdSaveData = new SqlCommand();
                cmdSaveData.CommandText = saveData;
                cmdSaveData.Connection = openCon;

                cmdSaveData.Parameters.Add("@PartyId", SqlDbType.Int).Value = exhId;
                cmdSaveData.Parameters.Add("@RefNo", SqlDbType.VarChar).Value = (refNo == null ? "" : refNo.ToString().Trim());
                cmdSaveData.Parameters.Add("@PaymentMode", SqlDbType.VarChar).Value = (PayMode == null ? "" : PayMode.ToString().Trim());
                cmdSaveData.Parameters.Add("@BeneficiaryName", SqlDbType.VarChar).Value = (BeneficiaryName == null ? "" : BeneficiaryName.ToString().Trim());
                cmdSaveData.Parameters.Add("@BeneficiaryAddress", SqlDbType.VarChar).Value = (BeneficiaryAddress == null ? "" : BeneficiaryAddress.ToString().Trim());
                cmdSaveData.Parameters.Add("@BeneficiaryAccountNo", SqlDbType.VarChar).Value = (AccountNo == null ? "" : AccountNo.ToString().Trim());
                cmdSaveData.Parameters.Add("@BankName", SqlDbType.VarChar).Value = (BankName == null ? "" : BankName.ToString().Trim());
                cmdSaveData.Parameters.Add("@BranchName", SqlDbType.VarChar).Value = (BranchName == null ? "" : BranchName.ToString().Trim());
                cmdSaveData.Parameters.Add("@RoutingNo", SqlDbType.VarChar).Value = (RoutingNo == null ? "" : RoutingNo.ToString().Trim());
                cmdSaveData.Parameters.Add("@Amount", SqlDbType.Float).Value = Amount;
                cmdSaveData.Parameters.Add("@SenderName", SqlDbType.VarChar).Value = (RemitterName == null ? "" : RemitterName.ToString().Trim());
                cmdSaveData.Parameters.Add("@SenderAddress", SqlDbType.VarChar).Value = (RemitterAddress == null ? "" : RemitterAddress.ToString().Trim());
                cmdSaveData.Parameters.Add("@Purpose", SqlDbType.VarChar).Value = (Purpose == null ? "" : Purpose.ToString().Trim());
                cmdSaveData.Parameters.Add("@UplodeBy", SqlDbType.VarChar).Value = (loggedUser == null ? "" : loggedUser.ToString().Trim());
                cmdSaveData.Parameters.Add("@UploadTime", SqlDbType.DateTime).Value = DateTime.Now;
                cmdSaveData.Parameters.Add("@IsSuccess", SqlDbType.Bit).Value = true;
                cmdSaveData.Parameters.Add("@Status", SqlDbType.VarChar).Value = "RECEIVED";

                try
                {
                    cmdSaveData.ExecuteNonQuery();
                }
                catch (Exception ec)
                {
                    return false;
                }
            }

            return true;
        }
        */

        internal DataTable GetExhFileUnAuthorizeData(string dtValue1, string dtValue2, int exhId)
        {
            DataTable dt = new DataTable();
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();
                string query = "SELECT fd.[AutoId] Sl, ehi.[ExchangeHouseName] ExchangeHouse,fd.[RefNo],fd.[PaymentMode] Mode,fd.[BeneficiaryName], fd.[BeneficiaryAccountNo] AccountNo, fd.[BankName],"
                + " fd.[RoutingNo], fd.[Amount], (SELECT uc.[UserName] FROM [NRBWork].[dbo].[ManualEFTProcessUserCredential] uc where uc.[UserId]=fd.[UplodeBy])UplodeBy,"
                + " convert(varchar, [UploadTime], 120)UploadTime,fd.[IsSuccess], fd.[BranchName], fd.[PartyId], fd.[BeneficiaryAddress], fd.[SenderName], fd.[SenderAddress], fd.[Purpose], fd.[Status], fd.[BeneficiaryContactNo], fd.[UplodeBy] UplodeByUserId, fd.[BatchId] "
                + " FROM [NRBWork].[dbo].[ManualEFTFileData] fd inner join [NRBWork].[dbo].[ManualEFTBasedExchangeHouseInfo] ehi "
                + " ON fd.[PartyId]=ehi.[PartyId] "
                + " WHERE fd.[IsSuccess]=1 AND fd.[Status]='RECEIVED' "
                + " AND convert(date,fd.[UploadTime])>='" + dtValue1 + "' AND  convert(date,fd.[UploadTime])<='" + dtValue2 + "'";

                if (exhId != 0)
                {
                    query += " AND fd.[PartyId]=" + exhId;
                }

                query += " AND fd.[ProcessBy] IS NULL AND fd.[ProcessingTime] IS NULL ORDER BY fd.[AutoId] DESC";

                dt = dbManager.GetDataTable(query.Trim());
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return dt;
        }

        internal DataTable GetExchangeHouseAccountInfo(int partyId)
        {
            DataTable dt = new DataTable();
            string exhUserId = "", error;

            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, drConnectionString);
                dbManager.OpenDatabaseConnection();

                string query = "SELECT u.[UserId] FROM [RemittanceDB].[dbo].[Users] u WHERE u.isActive=1 AND u.[PartyId]=" + partyId + "";
                dt = dbManager.GetDataTable(query.Trim());
                if(dt.Rows.Count>0)
                {
                    exhUserId = dt.Rows[0][0].ToString();
                }
            }
            catch (Exception ex) {
                error = ex.Message;
            }
            finally{ dbManager.CloseDatabaseConnection(); }


            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();

                string query = "SELECT ehi.[AutoId], ehi.[ExchangeHouseName], ehi.[NRTAccount], ehi.[PartyId], ehi.[Password], '"+exhUserId+"' UserId "
                    + " FROM [NRBWork].[dbo].[ManualEFTBasedExchangeHouseInfo] ehi "
                    + " WHERE ehi.isActive=1 AND ehi.[PartyId]=" + partyId + " ORDER BY ehi.[AutoId]";
                dt = dbManager.GetDataTable(query.Trim());
            }
            catch (Exception ex){    }
            finally{ dbManager.CloseDatabaseConnection(); }
            return dt;
        }

        internal bool UpdateEFTFileDataStatusTxnTable(string autoID, string refNo, int partyId, string loggedUser, string paystatus, string remarks)
        {
            bool status = false;
            try
            {
                string query = "";
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();

                if (paystatus.Equals("SUCCESS"))
                {
                    query = "UPDATE [NRBWork].[dbo].[ManualEFTFileData] SET [Status]='PROCESSED', [Remarks]='" + remarks + "', [ProcessBy]='" + loggedUser + "', [ProcessingTime]=getdate()  WHERE [AutoId]=" + autoID + " AND [PartyId]=" + partyId + " AND [RefNo]='" + refNo + "'";
                }
                else
                {
                    query = "UPDATE [NRBWork].[dbo].[ManualEFTFileData] SET [Status]='FAILED',  [Remarks]='" + remarks + "', [ProcessBy]='" + loggedUser + "', [ProcessingTime]=getdate()  WHERE [AutoId]=" + autoID + " AND [PartyId]=" + partyId + " AND [RefNo]='" + refNo + "'";
                }

                status = dbManager.ExcecuteCommand(query);
            }
            catch (Exception ex)
            {
                //throw ex;
            }
            finally
            {
                dbManager.CloseDatabaseConnection();
            }
            return status;
        }

        internal DataTable GetOwnAccountRemitTransferInfo(string refrnNo)
        {
            DataTable dt = new DataTable();
            try
            {
                //dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, connectionStringRemitDbLv);
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, remittanceDbLvConnectionString);                
                dbManager.OpenDatabaseConnection();
                string sqlQuery = "select * from [RemittanceDB].[dbo].[FundTransferRequest] where ltrim(rtrim([RefNo]))='" + refrnNo.Trim() + "'";
                dt = dbManager.GetDataTable(sqlQuery.Trim());
            }
            catch (Exception ex)
            {   }
            finally
            {
                dbManager.CloseDatabaseConnection();
            }
            return dt;
        }

        internal bool IsThisTransactionExistBefore(int exhId, string refNo)
        {
            DataTable dt = new DataTable();
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();

                string query = "SELECT [AutoId],[RefNo] FROM [NRBWork].[dbo].[ManualEFTFileData] WHERE [PartyId]=" + exhId + " AND [RefNo]='" + refNo + "' AND [Status] IN('PROCESSED','RECEIVED') ";
                dt = dbManager.GetDataTable(query.Trim());
            }
            catch (Exception ex){  }
            finally{  dbManager.CloseDatabaseConnection();  }

            if (dt.Rows.Count > 0)
                return true; // txn exist before
            return false; // txn not exist, go forward
        }

        internal bool SaveExhData(int exhId, DataRow dataRow, string loggedUser, string BatchId)
        {
            string refNo, BeneficiaryName, AccountNo, BankName, BranchName, RoutingNo, BeneficiaryAddress, RemitterName, RemitterAddress, Purpose, PayMode, BeneficiaryContactNo;
            Double Amount;
            string saveData = "";
            SqlConnection openCon = new SqlConnection(nrbworkConnectionString);
            SqlCommand cmdSaveData = new SqlCommand();

            if (openCon.State.Equals(ConnectionState.Closed))
            {
                openCon.Open();
            }
                       

            refNo = Convert.ToString(dataRow["RefNo"]);
            BeneficiaryName = Convert.ToString(dataRow["BeneficiaryName"]);
            AccountNo = Convert.ToString(dataRow["AccountNo"]);
            BankName = Convert.ToString(dataRow["BankName"]);
            BranchName = Convert.ToString(dataRow["BranchName"]);
            RoutingNo = Convert.ToString(dataRow["RoutingNo"]);
            Amount = Math.Round(Convert.ToDouble(dataRow["Amount"]), 2);
            BeneficiaryAddress = Convert.ToString(dataRow["BeneficiaryAddress"]);
            RemitterName = Convert.ToString(dataRow["RemitterName"]);
            RemitterAddress = Convert.ToString(dataRow["RemitterAddress"]);
            Purpose = Convert.ToString(dataRow["Purpose"]);
            PayMode = Convert.ToString(dataRow["PayMode"]);
            BeneficiaryContactNo = Convert.ToString(dataRow["BeneficiaryContactNo"]);

            saveData = "INSERT INTO [dbo].[ManualEFTFileData]([PartyId],[RefNo],[PaymentMode],[BeneficiaryName],[BeneficiaryAddress],[BeneficiaryAccountNo],[BankName],"
                + " [BranchName],[RoutingNo],[Amount],[SenderName],[SenderAddress],[Purpose],[UplodeBy],[UploadTime],[IsSuccess],[Status],[BeneficiaryContactNo],[BatchId])"
                + " VALUES(@PartyId,@RefNo,@PaymentMode,@BeneficiaryName,@BeneficiaryAddress,@BeneficiaryAccountNo,@BankName,@BranchName,@RoutingNo,@Amount,"
                + " @SenderName,@SenderAddress,@Purpose,@UplodeBy,@UploadTime,@IsSuccess,@Status,@BeneficiaryContactNo,@BatchId)";

            cmdSaveData = new SqlCommand();
            cmdSaveData.CommandText = saveData;
            cmdSaveData.Connection = openCon;

            cmdSaveData.Parameters.Add("@PartyId", SqlDbType.Int).Value = exhId;
            cmdSaveData.Parameters.Add("@RefNo", SqlDbType.VarChar).Value = (refNo == null ? "" : refNo.ToString().Trim());
            cmdSaveData.Parameters.Add("@PaymentMode", SqlDbType.VarChar).Value = (PayMode == null ? "" : PayMode.ToString().Trim());
            cmdSaveData.Parameters.Add("@BeneficiaryName", SqlDbType.VarChar).Value = (BeneficiaryName == null ? "" : BeneficiaryName.ToString().Trim());
            cmdSaveData.Parameters.Add("@BeneficiaryAddress", SqlDbType.VarChar).Value = (BeneficiaryAddress == null ? "" : BeneficiaryAddress.ToString().Trim());
            cmdSaveData.Parameters.Add("@BeneficiaryAccountNo", SqlDbType.VarChar).Value = (AccountNo == null ? "" : AccountNo.ToString().Trim());
            cmdSaveData.Parameters.Add("@BankName", SqlDbType.VarChar).Value = (BankName == null ? "" : BankName.ToString().Trim());
            cmdSaveData.Parameters.Add("@BranchName", SqlDbType.VarChar).Value = (BranchName == null ? "" : BranchName.ToString().Trim());
            cmdSaveData.Parameters.Add("@RoutingNo", SqlDbType.VarChar).Value = (RoutingNo == null ? "" : RoutingNo.ToString().Trim());
            cmdSaveData.Parameters.Add("@Amount", SqlDbType.Float).Value = Amount;
            cmdSaveData.Parameters.Add("@SenderName", SqlDbType.VarChar).Value = (RemitterName == null ? "" : RemitterName.ToString().Trim());
            cmdSaveData.Parameters.Add("@SenderAddress", SqlDbType.VarChar).Value = (RemitterAddress == null ? "" : RemitterAddress.ToString().Trim());
            cmdSaveData.Parameters.Add("@Purpose", SqlDbType.VarChar).Value = (Purpose == null ? "" : (Purpose.ToString().Trim().Length > 100 ? Purpose.ToString().Trim().Substring(0, 95) : Purpose.ToString().Trim()));
            cmdSaveData.Parameters.Add("@BeneficiaryContactNo", SqlDbType.VarChar).Value = (BeneficiaryContactNo == null ? "" : (BeneficiaryContactNo.ToString().Trim().Length > 50 ? BeneficiaryContactNo.ToString().Trim().Substring(48) : BeneficiaryContactNo.ToString().Trim()));
            cmdSaveData.Parameters.Add("@UplodeBy", SqlDbType.VarChar).Value = (loggedUser == null ? "" : loggedUser.ToString().Trim());
            cmdSaveData.Parameters.Add("@UploadTime", SqlDbType.DateTime).Value = DateTime.Now;
            cmdSaveData.Parameters.Add("@IsSuccess", SqlDbType.Bit).Value = true;
            cmdSaveData.Parameters.Add("@Status", SqlDbType.VarChar).Value = "UPLOADED";
            cmdSaveData.Parameters.Add("@BatchId", SqlDbType.VarChar).Value = BatchId;

            try
            {
                cmdSaveData.ExecuteNonQuery();
            }
            catch (Exception ec)
            {
                return false;
            }
            finally
            {
                try { if (openCon != null && openCon.State == ConnectionState.Open) { openCon.Close(); } }
                catch (SqlException sqlException) { throw sqlException; }
            }                       

            return true;
        }

        internal DataTable GetAuthorizerAndSuperAdminEmailList()
        {
            DataTable dt = new DataTable();
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();
                string query = "SELECT [UserId],[UserName],[UserType],[UserEmail] FROM [NRBWork].[dbo].[ManualEFTProcessUserCredential] WHERE  ([UserType]='Authorizer' OR [UserType]='Admin' OR [UserType]='SuperAdmin') AND [isActive]=1";
                dt = dbManager.GetDataTable(query.Trim());
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return dt;
        }

        
        internal DataTable GetLoggedUserInfoEmail(string loggedUser)
        {
            DataTable dt = new DataTable();
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();
                string query = "SELECT [UserId],[UserName],[UserType],[UserEmail] FROM [NRBWork].[dbo].[ManualEFTProcessUserCredential] WHERE  [UserId]='" + loggedUser + "'";
                dt = dbManager.GetDataTable(query.Trim());
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return dt;
        }

        internal bool saveCashDataIntoNRBWorkDbFileBasedCashTxnDataTable(string refno, string sendername, string benfname, decimal receivingAmount, string bank, 
            string branch, string exhouse, string batchId, string loggedUser)
        {
            bool insertSuccess = false;
            string saveData = "";
            SqlConnection _sqlConnection = null;
            SqlCommand cmdSaveData = new SqlCommand();

            try
            {
                _sqlConnection = new SqlConnection(nrbworkConnectionString);
                if (_sqlConnection.State.Equals(ConnectionState.Closed))
                {
                    _sqlConnection.Open();
                }

                saveData = "INSERT INTO [dbo].[FileBasedCashTxnData]([PINNumber],[SenderName],[BeneficiaryName],[Amount],[BankName],[BranchName],[ExchangeHouse],[InputDate],[TxnStatus],[BatchId],[TxnInsertDate],[UploadUserId]) "
                    + " VALUES (@PINNumber,@SenderName,@BeneficiaryName,@Amount,@BankName,@BranchName,@ExchangeHouse,@InputDate,@TxnStatus,@BatchId,@TxnInsertDate,@UploadUserId)";

                cmdSaveData.CommandText = saveData;
                cmdSaveData.Connection = _sqlConnection;

                cmdSaveData.Parameters.Add("@PINNumber", SqlDbType.VarChar).Value = refno.Trim();
                cmdSaveData.Parameters.Add("@SenderName", SqlDbType.VarChar).Value = sendername.Trim();
                cmdSaveData.Parameters.Add("@BeneficiaryName", SqlDbType.VarChar).Value = benfname.Trim();
                cmdSaveData.Parameters.Add("@Amount", SqlDbType.Float).Value = receivingAmount;
                cmdSaveData.Parameters.Add("@BankName", SqlDbType.VarChar).Value = bank.Trim();
                cmdSaveData.Parameters.Add("@BranchName", SqlDbType.VarChar).Value = branch.Trim();
                cmdSaveData.Parameters.Add("@ExchangeHouse", SqlDbType.VarChar).Value = exhouse.Trim();
                cmdSaveData.Parameters.Add("@InputDate", SqlDbType.VarChar).Value = DateTime.Now;
                cmdSaveData.Parameters.Add("@TxnStatus", SqlDbType.VarChar).Value = "UNPAID";
                cmdSaveData.Parameters.Add("@BatchId", SqlDbType.VarChar).Value = batchId;
                cmdSaveData.Parameters.Add("@TxnInsertDate", SqlDbType.VarChar).Value = DateTime.Now;
                cmdSaveData.Parameters.Add("@UploadUserId", SqlDbType.VarChar).Value = loggedUser;

                try
                {
                    cmdSaveData.ExecuteNonQuery();
                    insertSuccess = true;
                }
                catch (Exception ec)
                {
                    insertSuccess = false;
                    throw ec;
                }

            }
            catch (Exception ex)
            {
                insertSuccess = false;
                throw ex;
            }
            finally
            {
                try
                {
                    if (_sqlConnection != null && _sqlConnection.State == ConnectionState.Open)
                    {
                        _sqlConnection.Close();
                    }
                }
                catch (SqlException sqlException)
                {
                    throw sqlException;
                }
            }

            return insertSuccess;
        }

        internal bool saveCashDataIntoRemittanceDbRemittanceInfoTable(string exhouseId, string refno, int Status, string senderName, string SenderAddress, string benfName, 
            string BeneficiaryAddress, string BeneficiaryContactNo, decimal receivingAmount, string loggedUser)
        {
            bool insertSuccess = false;
            string saveData = "";
            SqlConnection _sqlConnection = null;
            SqlCommand cmdSaveData = new SqlCommand();

            try
            {
                _sqlConnection = new SqlConnection(remittanceDbLvConnectionString);
                if (_sqlConnection.State.Equals(ConnectionState.Closed))
                {
                    _sqlConnection.Open();
                }

                saveData = "INSERT INTO [dbo].[Remittanceinfo]([PartyId],[RemitenceId],[PinNo],[Status],[SenderName],[SenderAddress],[SenderMobileNo],[BeneficiaryName],[BeneficiaryAddress],[BeneficiaryMobileNo],[Amount],[RequestTime],[DownloadBranch],[DownloadUser]) "
                    + " VALUES (@PartyId,@RemitenceId,@PinNo,@Status,@SenderName,@SenderAddress,@SenderMobileNo,@BeneficiaryName,@BeneficiaryAddress,@BeneficiaryMobileNo,@Amount,@RequestTime,@DownloadBranch,@DownloadUser)";

                cmdSaveData.CommandText = saveData;
                cmdSaveData.Connection = _sqlConnection;

                cmdSaveData.Parameters.Add("@PartyId", SqlDbType.VarChar).Value = exhouseId.Trim();
                cmdSaveData.Parameters.Add("@RemitenceId", SqlDbType.VarChar).Value = refno.Trim();
                cmdSaveData.Parameters.Add("@PinNo", SqlDbType.VarChar).Value = refno.Trim();
                cmdSaveData.Parameters.Add("@Status", SqlDbType.Int).Value = Status;
                cmdSaveData.Parameters.Add("@SenderName", SqlDbType.VarChar).Value = senderName.Trim();
                cmdSaveData.Parameters.Add("@SenderAddress", SqlDbType.VarChar).Value = SenderAddress.Trim();
                cmdSaveData.Parameters.Add("@SenderMobileNo", SqlDbType.VarChar).Value = "";
                cmdSaveData.Parameters.Add("@BeneficiaryName", SqlDbType.VarChar).Value = benfName.Trim();
                cmdSaveData.Parameters.Add("@BeneficiaryAddress", SqlDbType.VarChar).Value = BeneficiaryAddress.Trim();
                cmdSaveData.Parameters.Add("@BeneficiaryMobileNo", SqlDbType.VarChar).Value = BeneficiaryContactNo.Trim();
                cmdSaveData.Parameters.Add("@Amount", SqlDbType.Float).Value = receivingAmount;
                cmdSaveData.Parameters.Add("@RequestTime", SqlDbType.DateTime).Value = DateTime.Now;
                cmdSaveData.Parameters.Add("@DownloadBranch", SqlDbType.VarChar).Value = "0100";
                cmdSaveData.Parameters.Add("@DownloadUser", SqlDbType.VarChar).Value = loggedUser;

                try
                {
                    cmdSaveData.ExecuteNonQuery();
                    insertSuccess = true;
                }
                catch (Exception ec)
                {
                    insertSuccess = false;
                    throw ec;
                }

            }
            catch (Exception ex)
            {
                insertSuccess = false;
                throw ex;
            }
            finally
            {
                try
                {
                    if (_sqlConnection != null && _sqlConnection.State == ConnectionState.Open)
                    {
                        _sqlConnection.Close();
                    }
                }
                catch (SqlException sqlException)
                {
                    throw sqlException;
                }
            }

            return insertSuccess;
        }

        internal DataTable GetSummaryTellerUploadedRecord(string ticks)
        {
            DataTable dt = new DataTable();
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();
                string query = "SELECT PaymentMode, count(*) no_of_txn,  round(sum(amount),2) TotalAmount FROM [NRBWork].[dbo].[ManualEFTFileData] WHERE  [BatchId]='" + ticks + "' group by PaymentMode";
                dt = dbManager.GetDataTable(query.Trim());
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return dt;
        }


        internal void ChangeStatusFromUploadedToReceived(string ticks)
        {
            bool status = false;
            try
            {
                string query = "";
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();
                query = "UPDATE [NRBWork].[dbo].[ManualEFTFileData] SET [Status]='RECEIVED'  WHERE [BatchId]='" + ticks + "'";                
                status = dbManager.ExcecuteCommand(query);
            }
            catch (Exception ex)
            {
                //throw ex;
            }
            finally
            {
                dbManager.CloseDatabaseConnection();
            }
        }

        
        internal DataTable GetUploadUserInfo(string uploadedUserId)
        {
            DataTable dt = new DataTable();
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();
                string query = "SELECT * FROM [NRBWork].[dbo].[ManualEFTProcessUserCredential] WHERE [UserId]='" + uploadedUserId + "'";
                dt = dbManager.GetDataTable(query.Trim());
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return dt;
        }

        internal DataTable GetAuthorizerAndSuperAdminEmailListExcludingMe(string loggedUser)
        {
            DataTable dt = new DataTable();
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();
                string query = "SELECT [UserId],[UserName],[UserType],[UserEmail] FROM [NRBWork].[dbo].[ManualEFTProcessUserCredential] WHERE  ([UserType]='Authorizer' OR [UserType]='Admin' OR [UserType]='SuperAdmin') AND [isActive]=1 AND [UserId] NOT IN ('" + loggedUser + "')";
                dt = dbManager.GetDataTable(query.Trim());
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return dt;
        }

        internal DataTable GetAuthorizedTxnList(string batchId)
        {
            DataTable dt = new DataTable();
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();
                string query = "SELECT ehi.[ExchangeHouseName] ExchangeHouse,fd.[RefNo],fd.[PaymentMode] Mode, fd.[Status], fd.[Remarks], fd.[BankName], "
                    + " fd.[Amount], (SELECT uc.[UserName] FROM [NRBWork].[dbo].[ManualEFTProcessUserCredential] uc where uc.[UserId]=fd.[UplodeBy])UplodeBy, convert(varchar, [UploadTime], 120)UploadTime,"
                    + " (SELECT uc.[UserName] FROM [NRBWork].[dbo].[ManualEFTProcessUserCredential] uc where uc.[UserId]=fd.[ProcessBy])ProcessBy, convert(varchar, fd.[ProcessingTime], 120)ProcessTime "
                    + " FROM [NRBWork].[dbo].[ManualEFTFileData] fd inner join [NRBWork].[dbo].[ManualEFTBasedExchangeHouseInfo] ehi "
                    + " ON fd.[PartyId]=ehi.[PartyId] WHERE fd.[BatchId] IN(" + batchId + ")  ORDER BY  fd.[ProcessingTime] DESC ";

                dt = dbManager.GetDataTable(query.Trim());
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return dt;
        }

        internal DataTable GetSuccessfulReportData(string dtValue1, string dtValue2, ref int eftCount, ref int mtbCount, ref int cashCount)
        {
            DataTable dt = new DataTable();
            DataTable dtEft = new DataTable();
            DataTable dtMtb = new DataTable();
            DataTable dtCash = new DataTable();

            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();
                string query = "SELECT ehi.[ExchangeHouseName] ExchangeHouse,fd.[RefNo],fd.[PaymentMode], fd.[BeneficiaryName], fd.[BeneficiaryAddress], fd.[BeneficiaryAccountNo],fd.[BeneficiaryContactNo],fd.[BankName],fd.[BranchName],"
                    + " fd.[RoutingNo],fd.[Amount],fd.[SenderName],fd.[SenderAddress],fd.[Purpose], "
                    + " (SELECT uc.[UserName] FROM [NRBWork].[dbo].[ManualEFTProcessUserCredential] uc where uc.[UserId]=fd.[UplodeBy])UplodeBy, "
                    + " convert(varchar, fd.[UploadTime], 120)UploadTime, "
                    + " (SELECT uc.[UserName] FROM [NRBWork].[dbo].[ManualEFTProcessUserCredential] uc where uc.[UserId]=fd.[ProcessBy])ProcessBy, "
                    + " convert(varchar, fd.[ProcessingTime], 120)ProcessTime,fd.[Remarks] "
                    + " FROM [NRBWork].[dbo].[ManualEFTFileData]  fd inner join [NRBWork].[dbo].[ManualEFTBasedExchangeHouseInfo] ehi "
                    + " ON fd.[PartyId]=ehi.[PartyId] "
                    + " WHERE convert(varchar, fd.[ProcessingTime], 23) between '" + dtValue1 + "' AND '" + dtValue2 + "' "
                    + " AND fd.[Status]= 'PROCESSED' AND fd.[PaymentMode] in('EFT','MTB','CASH')  ORDER BY  fd.AutoId desc ";

                dt = dbManager.GetDataTable(query.Trim());


                query = "Select count(*) FROM [NRBWork].[dbo].[ManualEFTFileData]  fd inner join [NRBWork].[dbo].[ManualEFTBasedExchangeHouseInfo] ehi "
                    + " ON fd.[PartyId]=ehi.[PartyId] where convert(varchar, fd.[ProcessingTime], 23) between '" + dtValue1 + "' AND '" + dtValue2 + "' "
                    + " AND fd.[Status]= 'PROCESSED' AND fd.[PaymentMode] in('EFT')";
                dtEft = dbManager.GetDataTable(query.Trim());
                eftCount = Convert.ToInt32(dtEft.Rows[0][0]);


                query = "Select count(*) FROM [NRBWork].[dbo].[ManualEFTFileData]  fd inner join [NRBWork].[dbo].[ManualEFTBasedExchangeHouseInfo] ehi "
                    + " ON fd.[PartyId]=ehi.[PartyId] where convert(varchar, fd.[ProcessingTime], 23) between '" + dtValue1 + "' AND '" + dtValue2 + "' "
                    + " AND fd.[Status]= 'PROCESSED' AND fd.[PaymentMode] in('MTB')";
                dtMtb = dbManager.GetDataTable(query.Trim());
                mtbCount = Convert.ToInt32(dtMtb.Rows[0][0]);

                query = "Select count(*) FROM [NRBWork].[dbo].[ManualEFTFileData]  fd inner join [NRBWork].[dbo].[ManualEFTBasedExchangeHouseInfo] ehi "
                    + " ON fd.[PartyId]=ehi.[PartyId] where convert(varchar, fd.[ProcessingTime], 23) between '" + dtValue1 + "' AND '" + dtValue2 + "' "
                    + " AND fd.[Status]= 'PROCESSED' AND fd.[PaymentMode] in('CASH')";
                dtCash = dbManager.GetDataTable(query.Trim());
                cashCount = Convert.ToInt32(dtCash.Rows[0][0]);
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return dt;
        }
        

        internal DataTable GetSuccessReportData(string dtValue1, string dtValue2, string paymode)
        {
            DataTable dt = new DataTable();

            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();
                string query = "SELECT ehi.[ExchangeHouseName] ExchangeHouse,fd.[RefNo],fd.[PaymentMode], fd.[BeneficiaryName], fd.[BeneficiaryAddress], fd.[BeneficiaryAccountNo],fd.[BeneficiaryContactNo],fd.[BankName],fd.[BranchName],"
                    + " fd.[RoutingNo],fd.[Amount],fd.[SenderName],fd.[SenderAddress],fd.[Purpose], "
                    + " (SELECT uc.[UserName] FROM [NRBWork].[dbo].[ManualEFTProcessUserCredential] uc where uc.[UserId]=fd.[UplodeBy])UplodeBy, "
                    + " convert(varchar, fd.[UploadTime], 120)UploadTime, "
                    + " (SELECT uc.[UserName] FROM [NRBWork].[dbo].[ManualEFTProcessUserCredential] uc where uc.[UserId]=fd.[ProcessBy])ProcessBy, "
                    + " convert(varchar, fd.[ProcessingTime], 120)ProcessTime,fd.[Remarks], ehi.[NRTAccount] "
                    + " FROM [NRBWork].[dbo].[ManualEFTFileData]  fd inner join [NRBWork].[dbo].[ManualEFTBasedExchangeHouseInfo] ehi "
                    + " ON fd.[PartyId]=ehi.[PartyId] "
                    + " WHERE convert(varchar, fd.[ProcessingTime], 23) between '" + dtValue1 + "' AND '" + dtValue2 + "' "
                    + " AND fd.[Status]= 'PROCESSED' AND fd.[PaymentMode] in('" + paymode + "')  ORDER BY  fd.AutoId desc ";

                dt = dbManager.GetDataTable(query.Trim());
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return dt;
        }

        internal DataTable GetFailedReportData(string dtValue1, string dtValue2)
        {
            DataTable dt = new DataTable();
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();

                // change at 12-Sep-2022
                string query = "SELECT p.ExchangeHouse, p.RefNo, p.Mode, p.Amount, p.CurrentStatus, p.LastProcessTime, p.Remarks,  p.BeneficiaryName, p.BeneficiaryAccountNo AccountNo, p.BankName, p.BranchName, p.RoutingNo, p.Purpose, "
	                +" p.UplodeBy, p.UploadTime, p.ProcessBy, p.NRTAccount FROM ( "                
                    +" SELECT ehi.[ExchangeHouseName] ExchangeHouse,fd.[RefNo],fd.[PaymentMode] Mode, fd.[Status], convert(varchar, fd.[ProcessingTime], 120)ProcessTime, fd.[Remarks],"
                    + " (select fdN.[Status] from [NRBWork].[dbo].[ManualEFTFileData] fdN where fdN.[RefNo]=fd.[RefNo] and fdN.[AutoId]=(select max (AutoId) from [NRBWork].[dbo].[ManualEFTFileData] where [RefNo] = fd.[RefNo]) ) CurrentStatus, "
                    + " (select convert(varchar, fdN.[ProcessingTime], 120) from [NRBWork].[dbo].[ManualEFTFileData] fdN where fdN.[RefNo]=fd.[RefNo] and fdN.[AutoId]=(select max (AutoId) from [NRBWork].[dbo].[ManualEFTFileData] where [RefNo] = fd.[RefNo]) ) LastProcessTime, "
                    + " fd.[BeneficiaryName], fd.[BeneficiaryAccountNo],fd.[BankName],fd.[BranchName], fd.[RoutingNo],fd.[Amount],fd.[SenderName],fd.[Purpose], "
                    + " (SELECT uc.[UserName] FROM [NRBWork].[dbo].[ManualEFTProcessUserCredential] uc where uc.[UserId]=fd.[UplodeBy])UplodeBy, "
                    + " convert(varchar, fd.[UploadTime], 120)UploadTime, (SELECT uc.[UserName] FROM [NRBWork].[dbo].[ManualEFTProcessUserCredential] uc where uc.[UserId]=fd.[ProcessBy])ProcessBy, ehi.[NRTAccount],fd.AutoId  "
                    + " FROM [NRBWork].[dbo].[ManualEFTFileData] fd inner join [NRBWork].[dbo].[ManualEFTBasedExchangeHouseInfo] ehi "
                    + " ON fd.[PartyId]=ehi.[PartyId] "
                    + " WHERE convert(varchar, fd.[ProcessingTime], 23) between '" + dtValue1 + "' AND '" + dtValue2 + "' "
                    + " )p "
                    + " WHERE p.CurrentStatus IN('FAILED','HOLD') AND p.Mode in('EFT','MTB','CASH')  ORDER BY  p.AutoId desc ";


                //string query = "SELECT ehi.[ExchangeHouseName] ExchangeHouse,fd.[RefNo],fd.[PaymentMode] Mode, fd.[Status], convert(varchar, fd.[ProcessingTime], 120)ProcessTime, fd.[Remarks],"
                //    + " (select fdN.[Status] from [NRBWork].[dbo].[ManualEFTFileData] fdN where fdN.[RefNo]=fd.[RefNo] and fdN.[AutoId]=(select max (AutoId) from [NRBWork].[dbo].[ManualEFTFileData] where [RefNo] = fd.[RefNo]) ) CurrentStatus, "
                //    + " (select convert(varchar, fdN.[ProcessingTime], 120) from [NRBWork].[dbo].[ManualEFTFileData] fdN where fdN.[RefNo]=fd.[RefNo] and fdN.[AutoId]=(select max (AutoId) from [NRBWork].[dbo].[ManualEFTFileData] where [RefNo] = fd.[RefNo]) ) LastProcessTime, "
                //    + " fd.[BeneficiaryName], fd.[BeneficiaryAccountNo],fd.[BankName],fd.[BranchName], fd.[RoutingNo],fd.[Amount],fd.[SenderName],fd.[Purpose], "
                //    + " (SELECT uc.[UserName] FROM [NRBWork].[dbo].[ManualEFTProcessUserCredential] uc where uc.[UserId]=fd.[UplodeBy])UplodeBy, "
                //    + " convert(varchar, fd.[UploadTime], 120)UploadTime, (SELECT uc.[UserName] FROM [NRBWork].[dbo].[ManualEFTProcessUserCredential] uc where uc.[UserId]=fd.[ProcessBy])ProcessBy, ehi.[NRTAccount]  "
                //    + " FROM [NRBWork].[dbo].[ManualEFTFileData] fd inner join [NRBWork].[dbo].[ManualEFTBasedExchangeHouseInfo] ehi "
                //    + " ON fd.[PartyId]=ehi.[PartyId] "
                //    + " WHERE convert(varchar, fd.[ProcessingTime], 23) between '" + dtValue1 + "' AND '" + dtValue2 + "' "
                //    + " AND fd.[Status] IN('FAILED','HOLD') AND fd.[PaymentMode] in('EFT','MTB', 'CASH')  ORDER BY  fd.AutoId desc ";

               

                dt = dbManager.GetDataTable(query.Trim());
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return dt;
        }

        internal string GetDistinctBatchIdForThisExchangeHouseUnAuthorizeData(string dtValue1, string dtValue2, int exhId)
        {
            DataTable dt = new DataTable();
            string batchIds = "";
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();
                string query = "SELECT distinct fd.[BatchId] "
                + " FROM [NRBWork].[dbo].[ManualEFTFileData] fd inner join [NRBWork].[dbo].[ManualEFTBasedExchangeHouseInfo] ehi "
                + " ON fd.[PartyId]=ehi.[PartyId] "
                + " WHERE fd.[IsSuccess]=1 AND fd.[Status]='RECEIVED' "
                + " AND convert(date,fd.[UploadTime])>='" + dtValue1 + "' AND  convert(date,fd.[UploadTime])<='" + dtValue2 + "'";

                if (exhId != 0)
                {
                    query += " AND fd.[PartyId]=" + exhId;
                }

                query += " AND fd.[ProcessBy] IS NULL AND fd.[ProcessingTime] IS NULL";

                dt = dbManager.GetDataTable(query.Trim());

                for (int row = 0; row < dt.Rows.Count; row++)
                {
                    if (batchIds.Equals(""))
                    {
                        batchIds = "'" + dt.Rows[row][0] + "'";
                    }
                    else
                    {
                        batchIds += ", '" + dt.Rows[row][0] + "'";
                    }
                }
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }

            return batchIds;
        }

        internal DataTable GetUnAuthorizeDataByRefNo(string refno)
        {
            DataTable dt = new DataTable();
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();
                string query = "SELECT fd.[AutoId] Sl, ehi.[ExchangeHouseName] ExchangeHouse,fd.[RefNo],fd.[PaymentMode] Mode, fd.[Status], fd.[Amount], fd.[BeneficiaryName] Bene, fd.[BeneficiaryAccountNo] AccountNo, fd.[BankName],"
                + " fd.[RoutingNo], (SELECT uc.[UserName] FROM [NRBWork].[dbo].[ManualEFTProcessUserCredential] uc where uc.[UserId]=fd.[UplodeBy])UplodeBy,"
                + " convert(varchar, [UploadTime], 120)UploadTime,fd.[IsSuccess], fd.[BranchName], fd.[PartyId], fd.[BeneficiaryAddress], fd.[SenderName], fd.[SenderAddress], fd.[Purpose], fd.[BeneficiaryContactNo] BeneContact, fd.[UplodeBy] UplodeByUserId, fd.[BatchId] "
                + " FROM [NRBWork].[dbo].[ManualEFTFileData] fd inner join [NRBWork].[dbo].[ManualEFTBasedExchangeHouseInfo] ehi "
                + " ON fd.[PartyId]=ehi.[PartyId] "
                + " WHERE fd.[IsSuccess]=1 AND fd.[Status]='RECEIVED' AND fd.[RefNo]='" + refno + "'";
                
                dt = dbManager.GetDataTable(query.Trim());
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return dt;
        }

        internal DataTable GetAuthorizedTxnSummaryList(string batchId)
        {
            DataTable dt = new DataTable();
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();
                string query = "SELECT fd.[PaymentMode] Mode, fd.[Status], count(*) No_of_Txn "
                    + " FROM [NRBWork].[dbo].[ManualEFTFileData] fd "
                    + " WHERE fd.[BatchId] IN(" + batchId + ")  group by [PaymentMode], [Status] ORDER BY  fd.[PaymentMode] ";

                dt = dbManager.GetDataTable(query.Trim());
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return dt;
        }

        internal DataTable GetPendingTxnToAuthorizeReportData(string dtValue1, string dtValue2)
        {
            DataTable dt = new DataTable();
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();

                string query = "SELECT ehi.[ExchangeHouseName] ExchangeHouse,fd.[RefNo],fd.[PaymentMode] Mode, fd.[Status], convert(varchar, fd.[ProcessingTime], 120)ProcessTime, fd.[Remarks],"
                    + " (select fdN.[Status] from [NRBWork].[dbo].[ManualEFTFileData] fdN where fdN.[RefNo]=fd.[RefNo] and fdN.[AutoId]=(select max (AutoId) from [NRBWork].[dbo].[ManualEFTFileData] where [RefNo] = fd.[RefNo]) ) CurrentStatus, "
                    + " (select convert(varchar, fdN.[ProcessingTime], 120) from [NRBWork].[dbo].[ManualEFTFileData] fdN where fdN.[RefNo]=fd.[RefNo] and fdN.[AutoId]=(select max (AutoId) from [NRBWork].[dbo].[ManualEFTFileData] where [RefNo] = fd.[RefNo]) ) LastProcessTime, "
                    + " fd.[BeneficiaryName], fd.[BeneficiaryAccountNo],fd.[BankName],fd.[BranchName], fd.[RoutingNo],fd.[Amount],fd.[SenderName],fd.[Purpose], "
                    + " (SELECT uc.[UserName] FROM [NRBWork].[dbo].[ManualEFTProcessUserCredential] uc where uc.[UserId]=fd.[UplodeBy])UplodeBy, "
                    + " convert(varchar, fd.[UploadTime], 120)UploadTime, (SELECT uc.[UserName] FROM [NRBWork].[dbo].[ManualEFTProcessUserCredential] uc where uc.[UserId]=fd.[ProcessBy])ProcessBy, ehi.[NRTAccount]  "
                    + " FROM [NRBWork].[dbo].[ManualEFTFileData] fd inner join [NRBWork].[dbo].[ManualEFTBasedExchangeHouseInfo] ehi "
                    + " ON fd.[PartyId]=ehi.[PartyId] "
                    + " WHERE convert(varchar, fd.[UploadTime], 23) between '" + dtValue1 + "' AND '" + dtValue2 + "' "
                    + " AND fd.[Status]= 'RECEIVED'  ORDER BY  fd.AutoId desc ";

                dt = dbManager.GetDataTable(query.Trim());
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return dt;
        }

        internal DataTable GetFailedDataByRefNo(string refno)
        {
            DataTable dt = new DataTable();
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();

                string query = "SELECT [AutoId],[PartyId],[RefNo],[PaymentMode],[BeneficiaryName],[BeneficiaryAddress],[BeneficiaryAccountNo],[BankName],[BranchName]"
                    +" ,[RoutingNo],[Amount],[SenderName],[SenderAddress],[Purpose],[UplodeBy],[UploadTime],[ProcessBy],[ProcessingTime],[Remarks],[IsSuccess],[Status],[BeneficiaryContactNo],[BatchId] "
                    + " FROM [NRBWork].[dbo].[ManualEFTFileData] "
                    + " WHERE [RefNo]='" + refno + "' "
                    + " AND [Status]= 'FAILED' AND [PaymentMode] in('EFT','MTB','CASH') ";
                                
                dt = dbManager.GetDataTable(query.Trim());
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return dt;
        }

        internal DataTable GetDataByRefNo(string refno)
        {
            DataTable dt = new DataTable();
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();

                string query = "SELECT [AutoId],[PartyId],[RefNo],[PaymentMode],[BeneficiaryName],[BeneficiaryAddress],[BeneficiaryAccountNo],[BankName],[BranchName]"
                    + " ,[RoutingNo],[Amount],[SenderName],[SenderAddress],[Purpose],[UplodeBy],[UploadTime],[ProcessBy],[ProcessingTime],[Remarks],[IsSuccess],[Status],[BeneficiaryContactNo],[BatchId] "
                    + " FROM [NRBWork].[dbo].[ManualEFTFileData] "
                    + " WHERE ltrim(rtrim([RefNo]))='" + refno + "' ";

                dt = dbManager.GetDataTable(query.Trim());
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return dt;
        }


        internal bool DeleteFromManualEFTDataTable(string pinNumToRemove, string txnStatus)
        {
            bool status = false;
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();
                string query = "DELETE FROM [NRBWork].[dbo].[ManualEFTFileData] WHERE [RefNo]='" + pinNumToRemove + "' AND [Status]='" + txnStatus + "'";
                status = dbManager.ExcecuteCommand(query);
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return status;
        }

        internal bool DeleteFromFundTransferDataTable(string pinNumToRemove)
        {
            bool status = false;
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, remittanceDbLvConnectionString);
                dbManager.OpenDatabaseConnection();
                string query = "DELETE FROM [RemittanceDB].[dbo].[FundTransferRequest] where ltrim(rtrim([RefNo]))='" + pinNumToRemove + "' AND [PaymentStatus]=1";
                status = dbManager.ExcecuteCommand(query);
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return status;
        }

        internal bool ModifyAccountNumberAtEFTTable(string refno, string accountNo, string ticks)
        {
            bool status = false;
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();
                string query = "UPDATE [NRBWork].[dbo].[ManualEFTFileData] SET [BeneficiaryAccountNo]='" + accountNo + "', [ProcessBy]=NULL, [ProcessingTime]=NULL, [Status]='RECEIVED', [BatchId]='" + ticks + "' where ltrim(rtrim([RefNo]))='" + refno + "'";
                status = dbManager.ExcecuteCommand(query);
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return status;
        }



        internal bool SaveNewUserInfo(string uId, string uName, string uPass, string uType, string uMail)
        {
            bool insertSuccess = false;
            string saveData = "";
            SqlConnection _sqlConnection = null;
            SqlCommand cmdSaveData = new SqlCommand();

            try
            {
                _sqlConnection = new SqlConnection(nrbworkConnectionString);
                if (_sqlConnection.State.Equals(ConnectionState.Closed))
                {
                    _sqlConnection.Open();
                }

                saveData = "INSERT INTO [NRBWork].[dbo].[ManualEFTProcessUserCredential]([UserId],[UserName],[UserPassword],[UserType],[isActive],[isPasswordChanged],[UserEmail]) "
                    + " VALUES (@UserId,@UserName,@UserPassword,@UserType,@isActive,@isPasswordChanged,@UserEmail)";

                cmdSaveData.CommandText = saveData;
                cmdSaveData.Connection = _sqlConnection;

                cmdSaveData.Parameters.Add("@UserId", SqlDbType.VarChar).Value = uId.Trim();
                cmdSaveData.Parameters.Add("@UserName", SqlDbType.VarChar).Value = uName.Trim();
                cmdSaveData.Parameters.Add("@UserPassword", SqlDbType.VarChar).Value = uPass.Trim();
                cmdSaveData.Parameters.Add("@UserType", SqlDbType.VarChar).Value = uType.Trim();
                cmdSaveData.Parameters.Add("@isActive", SqlDbType.Int).Value = 1;
                cmdSaveData.Parameters.Add("@isPasswordChanged", SqlDbType.VarChar).Value = "Y";
                cmdSaveData.Parameters.Add("@UserEmail", SqlDbType.VarChar).Value = uMail.Trim();
                
                try
                {
                    cmdSaveData.ExecuteNonQuery();
                    insertSuccess = true;
                }
                catch (Exception ec)
                {
                    insertSuccess = false;
                    throw ec;
                }

            }
            catch (Exception ex)
            {
                insertSuccess = false;
                throw ex;
            }
            finally
            {
                try
                {
                    if (_sqlConnection != null && _sqlConnection.State == ConnectionState.Open)
                    {
                        _sqlConnection.Close();
                    }
                }
                catch (SqlException sqlException)
                {
                    throw sqlException;
                }
            }

            return insertSuccess;
        }

        internal bool IsThisUserAlreadyExist(string uId)
        {
            DataTable dt = new DataTable();
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();
                string query = "SELECT * FROM [NRBWork].[dbo].[ManualEFTProcessUserCredential] WHERE ltrim(rtrim([UserId]))='" + uId + "'";
                dt = dbManager.GetDataTable(query.Trim());
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }

            if (dt.Rows.Count > 0)
                return true;
            return false;
        }

        internal bool UpdateTxnStatusToReceivedAtInputDataTable(string pinNumToUpdate, string ticks)
        {
            bool status = false;
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();
                string query = "UPDATE [NRBWork].[dbo].[ManualEFTFileData] SET [ProcessBy]=NULL, [ProcessingTime]=NULL, [Status]='RECEIVED', [Remarks]='', [BatchId]='" + ticks + "' where ltrim(rtrim([RefNo]))='" + pinNumToUpdate + "'";
                status = dbManager.ExcecuteCommand(query);
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return status;
        }

        internal DataTable GetAllUsersInfo()
        {
            DataTable dt = new DataTable();
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();
                string query = "SELECT [AutoId] Sl,[UserId],[UserName],[UserType],[isActive],[UserEmail] FROM [NRBWork].[dbo].[ManualEFTProcessUserCredential] Order By [AutoId]";
                dt = dbManager.GetDataTable(query.Trim());
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return dt;
        }

        internal bool UpdateUserRoleType(string userId, string userType)
        {
            bool status = false;
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();
                string query = "UPDATE [NRBWork].[dbo].[ManualEFTProcessUserCredential] SET [UserType]='" + userType + "' WHERE [UserId]='" + userId + "'";
                status = dbManager.ExcecuteCommand(query);
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return status;
        }

        internal bool UpdateUserActivity(string userId, int userActivity)
        {
            bool status = false;
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();
                string query = "UPDATE [NRBWork].[dbo].[ManualEFTProcessUserCredential] SET [isActive]=" + userActivity + " WHERE [UserId]='" + userId + "'";
                status = dbManager.ExcecuteCommand(query);
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return status;
        }

        internal bool UpdateUserEmail(string userId, string userEmail)
        {
            bool status = false;
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();
                string query = "UPDATE [NRBWork].[dbo].[ManualEFTProcessUserCredential] SET [UserEmail]='" + userEmail + "' WHERE [UserId]='" + userId + "'";
                status = dbManager.ExcecuteCommand(query);
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return status;
        }


        internal DataTable GetEFTDataInfoFromBEFTNRequestTable(string pinNumToRemove)
        {
            DataTable dt = new DataTable();
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, remittanceDbLvConnectionString);
                dbManager.OpenDatabaseConnection();
                string sqlQuery = "select * from [RemittanceDB].[dbo].[BEFTNRequest] where ltrim(rtrim([RefNo]))='" + pinNumToRemove.Trim() + "'";
                dt = dbManager.GetDataTable(sqlQuery.Trim());
            }
            catch (Exception ex)
            { }
            finally
            {
                dbManager.CloseDatabaseConnection();
            }
            return dt;
        }

        internal bool DeleteFromBEFTNRequestTable(string pinNumToRemove)
        {
            bool status = false;
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, remittanceDbLvConnectionString);
                dbManager.OpenDatabaseConnection();
                string query = "DELETE FROM [RemittanceDB].[dbo].[BEFTNRequest] where ltrim(rtrim([RefNo]))='" + pinNumToRemove + "' AND [PaymentStatus]=1";
                status = dbManager.ExcecuteCommand(query);
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return status;
        }

        internal bool HoldTxnManualEFTDataTable(string pinNumToHold, string stats)
        {
            bool status = false;
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();
                string query = "UPDATE [NRBWork].[dbo].[ManualEFTFileData] SET [Status]='HOLD', [HoldDate]= getdate() where ltrim(rtrim([RefNo]))='" + pinNumToHold + "' AND [Status]='" + stats + "' ";
                status = dbManager.ExcecuteCommand(query);
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return status;
        }

        internal bool CancelTxnManualEFTDataTable(string pinNumToCancel, string stats)
        {
            bool status = false;
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();
                string query = "UPDATE [NRBWork].[dbo].[ManualEFTFileData] SET [Status]='CANCELLED', [CancelDate]= getdate() WHERE [RefNo]='" + pinNumToCancel + "' AND [Status]='" + stats + "'";
                status = dbManager.ExcecuteCommand(query);
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return status;
        }

        internal DataTable GetCancelledTxnDataByDateRange(string dtValue1, string dtValue2)
        {
            DataTable dt = new DataTable();
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();

                string query = "SELECT fd.[AutoId] Sl, ehi.[ExchangeHouseName] ExchangeHouse,fd.[RefNo],fd.[PaymentMode] Mode, fd.[Status], convert(varchar, [CancelDate], 120)CancelTime, fd.[Amount], fd.[BeneficiaryName] Beneficiary, fd.[BeneficiaryAccountNo] AccountNo, fd.[BankName],"
                + " fd.[UplodeBy] UploderId, (SELECT uc.[UserName] FROM [NRBWork].[dbo].[ManualEFTProcessUserCredential] uc where uc.[UserId]=fd.[UplodeBy])UplodeBy, convert(varchar, [UploadTime], 120)UploadTime "
                + " FROM [NRBWork].[dbo].[ManualEFTFileData] fd inner join [NRBWork].[dbo].[ManualEFTBasedExchangeHouseInfo] ehi "
                + " ON fd.[PartyId]=ehi.[PartyId] "
                + " WHERE  fd.[Status]='CANCELLED' AND ( convert(varchar, fd.[CancelDate], 23) between '" + dtValue1 + "' AND '" + dtValue2 + "' ) "
                + " ORDER BY fd.[CancelDate] DESC ";

                dt = dbManager.GetDataTable(query.Trim());
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return dt;
        }

        internal DataTable GetHoldTxnDataByDateRange(string dtValue1, string dtValue2)
        {
            DataTable dt = new DataTable();
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();

                string query = "SELECT fd.[AutoId] Sl, ehi.[ExchangeHouseName] ExchangeHouse,fd.[RefNo],fd.[PaymentMode] Mode, fd.[Status], convert(varchar, [HoldDate], 120)HoldDate, fd.[Amount], fd.[BeneficiaryName] Beneficiary, fd.[BeneficiaryAccountNo] AccountNo, fd.[BankName],"
                + " fd.[UplodeBy] UploderId, (SELECT uc.[UserName] FROM [NRBWork].[dbo].[ManualEFTProcessUserCredential] uc where uc.[UserId]=fd.[UplodeBy])UplodeBy, convert(varchar, [UploadTime], 120)UploadTime "
                + " FROM [NRBWork].[dbo].[ManualEFTFileData] fd inner join [NRBWork].[dbo].[ManualEFTBasedExchangeHouseInfo] ehi "
                + " ON fd.[PartyId]=ehi.[PartyId] "
                + " WHERE fd.[Status]='HOLD' AND ( convert(varchar, fd.[HoldDate], 23) between '" + dtValue1 + "' AND '" + dtValue2 + "' ) "
                + " ORDER BY fd.[HoldDate] DESC ";

                dt = dbManager.GetDataTable(query.Trim());
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return dt;
        }

        internal DataTable GetHoldDataByRefNo(string pinNumToUnHold)
        {
            DataTable dt = new DataTable();
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();

                string query = "SELECT fd.[AutoId] Sl, ehi.[ExchangeHouseName] ExchangeHouse,fd.[RefNo],fd.[PaymentMode] Mode, fd.[Status], fd.[Amount], fd.[BeneficiaryName] Bene, fd.[BeneficiaryAccountNo] AccountNo, fd.[BankName],"
                + " (SELECT uc.[UserName] FROM [NRBWork].[dbo].[ManualEFTProcessUserCredential] uc where uc.[UserId]=fd.[UplodeBy])UplodeBy,"
                + " convert(varchar, [UploadTime], 120)UploadTime,fd.[IsSuccess], fd.[BranchName], fd.[SenderAddress], fd.[Purpose], fd.[BeneficiaryContactNo] BeneContact, fd.[UplodeBy] UplodeByUserId, fd.[BatchId] "
                + " FROM [NRBWork].[dbo].[ManualEFTFileData] fd inner join [NRBWork].[dbo].[ManualEFTBasedExchangeHouseInfo] ehi "
                + " ON fd.[PartyId]=ehi.[PartyId] "
                + " WHERE fd.[Status]='HOLD' AND fd.[RefNo]='" + pinNumToUnHold + "'";

                dt = dbManager.GetDataTable(query.Trim());
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return dt;
        }

        internal bool UnHoldTxnManualEFTDataTable(string pinNumToUnHold, string stats, string ticks)
        {
            bool status = false;
            try
            {
                dbManager = new MTBDBManager(MTBDBManager.DatabaseType.SqlServer, nrbworkConnectionString);
                dbManager.OpenDatabaseConnection();
                string query = "UPDATE [NRBWork].[dbo].[ManualEFTFileData] SET [Status]='RECEIVED', [HoldDate]= NULL, [UploadTime]=GETDATE(), [ProcessBy]=NULL, [ProcessingTime]=NULL, [BatchId]='" + ticks + "' where ltrim(rtrim([RefNo]))='" + pinNumToUnHold + "' AND [Status]='" + stats + "' ";
                status = dbManager.ExcecuteCommand(query);
            }
            catch (Exception ex) { }
            finally { dbManager.CloseDatabaseConnection(); }
            return status;
        }

    }
}
