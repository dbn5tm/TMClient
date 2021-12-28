using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace N5TMScrapper
{
    public partial class MySettings : Form
    {
        public PJCPlus.info myInfo { get; set; }
        public delegate void MySettingsEventHandler(PJCPlus.info myInfo);
        public event MySettingsEventHandler RetInfo;
        public MySettings()
        {
            InitializeComponent();
            this.RetInfo += new MySettingsEventHandler(ret_info);
            

        }

        void ret_info(PJCPlus.info myInfo)
        {

        }

        

        private void btnSave_Click(object sender, EventArgs e)
        {
            myInfo.callsign = txtMyCallsign.Text;
            myInfo.firstname = txtMyName.Text;
            myInfo.nickname = txtMyPJNick.Text;
            myInfo.nickname2 = txtMyEMENick.Text;
            myInfo.email = txtMyEmail.Text;
            myInfo.state = txtMyState.Text;
            myInfo.country = txtMyCountry.Text;
            myInfo.locator = txtMyGrid.Text;
            RetInfo(myInfo);
            this.Close();
        }

        private void MySettings_Load(object sender, EventArgs e)
        {
            txtMyCallsign.Text = myInfo.callsign;
            txtMyName.Text = myInfo.firstname;
            txtMyPJNick.Text = myInfo.nickname;
            txtMyEMENick.Text = myInfo.nickname2;
            txtMyEmail.Text = myInfo.email;
            txtMyGrid.Text = myInfo.locator;
            txtMyState.Text = myInfo.state;
            txtMyCountry.Text = myInfo.country;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
