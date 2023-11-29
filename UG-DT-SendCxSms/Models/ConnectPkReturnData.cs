using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UG_DT_SendCxSms.Models
{
    public class ConnectPkReturnData
    {
        public string Result { get; set; }
        public string Description { get; set; }
        public string AccountHolder { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
    }
}
