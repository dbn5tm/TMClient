using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Deployment.Application;

namespace N5TMScrapper
{
    public partial class MainForm : Form
    {
        private String[] PageURL = {"https://www.pingjockey.net/cgi-bin/pingtalk/", "https://www.chris.org/cgi-bin/jt65emeA",
        "https://www.chris.org/cgi-bin/jt65emeB", "https://www.chris.org/cgi-bin/jt65talk"};
        PJCPlus.WebClientThread RetrievePJPage = new PJCPlus.WebClientThread();
        string[] SplitChars = { "(", "{", "{", "(" };
        char[] rightSplitChars = { ')', '}', '}', ')' };
        string SplitChar = "";
        List<PJCPlus.PJ> nick_unique;
        List<String> freq_post;
        List<DateTime> myposts = new List<DateTime>();
        
        long minutetimer = 0;
        string[] post_time = new string[100];
        String[] post_nick = new string[100];
        String[] post_text = new string[100];
        String[] post_email = new string[100];
        String[] old_postTime = new string[100];
        PJCPlus.info myInfo = new PJCPlus.info();
        bool RefreshPage = true;
        int PageURLIndex = 0;// hard coded
        PJCPlus.DistBearing distBearing = new PJCPlus.DistBearing();
        int npj_count;
        Color defaultBackColor = Color.Black;
        Color[] post_color = new Color[] { Color.Orchid, Color.CadetBlue, Color.Green, Color.Red, Color.Magenta
            , Color.OrangeRed, Color.YellowGreen, Color.SpringGreen, Color.SeaGreen, Color.LightBlue };

        List<String> m_ColumnNames = new List<String>();
        List<int> m_ColumnIndexes = new List<int>();
        int[] ColumnWidth = { 0, 0, 0, 0, 0, 0, 0 };
        int callsignIndex = 0;
        string imageURl = "";
        string selectedCallsign = "blank";
        //string selectedGrid = "blank";
        string Oldfile2down = "";
        string highlited_callsign = "";
        Color highlited_color = Color.Black;
        //ListSortDirection lastSort = ListSortDirection.Descending;
        //bool InitializingListView = false;
        string text2edit = "";
        string line2edit = "";
        int History = 7200;
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            //StationsList.SelectedValueChanged += new EventHandler(StationsList_MouseUp);
            // MsgTextView.Buffer.UserActionEnded += new EventHandler(MsgTextView_KeyPress);
           
            this.Text = "TMClient (beta):" + Application.ProductVersion;
            GetSettings();
            PageURLIndex = cboWebPages.SelectedIndex;
            SplitChar = SplitChars[cboWebPages.SelectedIndex];
            initStationsList();
            nick_unique = new List<PJCPlus.PJ>();
            freq_post = new List<String>();
            RetrievePJPage.RetStr += new PJCPlus.WebClientThread.WebClientEventHandler(RetrievePJPageFromThread);
            RetrievePJPage.url = PageURL[PageURLIndex];   //"https://www.pingjockey.net/cgi-bin/pingtalk/";
            
            RetrievePJPage.go();

            cboWebPages.SelectedValue = PageURLIndex;
            if (PageURLIndex > 0)
                SplitChar = "{";
            else
                SplitChar = "(";
            
            dataGridViewPJResp.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dataGridViewPJResp.Columns[1].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dataGridViewPJResp.Columns[1].Width = dataGridViewPJResp.Width
                - (dataGridViewPJResp.Columns[0].Width + dataGridViewPJResp.Columns[2].Width) - 20;

            //createGraphicsColumn();
        }
        private void createGraphicsColumn()
        {
            //Icon treeIcon = new Icon(this.GetType(), "tree.png");
            Image tree = Image.FromFile("tree.png");
            DataGridViewImageColumn iconColumn = new DataGridViewImageColumn();
            iconColumn.Image = tree;
            iconColumn.Name = "Tree";
            iconColumn.HeaderText = "Nice tree";
            dataGridViewPJResp.Columns.Insert(1, iconColumn);
        }

        private void initStationsList()
        {
            StationsList.Columns[3].ValueType = typeof(double);
            StationsList.Columns[4].ValueType = typeof(int);
            StationsList.Rows.Add();
        }

        private void GetSettings()
        {
            cboWebPages.SelectedIndex = Properties.Settings.Default.WebPageIndex;
            if(Properties.Settings.Default.myCallsign != "")  // use defaults if nothing saved.
            {
                myInfo.callsign = Properties.Settings.Default.myCallsign;
                myInfo.firstname = Properties.Settings.Default.myName;
                myInfo.nickname = Properties.Settings.Default.myNicknamePJ;
                myInfo.nickname2 = Properties.Settings.Default.myNicknameEME;
                myInfo.locator = Properties.Settings.Default.myGrid;
                myInfo.email = Properties.Settings.Default.myEmail;
                myInfo.state = Properties.Settings.Default.myState;
                History = Properties.Settings.Default.History;
                switch (History)
                {
                    case 3600:
                        hrToolStripMenuItem.Checked = true;
                        break;
                    case 7200:
                        hrs2ToolStripMenuItem.Checked = true;
                        break;
                    case 21600:
                        hrs6ToolStripMenuItem.Checked = true;
                        break;
                    default:
                        allHistoryToolStripMenuItem.Checked = true;
                        break;
                }
            }
            
        }

