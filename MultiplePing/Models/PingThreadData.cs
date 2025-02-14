using System.Net;
using System.Xml;

namespace MultiplePing.Models
{
    /// <summary>
    /// Data required by ping thread
    /// </summary>
    internal class PingThreadData
    {
        public int Index { get; set; }
        public IPAddress IpAddress { get; set; }
        public XmlWriter XmlWriter { get; set; }
        public object XmlLock { get; set; }

        public PingThreadData(int index, IPAddress ip, XmlWriter wr, object xmlLock)
        {
            Index = index;
            IpAddress = ip;
            XmlWriter = wr;
            XmlLock = xmlLock;
        }
    }
}
