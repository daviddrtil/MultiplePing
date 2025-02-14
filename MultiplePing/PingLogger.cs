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
    /// Pings a single host in a loop and writes results.
    /// </summary>
    private static async Task PingHostAsync(int index, IPAddress ipAddress,
        XmlWriter xmlWriter, SemaphoreSlim semaphore)
    {
        using var ping = new Ping();
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < PingSettings.TotalDurationSeconds * 1000)
        {
            long duration;
            try
            {
                var reply = await ping.SendPingAsync(ipAddress, PingSettings.TimeoutMs);
                duration = reply.Status == IPStatus.Success ? reply.RoundtripTime : -1L;
            }
            catch (PingException)
            {
                duration = -1L;
            }

            await semaphore.WaitAsync();
            try
            {
                xmlWriter.WriteStartElement("reply");
                xmlWriter.WriteAttributeString("idx", index.ToString());
                xmlWriter.WriteAttributeString("duration", duration.ToString());
                xmlWriter.WriteEndElement(); // </reply>
            }
            finally
            {
                semaphore.Release();
            }

            await Task.Delay(PingSettings.PingIntervalMs);
        }
    }

    /// <summary>
    /// Runs ping tasks in parallel and writes results to XML.
    /// </summary>
    private static async Task ProcessPingsAsync(XmlWriter xmlWriter)
    {
        var semaphore = new SemaphoreSlim(1, 1); // Used to XML writing

        var tasks = PingSettings.IpAddresses
            .Select((ip, index) => PingHostAsync(index, ip, xmlWriter, semaphore))
            .ToArray();

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Performs ICMP pings and stores the responses in the XML file.
    /// </summary>
    public static async Task PerformAndStorePingsAsync()
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

        await ProcessPingsAsync(xmlWriter);

        xmlWriter.WriteEndElement(); // </ping>
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