        private void SaveSettings()
        {
            Properties.Settings.Default.WebPageIndex = cboWebPages.SelectedIndex;
            Properties.Settings.Default.myCallsign = myInfo.callsign;
            Properties.Settings.Default.myName = myInfo.firstname;
            Properties.Settings.Default.myNicknamePJ = myInfo.nickname;
            Properties.Settings.Default.myNicknameEME = myInfo.nickname2;
            Properties.Settings.Default.myGrid = myInfo.locator;
            Properties.Settings.Default.myEmail = myInfo.email;
            Properties.Settings.Default.myState = myInfo.state;
            Properties.Settings.Default.History = History;
            Properties.Settings.Default.Save();
        }

        void RetrievePJPageFromThread(String str)
        {
            if (str == "heartbeat")
            {
                this.InvokeAndClose((MethodInvoker)delegate
                {
                    toolStripProgressBar.Value += 10;
                    if (toolStripProgressBar.Value == 100) toolStripProgressBar.Value = 0;
                });
            }
            else
            {
                if (str.Contains("Error"))
                {
                    this.InvokeAndClose((MethodInvoker)delegate
                    {
                        toolStripStatusInfo.Text = str.Substring(80);
                    });
                }
                else
                {
                    PostReponse(str);
                    if (npj_count != nick_unique.Count) FillStationsList();
                }
                
            }
            
            //FillStationsList();
            //RetrievePJPage.StartTimer ();
        }

        private void FillStationsList()
        {
            
            for (int i = npj_count; i < nick_unique.Count; i++)
            {
                
                //MessageBox.Show("adding to Stations List");
                if (nick_unique[i].webpageindex == PageURLIndex)
                {
                    if (History > 0)
                    {
                        try
                        {
                            double ddiff = (DateTime.UtcNow - nick_unique[i].post).TotalSeconds;
                            if (ddiff < History)
                            { //' do not show old posts
                                addItemToStationList(nick_unique[i], StationsList);
                            }
                            else
                            {
                                //' remove any posts from freq tree for this call.
                                //AddTreeNode (-1, nick_unique (i).callsign.split ("/") (0), 0, "Clear", "0");
                            }
                        }
                        catch (Exception e)
                        {
                            Console.Write(e.ToString());
                        }
                        //addItemToStationList(nick_unique[i], StationsList);
                    }
                    else
                    {
                        //int r = StationsList.Rows.Count;
                        addItemToStationList(nick_unique[i], StationsList);
                    }
                    //'addToDatabase(nick_unique(i))
                }
            }
            npj_count = nick_unique.Count;
            //MessageBox.Show("done adding to Stations List");

        }

        private void addItemToStationList(PJCPlus.PJ npj, DataGridView dgv)
        {
            
            this.InvokeAndClose((MethodInvoker)delegate
            {

                
                dgv.Rows.Add();
                int r = dgv.Rows.Count - 2;
                dgv.EditDGVCell(dgv.Rows[r].Cells[0], npj.callsign, npj.nick_color, defaultBackColor);
                dgv.EditDGVCell(dgv.Rows[r].Cells[1], npj.firstname, npj.nick_color, defaultBackColor);
                dgv.EditDGVCell(dgv.Rows[r].Cells[2], npj.locator, npj.nick_color, defaultBackColor);
                dgv.EditDGVCell(dgv.Rows[r].Cells[3], npj.azmuth.ToString(), npj.nick_color, defaultBackColor);
                dgv.EditDGVCell(dgv.Rows[r].Cells[4], npj.distance.ToString(), npj.nick_color, defaultBackColor);
                dgv.EditDGVCell(dgv.Rows[r].Cells[5], npj.state, npj.nick_color, defaultBackColor);
                
                
            });
        }

        private void addItemToPJResponseGrid(DataGridView pjr, int col, object value, Color textColor, bool highlite, bool newrow, PJCPlus.PJ npj)
        {
            this.InvokeAndClose((MethodInvoker)delegate
            {
                //if (newrow) pjr.Rows.Add();
                if (newrow) pjr.Rows.Insert(0);
                
                int r = pjr.Rows.Count - 2;
                r = 0;
                Color thisbackground = defaultBackColor;
                if(col == 1)
                {
                    if (highlite || npj.highlite)
                    {
                        thisbackground = Color.Yellow;
                        textColor = Color.Black;
                    }
                    if (npj.callsign == myInfo.callsign)
                    {
                        thisbackground = Color.LightBlue;
                        textColor = Color.Black;
                    }
                }
                string celltext = (string)value;
                if( celltext.Contains("http://"))
                {
                    DataGridViewCellStyle httpStyle = GetHyperLinkStyleForGridCell();
                    pjr.Rows[r].Cells[col].Style = httpStyle;
                }
                pjr.EditDGVCell(pjr.Rows[r].Cells[col], (string)value, textColor, thisbackground);
                

            });
            
        }

