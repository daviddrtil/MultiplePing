using System.Globalization;
using System.Net;
using MultiplePing.Models;

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
    // todo  -a|--append  Append responses to an existing file.

    // todo run:
    // #1: cd C:\Users\daviddrtil\source\repos\MultiplePing\MultiplePing\bin\Debug\net8.0
    // #2: .\MultiplePing.exe www.google.com www.seznam.cz 5

    private static IPAddress[] ParseIpAddresses(string[] args)
    {
        int ipCount = args.Length - 1;
        var pingTargets = new IPAddress[ipCount];
        for (int i = 0; i < ipCount; i++)
        {
            string hostname = args[i];
            if (!IPAddress.TryParse(hostname, out pingTargets[i]))
            {
                pingTargets[i] = Dns.GetHostEntry(hostname).AddressList.FirstOrDefault()
                    ?? throw new ArgumentException($"Error: Unable to resolve IP address '{hostname}'");
            }
        }
        return pingTargets;
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

        string durationArg = args[^1];
        if (!int.TryParse(durationArg, NumberFormatInfo.InvariantInfo, out int durationInSec))
        {
            throw new ArgumentException($"Error: Invalid duration '{durationArg}', expected a numeric value.");
        }
        PingSettings.TotalDurationSeconds = durationInSec;

        PingSettings.IpAddresses = ParseIpAddresses(args);
    }

    static void Main(string[] args)
    {
        ParseArgs(args);

        PingSettings.XmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "Output", "ping.xml");
        Console.WriteLine($"Storing data into XML file: {PingSettings.XmlPath}");

        PingLogger.PerformAndStorePings();
        PingLogger.CreatePingStatistics();
    }
}
