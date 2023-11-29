using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using UG_DT_SendCxSms.Models;

namespace UG_DT_SendCxSms.Logic
{
    public class SendMessage
    {
        public async Task< string> SendSms(string msg, string phone)
        {
            string url = TrueAfricanEndPoints.Url;
            string feedBack = "";
            string UserName = TrueAfricanCredentials.username;
            string Password = TrueAfricanCredentials.passwprd;
            string receiver = phone;
            string message = msg;
            string parameters = "USERNAME=" + UserName + "&PASSWORD=" + Password;
            parameters += "&MSISDN=" + receiver + "&MESSAGE=" + message;

            try
            {
                HttpWebRequest r = (HttpWebRequest)System.Net.WebRequest.Create(url + "?" + parameters);
                r.Headers.Clear();
                r.KeepAlive = false;
                r.ContentType = "application / x - www - form - urlencoded";
                r.Credentials = CredentialCache.DefaultCredentials;
                r.UserAgent = "Mozilla/4.0 (compatible; MSIE 5.01; Windows NT 5.0)";
                r.Timeout = 150000;
                r.Timeout = 100000;
                Encoding byteArray = Encoding.GetEncoding("utf-8");
                Stream dataStream;
                WebResponse response = (HttpWebResponse)r.GetResponse();
                dataStream = response.GetResponseStream();
                StreamReader rdr = new StreamReader(dataStream);
                feedBack = rdr.ReadToEnd();
            }
            catch (Exception ee)
            {
            }

            return feedBack;
        }

