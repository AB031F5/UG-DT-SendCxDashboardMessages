using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using UG_DT_SendCxSms.Logic;

namespace UG_DT_SendCxSms
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            //ServiceBase[] ServicesToRun;
            //ServicesToRun = new ServiceBase[]
            //{
            //    new Service1()
            //};
            //ServiceBase.Run(ServicesToRun);
            //PendingInstructions pending = new PendingInstructions();
            //pending.FetchPendingInstructions();

            //PushVerifiedComplaints_New_Closed complaints = new PushVerifiedComplaints_New_Closed();
            //complaints.FetchVerifiedComplaints_New_Closed();

            PushWIPComplaints pushWIPComplaints = new PushWIPComplaints();
            pushWIPComplaints.FetchPendingComplaints();

            //VerifyAccounts verifyAccounts = new VerifyAccounts();
            //verifyAccounts.PendingAccounts();
            //FetchAccountsWithInfo accountsWithInfo = new FetchAccountsWithInfo();
            //accountsWithInfo.FetchAccounts();
        }
    }
}
