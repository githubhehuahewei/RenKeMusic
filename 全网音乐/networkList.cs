using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Windows.Forms;
namespace 全网音乐
{
    class networkList
    {
        static public string FileName = "network.xml";
        public struct structSong
        {
            public string SongID;
            public string SongAddress;
            public string SongName;

        }
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

                XmlElement xe = (XmlElement)x;
                string strSongID = xe.GetAttribute("SongID").ToString();
                ss.SongID = strSongID;
                XmlNodeList xnl = x.ChildNodes;
                ss.SongName = xnl.Item(0).InnerText;
                listSong.Add(ss);
            }
            reader.Close();
            return listSong;
        }

        //添加到歌曲列表
        static public bool WriteXmlList(string SongID, string SongName)
        {
            XmlDocument XmlDoc = new XmlDocument();
            XmlDoc.Load(FileName);


            XmlNodeList x = XmlDoc.SelectNodes("//Song[@SongID='" + SongID + "']");
            if (x.Count>0)
                return false;

            XmlNode nodeRoot = XmlDoc.SelectSingleNode("PlayList");


            // XmlNodeList nodeList = nodeRoot.ChildNodes;

            XmlElement xelKey = XmlDoc.CreateElement("Song");
            //添加歌曲ID属性
            XmlAttribute xelSongID = XmlDoc.CreateAttribute("SongID");
            xelSongID.InnerText = SongID;
            xelKey.SetAttributeNode(xelSongID);

            XmlElement xelSongName = XmlDoc.CreateElement("SongName");
            xelSongName.InnerText = SongName;
           
            xelKey.AppendChild(xelSongName);
            nodeRoot.AppendChild(xelKey);
            XmlDoc.Save(FileName);
            return true;
        }
        static public void DeleteItem(string SongID)
        {
            XmlDocument XmlDoc = new XmlDocument();
            XmlDoc.Load(FileName);
            XmlNode x = XmlDoc.SelectSingleNode("//Song[@SongID='" + SongID + "']");
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
