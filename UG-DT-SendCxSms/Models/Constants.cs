using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UG_DT_SendCxSms.Models
{
    public class ConnectPkEndPoints
    {
        public const string UAT = "";
        public const string PROD = "http://ugpbhkmapp000f:8082/ConnnectPKApi/mex";
        public const string Url = PROD;
    }
    public class TrueAfricanEndPoints
    {
        public const string UAT = "";
        public const string PROD = "http://nickel.trueafrican.com/esme.php";
        public const string Url = PROD;
    }
    public class TrueAfricanCredentials
    {
        public const string username = "barclaysCC";
        public const string passwprd = "BarC735Hdk";
        public const string completedProcessing = "Dear customer, we have completed processing your instruction today. For any clarifications, visit your nearest branch  or call 080022333/0312218348";
    }
    public static class LogConstants
    {
        public const string local = @"C:\Users\AB031F5\Documents\CxLogging\";
        public const string UAT_SERVER_LOG = @"D:\Projects\CexDashboard\SmsEmailService\";
        public const string PROD_SERVER_LOG = @"D:\Logs\CxLogging\SmsService\Logs\";
        public const string Errorlogs = PROD_SERVER_LOG;
    }
    public static class StoredProcedures
    {
        public const string updateVerifiedInfo = "sp_UpdateVerifiedAccountInfo";
        public const string fetchPreprocessedByVerifiedAccountNumber = "sp_FetchPreprocessedByVerifiedAccountNumber";
        public const string fetchPendingVerification = "sp_PendingVerification";
        public const string fetchAccountsWithInfo = "sp_GetCustomersWithInfo";
        public const string fetchPendingComplaints = "sp_FetchPendingPreprocessed";
        public const string fetchPendingComplaints_WIP_Only = "sp_FetchPendingPreprocessed_WIP_Only";
        public const string fetchPendingInstructions = "sp_FetchPendingFromAccMaintenance";
        public const string updateInstruction = "sp_UpdatePendingFromAccMaintenance";
        public const string updateComplaint = "sp_UpdatePendingFromPreprocess";
        public const string addInvalidContact = "addinvalidcontactmaint";
        public const string addcomsToAccMaintenance = "addcomsacctmaint";
        public const string addcoms = "addcoms";
        public const string removeprocessed = "removeprocessed";
    }
}
