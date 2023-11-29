using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UG_DT_SendCxSms.Models
{
    public class PendingInstructions
    {
        public string Branch { get; set; }
        public string InsertSeq { get; set; }
        public string CaseNumber { get; set; }
        public string AccountNumber { get; set; }
        public string Beneficiary { get; set; } = "";
        public string ComType { get; set; } = "SMS";
        public string Complaint { get; set; } = "Case Completion";
        public string MsgContent { get; set; } = TrueAfricanCredentials.completedProcessing;
        public string CustomerId { get; set; }
        public string SubProduct { get; set; }
        public string CaseStatus { get; set; }
    }
}
