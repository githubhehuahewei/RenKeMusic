using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
namespace 全网音乐
{
    class localList
    {
        static public string FileName="local.xml";
        public struct structSong
        {           
            public string SongAddress;
            public string SongName;

        }
        //读取本地播放列表
        static public List<structSong> ReadXmlList()
        {
            List<structSong> listSong = new List<structSong>();

            XmlDocument XmlDoc = new XmlDocument();
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreComments = true;//忽略文档里面的注释
            XmlReader reader = XmlReader.Create(FileName, settings);
            XmlDoc.Load(reader);

            XmlNode nodeRoot = XmlDoc.SelectSingleNode("PlayList");
            XmlNodeList nodeSong = nodeRoot.ChildNodes;

            foreach (XmlNode x in nodeSong)
            {
                structSong ss = new structSong();
                XmlNodeList xnl = x.ChildNodes;
                ss.SongName = xnl.Item(0).InnerText;
                ss.SongAddress = xnl.Item(1).InnerText;
                listSong.Add(ss);
            }
            reader.Close();
            return listSong;
        }

        //添加到歌曲列表
        static public bool WriteXmlList(string SongName,string SongAddress)
        {
            XmlDocument XmlDoc = new XmlDocument();
            XmlDoc.Load(FileName);
           
            XmlNodeList x = XmlDoc.SelectNodes("//Song[SongAddress='" + SongAddress + "']");
            if (x.Count > 0)
                return false;

            XmlNode nodeRoot = XmlDoc.SelectSingleNode("PlayList");

            XmlElement xelKey = XmlDoc.CreateElement("Song");

            //添加歌曲ID属性
           

            XmlElement xelSongName = XmlDoc.CreateElement("SongName");
            xelSongName.InnerText = SongName;
            XmlElement xelSongAddress = XmlDoc.CreateElement("SongAddress");
            xelSongAddress.InnerText = SongAddress;

            xelKey.AppendChild(xelSongName);
            xelKey.AppendChild(xelSongAddress);
            nodeRoot.AppendChild(xelKey);
            XmlDoc.Save(@"local.xml");
            return true;
        }
        //删除项
        static public void DeleteItem(string SongAddress)
        {            
            XmlDocument XmlDoc = new XmlDocument();
            XmlDoc.Load(FileName);
             XmlNode x = XmlDoc.SelectSingleNode("//Song[SongAddress='" + SongAddress + "']");
            if (x != null)
                x.ParentNode.RemoveChild(x);
            XmlDoc.Save(FileName);
        }
        static public void DeleteAllList()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<PlayList></PlayList>");//用这句话,会把以前的数据全部覆盖掉,只有你增加的数据
            doc.Save(FileName);
        }
    }
}
