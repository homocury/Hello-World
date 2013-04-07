using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace EggServer.Util.Xml
{
    class XmlHelper
    {
        public XmlDocument mXmlDoc;
        public readonly string mFile;

        public XmlHelper(string file)
        {
            mFile = file;

            mXmlDoc = new XmlDocument();
            mXmlDoc.Load(file);
        }

        public T GetSingleNodeValue<T>(string xpath)
        {
            XmlNode node = mXmlDoc.SelectSingleNode(xpath);
            return (T)Convert.ChangeType(node.InnerText, typeof(T));
        }

        public T GetSingleAttributeValue<T>(string xpath)
        {
            XmlNode node = mXmlDoc.SelectSingleNode(xpath);
            return (T)Convert.ChangeType(node.Value, typeof(T));
        }
    }
}
