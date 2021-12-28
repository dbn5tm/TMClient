using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PJCPlus
{
    public class info
    {
        public String WSJTLogPath { get; set; }
        public String QRZUser { get; set; }
        public String QRZpwd { get; set; }
        public Boolean metric { get; set; }
        public String nickname2 { get; set; }
        public String nickname { get; set; }
        public String callsign { get; set; }
        public String firstname { get; set; }
        public String email { get; set; }
        public String locator { get; set; }
        public String state { get; set; }
        public String country { get; set; }
        public String[] band { get; set; }
        public String[] power { get; set; }
        public String[] antenna { get; set; }

        public info()
        {
            // hard code for initial checkout
            this.firstname = "Dan";
            this.callsign = "N5TM";
            this.email = "n5tm@katytx.net";
            this.state = "TX";
            this.locator = "EL29ds";
            this.country = "US";
            this.nickname = "N5TM/2m/6m";
            this.nickname2 = "N5TM/2X13RPOL/K";

        }
    }
}