        public async Task<string> RevampedSms(string msg, string phone)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, $"{TrueAfricanEndPoints.Url}?" +
                $"USERNAME={TrueAfricanCredentials.username}&" +
                $"PASSWORD={TrueAfricanCredentials.passwprd}&" +
                $"MSISDN={phone}&" +
                $"MESSAGE={msg}");
            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return "SUCCESS";
            }
            else
            {
                return "Failed";
            }
        }

        public async Task Sender(List<Models.PendingComplaints> messages)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                // Number of requests to send
                int numberOfRequests = messages.Count;

                // Create an array of tasks
                Task<HttpResponseMessage>[] tasks = new Task<HttpResponseMessage>[numberOfRequests];

                foreach (var message in messages)
                {
                    var msgUpate = new ComplaintStatus();
                    msgUpate.Case = Convert.ToString(message.CaseNumber);
                    msgUpate.Status = "Picked";
                    var isUpdated = UpdateComplaintStatus(msgUpate);
                }
                    // Send multiple asynchronous requests
                    for (int i = 0; i < numberOfRequests; i++)
                {
                    // Optionally, you can customize each request (headers, content, etc.)
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{TrueAfricanEndPoints.Url}?" +
                $"USERNAME={TrueAfricanCredentials.username}&" +
                $"PASSWORD={TrueAfricanCredentials.passwprd}&" +
                $"MSISDN={messages[i].PhoneNumber}&" +
                $"MESSAGE={messages[i].Message}");

                    // Start the request asynchronously
                    tasks[i] = httpClient.SendAsync(request);
                }

                // Wait for all tasks to complete
                await Task.WhenAll(tasks);

                for (int i = 0; i < tasks.Length; i++)
                {
                    var msgUpate = new ComplaintStatus();
                    HttpResponseMessage response = tasks[i].Result;
                    msgUpate.Case = Convert.ToString(messages[i].CaseNumber);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        msgUpate.Status = "1";
                    }
                    else
                    {
                        msgUpate.Status = "Failed";
                    }
                    var isUpdated = UpdateComplaintStatus(msgUpate);
                    // Process the response as needed, along with the task position
                    Console.WriteLine($"Response for Task {i + 1}: Status code {response.StatusCode}");
                }
            }
        }

        public bool IsValidEmail(string email)
        {
            string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

            Regex regex = new Regex(pattern);

            return regex.IsMatch(email);
        }
        public async Task<HttpResponseMessage> SendEmail(string email, string message, string messageId)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://ugpbhkmapp000f/NotificationsApi/NotificationApi.asmx?");
            var content = new StringContent(GetEmailSoapBody(email, message, messageId), null, "text/xml");
            request.Content = content;
            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                await Console.Out.WriteLineAsync("Success");
            }
            else
            {
                await Console.Out.WriteLineAsync("Failed to send");
            }
            //response.EnsureSuccessStatusCode();
            //Console.WriteLine(await response.Content.ReadAsStringAsync());
            return response;
        }
        public string GetEmailSoapBody(string email, string message, string messageId)
        {
            XmlDocument SOAPReqBody = new XmlDocument();
            SOAPReqBody.LoadXml($@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
    <soap:Body>
        <SendEmail xmlns=""http://tempuri.org/"">
            <message>
                <Message>{message}</Message>
                <Subject>Complaint update</Subject>
                <Recipients>
                    <string>{email}</string>
                </Recipients>
                <MessageId>{messageId}</MessageId>
            </message>
        </SendEmail>
    </soap:Body>
</soap:Envelope>");

            StringWriter sw = new StringWriter();
            XmlTextWriter tx = new XmlTextWriter(sw);
            SOAPReqBody.WriteTo(tx);
            return sw.ToString();
        }
        public bool UpdateInstructionStatus(InstructionStatus message)
        {
            bool isUpdated = false;
            try
            {
                ArrayList errors = new ArrayList();
                String messageUpdated = JsonConvert.SerializeObject(message);

                DataBaseContext dt = new DataBaseContext();
                Hashtable parameters = new Hashtable
                {
                    {"P_message_id",message.Id },
                    {"P_message_status",message.Status }
                };
                Helpers helper = new Helpers();
                String updateParams = JsonConvert.SerializeObject(parameters);

                DataBaseContext dataBaseContext = new DataBaseContext();
                var isSuccessfull = dt.ExecuteNonQuery(StoredProcedures.updateInstruction, parameters, dataBaseContext.custServiceDbConnection);

                if (isSuccessfull)
                {
                    errors.Add($"Updated Params for RecId :::: " + messageUpdated);
                    //helper.writeToFile(errors);
                    isUpdated = true;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return isUpdated;
        }

        public bool UpdateComplaintStatus(ComplaintStatus message)
        {
            bool isUpdated = false;
            try
            {
                ArrayList errors = new ArrayList();
                String messageUpdated = JsonConvert.SerializeObject(message);

                DataBaseContext dt = new DataBaseContext();
                Hashtable parameters = new Hashtable
                {
                    {"P_complaint_id",message.Case },
                    {"P_complaint_status",message.Status }
                };
                Helpers helper = new Helpers();
                String updateParams = JsonConvert.SerializeObject(parameters);

                DataBaseContext dataBaseContext = new DataBaseContext();
                var isSuccessfull = dt.ExecuteNonQuery(StoredProcedures.updateComplaint, parameters, dataBaseContext.custServiceDbConnection);

                if (isSuccessfull)
                {
                    errors.Add($"Updated Params for RecId :::: " + messageUpdated);
                    //helper.writeToFile(errors);
                    isUpdated = true;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return isUpdated;
        }

        public bool UpdateVerifedAccount(String caseNum,string stat, int hasInfo, string phone, string email, string message)
        {
            bool isUpdated = false;
            try
            {
                ArrayList errors = new ArrayList();
                String messageUpdated = JsonConvert.SerializeObject(message);

                DataBaseContext dt = new DataBaseContext();
                Hashtable parameters = new Hashtable
                {
                    {"caseNum",caseNum },
                    {"stat",stat },
                    {"hasAccountInfo",hasInfo },
                    {"phone",phone },
                    {"email",email },
                    {"message",message }
                };
                Helpers helper = new Helpers();
                String updateParams = JsonConvert.SerializeObject(parameters);

                DataBaseContext dataBaseContext = new DataBaseContext();
                var isSuccessfull = dt.ExecuteNonQuery(StoredProcedures.updateVerifiedInfo, parameters, dataBaseContext.custServiceDbConnection);

                if (isSuccessfull)
                {
                    errors.Add($"Updated Params for RecId :::: " + messageUpdated);
                    //helper.writeToFile(errors);
                    isUpdated = true;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return isUpdated;
        }

        public bool UpdateWipSMSComplaintTime(String caseNum, string timeSmsWent)
        {
            bool isUpdated = false;
            try
            {
                ArrayList errors = new ArrayList();

                DataBaseContext dt = new DataBaseContext();
                Hashtable parameters = new Hashtable
                {
                    {"caseNum",caseNum },
                    {"smsTime",timeSmsWent },
                };
                Helpers helper = new Helpers();
                String updateParams = JsonConvert.SerializeObject(parameters);

                DataBaseContext dataBaseContext = new DataBaseContext();
                var isSuccessfull = dt.ExecuteNonQuery("sp_UpdateTimeSmsWent", parameters, dataBaseContext.custServiceDbConnection);

                if (isSuccessfull)
                {
                    //helper.writeToFile(errors);
                    isUpdated = true;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return isUpdated;
        }
        public bool UpdateWipEmailComplaintTime(String caseNum, string timeEmailWent)
        {
            bool isUpdated = false;
            try
            {
                ArrayList errors = new ArrayList();

                DataBaseContext dt = new DataBaseContext();
                Hashtable parameters = new Hashtable
                {
                    {"caseNum",caseNum },
                    {"emailTime",timeEmailWent },
                };
                Helpers helper = new Helpers();
                String updateParams = JsonConvert.SerializeObject(parameters);

                DataBaseContext dataBaseContext = new DataBaseContext();
                var isSuccessfull = dt.ExecuteNonQuery("sp_UpdateTimeEmailWent", parameters, dataBaseContext.custServiceDbConnection);

                if (isSuccessfull)
                {
                    //helper.writeToFile(errors);
                    isUpdated = true;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return isUpdated;
        }
    }
}
