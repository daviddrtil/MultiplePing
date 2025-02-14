using System.Net;

namespace MultiplePing
{
    internal class PingSettings
    {
        /// <summary>
        /// List of target IP addresses to be pinged
        /// </summary>
        public static IPAddress[] IpAddresses { get; set; } = [];

        /// <summary>
        /// List of hostnames entered as arguments
        /// </summary>
        public static string[] HostNames { get; set; } = [];

        /// <summary>
        /// Total duration in seconds for which pings will be sent
        /// </summary>
        public static int TotalDurationSeconds { get; set; } = 30;

        /// <summary>
        /// Interval between pings in milliseconds
        /// </summary>
        public static int PingIntervalMs { get; set; } = 100;

        /// <summary>
        /// Maximum time in milliseconds to wait for a ping response before considering it a failure
        /// </summary>
        public static int TimeoutMs { get; set; } = 1000;

        /// <summary>
        /// Path to the XML file where the ping responses will be stored
        /// </summary>
        public static string XmlPath { get; set; } = string.Empty;
    }
}
