using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Timers;

namespace PJCPlus
{
    class WebClientThread
    {
        System.Timers.Timer _Timer;
        public bool StopWebClient;
        //public event PageRcvd;
        //public event ClientError;
        public string url { get; set; }
        //private WebClient client;
        static readonly HttpClient client1 = new HttpClient();
        public delegate void WebClientEventHandler(String ret);
        public event WebClientEventHandler RetStr;

        // 12-27-2021 updated to HttpClient from WebClient
        public WebClientThread()
        {
            RetStr += new WebClientEventHandler(ret_page);
            this.StopWebClient = false;
            //client = new WebClient();
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            //client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
            _Timer = new Timer(10000);
            _Timer.Elapsed += new ElapsedEventHandler(_timer_elapsed);
        }

        void ret_page(String ret)
        {

        }

        public void go()
        {
            //DownLoadPJPage();
            DownloadPage(url);
            StartTimer();
        }
        async Task DownloadPage(string url)
        {
            // Call asynchronous network methods in a try/catch block to handle exceptions.
            try
            {
                HttpResponseMessage response = await client1.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                // Above three lines can be replaced with new helper method below
                // string responseBody = await client.GetStringAsync(uri);
                RetStr(responseBody);    
                Console.WriteLine(responseBody);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }
        }
        /*private void DownLoadPJPage()
        {
           
            try
            {
                RetStr("heartbeat");
                String str = client.DownloadString(this.url);
                RetStr(str);
            }
            catch (Exception e)
            {
                RetStr("Web Error: " + e.ToString());
            }


        }*/

        public void StartTimer()
        {

            _Timer.Enabled = true;
        }

        private void _timer_elapsed(object sender, ElapsedEventArgs e)
        {
            _Timer.Enabled = false;
            DownloadPage(url);
            _Timer.Enabled = true;
        }
    }
}