        private void PostReponse(String str)
        {
            Regex r = new Regex("DDMMM  UTC");
            String[] posted = r.Split(str);
            // get body of message ignor junk on top.
            
            if (posted.Length > 1)
            {


                //' find time/date
                Regex dt = new Regex(@"\d\d\w\w\w \d\d:\d\d");
                Regex de = new Regex("This service");
                string[] textpart = de.Split(posted[1]);
                string[] wholeline = dt.Split(textpart[0]);
                MatchCollection dte = dt.Matches(textpart[0]);
                //' find nick names
                Regex n = new Regex(">.*</a>");
                Regex n1 = new Regex("(.*)");
                Regex n2 = new Regex("{.*}");
                Regex ip = new Regex(@".\d{1,3}.\.\d{1,3}");  //'.\.\d{1,3}.\.\d{1,3}")

                //'find email
                Regex e = new Regex("mailto(.*)''");

                MatchCollection nick = n.Matches(textpart[0]);

                Regex t = new Regex(@"\(<a href.*</a>");
                Regex tJT65A = new Regex("====== {.*</a>");
                Regex ta = new Regex("</a>");
                string[] txt = n.Split(textpart[0]);


                int i = 0;

                try
                {
                    Match myNick;
                    //' start over with freq_post arraylist

                    //freq_post.Clear();

                    for (i = 0; i < dte.Count; i++)
                    {

                        if (wholeline[i + 1].Length > 2)
                        {
                            if (wholeline[i + 1].Contains("~edit|"))
                            {
                                Console.Write(wholeline[i + 1]);
                            }
                            myNick = n.Match(wholeline[i + 1]);
                            if (myNick.Success)
                            {
                                String[] nn = ta.Split(wholeline[i + 1].Substring(wholeline[i + 1].IndexOf('>') + 1));
                                string[] nnn = ip.Split(nn[1])[0].Split(rightSplitChars[cboWebPages.SelectedIndex]);
                                Match ipaddr = ip.Match(wholeline[i + 1]);
                                post_nick[i] = nn[0] + nnn[0];  
                            }
                            else
                            {
                                //int PageURLIndex = 0;
                                if (PageURLIndex > 0)
                                {
                                    myNick = n2.Match(wholeline[i + 1]);
                                }
                                else
                                {
                                    myNick = n1.Match(wholeline[i + 1]);
                                }


                                if (myNick.Length > 1)
                                {
                                    if (myNick.Value.Contains(SplitChar))
                                    {
                                        String nn = wholeline[i + 1].Substring(wholeline[i + 1].IndexOf(SplitChar) - 1);
                                        String[] nnn = ip.Split(nn)[0].Split(rightSplitChars[cboWebPages.SelectedIndex]);
                                        post_nick[i] = nnn[0].Substring(2);

                                    }
                                    else
                                    {
                                        post_nick[i] = "";
                                    }
                                }

                            }

                            string[] split_nick = post_nick[i].Split(' ');

                            PJCPlus.PJ npj = new PJCPlus.PJ();
                            post_time[i] = dte[i].Value;

                            Boolean item_found = false;
                            int item_index = 0;
                            //' check all items in nick_unique for this nickname
                            foreach (PJCPlus.PJ Item in nick_unique)
                            {
                                if (Item.nickname == post_nick[i])
                                {
                                    item_found = true;
                                    npj = Item;
                                    DateTime post_old = nick_unique[item_index].post;
                                    DateTime post_new = PostTimeToDate(post_time[i]);
                                    try
                                    {
                                        double ddiff = (PostTimeToDate(post_time[i]) - nick_unique[item_index].post).TotalSeconds;
                                        if (ddiff <= 0)
                                        {
                                            nick_unique[item_index].post = PostTimeToDate(post_time[i]);
                                        }
                                    }
                                    catch(Exception ex)
                                    {
                                        Console.Write(ex.ToString());
                                    }
                                    item_index += 1;
                                }
                            }


                            //' if not found in nick_unique, add it to collection
                            if (!item_found && split_nick[0] != "")
                            {
                                npj.callsign = split_nick[0].Split('/')[0];
                                npj.firstname = split_nick[1];
                                npj.state = split_nick[2];
                                npj.locator = split_nick[3];
                                npj.distance = MHDistance(myInfo.locator, npj.locator);
                                npj.azmuth = MHAzmuth(myInfo.locator, npj.locator);
                                npj.nickname = post_nick[i];
                                npj.post = PostTimeToDate(post_time[i]);

                                npj.nick_color = post_color[callsignIndex % 10];
                                callsignIndex++;
                                //npj.nick_color = call_colors.Item(nick_unique.Count % 12);
                                npj.webpageindex = PageURLIndex;  //' which web page we are on PJ or EME-1,EME-2 Terrestrial 

                                //' extract email address
                                try
                                {
                                    string em = e.Match(wholeline[i + 1]).ToString().Replace("','", "@");
                                    if (em.Length > 0)
                                    {
                                        npj.email = em.Substring(9, em.Length - 10); //Mid(em, 9, em.Length - 10);
                                    }
                                }
                                catch(Exception ex)
                                {
                                    Console.Write(ex.ToString());
                                }

                                nick_unique.Add(npj);

                            }



                            if (wholeline[i + 1].Contains("<a href"))
                            {
                                //' email embended post
                                //int PageURLIndex = 0;
                                String[] temp = t.Split(wholeline[i + 1]);
                                if (PageURLIndex > 0)
                                {
                                    post_text[i] = tJT65A.Split(wholeline[i + 1])[0];
                                }
                                else
                                {
                                    post_text[i] = t.Split(wholeline[i + 1])[0];
                                }
                            }
                            else
                            {

                                //' non email embedded post
                                int p = wholeline[i + 1].IndexOf(SplitChar); // + post_nick[i]);
                                if (p > 1)
                                {
                                    post_text[i] = wholeline[i + 1].Substring(1, p - 1);
                                }
                                else
                                {
                                    post_text[i] = wholeline[i + 1];
                                }
                            }

                            //'----save all posts for each station
                            if (npj.posts.Count > 0)
                            {

                                if (!npj.posts.Contains(post_time[i].Split(' ')[1] + ": " + post_text[i]))
                                {
                                    double pdiff = (PostTimeToDate(post_time[i]) - npj.post).TotalSeconds;
                                    if (pdiff > 0)
                                    {
                                        npj.posts.Add(post_time[i].Split(' ')[1] + ": " + post_text[i]);
                                    }
                                    else
                                    {

                                        npj.posts.Insert(0, (post_time[i].Split(' ')[1] + ": " + post_text[i]));
                                    }

                                    // to do...
                                    //'if this station is in tab update
                                    /*If Me.InfoTabControl.TabPages(2).Text = npj.callsign.Split("/")(0) Then

                                    Me.SelectedStationsTextBox.Text = ""

                                    For Each Item As String In npj.posts
                                    Me.SelectedStationsTextBox.Text += (Item + Chr(13) + Chr(10))
                                    Next
                                    End If*/
                                }
                            }
                            else
                            {

                                npj.posts.Add(post_time[i].Split(' ')[1] + ": " + post_text[i]);
                            }
                            //'--------------


                            String[] infoPost = post_text[i].Split('~');
                            String cmd = "";
                            if (infoPost.Length > 2)
                            {

                                String encrPost = post_text[i].Replace("~Plus~", "+");
                                encrPost = encrPost.Replace("~Percent~", "%");
                                encrPost = encrPost.Replace("~http~", "http://");
                                encrPost = encrPost.Replace("~GT~", ">");
                                encrPost = encrPost.Replace("~LT~", "<");
                                encrPost = encrPost.Replace("~w.w.w~", "www");
                                
                                post_text[i] = encrPost;
                                
                                cmd = infoPost[1];
                                if (cmd.IndexOf("CQ") != -1)
                                {
                                    if (infoPost.Length > 3)
                                        cmd = cmd + infoPost[3];
                                }
                            }
                            if (cmd.IndexOf("Running") != -1)
                            {
                                if (infoPost.Length > 3)
                                {
                                    cmd = cmd + infoPost[3];
                                }
                            }
                            if (cmd.IndexOf("QRV") != -1)
                            {
                                if (infoPost.Length > 3)
                                {
                                    cmd = cmd + infoPost[3];
                                }
                            }
                            if (cmd.Contains("CQ") || cmd.Contains("Running") || cmd.Contains("QRV") || cmd.Contains("Clear"))
                            {
                                String freq = infoPost[2];
                                if (freq_post.Contains(split_nick[0]))
                                {
                                    //' this is older post of frequency  todo----
                                    AddTreeNode(freq, split_nick[0].Split('/')[0], 1, cmd, post_time[i].Split(' ')[1]);
                                }
                                else
                                {
                                    //' this is most recent post of frequency
                                    freq_post.Add(split_nick[0]);
                                    AddTreeNode(freq, split_nick[0].Split('/')[0], 0, cmd, post_time[i].Split(' ')[1]);
                                }
                            }
                            if (post_text[i].Contains("http"))
                            {
                                Regex h = new Regex("http");
                                string url = "http" + h.Split(post_text[i])[1];
                                if(imageURl == "")
                                {
                                    imageURl = url;
                                    //webBrowser1.Navigate(url);
                                }
                                
                            }
                            if (post_text[i].Contains("ftp"))
                            {
                                string[] parse = post_text[i].Split('/');
                                string parse2 = parse[parse.Length - 1];
                                string file2down = parse2.Substring(0, parse2.Length - 1);
                                if(file2down != Oldfile2down)
                                {
                                    Oldfile2down = file2down;
                                    ftp ftpClient = new ftp(@"ftp://ftp.dbjsystems.net", "n5tm@dbjsystems.net", "dbN5TM$#0220");
                                
                                    ftpClient.download("public_ftp/incoming/" + file2down, @"C:\Users\dan\Documents\" + file2down);
                                    pictureBoxFTP.ImageLocation = "C:\\Users\\dan\\Documents\\" + file2down;
                                    pictureBoxFTP.Load();
                                }
                                

                            }
                            if (post_text[i].Contains("edit"))
                            {
                                /*
                                foreach (DataGridViewRow row in dataGridViewPJResp.Rows)
                                {
                                    if(row.Cells[1].Value != null)
                                    {
                                        string posttime = row.Cells[0].Value.ToString();
                                        if(posttime == line2edit)
                                        {
                                            row.Cells[1].Value = "(edited) " + post_text[i].Split('~')[2];
                                        }
                                    }
                                    
                                    //if(val == post_text)
                                }*/

                            }
                        }
                    }



                    //' find new messages
                    i = -1;
                    if (RefreshPage)
                    {
                        old_postTime[0] = "";
                        RefreshPage = false;
                        //dataGridViewPJResp.SelectAll();
                        //dataGridViewPJResp.ClearSelection();
                    }

                    do
                    {

                        i += 1;
                        if (i >= dte.Count) break;
                    } while (post_time[i] + post_nick[i] + post_text[i] != old_postTime[0]);



                    for (i = dte.Count - i; i < dte.Count; i++)
                    {

                        String newtext = post_text[dte.Count - 1 - i];
                        Boolean highliteThis = false;
                        PJCPlus.PJ npj = new PJCPlus.PJ();
                        foreach (PJCPlus.PJ item in nick_unique)
                        {
                            if (item.nickname == post_nick[dte.Count - 1 - i])
                            {
                                npj = item;
                                if (npj.callsign == selectedCallsign) highliteThis = true;
                            }

                        }

                        if (myInfo.callsign == "")
                        {
                            myInfo.callsign = myInfo.nickname.Split(' ')[0].Split('/')[0];
                        }
                        
                        String searchMyCallsign = myInfo.callsign.ToUpper();
                        if (newtext.ToUpper().Contains(searchMyCallsign) || newtext.ToUpper().Contains(selectedCallsign))
                        {
                            highliteThis = true;
                        }

                        if (npj.highlite)
                        {
                            highliteThis = true;
                        }
                        if (!post_text[i].Contains("~edit~"))
                        {
                            //SetTextBox(PJTextBox, post_time[dte.Count - 1 - i] + " ", true, Color.Gray, false);
                            addItemToPJResponseGrid(dataGridViewPJResp, 0, post_time[dte.Count - 1 - i], Color.Gray, false, true, npj);
                            //SetTextBox(PJTextBox, post_nick[dte.Count - 1 - i], true, npj.nick_color, false);  //npj.nick_color
                            //addItemToPJResponseGrid(dataGridViewPJResp, 2, post_nick[dte.Count - 1 - i], npj.nick_color, false, false);
                            addItemToPJResponseGrid(dataGridViewPJResp, 2, npj.callsign, npj.nick_color, false, false, npj);
                            // SetTextBox(PJTextBox, "=> ", true, Color.LightGray, false);

                            //SetTextBox(PJTextBox, post_text[dte.Count - 1 - i] + "\r" + "\n", true, Color.White, highliteThis);
                            addItemToPJResponseGrid(dataGridViewPJResp, 1, post_text[dte.Count - 1 - i], npj.nick_color, false, false, npj);
                        }
                        
                        if (newtext.Contains("CQ")) SetTextBox(rtxtCQBox, post_nick[dte.Count - 1 - i] + post_text[dte.Count - 1 - i] + "\r" + "\n", true, Color.White, false);
                    }




                    //'age old calls in listview, refresh after 60 postings 5 seconds apart = 5 minutes
                    minutetimer += 1;
                    if (minutetimer > 10)
                    {
                        minutetimer = 0;
                        //reFillListview();
                    }
                    toolStripStatusLabelUTC.Text = DateTime.Now.ToUniversalTime().ToString("HH:mm:ss") + " UTC";
                }
                catch (Exception exception)
                {
                    Console.Write(exception);
                }

                for (i = 0; i < dte.Count; i++)
                {

                    old_postTime[i] = post_time[i] + post_nick[i] + post_text[i];
                    i += 1;

                }
            }
            else
            {
                //PJCPlus.MessageBox MsgBox = new PJCPlus.MessageBox();
                //MsgBox("PJ failed to return proper page.");
            }

        }

        private DateTime PostTimeToDate(String t1)
        {
            try
            {
                String[] t1Date = t1.Split(' ')[0].Split('/');  //' 0 = month, 1 = day
                String[] t1Time = t1.Split(' ')[1].Split(':');  //' 0 = hour, 1 = minute
                String d = DateTime.Now.Year.ToString() + "-" + t1Date[0] + " " + t1Time[0] + ":" + t1Time[1] + ":" + "00";
                DateTime ret = DateTime.Parse(d);
                return ret;
            }
            catch
            {
                return DateTime.Now;
            }
        }

        private void AddTreeNode(string freq, string callsign, int i, string cmd, string t)
        {
            
            try
            {
                string nodefreq = "";
                string[] nodemsg = { "", "", "", "", "" };
                bool foundfreq = false;
                bool foundpost = false;
                TreeNode freqnode = new TreeNode();

                this.InvokeAndClose((MethodInvoker)delegate
                {
                    if (cmd.Contains("Clear"))
                    {
                        foreach (TreeNode n in treeViewCQ.Nodes.All())
                        {
                            if(n.Text == callsign)
                            {
                                treeViewCQ.Nodes.Remove(treeViewCQ.SelectedNode);
                            }
                        }
                    }
                    else
                    {
                        if(treeViewCQ.Nodes.Count == 0)
                        {
                            freqnode = treeViewCQ.Nodes.Add(freq);
                            treeViewCQ.SelectedNode = freqnode;
                            freqnode.Nodes.Add(callsign + " - " + t + " " + cmd);
                        }
                        else
                        {
                            
                            foreach (TreeNode n in treeViewCQ.Nodes.All())
                            {
                                // check if freq node exists
                                                            
                                if (n.Level == 0)  // parent node = freq
                                {
                                    freqnode = n;
                                    nodefreq = n.Text;
                                }
                                if(n.Level > 0)  // child node = callsign and message
                                {
                                    nodemsg = n.Text.Split(' ');
                                }
                                if (nodefreq == freq)
                                {
                                    foundfreq = true;
                                    if (nodemsg[0] == callsign)
                                    {
                                        foundpost = true;
                                        //found this post
                                        break;
                                    }
                                }
                           
                            }
                            if (!foundpost)
                            {
                                if(!foundfreq)freqnode = treeViewCQ.Nodes.Add(freq);
                                treeViewCQ.SelectedNode = freqnode;
                                freqnode.Nodes.Add(callsign + " - " + t + " " + cmd);
                            }
                        }
                    
                    }
                });
            }
            catch (Exception e)
            {

                Console.Write(e.ToString());
            }
        }
        //private delegate void newSetTextCallback(TextBox textbox, string value, bool concat, Color color);
        public void SetTextBox(RichTextBox textbox, string value, bool concat, Color color, bool highlite)
        {
            Color thisbackground;
            try
            {
                this.InvokeAndClose((MethodInvoker)delegate
                {
                    if (concat)
                    {
                        if (highlite)
                        {
                            thisbackground = Color.YellowGreen;
                            color = Color.DarkRed;
                        }
                        else
                        {
                            thisbackground = defaultBackColor;
                        }
                        textbox.AppendText(value, color, thisbackground);
                        //textbox.SelectionColor = Color.Blue;
                        //textbox.Text += value; /// + "\r\n";
                        //textbox.SelectionStart = textbox.Text.Length;
                        textbox.ScrollToCaret();


                        //if (textbox.Lines.Length > 1000) textbox.Text = "";

                    }
                    else
                    {
                        textbox.Text = value;
                    }
                });
            }
            catch (Exception e)
            {

                Console.Write(e.ToString());
            }

        }

        private double MHDistance(String myGrid, String hisGrid)
        {
            bool metricDistance = false;  // hard coded

            try
            {
                double d = distBearing.Distance(myGrid, hisGrid);
                if (metricDistance)
                {
                    return Math.Ceiling(d);
                }
                else
                {
                    return Math.Ceiling(d * 0.621371);
                }
            }
            catch
            {
                return 0;
            }
            //return 0;
        }

        private double MHAzmuth(String myGrid, String hisGrid)
        {

            try
            {
                double az = distBearing.Azimuth(myGrid, hisGrid);
                return Math.Ceiling(az);

            }
            catch
            {
                return 0;
            }
        }

        private HttpWebResponse webrequest(String MsgToPost, String ConnectURL)
        {
            CookieContainer CookieJar = new CookieContainer();
            CookieCollection CookieList = new CookieCollection();
            String PostData;

            System.Net.HttpWebRequest Request = System.Net.HttpWebRequest.CreateHttp(ConnectURL);
            Request.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.8.1.14) Gecko/20080404 Firefox/2.0.0.14";
            Request.CookieContainer = CookieJar;
            Request.AllowAutoRedirect = false;
            if (PageURLIndex == 0)
            {
                Request.CookieContainer.Add(new Uri(ConnectURL), new Cookie("callsign", myInfo.nickname));
            }
            else
            {
                Request.CookieContainer.Add(new Uri(ConnectURL), new Cookie("callsign", myInfo.nickname2));
            }

            Request.CookieContainer.Add(new Uri(ConnectURL), new Cookie("firstname", myInfo.firstname));
            Request.CookieContainer.Add(new Uri(ConnectURL), new Cookie("state", myInfo.state));
            Request.CookieContainer.Add(new Uri(ConnectURL), new Cookie("locator", myInfo.locator));
            Request.CookieContainer.Add(new Uri(ConnectURL), new Cookie("email", myInfo.email));
            //'Turn POST login string into ASCII and insert into Request... somehow 
            Request.ContentType = "application/x-www-form-urlencoded";
            PostData = MsgToPost;
            Request.Method = "POST";

            Request.ContentLength = PostData.Length;
            System.IO.Stream requestStream = Request.GetRequestStream();
            byte[] postBytes = Encoding.ASCII.GetBytes(PostData);
            requestStream.Write(postBytes, 0, postBytes.Length);
            requestStream.Close();

            //'POST query, and display cookies 
            System.Net.HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();
            return Response;
        }
        private String postMsg(String MsgToPost)
        {
            //Stream _stream;
            //CookieContainer CookieJar = new CookieContainer();
            //CookieCollection CookieList = new CookieCollection();
            //String PostData;

            String ConnectURL = PageURL[PageURLIndex];
            //Const ConnectURL = "http://www.pingjockey.net/cgi-bin/pingtalk/"
            //'Const EME1URL = "http://www.chris.org/cgi-bin/jt65emeA"
            myposts.Add(DateTime.UtcNow);
            //'chkposts()

            try
            {

                //'How to display BODY from response stream? 
                //_stream = Response.GetResponseStream();
                HttpWebResponse Response = webrequest(MsgToPost, ConnectURL);

                if (Response.StatusCode == HttpStatusCode.OK)
                {
                    StreamReader responseStream = new StreamReader(Response.GetResponseStream());
                    String responseData = responseStream.ReadToEnd();
                    return responseData;
                }
                else
                {
                    if (Response.StatusCode == HttpStatusCode.Redirect)
                    {
                        String newUrl = Response.GetResponseHeader("Location");
                        PageURL[PageURLIndex] = newUrl;
                        Response = webrequest(MsgToPost, newUrl);
                        if (Response.StatusCode == HttpStatusCode.OK)
                        {
                            StreamReader responseStream = new StreamReader(Response.GetResponseStream());
                            String responseData = responseStream.ReadToEnd();
                            return responseData;
                        }
                    }
                }

            }
            catch(Exception e)
            {
                Console.Write(e.ToString()); 
                //Catch ex As Exception
                //MsgBox(ex.Message.ToString)
            }

            return "";
        }

