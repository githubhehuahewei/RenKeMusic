using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using post;
using System.IO;
using LitJson;
using System.Net;
using Un4seen.Bass;
using System.Threading;

using DevExpress.XtraTreeList.Nodes;
using System.Text.RegularExpressions;
using System.Drawing.Imaging;
using System.Runtime.InteropServices; //引用此名称空间，简化后面的代码
using System.Xml;
using DevExpress.XtraGrid.Menu;
using Microsoft.WindowsAPICodePack.Taskbar;
using Microsoft.WindowsAPICodePack.Shell;
using WfpApp;
using DevExpress.XtraSplashScreen;//添加多线程下载文件的引用

namespace 全网音乐
{
    public partial class Form1 : DevExpress.XtraEditors.XtraForm
    {
        public enum PlayMode
        {
            NetWork = 1,
            Local = 2
        }
        public TaskbarManager windowsTaskbar = TaskbarManager.Instance;


        public int ImageListIndex = -1;
        public int ImageListIndex2 = -1;

        public PlayMode playMode = PlayMode.NetWork;  //1为网络模式，2为本地模式

        public int stream;
        DataTable dataTable = new DataTable();
        Thread thread;//显示歌词的线程
        List<networkList.structSong> list = new List<networkList.structSong>();
        List<localList.structSong> list2 = new List<localList.structSong>();

        //结构体，URL和文件名
        private struct jg
        {
            public string url;
            public string filename;
        }

        public Form1()
        {
            SplashScreenManager.ShowForm(typeof(SplashScreen1));
            InitializeComponent();
            //设置皮肤
            this.defaultLookAndFeel1.LookAndFeel.SkinName = "Caramel";

            BassNet.Registration("52pojie@qq.com", "2X211223140022");
            Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_NET_TIMEOUT, 15000);
            if (Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero) == false)
                MessageBox.Show("初始化失败！" + Bass.BASS_ErrorGetCode().ToString());

            //初始化任务栏进度条
            windowsTaskbar.SetProgressState(TaskbarProgressBarState.Normal, this.Handle);
            windowsTaskbar.SetProgressValue(0, 10000, this.Handle);

            //设置复选框
            gridView1.OptionsSelection.MultiSelect = true;
            gridView1.OptionsSelection.MultiSelectMode = DevExpress.XtraGrid.Views.Grid.GridMultiSelectMode.CheckBoxRowSelect;
            //gridView1.OptionsSelection.ShowCheckBoxSelectorInColumnHeader = DevExpress.Utils.DefaultBoolean.True;

            gridView1.OptionsCustomization.AllowSort = false;//禁用排序
            gridView1.OptionsMenu.EnableColumnMenu = true;//禁用表头菜单
            gridView1.BestFitColumns();//自动设置列宽
            gridView1.OptionsCustomization.AllowColumnResizing = false;//不允许调整列宽

            //屏幕右键菜单
            ContextMenu emptyMenu = new ContextMenu();
            buttonEdit1.Properties.ContextMenu = emptyMenu;
            pictureEdit1.Properties.ContextMenu = emptyMenu;


            this.richTextBox1.BackColor = Color.White;

            //显示新歌榜
            GetBangDan("http://www.kuwo.cn/api/www/bang/bang/musicList?bangId=17&pn=1&rn=200");

            Init(); //初始化频谱的一些信息

            LoadSongList();//加载播放列表
            //枚举皮肤
            foreach (DevExpress.Skins.SkinContainer skin in DevExpress.Skins.SkinManager.Default.Skins)
            {
                comboBoxEdit1.Properties.Items.Add(skin.SkinName);

            }



