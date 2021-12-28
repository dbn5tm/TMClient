using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;

namespace PJCPlus
{
    class PJ
    {
        public List<string> posts;
        public String station { get; set; }
        public int webpageindex { get; set; }
        public bool highlite { get; set; }
        public String nickname { get; set; }
        public String callsign { get; set; }
        public String firstname { get; set; }
        public String email { get; set; }
        public Color nick_color { get; set; }
        public String locator { get; set; }
        public String addr1 { get; set; }
        public String addr2 { get; set; }
        public String state { get; set; }
        public String county { get; set; }
        public String country { get; set; }
        public double distance { get; set; }
        public double azmuth { get; set; }
        public List<String> logged { get; set; }  // todo here, needs index
        public int logcount { get; set; }
        public DateTime post { get; set; }
        public bool call3 { get; set; }
        public int logCount { get; set; }

        public PJ()
        {
            posts = new List<String>();
            logged = new List<String>();
            station = "";
            county = "";
            country = "";
            state = "";
            azmuth = 0;
            distance = 0;
            callsign = "";
            email = "";
            nick_color = Color.Black;
            firstname = "";
            locator = "";
            nickname = "";
            station = "";
            call3 = false;
        }
    }
}
