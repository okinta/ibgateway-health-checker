using AutoFinance.Broker.InteractiveBrokers.Controllers;
using AutoFinance.Broker.InteractiveBrokers;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace IbGatewayHealthChecker
{
    /// <summary>
    /// Defines ability to monitor a connection to IB.
    /// </summary>
    internal class IbClient : IDisposable
    {
        private CancellationTokenSource Token { get; } = new CancellationTokenSource();
        private const int ConnectionTimeout = 15000;
        private const int Sleep = 15000;
        private Task Task { get; }

        /// <summary>
        /// Establishes a connection to IB and monitors it in the background.
        /// </summary>
        /// <param name="host">The host to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <param name="clientId">The client ID to connect as.</param>
        /// <param name="pagerTreeIntId">The PagerTree integration ID to notify when
        /// there is an error connecting to IB.</param>
        public IbClient(string host, int port, int clientId, string pagerTreeIntId)
        {
            if (string.IsNullOrEmpty(host))
                throw new ArgumentNullException(
                    nameof(host), "host must be provided");

            if (port < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(port), "port must be larger than 1");

            if (clientId < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(clientId), "clientId must be larger than 1");

            // Start maintaining the connection to IQFeed in a background thread
            Task = Task.Run(() => Run(
                host, port, clientId, pagerTreeIntId, Token.Token));
        }

        /// <summary>
        /// Closes the connection to IB.
        /// </summary>
        public void Dispose()
        {
            Token.Cancel();

            try
            {
                Task.GetAwaiter().GetResult();
            }
            catch (TaskCanceledException)
            {
            }
        }

        /// <summary>
        /// Continuously checks the health of a connection to IB.
        /// </summary>
        /// <param name="host">The host to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <param name="clientId">The client ID to connect as.</param>
        /// <param name="pagerTreeIntId">The PagerTree integration ID to notify when
        /// there is an error connecting to IB.</param>
        /// <param name="token">The token to check to end the task.</param>
        private static async Task Run(
            string host, int port, int clientId, string pagerTreeIntId,
            CancellationToken token)
        {
            Incident incident = null;
            var factory = new TwsObjectFactory(host, port, clientId);
            var tws = factory.TwsController;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await CheckConnection(tws, token);
                    await tws.RequestPositions();

                    if (incident == null) continue;
                    await incident.Resolve(token);
                    incident = null;
                    await ConsoleX.WriteLineAsync(
                        "Resolved PagerTree incident", token);
                }
                catch (ConnectionException e)
                {
                    if (incident is null && !string.IsNullOrEmpty(pagerTreeIntId))
                    {
                        incident = new Incident(
                            pagerTreeIntId, "IB is down", e.Message);
                        await incident.Notify(token);
                        await ConsoleX.WriteErrorLineAsync(
                            "Created PagerTree incident", token);
                    }

                    await ConsoleX.WriteErrorLineAsync(
                        e.Message, token);
                }

                await Task.Delay(Sleep, token);
            }
        }

        /// <summary>
        /// Checks that a connection to IB is established.
        /// </summary>
        /// <param name="tws">The instance to check the connection for.</param>
        /// <param name="token">The token to check to cancel the task.</param>
        /// <exception cref="ConnectionException">If a connection cannot be
        /// established.</exception>
        /// <exception cref="TaskCanceledException">If the task is canceled before
        /// completion.</exception>
        private static async Task CheckConnection(
            ITwsControllerBase tws, CancellationToken token)
        {
            try
            {
                await tws.EnsureConnectedAsync(ConnectionTimeout, token);
            }
            catch (TimeoutException e)
            {
                await tws.DisconnectAsync();
                throw new ConnectionException(
                    "Timed out waiting for connection to establish", e);
            }
            catch (TaskCanceledException)
            {
                await tws.DisconnectAsync();
                throw;
            }
            catch (Exception e)
            {
                await tws.DisconnectAsync();
                throw new ConnectionException(e.Message, e);
            }

            try
            {
                await tws.RequestPositions();
            }
            catch (TaskCanceledException e)
            {
                await tws.DisconnectAsync();
                throw new ConnectionException(
                    "Timed out waiting for response from establish", e);
            }
            catch (Exception e)
            {
                await tws.DisconnectAsync();
                throw new ConnectionException(e.Message, e);
            }
        }
    }
}
