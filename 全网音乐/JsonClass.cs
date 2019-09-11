using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace 全网音乐
{
    public class BangRoot
    {
        /// <summary>
        /// 
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// 第一对手
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 欧阳靖&林书豪
        /// </summary>
        public string artist { get; set; }
        /// <summary>
        /// 篮球大唱片
        /// </summary>
        public string album { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string pay { get; set; }
    }
    public class DataItem
    {
        /// <summary>
        /// 歌曲ID
        /// </summary>
        public string id { get; set; }
        /// <summary>
        ///歌名
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 歌手
        /// </summary>
        public string singer { get; set; }
        /// <summary>
        /// 图片网址
        /// </summary>
        public string pic { get; set; }
        /// <summary>
        /// 歌词网址
        /// </summary>
        public string lrc { get; set; }
        /// <summary>
        /// 歌曲网址
        /// </summary>
        public string url { get; set; }
        /// <summary>
        /// 时长
        /// </summary>
        public string time { get; set; }
    }

    public class Root
    {
        /// <summary>
        /// 
        /// </summary>
        public string result { get; set; }
        /// <summary>
        /// 状态码
        /// </summary>
        public int code { get; set; }
        /// <summary>
        /// 歌曲Data信息
        /// </summary>
        public List<DataItem> data { get; set; }
    }
}