        private void txtMsgToPost_KeyPress(object sender, KeyPressEventArgs e)
        {
            
        }
        private void MsgToPost()
        {
            String msgToPost = txtMsgToPost.Text.Replace("%", "~Percent~");
            msgToPost = msgToPost.Replace("+", "~Plus~");
            msgToPost = msgToPost.Replace("http://", "~http~");
            msgToPost = msgToPost.Replace(">", "~GT~");
            msgToPost = msgToPost.Replace("<", "~LT~");
            msgToPost = msgToPost.Replace("www", "~w.w.w~");
            txtMsgToPost.Text = "";

            postMsg(msgToPost);
        }
        private void txtMsgToPost_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                MsgToPost();

                //'postMsg(Me.MsgTextBox.Text)
                //RetrievePJPage.refreshtime = 5000*/
                //'PostReponse(postMsg(Me.MsgTextBox.Text))
                //this.MsgTextBox.Text = ""
                //Me.MsgTextBox.Focus()
            }
        }
        // Navigates to the given URL if it is valid.
        private void Navigate(String address)
        {
            if (String.IsNullOrEmpty(address)) return;
            if (address.Equals("about:blank")) return;
            if (!address.StartsWith("http://") &&
                !address.StartsWith("https://"))
            {
                address = "http://" + address;
            }
            try
            {
                //webBrowser1.Navigate(new Uri(address));
            }
            catch (System.UriFormatException)
            {
                return;
            }
        }

        private void PJTextBox_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            Process.Start(e.LinkText);
        }

        private void highlitePosts(string selectedcall, Color npjColor)
        {
            if (selectedcall != myInfo.callsign)
            {

           
                
                if (highlited_callsign != selectedcall)  
                {
                    // un highlite old callsign posts first.
                    foreach (DataGridViewRow r in dataGridViewPJResp.Rows)
                    {

                        if (r.Cells[2].Value != null)
                        {
                            sethighlite(false, r, highlited_callsign, highlited_color);
                        }
                    }
                
                    // look for anyposts from this call and highlite.
                    foreach (DataGridViewRow r in dataGridViewPJResp.Rows)
                    {
                
                        if (r.Cells[2].Value != null) {
                            sethighlite(true, r, selectedcall, npjColor);
                        }
                    }
                    highlited_callsign = selectedcall;
                    highlited_color = npjColor;
                }
                else
                {
                    foreach (DataGridViewRow r in dataGridViewPJResp.Rows)
                    {

                        if (r.Cells[2].Value != null)
                        {
                            sethighlite(false, r, highlited_callsign, highlited_color);
                        }
                    }
                    highlited_callsign = "";

                }
                // save the color and callsign so we can put them back if another row in the StationsList is clicked.
                //highlited_callsign = selectedcall;
                //highlited_color = npjColor;
            }
        }
        private void sethighlite(bool highlite,DataGridViewRow r, string selectedcall, Color npjColor)
        {
            if (r.Cells[2].Value != null)
            {
                if (r.Cells[2].Value.ToString() == selectedcall)
                {
                    DataGridView pjr = dataGridViewPJResp;
                    Color thisbackground = defaultBackColor;
                    Color thisforeground = npjColor;
                    if (highlite)
                    {
                        thisbackground = Color.Yellow;
                        thisforeground = Color.Black;
                    }
                    int rowIndex = r.Index;
                    
                    string cellvalue = r.Cells[1].Value.ToString();
                    pjr.EditDGVCell(pjr.Rows[rowIndex].Cells[1], cellvalue, thisforeground, thisbackground);
                }
            }
        }

        private void StationsList_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if(e.RowIndex > -1)
            {
                selectedCallsign = StationsList.Rows[e.RowIndex].Cells[0].Value.ToString();
                selectedSet(StationsList);
                
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            N5TMScrapper.MySettings mySettings = new MySettings();
            mySettings.myInfo = myInfo;
            mySettings.RetInfo += new MySettings.MySettingsEventHandler(retMyInfo);
            mySettings.Show();
        }

        private void retMyInfo(PJCPlus.info newInfo)
        {
            myInfo = newInfo;
            SaveSettings();
        }

        private void StationsList_SortCompare(object sender, DataGridViewSortCompareEventArgs e)
        {
            if(StationsList.Columns[e.Column.Index].ValueType == typeof(int))  // for distance need to sort numeric.
            {
                e.SortResult = int.Parse(e.CellValue1.ToString()).CompareTo(int.Parse(e.CellValue2.ToString()));
                e.Handled = true;//pass by the default sorting
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            dataGridViewPJResp.Columns[1].Width = dataGridViewPJResp.Width 
                - (dataGridViewPJResp.Columns[0].Width + dataGridViewPJResp.Columns[2].Width) -20;
        }

        private void btnName_Click(object sender, EventArgs e)
        {
            txtMsgToPost.Text = btnCallsign.Text + " " + btnName.Text + ", ";
        }

        private void btnCallsign_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(btnCallsign.Text);
        }

        private void btnGrid_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(btnGrid.Text);
        }

        private void pictureBoxFTP_Click(object sender, EventArgs e)
        {
            pictureBoxFTP.Image = Clipboard.GetImage();
        }

        private void selectedSet(DataGridView dgv)
        {
            btnCallsign.Text = selectedCallsign;

            Color nickColor = Color.Black;
            foreach (PJCPlus.PJ n in nick_unique)
            {
                if (n.callsign == selectedCallsign)
                {
                    nickColor = n.nick_color;
                    btnGrid.Text = n.locator;
                    btnName.Text = n.firstname;
                    lblAz.Text = n.azmuth.ToString();
                    lbldist.Text = n.distance.ToString();
                    lblST.Text = n.state;
                    n.highlite = n.highlite ^ true;
                }
            }
            highlitePosts(selectedCallsign, nickColor);
        }

        private void dataGridViewPJResp_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex > -1)
            {
                // is this a url post?
                if (dataGridViewPJResp.CurrentCell.Value.ToString().Contains("http:"))
                {
                    System.Diagnostics.Process.Start("" + dataGridViewPJResp.CurrentCell.EditedFormattedValue);
                }
                else
                {
                    selectedCallsign = dataGridViewPJResp.Rows[e.RowIndex].Cells[2].Value.ToString();
                    selectedSet(dataGridViewPJResp);
                }
                
            }
        }

        private void dataGridViewPJResp_MouseDown(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Right:
                    {
                        rightClickMenuStrip.Show(this, new Point(e.X, e.Y));//places the menu at the pointer position
                        text2edit = dataGridViewPJResp.CurrentRow.Cells[1].Value.ToString();
                        line2edit = dataGridViewPJResp.CurrentRow.Cells[0].Value.ToString();
                        //dataGridViewPJResp.Rows[e.RowIndex].Cells[1].Value.ToString();
                    }
                    break;
            }
        }
        /// <summary>  
        /// This function is use to get hyperlink style .  
        /// </summary>  
        /// <returns></returns>  
        private DataGridViewCellStyle GetHyperLinkStyleForGridCell()
        {
            // Set the Font and Uderline into the Content of the grid cell .  
            {
                DataGridViewCellStyle l_objDGVCS = new DataGridViewCellStyle();
                System.Drawing.Font l_objFont = new System.Drawing.Font(FontFamily.GenericSansSerif, 8, FontStyle.Underline);
                l_objDGVCS.Font = l_objFont;
                l_objDGVCS.ForeColor = Color.Blue;
                return l_objDGVCS;
            }
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtMsgToPost.Text = "~edit|" + line2edit + "~" + text2edit;

        }

        private void cboWebPages_SelectedIndexChanged(object sender, EventArgs e)
        {
            PageURLIndex = cboWebPages.SelectedIndex;
            RetrievePJPage.url = PageURL[PageURLIndex];
            SplitChar = SplitChars[PageURLIndex];
            refreshPJResp();
        }
        private void ClearStringArrys()
        {
            Array.Clear(post_time, 0, post_time.Length);
            Array.Clear(post_text, 0, post_text.Length);
            Array.Clear(post_nick, 0, post_nick.Length);
            Array.Clear(post_email, 0, post_email.Length);
            Array.Clear(old_postTime, 0, old_postTime.Length);
        }
        
        private void refreshPJResp()
        {
            if(dataGridViewPJResp.Rows.Count > 1)
            {
                dataGridViewPJResp.Rows.Clear();
                dataGridViewPJResp.Refresh();
                RefreshPage = true;
                ClearStringArrys();
                nick_unique.Clear();
                StationsList.Rows.Clear();
                StationsList.Rows.Add();
                FillStationsList();
            }
            
        }
        private void reFillStationsList()
        {
            nick_unique.Clear();
            StationsList.Rows.Clear();
            StationsList.Rows.Add();
            FillStationsList();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            MsgToPost();
        }

        private void btnPostCQ_Click(object sender, EventArgs e)
        {
            if(isnumeric(txtQRG.Text))
            {
                string seqmsg = "1st";
                if (rdoBtnCQ2nd.Checked) seqmsg = "2nd";
                postMsg("~ CQ " + seqmsg + " ~ " + txtQRG.Text + " ~" + txtQRGSplit.Text + " " + txtCQAz.Text + " " + txtCQMode.Text);
            }
            else
            {
                MessageBox.Show("Please Enter QRG MHz");
            }
            
        }
        private bool isnumeric(string numString)
        {
            decimal number1 = 0;
            bool canConvert = decimal.TryParse(numString, out number1);
            
            if (canConvert == true)
                return true;
            else
                return false;
        }

        private void btnClearCQ_Click(object sender, EventArgs e)
        {
            if (isnumeric(txtQRG.Text))
            {
                postMsg("~ Clear ~ " + txtQRG.Text + " ~ CQ Stopped");
            }
            else
            {
                MessageBox.Show("Please Enter Frequency (MHz)");
            }
                   
        }

        private void dataGridViewPJResp_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.ColumnIndex == 1) 
                {
                   if(e.RowIndex >= 0) if(dataGridViewPJResp.Rows[e.RowIndex].Cells[1].Value.ToString().Contains("http")) dataGridViewPJResp.Cursor = Cursors.Hand;
                }
                else
                {
                    dataGridViewPJResp.Cursor = Cursors.Default;
                }
            }
            catch (Exception)
            {

                
            }
            
        }

        private void hrToolStripMenuItem_Click(object sender, EventArgs e)
        {
            clearHistoryChecks();
            hrToolStripMenuItem.Checked = true;
            History = 3600;
            SaveSettings();
            reFillStationsList();
        }

        private void clearHistoryChecks()
        {
            hrToolStripMenuItem.Checked = false;
            hrs2ToolStripMenuItem.Checked = false;
            hrs6ToolStripMenuItem.Checked = false;
            allHistoryToolStripMenuItem.Checked = false;
        }

        private void hrs2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            clearHistoryChecks();
            hrs2ToolStripMenuItem.Checked = true;
            History = 7200;
            SaveSettings();
            reFillStationsList();
        }

        private void hrs6ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            clearHistoryChecks();
            hrs6ToolStripMenuItem.Checked = true;
            History = 21600;
            SaveSettings();
            reFillStationsList();
        }

        private void allHistoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            clearHistoryChecks();
            allHistoryToolStripMenuItem.Checked = true;
            History = 0;
            SaveSettings();
            reFillStationsList();
        }
    }

    public static class ExtensionMethods
    {
        public static void InvokeAndClose(this Control self, MethodInvoker func)
        {
            IAsyncResult result = self.BeginInvoke(func);
            self.EndInvoke(result);
            result.AsyncWaitHandle.Close();
            //GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    public static class RichTextBoxExtensions
    {
        public static void AppendText(this RichTextBox box, string text, Color color, Color background)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;

            box.SelectionColor = color;
            box.SelectionBackColor = background;
            box.AppendText(text);
            box.SelectionColor = box.ForeColor;
        }
    }

    public static class DataGridViewExtensions
    {
        public static void EditDGVCell(this DataGridView dgv, DataGridViewCell cell, string text, Color color, Color background)
        {

            cell.Value = text;
            cell.Style.ForeColor = color;
            cell.Style.BackColor = background;
        }
    }
    public static class MyExtensions
    {
        public static IEnumerable<TreeNode> All(this TreeNodeCollection nodes)
        {
            foreach (TreeNode n in nodes)
            {
                yield return n;
                foreach (TreeNode child in n.Nodes.All())
                    yield return child;
            }
        }
    }

}
