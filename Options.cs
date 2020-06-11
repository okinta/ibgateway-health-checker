using CommandLine;

namespace IbGatewayHealthChecker
{
    /// <summary>
    /// Command line options.
    /// </summary>
    internal class Options
    {
        [Option('h', "host", Default = "127.0.0.1",
            HelpText = "The IB host to connect to")]
        public string Host { get; set; }

        [Option('p', "port", Default = 7000,
            HelpText = "The IB port to connect to")]
        public int Port { get; set; }

        [Option('i', "id", Default = 987,
            HelpText = "The client ID to connect to IB as")]
        public int ClientId { get; set; }

        [Option('x', "pagertree-int-id",
            HelpText = "The PagerTree integration ID to notify if IB is unavailable")]
        public string PagerTreeIntegrationId { get; set; }
    }
}
