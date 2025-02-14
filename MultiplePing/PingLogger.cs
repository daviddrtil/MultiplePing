using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using MultiplePing.Models;

namespace MultiplePing;

internal class PingLogger
{
    /// <summary>
    /// Writes the header section with IP addresses.
    /// </summary>
    private static void WriteXMLHeader(XmlWriter xmlWriter)
    {
        xmlWriter.WriteStartElement("ipAddresses");

        for (int i = 0; i < PingSettings.IpAddresses.Length; i++)
        {
            var ipAddress = PingSettings.IpAddresses[i];
            xmlWriter.WriteStartElement("ipAddress");
            xmlWriter.WriteAttributeString("name", ipAddress.ToString());
            xmlWriter.WriteAttributeString("idx", i.ToString());
            xmlWriter.WriteEndElement(); // </ipAddress>
        }

        xmlWriter.WriteEndElement(); // </ipAddresses>
    }


    /// <summary>
    /// Performs ICMP pings and stores the responses in the XML file.
    /// </summary>
    public static void PerformAndStorePings()
    {
        var xmlSettings = new XmlWriterSettings()
        {
            Encoding = Encoding.UTF8,
            Indent = true
        };

        using var fileStream = new FileStream(PingSettings.XmlPath,
            FileMode.Create, FileAccess.Write, FileShare.Read);
        using var xmlWriter = XmlWriter.Create(fileStream, xmlSettings);

        xmlWriter.WriteStartDocument();
        xmlWriter.WriteStartElement("ping");

        WriteXMLHeader(xmlWriter);

        ProcessPings(xmlWriter);

        xmlWriter.WriteEndElement(); // </ping>
    }

    private static void ProcessPings(XmlWriter xmlWriter)
    {
        var xmlLock = new object();
        var threads = new Thread[PingSettings.IpAddresses.Length];
        for (var i = 0; i < PingSettings.IpAddresses.Length; i++)
        {
            var threadData = new PingThreadData(i, PingSettings.IpAddresses[i], xmlWriter, xmlLock);
            threads[i] = new Thread(PingThread);
            threads[i].Start(threadData);
        }
        foreach (var thread in threads)
        {
            thread.Join();
        }
    }

    private static void PingThread(object obj)
    {
        using var ping = new Ping();
        var threadData = (PingThreadData)obj;
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < PingSettings.TotalDurationSeconds * 1000)
        {
            long duration;
            try
            {
                var reply = ping.Send(threadData.IpAddress, PingSettings.TimeoutMs);
                duration = reply.Status == IPStatus.Success ? reply.RoundtripTime : -1L;
            }
            catch (PingException)
            {
                duration = -1L;
            }
            lock (threadData.XmlLock)
            {
                threadData.XmlWriter.WriteStartElement("reply");
                threadData.XmlWriter.WriteAttributeString("idx", threadData.Index.ToString());
                threadData.XmlWriter.WriteAttributeString("duration", duration.ToString());
                threadData.XmlWriter.WriteEndElement(); // </reply>
            }
            Thread.Sleep(PingSettings.PingIntervalMs);
        }
    }

    /// <summary>
    /// Reads the XML file and calculates the average response times.
    /// </summary>
    public static void CreatePingStatistics()
    {
        if (!File.Exists(PingSettings.XmlPath))
        {
            throw new FileNotFoundException("Error: XML file not found.");
        }

        try
        {
            Console.WriteLine($"Number of IP addresses: {PingSettings.IpAddresses.Length}\n");
            var xmlDoc = XDocument.Load(PingSettings.XmlPath);
            var ipAddresses = xmlDoc.Root!.Elements("ipAddresses").Elements("ipAddress");
            foreach (var ipAddress in ipAddresses)
            {
                var ipName = ipAddress.Attribute("name")?.Value ?? "Unknown";
                var replies = xmlDoc.Root
                    .Elements("reply")
                    .Where(r => r.Attribute("idx")?.Value == ipAddress.Attribute("idx")?.Value)
                    .Select(r => int.Parse(r.Attribute("duration")?.Value ?? "-1"))
                    .Where(duration => duration >= 0)
                    .ToList();

                if (replies.Count != 0)
                {
                    var averageLatency = (int)replies.Average();
                    Console.WriteLine($"IP address {ipName} average latency: {averageLatency} ms");
                }
                else
                {
                    Console.WriteLine($"IP address {ipName} had no valid responses.");
                }
            }
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error reading XML file", ex);
        }
    }
}
