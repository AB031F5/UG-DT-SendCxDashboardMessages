using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UG_DT_SendCxSms.Logic;

namespace UG_DT_SendCxSms
{
    [RunInstaller(true)]
    public partial class Service1 : ServiceBase
    {
        public Thread InstructionsServiceThread = null;
        public Thread ComplaintsServiceThread = null;
        public Thread AccVerificationServiceThread = null;
        public Thread PushWIPServiceThread = null;
        public string custThread = "";
        public Service1()
        {
            InitializeComponent();
            var custServiceThread = System.Configuration.ConfigurationManager.AppSettings["ThreadTime"];
            custThread = custServiceThread;
        }
        protected override void OnStart(string[] args)
        {
            try
            {
                //InstructionsServiceThread = new Thread(InstructionsService);
                //InstructionsServiceThread.Start();

                AccVerificationServiceThread = new Thread(AccountVerification);
                AccVerificationServiceThread.Start();

                ComplaintsServiceThread = new Thread(ComplaintsService);
                ComplaintsServiceThread.Start();

                PushWIPServiceThread = new Thread(PushWIPComplaintsService);
                PushWIPServiceThread.Start();


            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public void InstructionsService()
        {
            while (true)
            {
                PendingInstructions pending = new PendingInstructions();
                pending.FetchPendingInstructions();
                Thread.Sleep(Convert.ToInt32(custThread) * 1000);
            }
        }
        public void ComplaintsService()
        {
            while (true)
            {
                PushVerifiedComplaints_New_Closed pending = new PushVerifiedComplaints_New_Closed();
                pending.FetchVerifiedComplaints_New_Closed();
                Thread.Sleep(10000);
            }
        }
        public void PushWIPComplaintsService()
        {
            while (true)
            {
                PushWIPComplaints pending = new PushWIPComplaints();
                pending.FetchPendingComplaints();
                Thread.Sleep(10000);
            }
        }
        public void AccountVerification()
        {
            while (true)
            {
                VerifyAccounts pending = new VerifyAccounts();
                pending.PendingAccounts();
                Thread.Sleep(Convert.ToInt32(custThread) * 1000);
            }
        }
        protected override void OnStop()
        {
            try
            {
                //if (InstructionsServiceThread != null & InstructionsServiceThread.IsAlive)
                //{
                //    Helpers helper = new Helpers();
                //    ArrayList errors = new ArrayList();
                //    errors.Add("!!!!!!!!!!!!!!!!!!!!!!!   Cx Dashboard InstructionsServiceThread Stopped   !!!!!!!!!!!!!!!!!!!!!!!");
                //    helper.writeToFile(errors);
                //    InstructionsServiceThread.Abort();
                //}

                if (ComplaintsServiceThread != null & ComplaintsServiceThread.IsAlive)
                {
                    Helpers helper = new Helpers();
                    ArrayList errors = new ArrayList();
                    errors.Add("!!!!!!!!!!!!!!!!!!!!!!!   Cx Dashboard ComplaintsServiceThread Stopped   !!!!!!!!!!!!!!!!!!!!!!!");
                    helper.writeToFile(errors);
                    ComplaintsServiceThread.Abort();
                }

                if (AccVerificationServiceThread != null & ComplaintsServiceThread.IsAlive)
                {
                    Helpers helper = new Helpers();
                    ArrayList errors = new ArrayList();
                    errors.Add("!!!!!!!!!!!!!!!!!!!!!!!   Cx Dashboard AccVerificationServiceThread Stopped   !!!!!!!!!!!!!!!!!!!!!!!");
                    helper.writeToFile(errors);
                    AccVerificationServiceThread.Abort();
                }
                if (PushWIPServiceThread != null & PushWIPServiceThread.IsAlive)
                {
                    Helpers helper = new Helpers();
                    ArrayList errors = new ArrayList();
                    errors.Add("!!!!!!!!!!!!!!!!!!!!!!!   Cx Dashboard PushWIPServiceThread Stopped   !!!!!!!!!!!!!!!!!!!!!!!");
                    helper.writeToFile(errors);
                    PushWIPServiceThread.Abort();
                }
            }
            catch (Exception ex)
            {
                Helpers helper = new Helpers();
                ArrayList errors = new ArrayList();
                errors.Add("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@   Cx Dashboard Exception on Stop   @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
                errors.Add($"Exception :::: {ex.Message}");
                errors.Add($"Stacktrace :::: {ex.StackTrace}");
                helper.writeToFile(errors);
            }
        }
    }
}
