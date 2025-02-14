using System.Globalization;
using System.Net;

namespace MultiplePing;

/// <summary>
/// MultiPing - Perform ICMP ping requests to the specified IP addresses
/// and store the responses in an XML file.
/// </summary>
class Program
{
    private static void PrintHelp() =>
        Console.WriteLine(@"
Usage: multiping [ADDRESSES...] [DURATION]
Example: multiping www.google.com www.seznam.cz 30

Arguments:
  ADDRESSES:   One or more web addresses or IP addresses.
  DURATION:    Time in seconds.

Options:
  -h|--help    Display this help.
");

    private static void ParseIpAddresses(string[] args)
    {
        var ipCount = args.Length - 1;
        PingSettings.IpAddresses = new IPAddress[ipCount];
        PingSettings.HostNames = new string[ipCount];
        for (var i = 0; i < ipCount; i++)
        {
            var hostname = args[i];
            PingSettings.HostNames[i] = hostname;
            if (!IPAddress.TryParse(hostname, out PingSettings.IpAddresses[i]!))
            {
                PingSettings.IpAddresses[i] = Dns.GetHostEntry(hostname).AddressList.FirstOrDefault()
                    ?? throw new ArgumentException($"Error: Unable to resolve IP address '{hostname}'");
            }
        }
    }

    private static void ParseArgs(string[] args)
    {
        if (args.Contains("-h") || args.Contains("--help"))
        {
            PrintHelp();
            Environment.Exit(0);
        }
        if (args.Length < 2)
        {
            throw new ArgumentException($"Error: Invalid arguments count.");
        }

        var durationArg = args[^1];
        if (!int.TryParse(durationArg, NumberFormatInfo.InvariantInfo, out var durationInSec))
        {
            throw new ArgumentException($"Error: Invalid duration '{durationArg}', expected a numeric value.");
        }
        PingSettings.TotalDurationSeconds = durationInSec;

        ParseIpAddresses(args);
    }

    static async Task Main(string[] args)
    {
        ParseArgs(args);

        PingSettings.XmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "ping.xml");
        Console.WriteLine($"Storing data into XML file: {PingSettings.XmlPath}");

        await PingLogger.PerformAndStorePingsAsync();
        PingLogger.CreatePingStatistics();
    }
}
