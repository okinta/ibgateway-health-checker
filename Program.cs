using CommandLine;
using Nito.AsyncEx;
using System.Threading.Tasks;
using System;

namespace IbGatewayHealthChecker
{
    /// <summary>
    /// Runs the program.
    /// </summary>
    internal class Program
    {
        private readonly AsyncManualResetEvent _exitEvent = new AsyncManualResetEvent();

        /// <summary>
        /// Instantiates the instance. Creates event handler for ctrl+c.
        /// </summary>
        public Program()
        {
            Console.CancelKeyPress += OnCancelKeyPress;
        }

        /// <summary>
        /// Entrypoint to start the program.
        /// </summary>
        /// <param name="args">The arguments passed via the command line.</param>
        public static async Task Main(string[] args)
        {
            var program = new Program();
            await Parser.Default.ParseArguments<Options>(args)
                .WithParsedAsync(program.Run);
        }

        /// <summary>
        /// Monitors a connection to IB in the background. Waits until a signal is
        /// received to exit the program.
        /// </summary>
        /// <param name="opts">The parsed command line options.</param>
        private async Task Run(Options opts)
        {
            using var _ = new IbClient(
                opts.Host, opts.Port, opts.ClientId, opts.PagerTreeIntegrationId);
            await _exitEvent.WaitAsync();
            await ConsoleX.WriteLineAsync("Goodbye");
        }

        /// <summary>
        /// Called when ctrl+c is pressed. Signal that we should exit the program.
        /// </summary>
        /// <param name="sender">The object that sent the event.</param>
        /// <param name="eventArgs">The event arguments.</param>
        private void OnCancelKeyPress(object sender, ConsoleCancelEventArgs eventArgs)
        {
            Console.CancelKeyPress -= OnCancelKeyPress;
            eventArgs.Cancel = true;
            _exitEvent.Set();
        }
    }
}
