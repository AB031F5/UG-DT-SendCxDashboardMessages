using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UG_DT_SendCxSms.Models
{
    public class PendingComplaints
    {
        public string CaseNumber { get; set; }
        public string CustomerName { get; set; }
        public string AccountNumber { get; set; }
        public string PhoneNumber { get; set; } = "";
        public string Email { get; set; } = "";
        public string CustomerId { get; set; }
        public string Status { get; set; }
        public string Processed { get; set; }
        public string Message { get; set; }
        public string SmsSentAt { get; set; }
        public string EmailSentAt { get; set; }
        public string RecordDate { get; set; }
        public string HasAccountInfo { get; set; } = "";
    }
}
