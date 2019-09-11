using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
namespace 全网音乐
{
    class Lrc
    {
        public List<int> listTime = new List<int>();
        public struct TLrc
        {
            public int ms;//毫秒
            public string lrc;//对应的歌词
        }
        /// <summary>
        /// 标准的歌词结构数组
        /// </summary>
        public TLrc[] tlrc;
        /// <summary>
        /// 输入歌词文件路径处理歌词信息
        /// </summary>
        /// <param name="file"></param>
        public void Lyrics(string file)
        {
            //StreamReader sr = new StreamReader(getLrcFile(file), Encoding.Default);
            //StreamReader sr = new StreamReader(file);
            //string[] lrc_1 = sr.ReadToEnd().Split(new char[] { '[', ']' });
            string[] lrc_1 = file.Split(new char[] { '[', ']' });
            //sr.Close();
            format_1(lrc_1);
            format_2(lrc_1);
            format_3();
            listTime.Add(0);
            for (int i = 0; i < tlrc.Length; i++)
            {
                int len=tlrc[i].lrc.Length-1;
                listTime.Add(listTime[listTime.Count-1]+len);
            }
            
        }

        /// <summary>
        /// 获取所有歌词
        /// </summary>
        /// <returns></returns>
        private string GetAllLrc()
        {
            string strTemp="";
            for(int i=0;i<tlrc.Length;i++)
            {
                strTemp= strTemp + tlrc[i].lrc;
            }
            return strTemp;
        }
        /// <summary>
        /// 格式化不同时间相同字符如“[00:34.52][00:34.53][00:34.54]因为我已经没有力气”
        /// </summary>
        /// <param name="lrc_1"></param>
        /// 
        private void format_1(string[] lrc_1)
        {
            for (int i = 2, j = 0; i < lrc_1.Length; i += 2, j = i)
            {
                while (lrc_1[j] == string.Empty)
                {
                    lrc_1[i] = lrc_1[j += 2];
                }
            }
        }
        /// <summary>
        /// 数据添加到结构体
        /// </summary>
        /// <param name="lrc_1"></param>
        private void format_2(string[] lrc_1)
        {
            tlrc = new TLrc[lrc_1.Length / 2];
            for (int i = 1, j = 0; i < lrc_1.Length; i++, j++)
            {
                tlrc[j].ms = timeToMs(lrc_1[i]);
                tlrc[j].lrc = lrc_1[++i];
            }
        }
        /// <summary>
        /// 时间格式”00:37.61“转毫秒
        /// </summary>
        /// <param name="lrc_t"></param>
        /// <returns></returns>
        private int timeToMs(string lrc_t)
        {
            float m, s, ms;
            string[] lrc_t_1 = lrc_t.Split(':');
            //这里不能用TryParse如“by:253057646”则有问题
            try
            {
                m = float.Parse(lrc_t_1[0]);
            }
            catch
            {
                return 0;
            }
            float.TryParse(lrc_t_1[1], out s);
            ms = m * 60000 + s * 1000;
            return (int)ms;
        }
        /// <summary>
        /// 排序，时间顺序
        /// </summary>
        private void format_3()
        {
            TLrc tlrc_temp;
            bool b = true;
            for (int i = 0; i < tlrc.Length - 1; i++, b = true)
            {
                for (int j = 0; j < tlrc.Length - i - 1; j++)
                {
                    if (tlrc[j].ms > tlrc[j + 1].ms)
                    {
                        tlrc_temp = tlrc[j];
                        tlrc[j] = tlrc[j + 1];
                        tlrc[j + 1] = tlrc_temp;
                        b = false;
                    }
                }
                if (b) break;
            }
        }
        public int mark;



        /// <summary>
        /// 读取下一条记录,并跳到下一条记录
        /// </summary>
        /// <returns></returns>
        public string ReadLine()
        {
            if (mark < tlrc.Length)
            {
                return tlrc[mark++].lrc;
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 获取所有歌词
        /// </summary>
        public string GetLrc
        {
            get
            {
                return GetAllLrc();
            }
        }


        /// <summary>
        /// 读取当前行的歌词的时间
        /// </summary>
        /// <returns></returns>
        public int currentTime
        {
            get
            {
                if (mark < tlrc.Length)
                {
                    return tlrc[mark].ms;
                }
                else
                {
                    return -1;
                }
            }
        }

        
        /// <summary>
        /// 得到lrc歌词文件(当前目录)
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private string getLrcFile(string file)
        {
            return Path.GetDirectoryName(file) + "//" + Path.GetFileNameWithoutExtension(file) + ".lrc";
        }

    }
}
