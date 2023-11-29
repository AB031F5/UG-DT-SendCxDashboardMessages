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
using System.Text.RegularExpressions;

namespace UG_DT_SendCxSms.Logic
{
    public class PendingInstructions
    {
        DataBaseContext dbContext = null;

        private static bool IsCxServiceBusy = false;
        public async void FetchPendingInstructions()
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
                Hashtable fieldParams = new Hashtable { };

                DataTable dataTable = dbContext.ExecuteQuery(StoredProcedures.fetchPendingInstructions, fieldParams, dbContext.custServiceDbConnection);
                if(dataTable != null )
                {
                    var messagesToSend = GetPendingInstructions(dataTable);
                    if( messagesToSend.Count > 0 )
                    {
                        foreach( var message in messagesToSend )
                        {
                            var msgUpate = new InstructionStatus();
                            msgUpate.Id = Convert.ToInt32(message.InsertSeq);
                            msgUpate.Status = "Picked";
                            var isUpdated = sender.UpdateInstructionStatus(msgUpate);

                            if( isUpdated )
                            {
                                var connectPk = new ConnectPkReturnData();
                                connectPk = FetchAccountInformation(message);
                                message.Beneficiary = connectPk.Phone;
                                if (message.Beneficiary.Substring(0, 1) == "0")
                                {
                                    message.Beneficiary = "256" + message.Beneficiary.Substring(1, message.Beneficiary.Length - 1);
                                }

                                if (message.Beneficiary.Length > 5)
                                {
                                    string feedback = await sender.SendSms(message.MsgContent, message.Beneficiary);
                                    if (feedback == "SUCCESS")
                                    {
                                        msgUpate.Status = feedback;
                                    }
                                    else
                                    {
                                        msgUpate.Status = "Failed";
                                    }
                                    sender.UpdateInstructionStatus(msgUpate);
                                    AddComsToDb(message);
                                    errors.Add($"Message sent :::: {JsonConvert.SerializeObject(message)}");
                                }
                                else
                                {
                                    errors.Add($"Invalid Account Contacts :::: {JsonConvert.SerializeObject(message)}");
                                    AddInvalidAccount(message);
                                }
                            }
                            else
                            {
                                //errors.Add($"Unable to update Message :::: {JsonConvert.SerializeObject(message)}");
                            }
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
                //errors.Add(":::::::::::::::::::: 4. SendToAmol Service freed ::::::::::::::::::::");
            }

            helpers.writeToFile(errors);
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
            var executed = dbContext.ExecuteNonQuery(StoredProcedures.addcomsToAccMaintenance, field, dbContext.custServiceDbConnection);
            return executed;
        }
        private ConnectPkReturnData FetchAccountInformation(Models.PendingInstructions pending)
        {
            try
            {
                HttpWebRequest request = CreateSOAPWebRequest();
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
        public HttpWebRequest CreateSOAPWebRequest()
        {
            Uri myUri = new Uri(ConnectPkEndPoints.Url, UriKind.Absolute);
            HttpWebRequest Req = (HttpWebRequest)WebRequest.Create(myUri);
            Req.Headers.Add("SOAPAction:http://tempuri.org/IConnectPKServiceApi/GetCustomer");
            Req.ContentType = "text/xml;charset=\"utf-8\"";
            Req.Accept = "text/xml";
            Req.Method = "POST";
            return Req;
        }
        private List<Models.PendingInstructions> GetPendingInstructions(DataTable dataTableData)
        {
            List<Models.PendingInstructions> pendingInstruction = new List<Models.PendingInstructions>();

            foreach (DataRow row in dataTableData.Rows)
            {
                try
                {
                    Models.PendingInstructions inst = new Models.PendingInstructions();
                    inst.InsertSeq = row["Insert_Seq"].ToString();
                    inst.Branch = row["Branch"].ToString();
                    inst.CaseNumber = row["Case_No"].ToString();
                    inst.AccountNumber = row["Account_Number"].ToString();
                    inst.SubProduct = row["Sub_Product"].ToString();
                    inst.CaseStatus = row["Case_Status"].ToString();

                    pendingInstruction.Add(inst);
                }
                catch (Exception ex)
                {

                }

            }
            return pendingInstruction;
        }
    }
}
