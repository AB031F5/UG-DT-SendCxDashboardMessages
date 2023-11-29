using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using UG_DT_SendCxSms.Models;

namespace UG_DT_SendCxSms.Logic
{
    public class PushWIPComplaints
    {
        DataBaseContext dbContext = null;

        private static bool IsCxServiceBusy = false;
        private static bool EmailConfig = false;
        static ManualResetEvent allThreadsDone = new ManualResetEvent(false);
        public async void FetchPendingComplaints()
        {
            ArrayList errors = new ArrayList();
            if (IsCxServiceBusy)
            {
                return;
            };
            IsCxServiceBusy = true;

            dbContext = new DataBaseContext();

            Helpers helpers = new Helpers();
            SendMessage sender = new SendMessage();

            try
            {

                Hashtable fieldParams3 = new Hashtable { };

                DataTable dataTable3 = dbContext.ExecuteQuery("sp_CheckEmailSendingConfig", fieldParams3, dbContext.custServiceDbConnection);

                if (dataTable3 != null)
                {
                    var config = GetConfigEmailSetting(dataTable3);
                    if (config > 0)
                    {
                        EmailConfig = true;
                    }
                }

                Hashtable fieldParams = new Hashtable { };

                DataTable dataTable = dbContext.ExecuteQuery("sp_FetchPendingPreprocessed_WIP_Only", fieldParams, dbContext.custServiceDbConnection);
                if (dataTable != null)
                {
                    var messagesToSend = GetPendingComplaints(dataTable);
                    int threadCount = messagesToSend.Count;
                    if (threadCount > 0)
                    {
                        foreach (var message in messagesToSend)
                        {
                            Thread thread = new Thread(() =>
                            {
                                _ = NastyWork(message, EmailConfig);
                                if (Interlocked.Decrement(ref threadCount) == 0)
                                {
                                    allThreadsDone.Set();
                                }
                            });
                            thread.Start();
                            //_ = Parallel.ForEach(Enumerable.Range(1, messagesToSend.Count), i =>
                            //{

                            //});
                        }
                    }
                }

            }
            catch (Exception ex)
            {

                throw;
            }
            finally
            {
                IsCxServiceBusy = false;
                allThreadsDone.WaitOne();
            }
        }
        private async Task NastyWork(Models.PendingComplaints message, bool emailConfig)
        {
            ArrayList errors = new ArrayList();
            Helpers helpers = new Helpers();
            SendMessage sender = new SendMessage();
            var msgUpate = new ComplaintStatus();
            msgUpate.Case = Convert.ToString(message.CaseNumber);
            msgUpate.Status = "Retry in 3 Days";
            var isUpdated = sender.UpdateComplaintStatus(msgUpate);
            bool send = false;
            if (isUpdated)
            {
                Models.PendingInstructions coms = new Models.PendingInstructions();

                DateTime startDate = DateTime.Now;
                string endDateString = message.RecordDate;
                string lastSmsSentAt = message.SmsSentAt;

                string complaintType = "";

                if (lastSmsSentAt.Equals("") || lastSmsSentAt == null)
                {
                    if (message.Status.Equals("WIP"))
                    {
                        complaintType = "First Holding Response";
                        send = true;
                    }
                }
                else
                {
                    DateTime endDate = DateTime.Parse(endDateString);

                    TimeSpan difference = startDate - endDate;

                    int daysDifference = difference.Days;
                    int hoursDifference = difference.Hours;
                    int minutesDifference = difference.Minutes;
                    int secondsDifference = difference.Seconds;

                    if(daysDifference == 3)
                    {
                        if (message.Status.Equals("WIP"))
                        {
                            complaintType = "Second Holding Response";
                            send = true;
                        }
                    }
                    if (daysDifference == 6 && (hoursDifference > 9 && minutesDifference == 30))
                    {
                        if (message.Status.Equals("WIP"))
                        {
                            complaintType = "Third Holding Response";
                            send = true;
                        }
                    }
                    if (daysDifference == 9 && (hoursDifference > 9 && minutesDifference == 30))
                    {
                        if (message.Status.Equals("WIP"))
                        {
                            complaintType = "Forth Holding Response";
                            send = true;
                        }
                    }
                    if (daysDifference == 12 && (hoursDifference > 9 && minutesDifference == 30))
                    {
                        if (message.Status.Equals("WIP"))
                        {
                            complaintType = "Fifth Holding Response";
                            send = true;
                        }
                    }
                    if (daysDifference == 15 && (hoursDifference > 9 && minutesDifference == 30))
                    {
                        if (message.Status.Equals("WIP"))
                        {
                            complaintType = "Sixth Holding Response";
                            send = true;
                        }
                    }
                }
                if(send == true)
                {
                    coms.AccountNumber = message.AccountNumber;
                    coms.CaseNumber = Convert.ToString(message.CaseNumber);
                    coms.MsgContent = message.Message;
                    coms.Complaint = complaintType;

                    string feedback = await sender.RevampedSms(message.Message, message.PhoneNumber);
                    sender.UpdateWipSMSComplaintTime(coms.CaseNumber, $"{DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")}");
                    if (emailConfig == true)
                    {
                        var emailFeedback = await sender.SendEmail(message.Email, message.Message, message.CaseNumber);
                        if (emailFeedback.IsSuccessStatusCode)
                        {
                            if (feedback == "SUCCESS")
                            {
                                msgUpate.Status = "1";
                            }
                            else
                            {
                                msgUpate.Status = "Failed";
                            }
                            coms.ComType = "Email";
                            coms.Beneficiary = message.Email;
                            AddComsToDb(coms);
                            sender.UpdateWipEmailComplaintTime(coms.CaseNumber, $"{DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")}");
                        }
                    }
                    if (feedback == "SUCCESS")
                    {
                        msgUpate.Status = "1";
                    }
                    else
                    {
                        msgUpate.Status = "Failed";
                    }

                    coms.ComType = "SMS";
                    coms.Beneficiary = message.PhoneNumber;
                    AddComsToDb(coms);
                }
            }
        }
        private bool AddInvalidAccount(Models.PendingInstructions msg)
        {
            Hashtable field = new Hashtable {
                { "acctnum",msg.AccountNumber },
                { "phinvalid","Y" },
                { "email_invalid","N" }
            };

            return dbContext.ExecuteNonQuery(StoredProcedures.fetchPendingInstructions, field, dbContext.custServiceDbConnection);
        }
        private bool AddComsToDb(Models.PendingInstructions msg)
        {
            Hashtable field = new Hashtable {
                { "ccomtype",msg.ComType },
                { "cbeneficiary",msg.Beneficiary },
                { "ccomms",msg.MsgContent },
                { "csent_date",DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                { "csend_reason",msg.Complaint },
                { "zeecmplt",msg.CaseNumber },
                { "custacc",msg.AccountNumber },
            };
            dbContext = new DataBaseContext();
            var executed = dbContext.ExecuteNonQuery(StoredProcedures.addcoms, field, dbContext.custServiceDbConnection);
            return executed;
        }
        private async Task<ConnectPkReturnData> FetchAccountInformation(Models.PendingComplaints pending)
        {
            try
            {
                HttpWebRequest request = ConnectPkSOAPWebRequest();
                XmlDocument SOAPReqBody = new XmlDocument();
                SOAPReqBody.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>  
        <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-   instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">  
         <soap:Body>  
            <GetCustomer xmlns=""http://tempuri.org/"">  
              <ApiUsername>Api</ApiUsername>  
              <ApiPassword>C0ld@f33xnes</ApiPassword> 
              <AccountNumber>" + pending.AccountNumber + @"</AccountNumber>
            </GetCustomer>  
          </soap:Body>  
        </soap:Envelope>");

                using (Stream stream = request.GetRequestStream())
                {
                    SOAPReqBody.Save(stream);
                }

                using (WebResponse Serviceres = request.GetResponse())
                {
                    using (StreamReader rd = new StreamReader(Serviceres.GetResponseStream()))
                    {
                        var ServiceResult = rd.ReadToEnd();
                        var Xml = System.Xml.Linq.XElement.Parse(ServiceResult);
                        // lblerror.Text = Xml.Elements.ToString()
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(ServiceResult);
                        string jsonText = JsonConvert.SerializeXmlNode(doc);
                        var jObj = JObject.Parse(jsonText);
                        var pk = new ConnectPkReturnData();
                        pk.Result = jObj.SelectToken("s:Envelope.s:Body.GetCustomerResponse.GetCustomerResult.a:ErrorCode").ToString();
                        pk.Description = jObj.SelectToken("s:Envelope.s:Body.GetCustomerResponse.GetCustomerResult.a:ErrorDescription").ToString();
                        pk.AccountHolder = jObj.SelectToken("s:Envelope.s:Body.GetCustomerResponse.GetCustomerResult.a:Name").ToString();
                        pk.Email = jObj.SelectToken("s:Envelope.s:Body.GetCustomerResponse.GetCustomerResult.a:EmailAddress").ToString();
                        pk.Phone = jObj.SelectToken("s:Envelope.s:Body.GetCustomerResponse.GetCustomerResult.a:PhoneNumber").ToString();
                        return pk;
                    }
                }
            }
            catch (Exception ex)
            {
                string errortext = ex.Message;
                return null;
            }
        }
        public HttpWebRequest ConnectPkSOAPWebRequest()
        {
            Uri myUri = new Uri(ConnectPkEndPoints.Url, UriKind.Absolute);
            HttpWebRequest Req = (HttpWebRequest)WebRequest.Create(myUri);
            Req.Headers.Add("SOAPAction:http://tempuri.org/IConnectPKServiceApi/GetCustomer");
            Req.ContentType = "text/xml;charset=\"utf-8\"";
            Req.Accept = "text/xml";
            Req.Method = "POST";
            return Req;
        }
        public HttpWebRequest EmailSOAPWebRequest()
        {
            Uri myUri = new Uri(ConnectPkEndPoints.Url, UriKind.Absolute);
            HttpWebRequest Req = (HttpWebRequest)WebRequest.Create(myUri);
            Req.Headers.Add("SOAPAction:http://tempuri.org/IConnectPKServiceApi/GetCustomer");
            Req.ContentType = "text/xml;charset=\"utf-8\"";
            Req.Accept = "text/xml";
            Req.Method = "POST";
            return Req;
        }
        private List<Models.PendingComplaints> GetPendingComplaints(DataTable dataTableData)
        {
            List<Models.PendingComplaints> pendingComplaint = new List<Models.PendingComplaints>();

            foreach (DataRow row in dataTableData.Rows)
            {
                try
                {
                    Models.PendingComplaints comp = new Models.PendingComplaints();
                    comp.CustomerName = row["Customer_or_Prospect_Name"].ToString();
                    comp.CaseNumber = row["Case_Number"].ToString();
                    comp.AccountNumber = row["Account_No_Number"].ToString();
                    comp.Processed = row["processed"].ToString();
                    comp.Status = row["Status"].ToString();
                    comp.PhoneNumber = row["PhoneNumber"].ToString();
                    comp.Message = row["Message"].ToString();
                    comp.SmsSentAt = row["SmsSentAt"].ToString();
                    comp.EmailSentAt = row["EmailSentAt"].ToString();
                    comp.RecordDate = row["record_date"].ToString();

                    pendingComplaint.Add(comp);
                }
                catch (Exception ex)
                {

                }

            }
            return pendingComplaint;
        }
        private int GetConfigEmailSetting(DataTable dataTableData)
        {
            int configEmailSetting = 0;
            foreach (DataRow row in dataTableData.Rows)
            {
                try
                {
                    Models.PendingComplaints comp = new Models.PendingComplaints();
                    configEmailSetting = Convert.ToInt32(row["allowEmailSending"].ToString());
                }
                catch (Exception ex)
                {
                    return configEmailSetting;
                }

            }
            return configEmailSetting;
        }
    }
}
