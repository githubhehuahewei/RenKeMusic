using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LitJson;
namespace 全网音乐
{
    class MyLrc
    {
       /// <summary>
       /// 获取歌词
       /// </summary>
        public string GetLrc
        {
            get
            {
                return GetAllLrc();
            }
        }
        public int mark;//当前歌词条目
        public List<TimeLrc> list=new List<TimeLrc>();
        public List<int> listTime = new List<int>();
        public struct TimeLrc
        {            
            public int time;
            public string lrc;
        }
        public  MyLrc(string strLrc)
        {
            JieXiLrc(strLrc);
            GetListTime();
        }
        
        /// <summary>
        /// 解析音乐
        /// </summary>
        /// <param name="strLrc"></param>
        private void JieXiLrc(string strLrc)
        {

            JsonData root = JsonMapper.ToObject(strLrc);
            if (root== null)
                return;
            JsonData data = root["data"];
            if (data == null)
                return;
            JsonData lrclist = data["lrclist"];
            if (lrclist== null)
                return;
            for (int i = 0; i < lrclist.Count;i++)
            {
                JsonData jd = lrclist[i];
                TimeLrc tl = new TimeLrc();
                tl.time = TimeToMs(jd["time"].ToString());
                tl.lrc =jd["lineLyric"].ToString();
                list.Add(tl);
            }            
        }
        //获取所有歌词
        private string GetAllLrc()
        {
            string str=string.Empty;
            
            foreach (TimeLrc tl in list)
            {
                str = str + tl.lrc + "\r\n";               
                int len = tl.lrc.Length;
                listTime.Add(listTime[listTime.Count - 1] + len);
                
            }
            return str;
        }

        private void GetListTime()
        {
            listTime.Add(0);
            foreach (TimeLrc tl in list)
            {                
                int len = tl.lrc.Length+1;
                listTime.Add(listTime[listTime.Count - 1] + len);

            }
        }
        /// <summary>
        /// 时间转成毫秒
        /// </summary>
        /// <param name="strTime"></param>
        /// <returns></returns>
        private int TimeToMs(string strTime)
        {
            int s, ms;
            string[] strLrc = strTime.Split('.');
            //这里不能用TryParse如“by:253057646”则有问题
            int.TryParse(strLrc[0], out s);
            int.TryParse(strLrc[1], out ms);           
            return s * 1000 + ms * 10;
        }
    }
}
