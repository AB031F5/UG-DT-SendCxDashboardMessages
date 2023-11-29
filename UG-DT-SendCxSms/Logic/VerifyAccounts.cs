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
using System.Threading.Tasks;
using System.Xml;
using UG_DT_SendCxSms.Models;
using System.Security.Principal;

namespace UG_DT_SendCxSms.Logic
{
    public class VerifyAccounts
    {
        DataBaseContext dbContext = null;

        private static bool IsCxServiceBusy = false;
        public async void PendingAccounts()
        {
            ArrayList errors = new ArrayList();
            if (IsCxServiceBusy)
            {
                return;
            };
            IsCxServiceBusy = true;

            dbContext = new DataBaseContext();

            try
            {
                Hashtable fieldParams = new Hashtable { };

                DataTable dataTable = dbContext.ExecuteQuery("spPendingVerification", fieldParams, dbContext.custServiceDbConnection);
                if (dataTable != null)
                {
                    var messagesToSend = GetPendingComplaints(dataTable);
                    if (messagesToSend.Count > 0)
                    {
                        //await sender.Sender(messagesToSend);
                        foreach (var message in messagesToSend)
                        {
                            string messageContent = ConstructMessage(message);
                            
                            message.Message = messageContent;

                            await GetCustomerInfo(message);

                            //Hashtable parameters = new Hashtable
                            //    {
                            //        {"account",message.AccountNumber }
                            //    };
                            //DataTable dataTable2 = dbContext.ExecuteQuery(StoredProcedures.fetchPreprocessedByVerifiedAccountNumber, parameters, dbContext.custServiceDbConnection);
                            //if (dataTable2 != null)
                            //{
                            //    var verifiedAccounts = GetPendingComplaints(dataTable2);
                            //    if(verifiedAccounts.Count > 0)
                            //    {
                            //        foreach (var account in verifiedAccounts)
                            //        {
                            //            var isUpdated = sender.UpdateVerifedAccount(message.CaseNumber,"0", 1, account.PhoneNumber, account.Email, message.Message);
                            //        }
                            //    }
                            //    else
                            //    {
                            //        //make connectpk call
                            //        await GetCustomerInfo(message);
                            //    }
                            //}
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
            }
        }
        private string ConstructMessage(Models.PendingComplaints message)
        {
            string messageContent = "";
            if (message.Status.Equals("New"))
            {
                messageContent = $"Dear {message.CustomerName}, " +
                    $"We aknowledge receipt of your complaint logged with us. Ref {message.CaseNumber}. " +
                    $"Inquiries : 0800222333/25612218348. " +
                    $"Whatsapp : 256700375750";
            }
            if (message.Status.Equals("WIP"))
            {
                messageContent = $"Dear {message.CustomerName}, " +
                    $"We would like to confirm that your complaint Reference number {message.CaseNumber} is still under investigation. " +
                    $"We will contact you upon resolution. " +
                    $"Inquiries : 0800222333/25612218348. " +
                    $"Whatsapp : 256700375750";
            }
            if (message.Status.Equals("Closed"))
            {
                messageContent = $"Dear {message.CustomerName}, " +
                    $"We would like to confirm that your complaint Reference number {message.CaseNumber} has been resolved. " +
                    $"Inquiries : 0800222333/25612218348. " +
                    $"Whatsapp : 256700375750";
            }
            return messageContent;
        }
        private async Task<bool> GetCustomerInfo(Models.PendingComplaints message)
        {
            SendMessage sender = new SendMessage();
            var connectPk = new ConnectPkReturnData();
            connectPk = await FetchAccountInformation(message);
            message.CustomerName = connectPk.AccountHolder;
            message.PhoneNumber = connectPk.Phone;
            message.Email = connectPk.Email;
            message.Message = ConstructMessage(message);
            if (message.PhoneNumber.Substring(0, 1) == "0")
            {
                message.PhoneNumber = "256" + message.PhoneNumber.Substring(1, message.PhoneNumber.Length - 1);
            }
            if (message.PhoneNumber.Length > 5)
            {
                return sender.UpdateVerifedAccount(message.CaseNumber,"0", 1, message.PhoneNumber, message.Email, message.Message);
            }
            else
                return false;
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
                    comp.HasAccountInfo = row["HasAccountInfo"].ToString();
                    comp.PhoneNumber = row["PhoneNumber"].ToString();
                    comp.Email = row["EmailAddress"].ToString();
                    comp.Message = row["Message"].ToString();
                    pendingComplaint.Add(comp);
                }
                catch (Exception ex)
                {

                }

            }
            return pendingComplaint;
        }
    }
}