            SplashScreenManager.CloseForm(true);
        }


        //My自己的Get方法
        public static string HttpGetSearch(string strSearchText)
        {
            string strURL = "http://www.kuwo.cn/api/www/search/searchMusicBykeyWord?key=" + BianMa(strSearchText) + "&pn=1&rn=50";
            HttpHelper http = new HttpHelper();
            HttpItem item = new HttpItem()
            {
                URL = strURL,//URL这里都是测试     必需项
                Method = "get",//URL     可选项 默认为Get
                //Allowautoredirect = true,
                //ContentType = "application/x-www-form-urlencoded",//返回类型    可选项有默认值
                //Accept = "application/json, text/plain, */*",
                //Referer = "http://www.kuwo.cn/search/list?key=" + BianMa(strSearchText),                
                UserAgent = "Mozilla/5.0 (Windows NT 6.2) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.132 Safari/537.36",

            };
            //得到HTML代码
            HttpResult result = http.GetHtml(item);
            return result.Html;
        }

        //My自己的Get方法
        public static string HttpGet(string strURL)
        {
            HttpHelper http = new HttpHelper();
            HttpItem item = new HttpItem()
            {
                URL = strURL,//URL这里都是测试     必需项
                Method = "get",//URL     可选项 默认为Get
                Allowautoredirect = true,
                ContentType = "application/x-www-form-urlencoded",//返回类型    可选项有默认值
                UserAgent = "Mozilla/5.0 (Windows NT 6.2) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.132 Safari/537.36",
            };
            //得到HTML代码
            HttpResult result = http.GetHtml(item);
            return result.Html;
        }

        //下载MP3
        private void DownLoadMp3(networkList.structSong ss)
        {

            Thread t = new Thread(new ParameterizedThreadStart(HttpDownloadFile));
            jg jg;
            jg.filename = @"Mp3\" + ss.SongName + ".mp3";
            jg.url = ss.SongAddress;
            t.IsBackground = true;
            t.Start(jg);

        }



        //文本框 
        private void buttonEdit1_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            if (e.Button.Kind == DevExpress.XtraEditors.Controls.ButtonPredefines.Close)
            {
                buttonEdit1.Text = "";
            }
            //搜索并解析
            else if (e.Button.Kind == DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph)
            {
                if (buttonEdit1.Text == "")
                {
                    MessageBox.Show("你没有输入任何关键字！");
                    return;
                }
                SerachSong(buttonEdit1.Text);
            }
        }

        //搜索歌曲
        private void SerachSong(string SearchText)
        {
            gridView1.Columns.Clear();
            gridControl1.DataSource = null;
            dataTable.Columns.Clear();
            dataTable.Clear();

            DataColumn num = new DataColumn("序号", Type.GetType("System.String"));
            DataColumn id = new DataColumn("歌曲ID", Type.GetType("System.String"));
            DataColumn name = new DataColumn("歌曲名称", Type.GetType("System.String"));
            DataColumn singer = new DataColumn("歌手", Type.GetType("System.String"));
            DataColumn pic = new DataColumn("专辑", Type.GetType("System.String"));
            //DataColumn img = new DataColumn("状态", Type.GetType("System.String"));
            //DataColumn download = new DataColumn("下载", Type.GetType("System.String"));
            //Image image = Image.FromFile("play.png");
            //byte[] imgBytes = ImageToBytes(image);
            dataTable.Columns.Add(num);
            dataTable.Columns.Add(id);
            dataTable.Columns.Add(name);
            dataTable.Columns.Add(singer);
            dataTable.Columns.Add(pic);
            //dataTable.Columns.Add(img);
            //dataTable.Columns.Add(download);


            string strHtml = HttpGetSearch(SearchText);
            JsonData jsonData = JsonMapper.ToObject(strHtml);
            JsonData data = jsonData[2];
            JsonData jsonList = data[1];
            int intNum = 0;
            foreach (JsonData temp in jsonList)
            {
                intNum++;
                DataRow dr = dataTable.NewRow();
                dr["序号"] = intNum.ToString();
                dr["歌曲ID"] = temp["musicrid"].ToString().Substring(6);
                dr["歌曲名称"] = temp["name"].ToString();
                dr["歌手"] = temp["artist"].ToString();
                dr["专辑"] = temp["album"].ToString();
                //dr["状态"] = "1";
                //dr["下载"] = "1";
                dataTable.Rows.Add(dr);
            }
            gridControl1.DataSource = dataTable;
            gridView1.Columns["序号"].MaxWidth = 60;
            //序号列文字居中
            gridView1.Columns["序号"].AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            gridView1.Columns["歌曲ID"].Visible = false;
        }

        private void DownLoadFile(string URL, string FileName = "DownLoad.mp3")
        {

            HttpHelper http = new HttpHelper();
            HttpItem item = new HttpItem()
            {
                URL = URL,//URL这里都是测试     必需项
                Method = "GET",//URL     可选项 默认为Get               
                Allowautoredirect = true,
                ContentType = "application/x-www-form-urlencoded",//返回类型
                ResultType = ResultType.Byte,
            };
            //得到HTML代码
            HttpResult result = http.GetHtml(item);
            //得到新的HTML代码
            byte[] buf = result.ResultByte;//GetBuffer();//我得到的比特数组

            FileStream fs = new FileStream(FileName, FileMode.Create, FileAccess.Write);
            fs.Write(buf, 0, buf.Length);
            fs.Flush();
            fs.Close();
            string html = result.Html;

        }


        private void DownJson(string URL)
        {
            HttpHelper http = new HttpHelper();
            HttpItem item = new HttpItem()
            {
                URL = URL,//URL这里都是测试     必需项
                Method = "GET",//URL     可选项 默认为Get               
                Allowautoredirect = true,
                ContentType = "application/x-www-form-urlencoded",//返回类型
                ResultType = ResultType.Byte,
            };
            //得到HTML代码
            HttpResult result = http.GetHtml(item);
            //得到新的HTML代码
            byte[] buf = result.ResultByte;//GetBuffer();//我得到的比特数组

            FileStream fs = new FileStream("aaa.json", FileMode.Create, FileAccess.Write);
            fs.Write(buf, 0, buf.Length);
            fs.Flush();
            fs.Close();
            string html = result.Html;
        }



        private void Form1_Load(object sender, EventArgs e)
        {

            


        }


        private string GetSongAddress(string URL)
        {

            string redirectUrl = "否";
            try
            {
                WebRequest myRequest = WebRequest.Create(URL);
                WebResponse myResponse = myRequest.GetResponse();
                redirectUrl = myResponse.ResponseUri.ToString();
                myResponse.Close();
            }
            catch
            {
            }
            return redirectUrl;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Bass.BASS_ChannelStop(stream);
            Bass.BASS_StreamFree(stream);
            Bass.BASS_Stop();
            Bass.BASS_Free();
        }




        private void timer1_Tick(object sender, EventArgs e)
        {
            long pos = Bass.BASS_ChannelGetPosition(stream);
            double PosTime = Bass.BASS_ChannelBytes2Seconds(stream, pos);
            trackBarControl1.Value = Convert.ToInt32(PosTime);
            //显示当前时间
            this.labelControl2.Text = GetTime(Convert.ToInt32(PosTime));

            if (Bass.BASS_ChannelIsActive(stream) == BASSActive.BASS_ACTIVE_STOPPED)
            {
                if (playMode == PlayMode.NetWork)
                {
                    if (ImageListIndex < (imageListBoxControl1.ItemCount - 1))
                    {
                        networkList.structSong ss = new networkList.structSong();
                        ss.SongID = list[ImageListIndex + 1].SongID;
                        ss.SongName = list[ImageListIndex + 1].SongName;
                        ss.SongAddress = GetMp3Address(ss.SongID);
                        PlayBangDan(ss);
                    }
                    else
                    {
                        timer1.Stop();
                        timer2.Stop();
                        pictureEdit1.Image = Image.FromFile(@"Image\" + "bj.jpg");
                        pictureEdit4.Image = Image.FromFile(@"Image\" + "播放.png");

                    }
                }
                else
                {
                    if (ImageListIndex2 < (imageListBoxControl2.ItemCount - 1))
                    {
                        imageListBoxControl2.Items[ImageListIndex2].ImageIndex = 3;
                        ImageListIndex2++;
                        imageListBoxControl2.Items[ImageListIndex2].ImageIndex = 2;
                        imageListBoxControl2.SelectedIndex = ImageListIndex2;
                        localList.structSong ss = new localList.structSong();

                        ss.SongName = list2[ImageListIndex2].SongName;
                        ss.SongAddress = list2[ImageListIndex2].SongAddress;

                        PlayBangDan2(ss);
                    }
                    else
                    {
                        timer1.Stop();
                        timer2.Stop();
                        pictureEdit1.Image = Image.FromFile(@"Image\" + "bj.jpg");
                        pictureEdit4.Image = Image.FromFile(@"Image\" + "播放.png");

                    }
                }
            }
        }




        private void DisplayLrc(object obj)
        {

            MyLrc lrc = (MyLrc)obj;
            setColor(lrc);
            int time = 0;

            while (true)
            {
                double PosTime = Bass.BASS_ChannelBytes2Seconds(stream, Bass.BASS_ChannelGetPosition(stream));
                time = Convert.ToInt32(PosTime * 1000);
                int mark = lrc.list.Count;
                for (int i = lrc.list.Count - 1; i >= 0; i--)
                {
                    int m = lrc.list[i].time;
                    if (time >= m)
                    {
                        if (lrc.mark != i)
                        {

                            lrc.mark = i;
                            setColor(lrc);
                            if (lrc.mark > 5)
                            {
                                if (this.richTextBox1.InvokeRequired)
                                {
                                    Action MyAction = () =>
                                    {
                                        richTextBox1.Select(lrc.listTime[lrc.mark - 5], lrc.listTime[lrc.mark - 5]);
                                        richTextBox1.ScrollToCaret();
                                    };
                                    this.richTextBox1.Invoke(MyAction);
                                }
                                else
                                {
                                    richTextBox1.Select(lrc.listTime[lrc.mark - 5], lrc.listTime[lrc.mark - 5]);
                                    richTextBox1.ScrollToCaret();
                                }

                            }

                            if (lrc.mark > lrc.list.Count)
                                return;
                        }
                        break;
                    }
                }

                Thread.Sleep(40);
            }

        }


        //根据行数设置颜色
        private void setColor(MyLrc lrc)
        {
            if (this.richTextBox1.InvokeRequired)
            {
                Action actionDelegate = () =>
                {
                    richTextBox1.Select(0, richTextBox1.TextLength);
                    richTextBox1.SelectionColor = Color.Black;
                    richTextBox1.SelectionFont = new Font("tahoma", 15);// new Font(FontFamily.GenericMonospace, 14, FontStyle.Regular);         

                    int mark = lrc.mark;

                    richTextBox1.Select(lrc.listTime[mark], lrc.listTime[mark + 1] - lrc.listTime[mark]);
                    richTextBox1.SelectionColor = Color.Red;
                    richTextBox1.SelectionFont = new Font("tahoma", 22); //new Font(FontFamily.GenericMonospace, 20, FontStyle.Regular);

                };
                this.richTextBox1.Invoke(actionDelegate);
            }
            else
            {
                richTextBox1.Select(0, richTextBox1.TextLength);
                richTextBox1.SelectionColor = Color.Black;
                richTextBox1.SelectionFont = new Font("tahoma", 15);// new Font(FontFamily.GenericMonospace, 14, FontStyle.Regular);         

                int mark = lrc.mark;

                richTextBox1.Select(lrc.listTime[mark], lrc.listTime[mark + 1] - lrc.listTime[mark]);
                richTextBox1.SelectionColor = Color.Red;
                richTextBox1.SelectionFont = new Font("tahoma", 22); //new Font(FontFamily.GenericMonospace, 20, FontStyle.Regular);
            }

        }


        //拖动滑块更改播放时间
        private void trackBarControl1_MouseClick(object sender, MouseEventArgs e)
        {

            int pos = trackBarControl1.Value;
            Bass.BASS_ChannelSetPosition(stream, Convert.ToDouble(pos));


        }

        private void treeList1_FocusedNodeChanged(object sender, DevExpress.XtraTreeList.FocusedNodeChangedEventArgs e)
        {
            TreeListNode curNode = e.Node;
            if (curNode == null) return;
            MessageBox.Show(curNode.GetValue("歌曲排行榜").ToString());

        }


        //截取中间字符串
        public static string SubString(string text, string start, string end)
        {
            Regex rg = new Regex("(?<=(" + start + "))[.\\s\\S]*?(?=(" + end + "))", RegexOptions.Multiline | RegexOptions.Singleline);
            string NameText = rg.Match(text).Value;
            return NameText;
        }



        private string GetMp3Address(string SongID)
        {
            string strAddress = "http://www.kuwo.cn/url?format=mp3&rid=" + SongID + "&response=url&type=convert_url3&br=320kmp3&from=web&t=1560298230797";
            string strHtml = HttpGet(strAddress);
            return SubString(strHtml, @"url"": """, @"""");
        }

        private networkList.structSong GetGridSongInfo()
        {
            networkList.structSong ss = new networkList.structSong();
            DataRow myDataRow = gridView1.GetDataRow(gridView1.FocusedRowHandle);
            ss.SongName = myDataRow["歌曲名称"].ToString();
            ss.SongID = myDataRow["歌曲ID"].ToString();
            ss.SongAddress = GetMp3Address(ss.SongID);
            return ss;
        }

        private bool PlaySong(networkList.structSong ss)
        {

            Bass.BASS_ChannelStop(stream);
            Bass.BASS_StreamFree(stream); //释放音频流            
            stream = Bass.BASS_StreamCreateURL(ss.SongAddress, 0, BASSFlag.BASS_DEFAULT, null, IntPtr.Zero);
            if (stream != 0)
            {
                Bass.BASS_ChannelPlay(stream, true);
                long l = Bass.BASS_ChannelGetLength(stream);
                double TotalTime = Bass.BASS_ChannelBytes2Seconds(stream, Bass.BASS_ChannelGetLength(stream));
                trackBarControl1.Properties.Maximum = Convert.ToInt32(TotalTime);
                this.labelControl3.Text = GetTime(Convert.ToInt32(TotalTime));
                timer1.Start();
                timer2.Start();

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool PlaySong2(localList.structSong ss)
        {

            Bass.BASS_ChannelStop(stream);
            Bass.BASS_StreamFree(stream); //释放音频流            
            stream = Bass.BASS_StreamCreateFile(ss.SongAddress, 0, 0, BASSFlag.BASS_DEFAULT);
            if (stream != 0)
            {
                Bass.BASS_ChannelPlay(stream, true);
                long l = Bass.BASS_ChannelGetLength(stream);
                double TotalTime = Bass.BASS_ChannelBytes2Seconds(stream, Bass.BASS_ChannelGetLength(stream));
                trackBarControl1.Properties.Maximum = Convert.ToInt32(TotalTime);
                this.labelControl3.Text = GetTime(Convert.ToInt32(TotalTime));
                timer1.Start();
                timer2.Start();

                return true;
            }
            else
            {
                return false;
            }
        }

        //下载歌词
        private bool DownloadLrc(networkList.structSong ss)
        {



            richTextBox1.Clear();

            string strLrc = HttpGet("http://m.kuwo.cn/newh5/singles/songinfoandlrc?musicId=" + ss.SongID);

            MyLrc myLrc = new MyLrc(strLrc);
            if (myLrc.list.Count == 0)
            {
                richTextBox1.Text = "未获取到歌词！";
                return false;
            }
            if (thread != null)
                thread.Abort();
            thread = new Thread(new ParameterizedThreadStart(DisplayLrc));
            thread.IsBackground = true;

            richTextBox1.Text = myLrc.GetLrc + "\r\n\r\n\r\n\r\n\r\n";
            thread.Start(myLrc);
            return true;
            //string strLrc = HttpGet("https://api.itooi.cn/music/kuwo/lrc?key=579621905&id=" + ss.SongID);
            //if (strLrc.IndexOf(@"""ERROR""") > 0)
            //{
            //    richTextBox1.Text = "未获取到歌词！";
            //    return false;
            //}

            ////File.WriteAllText(ss.SongName + ".lrc", strLrc);
            //if (thread != null)
            //    thread.Abort();
            //thread = new Thread(new ParameterizedThreadStart(DisplayLrc));
            //thread.IsBackground = true;
            //Lrc lrc = new Lrc();
            ////lrc.Lyrics(ss.SongName + ".lrc");
            //lrc.Lyrics(strLrc);
            //richTextBox1.Text = lrc.GetLrc + "\r\n\r\n\r\n\r\n\r\n";
            //thread.Start(lrc);
            //return true;
        }
        //保存歌曲信息到列表
        private void SaveSongInfo(networkList.structSong ss)
        {
            string SongID, SongName;
            SongID = ss.SongID;
            SongName = ss.SongName;

            //写入xml文件和写入List列表中
            if (networkList.WriteXmlList(SongID, SongName))
            {
                if (ImageListIndex != -1)
                    imageListBoxControl1.Items[ImageListIndex].ImageIndex = 3;
                ImageListIndex = imageListBoxControl1.Items.Count;
                imageListBoxControl1.Items.Add(SongName, 2);
                imageListBoxControl1.SelectedIndex = ImageListIndex;

                networkList.structSong ssTemp = new networkList.structSong();
                ssTemp.SongID = SongID;
                ssTemp.SongName = SongName;
                list.Add(ssTemp);
            }
            else
            {

                int intTemp = list.FindIndex(item => item.SongID == SongID);
                imageListBoxControl1.SelectedIndex = intTemp;
                if (ImageListIndex != -1)
                    imageListBoxControl1.Items[ImageListIndex].ImageIndex = 3;
                imageListBoxControl1.Items[intTemp].ImageIndex = 2;
                ImageListIndex = intTemp;
            }
        }

        //播放榜单歌曲
        private void PlayBangDan(networkList.structSong SongGridInfo)
        {
            //切换到网络播放模式的一些信息的更改
            playMode = PlayMode.NetWork;
            if (ImageListIndex2 != -1)
                imageListBoxControl2.Items[ImageListIndex2].ImageIndex = 3;
            ImageListIndex2 = -1;


            bool isSuccess = PlaySong(SongGridInfo);
            pictureEdit1.Image = null;
            if (isSuccess)
            {
                DownloadLrc(SongGridInfo);
            }

            SaveSongInfo(SongGridInfo);


        }
        //播放榜单歌曲
        private void PlayBangDan2(localList.structSong SongGridInfo)
        {
            //切换到本地模式的一些信息的更改
            playMode = PlayMode.Local;
            if (ImageListIndex != -1)
                imageListBoxControl1.Items[ImageListIndex].ImageIndex = 3;
            ImageListIndex = -1;



            bool isSuccess = PlaySong2(SongGridInfo);
            pictureEdit1.Image = null;

        }
        public void HttpDownloadFile(object obj)
        {
            jg jg = (jg)obj;
            string path = jg.filename;
            string url = jg.url;
            // 设置参数
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            //发送请求并获取相应回应数据
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            //直到request.GetResponse()程序才开始向目标网页发送Post请求
            Stream responseStream = response.GetResponseStream();
            //创建本地文件写入流
            Stream stream = new FileStream(path, FileMode.Create);
            long sum = response.ContentLength;//获取文件的大小
            int dq = 0;
            float jd = 0;
            byte[] bArr = new byte[1024];
            int size = responseStream.Read(bArr, 0, (int)bArr.Length);
            while (size > 0)
            {
                stream.Write(bArr, 0, size);
                dq = dq + size;
                jd = (float)dq / sum * 100;
                if (this.InvokeRequired)
                {
                    Action<int> actionDelegate = (x) => { this.barEditItem3.EditValue = x; windowsTaskbar.SetProgressValue((int)(jd * 100), 10000, this.Handle); };
                    this.Invoke(actionDelegate, (int)(jd * 100));


                }
                else
                {
                    barEditItem3.EditValue = (int)(jd * 100);
                    windowsTaskbar.SetProgressValue((int)(jd * 100), 10000, this.Handle);
                }

                size = responseStream.Read(bArr, 0, (int)bArr.Length);
            }
            stream.Close();
            responseStream.Close();

        }


        //图片转字节

        public static byte[] ImageToBytes(Image image)
        {
            ImageFormat format = image.RawFormat;
            using (MemoryStream ms = new MemoryStream())
            {
                if (format.Equals(ImageFormat.Jpeg))
                {
                    image.Save(ms, ImageFormat.Jpeg);
                }
                else if (format.Equals(ImageFormat.Png))
                {
                    image.Save(ms, ImageFormat.Png);
                }
                else if (format.Equals(ImageFormat.Bmp))
                {
                    image.Save(ms, ImageFormat.Bmp);
                }
                else if (format.Equals(ImageFormat.Gif))
                {
                    image.Save(ms, ImageFormat.Gif);
                }
                else if (format.Equals(ImageFormat.Icon))
                {
                    image.Save(ms, ImageFormat.Icon);
                }
                byte[] buffer = new byte[ms.Length];
                //Image.Save()会改变MemoryStream的Position，需要重新Seek到Begin
                ms.Seek(0, SeekOrigin.Begin);
                ms.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        private void gridView1_CustomDrawCell(object sender, DevExpress.XtraGrid.Views.Base.RowCellCustomDrawEventArgs e)
        {

            //if (e.Column.FieldName == "状态")
            //{
            //    int a = Convert.ToInt32(gridView1.GetRowCellValue(e.RowHandle, "状态").ToString());
            //    Image imgPlay = Image.FromFile("play.png");
            //    Image imgStop = Image.FromFile("stop.png");
            //    Image img = null;

            //    switch (a)
            //    {
            //        case 1:
            //            img = imgPlay;
            //            break;
            //        case 2:
            //            img = imgStop;
            //            break;
            //    }

            //    //Rectangle rect = new Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height);
            //    Rectangle rect = new Rectangle(e.Bounds.X, e.Bounds.Y, 30, 30);
            //    rect.Inflate(-3, -3);
            //    e.Graphics.DrawImage(img, rect);
            //    e.Handled = true;
            //}
            //else if (e.Column.FieldName == "下载")
            //{
            //    Image imgDown = Image.FromFile("download.png");
            //    Rectangle rect = new Rectangle(e.Bounds.X, e.Bounds.Y, 30, 30);
            //    rect.Inflate(-3, -3);
            //    e.Graphics.DrawImage(imgDown, rect);
            //    e.Handled = true;
            //}
        }



        private void gridView1_RowCellClick(object sender, DevExpress.XtraGrid.Views.Grid.RowCellClickEventArgs e)
        {
            if (e.Column.FieldName == "状态")
            {

                int a = Convert.ToInt32(gridView1.GetRowCellValue(e.RowHandle, "状态").ToString());
                if (a == 1)
                    gridView1.SetRowCellValue(e.RowHandle, "状态", 2);
                else
                    gridView1.SetRowCellValue(e.RowHandle, "状态", 1);
            }
        }


        #region 显示频谱要用到的一些变量和结构体

        public double Sqrt(double num)
        {
            return Math.Pow(num, 0.5);
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            UpdateSpectrum(0, 0, 0, 0, 0);
        }



        [DllImport("gdi32.dll", EntryPoint = "SetDIBitsToDevice")]
        public static extern int SetDIBitsToDevice(
            IntPtr hdc,
            int x,
            int y,
            int dx,
            int dy,
            int SrcX,
            int SrcY,
            int Scan,
            int NumScans,
            byte[] Bits,
            ref BITMAPINFO BitsInfo,
            int wUsage
        );


        public struct RGBQUAD
        {
            public byte rgbBlue;
            public byte rgbGreen;
            public byte rgbRed;
            public byte rgbReserved;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFOHEADER
        {
            public int biSize;//=Marshal.SizeOf(typeof(BITMAPINFOHEADER));
            public int biWidth;
            public int biHeight;
            public short biPlanes;
            public short biBitCount;
            public int biCompression;
            public int biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public int biClrUsed;
            public int biClrImportant;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFO
        {
            public BITMAPINFOHEADER bmiHeader;// = new BITMAPINFOHEADER();
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 255, ArraySubType = UnmanagedType.Struct)]
            public RGBQUAD[] bmiColors;//=new RGBQUAD[255];
        }

        BITMAPINFO bh = new BITMAPINFO();
        public byte[] specbuf;
        //public const int SPECWIDTH = 368;
        //public const int SPECHEIGHT = 127;
        public const int SPECWIDTH = 368;
        public const int SPECHEIGHT = 127;
        public const int BI_RGB = 0x0;
        public const int DIB_RGB_COLORS = 0x0;



        public void UpdateSpectrum(int uTimerID, int uMsg, int dwUser, int dw1, int dw2)
        {

            int X, Y, y1 = int.MinValue;

            Single[] fft = new Single[1024];

            specbuf = new byte[SPECWIDTH * (SPECHEIGHT + 1)];
            Bass.BASS_ChannelGetData(stream, fft, Convert.ToInt32(BASSData.BASS_DATA_FFT2048));

            for (X = 0; X <= (SPECWIDTH / 2); X++)
            {
                Y = Convert.ToInt32(Sqrt(fft[X + 1]) * 3 * SPECHEIGHT - 4);
                if (Y > SPECHEIGHT)
                    Y = SPECHEIGHT;
                if (X > 0)
                {

                    y1 = (Y + y1) / 2;
                    y1 = y1 - 1;
                    while (y1 >= 0)
                    {
                        specbuf[y1 * SPECWIDTH + X * 2 - 1] = Convert.ToByte(y1 + 1);
                        y1 = y1 - 1;
                    }

                }
                y1 = Y;
                Y = Y - 1;
                while (Y >= 0)
                {
                    specbuf[Y * SPECWIDTH + X * 2] = Convert.ToByte(Y + 1);
                    Y = Y - 1;
                }
            }

            Graphics g = this.pictureEdit1.CreateGraphics();
            SetDIBitsToDevice(g.GetHdc(), 0, 0, SPECWIDTH, SPECHEIGHT, 0, 0, 0, SPECHEIGHT, specbuf, ref bh, 0);

        }


        public void Init()
        {
            bh.bmiHeader = new BITMAPINFOHEADER();
            RGBQUAD[] r = new RGBQUAD[255];
            bh.bmiColors = r;
            bh.bmiHeader.biBitCount = 8;
            bh.bmiHeader.biPlanes = 1;
            bh.bmiHeader.biSize = Marshal.SizeOf(typeof(BITMAPINFOHEADER));
            bh.bmiHeader.biWidth = SPECWIDTH;
            bh.bmiHeader.biHeight = SPECHEIGHT;
            bh.bmiHeader.biClrUsed = 256;
            bh.bmiHeader.biClrImportant = 256;
            byte a;

            for (a = 1; a < 128; a++)
            {

                bh.bmiColors[a].rgbGreen = Convert.ToByte(256 - 2 * a);
                bh.bmiColors[a].rgbRed = Convert.ToByte(2 * a);

            }
            for (a = 0; a < 31; a++)
            {

                bh.bmiColors[128 + a].rgbBlue = Convert.ToByte(8 * a);
                bh.bmiColors[128 + 32 + a].rgbBlue = 255;
                bh.bmiColors[128 + 32 + a].rgbRed = Convert.ToByte(8 * a);
                bh.bmiColors[128 + 64 + a].rgbRed = 255;
                bh.bmiColors[128 + 64 + a].rgbBlue = Convert.ToByte(8 * (31 - a));
                bh.bmiColors[128 + 64 + a].rgbGreen = Convert.ToByte(8 * a);
                bh.bmiColors[128 + 96 + a].rgbRed = 255;
                bh.bmiColors[128 + 96 + a].rgbGreen = 255;
                bh.bmiColors[128 + 96 + a].rgbBlue = Convert.ToByte(8 * a);
            }
        }
        #endregion

        //抖音榜
        private void barLargeButtonItem5_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            GetBangDan("http://www.kuwo.cn/api/www/bang/bang/musicList?bangId=158&pn=1&rn=200 ");
        }



        private void imageListBoxControl1_DoubleClick(object sender, EventArgs e)
        {


            if (ImageListIndex != -1)
                imageListBoxControl1.Items[ImageListIndex].ImageIndex = 3;

            ImageListIndex = imageListBoxControl1.SelectedIndex;
            imageListBoxControl1.Items[imageListBoxControl1.SelectedIndex].ImageIndex = 2;

            networkList.structSong ss = new networkList.structSong();
            ss.SongID = list[ImageListIndex].SongID;
            ss.SongName = list[ImageListIndex].SongName;
            ss.SongAddress = GetMp3Address(ss.SongID);

            PlayBangDan(ss);
            pictureEdit4.Image = Image.FromFile(@"Image\" + "暂停.png");
        }

        private void imageListBoxControl1_DrawItem(object sender, DevExpress.XtraEditors.ListBoxDrawItemEventArgs e)
        {
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                //e.Appearance.BackColor = Color.Red;

                // e.Appearance.ForeColor = Color.Red;
                if (e.Index == ImageListIndex)
                {
                    //Image img = Image.FromFile("listback.jpg");
                    //Rectangle rect = e.Bounds;
                    //e.Graphics.DrawImage(img, rect);
                    e.Appearance.ForeColor = Color.Red;
                    Font font = new Font("宋体", 18);
                    e.Appearance.Font = font;
                }
            }

            if ((e.State & DrawItemState.None) == DrawItemState.None)
            {

                if (e.Index == ImageListIndex)
                {
                    //Image img = Image.FromFile("listback.jpg");
                    //Rectangle rect = e.Bounds;
                    //e.Graphics.DrawImage(img, rect);
                    e.Appearance.ForeColor = Color.Red;
                    Font font = new Font("宋体", 18);
                    e.Appearance.Font = font;
                }
            }
        }


        private void LoadSongList()
        {

            list = networkList.ReadXmlList();
            foreach (networkList.structSong song in list)
            {
                imageListBoxControl1.Items.Add(song.SongName, 3);

            }
            list2 = localList.ReadXmlList();
            foreach (localList.structSong song in list2)
            {
                imageListBoxControl2.Items.Add(song.SongName, 3);

            }
        }

        private void imageListBoxControl1_MouseUp(object sender, MouseEventArgs e)
        {
            DevExpress.XtraEditors.ImageListBoxControl ILB = sender as DevExpress.XtraEditors.ImageListBoxControl;
            if (e.Button == MouseButtons.Right && ModifierKeys == Keys.None)
            {
                Point p = new Point(Cursor.Position.X, Cursor.Position.Y);
                //int intHot = ILB.HotItemIndex;
                int intHot = ILB.SelectedIndex;
                if (intHot >= 0)
                    popupMenu1.ShowPopup(p);
            }
        }
        //清空网络列表
        private void barLargeButtonItem13_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            networkList.DeleteAllList();
            imageListBoxControl1.Items.Clear();
            list.Clear();
            ImageListIndex = -1;
        }
        //网络列表播放菜单
        private void barLargeButtonItem10_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (ImageListIndex != -1)
                imageListBoxControl1.Items[ImageListIndex].ImageIndex = 3;

            ImageListIndex = imageListBoxControl1.SelectedIndex;
            imageListBoxControl1.Items[imageListBoxControl1.SelectedIndex].ImageIndex = 2;

            networkList.structSong ss = new networkList.structSong();
            ss.SongID = list[ImageListIndex].SongID;
            ss.SongName = list[ImageListIndex].SongName;
            ss.SongAddress = GetMp3Address(ss.SongID);

            PlayBangDan(ss);
        }
        //删除一项菜单
        private void barLargeButtonItem12_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            int intIndex = imageListBoxControl1.SelectedIndex;
            networkList.DeleteItem(list[intIndex].SongID);
            list.RemoveAt(intIndex);
            imageListBoxControl1.Items.RemoveAt(intIndex);
            if (list.Count == intIndex && ImageListIndex == list.Count)
            {
                timer1.Stop();
                timer2.Stop();
                Bass.BASS_ChannelStop(stream);
                Bass.BASS_StreamFree(stream);
                ImageListIndex = -1;
                pictureEdit1.Image = Image.FromFile(@"Image\" + "bj.jpg");

            }

            else
            {

                if (intIndex == ImageListIndex)
                {
                    networkList.structSong ss = new networkList.structSong();
                    ss.SongID = list[intIndex].SongID;
                    ss.SongName = list[intIndex].SongName;
                    ss.SongAddress = GetMp3Address(ss.SongID);
                    PlayBangDan(ss);
                }
                if (intIndex < ImageListIndex)
                    ImageListIndex--;
            }

        }
        //网络列表下载菜单
        private void barLargeButtonItem11_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            int intIndex = imageListBoxControl1.SelectedIndex;
            networkList.structSong ss = new networkList.structSong();
            ss.SongID = list[intIndex].SongID;
            ss.SongAddress = GetMp3Address(ss.SongID);
            ss.SongName = list[intIndex].SongName;
            DownLoadMp3(ss);
        }

        //转换成网页中需要的码
        public static string BianMa(string text)
        {
            return System.Web.HttpUtility.UrlEncode(text, System.Text.Encoding.UTF8);
        }
        //Grid双击播放音乐
        private void gridControl1_DoubleClick(object sender, EventArgs e)
        {
            networkList.structSong ss = GetGridSongInfo();
            PlayBangDan(ss);
        }

        private void gridControl1_MouseUp(object sender, MouseEventArgs e)
        {
            //if (e.Button == MouseButtons.Right)
            //{

            //    popupMenu2.ShowPopup(Control.MousePosition);
            //}
            DevExpress.XtraGrid.Views.Grid.ViewInfo.GridHitInfo hi = this.gridView1.CalcHitInfo(e.Location);
            //表头
            if (hi.InRow && e.Button == MouseButtons.Right)
            {
                popupMenu2.ShowPopup(Control.MousePosition);
            }
        }
        //播放
        private void barLargeButtonItem14_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            networkList.structSong ss = GetGridSongInfo();
            PlayBangDan(ss);
        }
        //列表
        private void barLargeButtonItem15_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            networkList.structSong ss = GetGridSongInfo();
            if (networkList.WriteXmlList(ss.SongID, ss.SongName))
            {
                imageListBoxControl1.Items.Add(ss.SongName, 3);
                list.Add(ss);
            }

        }
        //下载
        private void barLargeButtonItem16_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            networkList.structSong ss = GetGridSongInfo();
            DownLoadMp3(ss);
        }
        //更换皮肤
        private void comboBoxEdit1_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.defaultLookAndFeel1.LookAndFeel.SkinName = comboBoxEdit1.EditValue.ToString();
        }


        //移除右键菜单
        private void gridView1_PopupMenuShowing(object sender, DevExpress.XtraGrid.Views.Grid.PopupMenuShowingEventArgs e)
        {
            if (e.MenuType == DevExpress.XtraGrid.Views.Grid.GridMenuType.Column)//判断是否是列标题的右键菜单
            {

                GridViewColumnMenu menu = e.Menu as GridViewColumnMenu;

                //menu.Items.RemoveAt(6);//移除右键菜单中的第7个功能，从0开始

                menu.Items.Clear();//清除所有功能

                //Items.Add(参数，参数，参数)添加功能

            }
        }

        //停止按钮 
        private void pictureEdit2_Click(object sender, EventArgs e)
        {
            if ((Bass.BASS_ChannelIsActive(stream) == BASSActive.BASS_ACTIVE_PLAYING) || (Bass.BASS_ChannelIsActive(stream) == BASSActive.BASS_ACTIVE_PAUSED))
            {
                Bass.BASS_ChannelStop(stream);
                Bass.BASS_StreamFree(stream); //释放音频流
                if (playMode == PlayMode.NetWork)
                {
                    if (ImageListIndex != -1)
                        imageListBoxControl1.Items[ImageListIndex].ImageIndex = 3;
                    ImageListIndex = -1;
                }
                else
                {
                    if (ImageListIndex2 != -1)
                        imageListBoxControl2.Items[ImageListIndex2].ImageIndex = 3;
                    ImageListIndex2 = -1;
                }
                timer1.Stop();
                timer2.Stop();
                pictureEdit1.Image = Image.FromFile(@"Image\" + "bj.jpg");
                pictureEdit4.Image = Image.FromFile(@"Image\" + "播放.png");

            }

        }
        //上一曲      

        private void pictureEdit3_Click(object sender, EventArgs e)
        {
            if (playMode == PlayMode.NetWork)
            {
                if (ImageListIndex == 0 || ImageListIndex == -1)
                    return;
                imageListBoxControl1.Items[ImageListIndex].ImageIndex = 3;
                ImageListIndex--;
                imageListBoxControl1.Items[ImageListIndex].ImageIndex = 2;

                networkList.structSong ss = new networkList.structSong();
                ss.SongID = list[ImageListIndex].SongID;
                ss.SongName = list[ImageListIndex].SongName;
                ss.SongAddress = GetMp3Address(ss.SongID);

                PlayBangDan(ss);
                pictureEdit4.Image = Image.FromFile(@"Image\" + "暂停.png");
            }
            else
            {
                if (ImageListIndex2 == 0 || ImageListIndex2 == -1)
                    return;
                imageListBoxControl2.Items[ImageListIndex2].ImageIndex = 3;
                ImageListIndex2--;
                imageListBoxControl2.Items[ImageListIndex2].ImageIndex = 2;
                imageListBoxControl2.SelectedIndex = ImageListIndex2;

                localList.structSong ss = new localList.structSong();

                ss.SongName = list2[ImageListIndex2].SongName;
                ss.SongAddress = list2[ImageListIndex2].SongAddress;

                PlayBangDan2(ss);
                pictureEdit4.Image = Image.FromFile(@"Image\" + "暂停.png");
            }
        }
        //下一曲
        private void pictureEdit5_Click(object sender, EventArgs e)
        {
            //网络歌曲
            if (playMode == PlayMode.NetWork)
            {
                if (ImageListIndex == imageListBoxControl1.Items.Count - 1 || ImageListIndex == -1)
                    return;
                imageListBoxControl1.Items[ImageListIndex].ImageIndex = 3;
                ImageListIndex++;
                imageListBoxControl1.Items[ImageListIndex].ImageIndex = 2;


                networkList.structSong ss = new networkList.structSong();
                ss.SongID = list[ImageListIndex].SongID;
                ss.SongName = list[ImageListIndex].SongName;
                ss.SongAddress = GetMp3Address(ss.SongID);

                PlayBangDan(ss);
                pictureEdit4.Image = Image.FromFile(@"Image\" + "暂停.png");
            }
            else
            {
                if (ImageListIndex2 == imageListBoxControl2.Items.Count - 1 || ImageListIndex2 == -1)
                    return;
                imageListBoxControl2.Items[ImageListIndex2].ImageIndex = 3;
                ImageListIndex2++;
                imageListBoxControl2.Items[ImageListIndex2].ImageIndex = 2;
                imageListBoxControl2.SelectedIndex = ImageListIndex2;

                localList.structSong ss = new localList.structSong();

                ss.SongName = list2[ImageListIndex2].SongName;
                ss.SongAddress = list2[ImageListIndex2].SongAddress;

                PlayBangDan2(ss);

                pictureEdit4.Image = Image.FromFile(@"Image\" + "暂停.png");
            }

        }
        //将秒转换成00:00格式
        private string GetTime(int num)
        {
            TimeSpan t = TimeSpan.FromSeconds(num);
            //string strReturn = string.Format("{0:D2}:{0:D2}", t.Minutes, t.Seconds);
            //return t.Minutes.ToString() + ":" + t.Seconds.ToString();
            int m = t.Minutes;
            int s = t.Seconds;
            string strM, strS;
            if (m < 10)
                strM = "0" + m.ToString();
            else
                strM = m.ToString();
            if (s < 10)
                strS = "0" + s.ToString();
            else
                strS = s.ToString();
            return strM + ":" + strS;

        }
        //播放暂停按钮
        private void pictureEdit4_Click(object sender, EventArgs e)
        {
            //暂停
            if (Bass.BASS_ChannelIsActive(stream) == BASSActive.BASS_ACTIVE_PLAYING)
            {
                Bass.BASS_ChannelPause(stream);
                pictureEdit4.Image = Image.FromFile(@"Image\" + "播放.png");
                timer1.Stop();
                timer2.Stop();
                pictureEdit1.Image = Image.FromFile(@"Image\" + "bj.jpg");

            }//继续播放
            else if (Bass.BASS_ChannelIsActive(stream) == BASSActive.BASS_ACTIVE_PAUSED)
            {
                Bass.BASS_ChannelPlay(stream, false);
                pictureEdit4.Image = Image.FromFile(@"Image\" + "暂停.png");
                timer1.Start();
                timer2.Start();
                pictureEdit1.Image = null;
            }
            else//未开始播放时播放
            {
                if (playMode == PlayMode.NetWork)
                {
                    if (ImageListIndex != -1)
                        imageListBoxControl1.Items[ImageListIndex].ImageIndex = 3;

                    ImageListIndex = imageListBoxControl1.SelectedIndex;
                    imageListBoxControl1.Items[imageListBoxControl1.SelectedIndex].ImageIndex = 2;

                    networkList.structSong ss = new networkList.structSong();
                    ss.SongID = list[ImageListIndex].SongID;
                    ss.SongName = list[ImageListIndex].SongName;
                    ss.SongAddress = GetMp3Address(ss.SongID);

                    PlayBangDan(ss);
                }
                else
                {
                    if (ImageListIndex2 != -1)
                        imageListBoxControl2.Items[ImageListIndex2].ImageIndex = 3;

                    ImageListIndex2 = imageListBoxControl2.SelectedIndex;
                    imageListBoxControl2.Items[imageListBoxControl2.SelectedIndex].ImageIndex = 2;

                    localList.structSong ss = new localList.structSong();

                    ss.SongName = list2[ImageListIndex2].SongName;
                    ss.SongAddress = list2[ImageListIndex2].SongAddress;

                    PlayBangDan2(ss);

                }
                pictureEdit4.Image = Image.FromFile(@"Image\" + "暂停.png");
            }
        }
        //设置行高
        private void gridView1_CalcRowHeight(object sender, DevExpress.XtraGrid.Views.Grid.RowHeightEventArgs e)
        {
            if (e.RowHandle >= 0)
                e.RowHeight = 40;
        }
        //添加序号列
        //private void gridView1_CustomDrawRowIndicator(object sender, DevExpress.XtraGrid.Views.Grid.RowIndicatorCustomDrawEventArgs e)
        //{
        //    if (e.Info.IsRowIndicator && e.RowHandle!= 0)
        //    {
        //        e.Info.DisplayText ="序号";
        //        e.Appearance.Font = new Font("tahoma", 10, FontStyle.Regular);
        //    }

        //    if (e.Info.IsRowIndicator && e.RowHandle >= 0)
        //    {
        //        e.Info.DisplayText = (e.RowHandle + 1).ToString();
        //        e.Appearance.Font = new Font("tahoma", 10, FontStyle.Regular);
        //    }
        //}


        private void GetBangDan(string URL)
        {
            this.splashScreenManager1.ShowWaitForm();
            string strHtml = HttpGet(URL);

            //清空GridView表格中的原有数据
            gridView1.Columns.Clear();
            gridControl1.DataSource = null;
            dataTable.Columns.Clear();
            dataTable.Clear();

            //添加列
            DataColumn num = new DataColumn("序号", Type.GetType("System.String"));
            DataColumn id = new DataColumn("歌曲ID", Type.GetType("System.String"));
            DataColumn name = new DataColumn("歌曲名称", Type.GetType("System.String"));
            DataColumn singer = new DataColumn("歌手", Type.GetType("System.String"));
            DataColumn pic = new DataColumn("专辑", Type.GetType("System.String"));

            dataTable.Columns.Add(num);
            dataTable.Columns.Add(id);
            dataTable.Columns.Add(name);
            dataTable.Columns.Add(singer);
            dataTable.Columns.Add(pic);


            int intNum = 0;
            JsonData root = JsonMapper.ToObject(strHtml);
            JsonData data = root["data"];
            JsonData musicList = data["musicList"];
            for (int i = 0; i < musicList.Count; i++)
            {
                DataRow dr = dataTable.NewRow();
                intNum++;
                JsonData music = musicList[i];
                dr["序号"] = intNum.ToString();
                dr["歌曲ID"] = music["musicrid"].ToString().Substring(6);
                dr["歌曲名称"] = music["name"].ToString();// root.name;
                dr["歌手"] = music["artist"].ToString();// root.artist;
                dr["专辑"] = music["album"].ToString(); //root.album;
                dataTable.Rows.Add(dr);
            }


            gridControl1.DataSource = dataTable;
            gridView1.Columns["序号"].MaxWidth = 60;
            //序号列文字居中
            gridView1.Columns["序号"].AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            gridView1.Columns["歌曲ID"].Visible = false;
            this.splashScreenManager1.CloseWaitForm();
        }

        #region 榜单
        //会员版
        private void barLargeButtonItem18_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            GetBangDan("http://www.kuwo.cn/api/www/bang/bang/musicList?bangId=145&pn=1&rn=200");
        }
        //粤语榜
        private void barLargeButtonItem25_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            GetBangDan("http://www.kuwo.cn/api/www/bang/bang/musicList?bangId=182&pn=1&rn=200");
        }
        //儿歌榜
        private void barLargeButtonItem21_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            GetBangDan("http://www.kuwo.cn/api/www/bang/bang/musicList?bangId=181&pn=1&rn=200");
        }
        //翻唱榜
        private void barLargeButtonItem19_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            GetBangDan("http://www.kuwo.cn/api/www/bang/bang/musicList?bangId=185&pn=1&rn=200");
        }
        //神曲榜
        private void barLargeButtonItem20_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            GetBangDan("http://www.kuwo.cn/api/www/bang/bang/musicList?bangId=186&pn=1&rn=200");
        }
        //怀旧榜
        private void barLargeButtonItem22_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            GetBangDan("http://www.kuwo.cn/api/www/bang/bang/musicList?bangId=26&pn=1&rn=200");
        }
        //金曲榜
        private void barLargeButtonItem23_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            GetBangDan("http://www.kuwo.cn/api/www/bang/bang/musicList?bangId=64&pn=1&rn=200");
        }

        //新歌榜
        private void barLargeButtonItem1_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            //string strHtml = HttpGet("http://www.kuwo.cn/bang/content?name=%E9%85%B7%E6%88%91%E6%96%B0%E6%AD%8C%E6%A6%9C&bangId=17");
            //Regex reg = new Regex("data-music='({.*?})");
            //MatchCollection mc = reg.Matches(strHtml);
            //if (mc.Count > 0)
            //{
            //    CreateTable(mc);
            //}
            GetBangDan("http://www.kuwo.cn/api/www/bang/bang/musicList?bangId=17&pn=1&rn=200");
        }
        //热歌榜
        private void barLargeButtonItem2_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            GetBangDan("http://www.kuwo.cn/api/www/bang/bang/musicList?bangId=16&pn=1&rn=200");
        }
        //飙升榜
        private void barLargeButtonItem3_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            GetBangDan("http://www.kuwo.cn/api/www/bang/bang/musicList?bangId=93&pn=1&rn=200");
        }
        //华语榜
        private void barLargeButtonItem4_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            GetBangDan("http://www.kuwo.cn/api/www/bang/bang/musicList?bangId=62&pn=1&rn=200");
        }
        #endregion


        //本地播放列表右键菜单
        private void imageListBoxControl2_MouseUp(object sender, MouseEventArgs e)
        {
            DevExpress.XtraEditors.ImageListBoxControl ILB = sender as DevExpress.XtraEditors.ImageListBoxControl;
            if (e.Button == MouseButtons.Right && ModifierKeys == Keys.None)
            //if (e.Button == MouseButtons.Right)
            {
                Point p = new Point(Cursor.Position.X, Cursor.Position.Y);
                int intHot = ILB.SelectedIndex;
                //if (intHot >= 0)
                popupMenu3.ShowPopup(p);
            }
        }
        //打开本地音乐
        private void barLargeButtonItem26_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Title = "请选择音乐文件";
            openFileDialog.InitialDirectory = "D:\\";//注意这里写路径时要用c:\\而不是c:\
            openFileDialog.Filter = "音乐文件|*.*|MP3|*.mp3|WAV|*.wav|WMA|*.wma|MIDI|*.midi|VQF|*.vqf|AMR|*.amr|APE|*.ape";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.FilterIndex = 1;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {

                for (int i = 0; i < openFileDialog.SafeFileNames.Count(); i++)
                {
                    localList.structSong ss = new localList.structSong();
                    ss.SongName = openFileDialog.SafeFileNames[i];
                    ss.SongAddress = openFileDialog.FileNames[i];
                    if (isMusicFile(ss.SongAddress) == false)
                        continue;
                    if (localList.WriteXmlList(ss.SongName, ss.SongAddress))
                    {
                        imageListBoxControl2.Items.Add(ss.SongName, 3);
                        list2.Add(ss);
                    }
                }

            }
        }
        //本地音乐播放
        private void imageListBoxControl2_DoubleClick(object sender, EventArgs e)
        {

            if (thread != null)
                thread.Abort();
            this.richTextBox1.Text = "当前模式为本地模式，无法显示歌词。";

            if (ImageListIndex2 != -1)
                imageListBoxControl2.Items[ImageListIndex2].ImageIndex = 3;

            ImageListIndex2 = imageListBoxControl2.SelectedIndex;
            imageListBoxControl2.Items[ImageListIndex2].ImageIndex = 2;

            localList.structSong ss = new localList.structSong();
            ss.SongName = list2[ImageListIndex2].SongName;
            ss.SongAddress = list2[ImageListIndex2].SongAddress;

            PlayBangDan2(ss);
            pictureEdit4.Image = Image.FromFile(@"Image\" + "暂停.png");
        }

        private void xtraTabControl1_SelectedPageChanged(object sender, DevExpress.XtraTab.TabPageChangedEventArgs e)
        {
            //if (e.Page == this.xtraTabPage1)
            //    playMode = PlayMode.NetWork;
            //else
            //    playMode = PlayMode.Local;
        }
        //本地音乐列表自绘
        private void imageListBoxControl2_DrawItem(object sender, DevExpress.XtraEditors.ListBoxDrawItemEventArgs e)
        {
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {

                if (e.Index == ImageListIndex2)
                {
                    e.Appearance.ForeColor = Color.Red;
                    Font font = new Font("宋体", 18);
                    e.Appearance.Font = font;
                }
            }

            if ((e.State & DrawItemState.None) == DrawItemState.None)
            {

                if (e.Index == ImageListIndex2)
                {
                    e.Appearance.ForeColor = Color.Red;
                    Font font = new Font("宋体", 18);
                    e.Appearance.Font = font;
                }
            }
        }
        //本地删除列表
        private void barLargeButtonItem31_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            int intIndex = imageListBoxControl2.SelectedIndex;
            localList.DeleteItem(list2[intIndex].SongAddress);
            list2.RemoveAt(intIndex);
            imageListBoxControl2.Items.RemoveAt(intIndex);
            if (list2.Count == intIndex && ImageListIndex2 == list2.Count)
            {
                timer1.Stop();
                timer2.Stop();
                Bass.BASS_ChannelStop(stream);
                Bass.BASS_StreamFree(stream);
                ImageListIndex2 = -1;
                pictureEdit1.Image = Image.FromFile(@"Image\" + "bj.jpg");

            }

            else
            {

                if (intIndex == ImageListIndex2)
                {
                    localList.structSong ss = new localList.structSong();

                    ss.SongName = list2[intIndex].SongName;
                    ss.SongAddress = list2[intIndex].SongAddress;
                    PlayBangDan2(ss);
                }
                if (intIndex < ImageListIndex2)
                    ImageListIndex2--;
            }
        }
        //本地音乐清空列表
        private void barLargeButtonItem32_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            localList.DeleteAllList();
            imageListBoxControl2.Items.Clear();
            list2.Clear();
            ImageListIndex2 = -1;
        }
        //本地列表播放菜单
        private void barLargeButtonItem27_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (ImageListIndex2 != -1)
                imageListBoxControl2.Items[ImageListIndex2].ImageIndex = 3;

            ImageListIndex2 = imageListBoxControl2.SelectedIndex;
            imageListBoxControl2.Items[imageListBoxControl2.SelectedIndex].ImageIndex = 2;

            localList.structSong ss = new localList.structSong();

            ss.SongName = list2[ImageListIndex2].SongName;
            ss.SongAddress = list2[ImageListIndex2].SongAddress;

            PlayBangDan2(ss);
        }
        //在完成拖放操作时发生 
        private void imageListBoxControl2_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {

                string[] s = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string strFile in s)
                {
                    localList.structSong ss = new localList.structSong();
                    ss.SongAddress = strFile;
                    if (isMusicFile(ss.SongAddress) == false)
                        continue;
                    ss.SongName = strFile.Substring(strFile.LastIndexOf("\\") + 1);
                    if (localList.WriteXmlList(ss.SongName, ss.SongAddress))
                    {
                        imageListBoxControl2.Items.Add(ss.SongName, 3);
                        list2.Add(ss);
                    }
                }

            }


        }
        //当用户在拖放操作过程中首次将鼠标光标拖到控件上时，会引发该事件
        private void imageListBoxControl2_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.All;                                                              //重要代码：表明是所有类型的数据，比如文件路径
            //else if (e.Data.GetDataPresent(DataFormats.Text))
            //    e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }


        private bool isMusicFile(string FileName)
        {
            FileInfo fi = new FileInfo(FileName);
            string str = fi.Extension;//获取扩展名
            str = str.ToUpper();
            if (str == "MP3" || str == "WAV" || str == "MIDI" || str == "VQF" || str == "AMK" || str == "APE")
                return true;
            else
                return false;
        }

        private void barLargeButtonItem33_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            System.Diagnostics.Process.Start(@"Mp3");
        }

        private void CeShiLrc(string SongID)
        {
            string strHtml = HttpGet("http://m.kuwo.cn/newh5/singles/songinfoandlrc?musicId=" + SongID);
            MyLrc ml = new MyLrc(strHtml);
            string strLrc = ml.GetLrc;
        }
        //全部加入列表
        private void barLargeButtonItem34_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            for (int i = 0; i < this.gridView1.RowCount; i++)
            {
                if (this.gridView1.IsRowSelected(i))
                {
                    networkList.structSong ss = new networkList.structSong();
                    ss.SongID = this.gridView1.GetDataRow(i)["歌曲ID"].ToString();
                    ss.SongName = this.gridView1.GetDataRow(i)["歌曲名称"].ToString();
                    if (networkList.WriteXmlList(ss.SongID, ss.SongName))
                    {
                        imageListBoxControl1.Items.Add(ss.SongName, 3);
                        list.Add(ss);
                    }
                    this.gridView1.UnselectRow(i);
                }

            }

        }

        private void buttonEdit1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == 13)
            {
                if (buttonEdit1.Text == "")
                {
                    MessageBox.Show("你没有输入任何关键字！");
                    return;
                }
                SerachSong(buttonEdit1.Text);
            }
        }

        public void MyHttpDownloadFile(object obj)
        {
            List<networkList.structSong> listSong = (List<networkList.structSong>)obj;
            //long longTotal=0;
            int intNum = listSong.Count;
            int intCurrentNum = 0;
            foreach (networkList.structSong ss in listSong)
            {
                intCurrentNum++;
                if (this.InvokeRequired)
                {
                    Action actionDisplay = () =>
                    {
                        barStaticItem5.Caption = "正在下载(" + intCurrentNum.ToString() + "/" + intNum.ToString() + "):" + ss.SongName;
                    };
                    this.Invoke(actionDisplay);
                }
                else
                {
                    barStaticItem5.Caption = "正在下载(" + intCurrentNum.ToString() + "/" + intNum.ToString() + "):" + ss.SongName;
                }


                string path = @"Mp3\" + ss.SongName + ".mp3";
                string url = ss.SongAddress;
                // 设置参数
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                //发送请求并获取相应回应数据
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                //直到request.GetResponse()程序才开始向目标网页发送Post请求
                Stream responseStream = response.GetResponseStream();
                //创建本地文件写入流
                Stream stream = new FileStream(path, FileMode.Create);
                long sum = response.ContentLength;//获取文件的大小
                int dq = 0;//当前大小
                float jd = 0;//进度
                byte[] bArr = new byte[1024];//缓冲区大小
                int size = responseStream.Read(bArr, 0, (int)bArr.Length);
                while (size > 0)
                {
                    stream.Write(bArr, 0, size);
                    dq = dq + size;
                    jd = (float)dq / sum * 100;
                    if (this.InvokeRequired)
                    {
                        Action<int> actionDelegate = (x) =>
                        {
                            this.barEditItem3.EditValue = x; //设置进度条
                            windowsTaskbar.SetProgressValue((int)(jd * 100), 10000, this.Handle); //设置任务栏进度条
                            barStaticItem6.Caption = HumanReadableFilesize(dq) + "/" + HumanReadableFilesize(sum);

                        };
                        this.Invoke(actionDelegate, (int)(jd * 100));


                    }
                    else
                    {
                        barEditItem3.EditValue = (int)(jd * 100);
                        windowsTaskbar.SetProgressValue((int)(jd * 100), 10000, this.Handle);
                    }

                    size = responseStream.Read(bArr, 0, (int)bArr.Length);
                }
                stream.Close();
                responseStream.Close();
            }
            //string strTemp=HumanReadableFilesize(longTotal);





        }


        //字节转换成大B、KB、MB、GB、TB、PB
        private String HumanReadableFilesize(double size)
        {
            String[] units = new String[] { "B", "KB", "MB", "GB", "TB", "PB" };
            double mod = 1024.0;
            int i = 0;
            while (size >= mod)
            {
                size /= mod;
                i++;
            }
            return Math.Round(size, 2) + units[i];
        }
        //全部下载
        private void barLargeButtonItem35_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            Thread t = new Thread(new ParameterizedThreadStart(MyHttpDownloadFile));
            List<networkList.structSong> listTemp = new List<networkList.structSong>();
            for (int i = 0; i < this.gridView1.RowCount; i++)
            {
                if (this.gridView1.IsRowSelected(i))
                {
                    networkList.structSong ss = new networkList.structSong();
                    ss.SongID = this.gridView1.GetDataRow(i)["歌曲ID"].ToString();
                    ss.SongName = this.gridView1.GetDataRow(i)["歌曲名称"].ToString();
                    ss.SongAddress = GetMp3Address(ss.SongID);
                    listTemp.Add(ss);
                    this.gridView1.UnselectRow(i);
                }

            }
            t.IsBackground = true;
            t.Start(listTemp);

        }
        //多线程下载文件
        private void barLargeButtonItem36_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            networkList.structSong ss = GetGridSongInfo();
            //string httpUrl = GetMp3Address(ss.SongID);//下载地址
            string httpUrl = "http://down.dabaicai.org/20190121/DaBaiCai_STA_gw.exe";
            //string saveUrl = "D:\\" + ss.SongName + ".mp3";
            string saveUrl = "D:\\" + ss.SongName + ".exe";
            int threadNumber = 3;
            MultiDownload md = new MultiDownload(threadNumber, httpUrl, saveUrl);
            md.Start(); ;
        }
    }
}
