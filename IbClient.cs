using AutoFinance.Broker.InteractiveBrokers.Controllers;
using AutoFinance.Broker.InteractiveBrokers;
using DnsClient.Protocol;
using DnsClient;
using System.Linq;
using System.Net;
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
            ITwsControllerBase tws = null;
            var connected = false;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (tws is null)
                    {
                        var ip = await GetIpAddress(host, token);
                        tws = new TwsObjectFactory(ip.ToString(), port, clientId)
                            .TwsController;
                    }

                    await CheckConnection(tws, token);
                    await tws.RequestPositions();

                    if (incident != null)
                    {
                        await incident.Resolve(token);
                        incident = null;
                        await ConsoleX.WriteLineAsync(
                            "Resolved PagerTree incident", token);
                    }

                    if (!connected)
                    {
                        await ConsoleX.WriteLineAsync("Active", token);
                        connected = true;
                    }
                }
                catch (Exception e)
                {
                    tws = null;
                    connected = false;

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
                await tws.EnsureConnectedAsync(token);
            }
            catch (TimeoutException e)
            {
                if (tws.Connected)
                    await tws.DisconnectAsync();
                throw new ConnectionException(
                    "Timed out waiting for connection to establish", e);
            }
            catch (TaskCanceledException)
            {
                if (tws.Connected)
                    await tws.DisconnectAsync();
                throw;
            }
            catch (Exception e)
            {
                if (tws.Connected)
                    await tws.DisconnectAsync();
                throw new ConnectionException(e.Message, e);
            }

            try
            {
                await tws.RequestPositions();
            }
            catch (TaskCanceledException e)
            {
                if (tws.Connected)
                    await tws.DisconnectAsync();
                throw new ConnectionException(
                    "Timed out waiting for response from establish", e);
            }
            catch (Exception e)
            {
                if (tws.Connected)
                    await tws.DisconnectAsync();
                throw new ConnectionException(e.Message, e);
            }
        }

        /// <summary>
        /// Performs a DNS query to resolve the given host to an IPAddress.
        /// </summary>
        /// <param name="host">The host to resolve.</param>
        /// <param name="token">The token to check for cancellation requests.</param>
        /// <returns>The resolved IPAddress of the <paramref name="host"/>.</returns>
        private static async Task<IPAddress> GetIpAddress(
            string host, CancellationToken token)
        {
            if (IPAddress.TryParse(host, out var address)) return address;

            var lookupClient = new LookupClient();
            var result = await lookupClient.QueryAsync(
                new DnsQuestion(host, QueryType.A), token);

            // Pick a random record
            try
            {
                return result.AllRecords
                    .OfType<AddressRecord>()
                    .OrderBy(x => Guid.NewGuid())
                    .First().Address;
            }
            catch (Exception e)
            {
                throw new ArgumentException(
                    $"Could not resolve {host}", nameof(host), e);
            }
        }
    }
}
