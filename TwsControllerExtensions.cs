using AutoFinance.Broker.InteractiveBrokers.Controllers;
using Nito.AsyncEx;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace IbGatewayHealthChecker
{
    /// <summary>
    /// Extends the TwsController class.
    /// </summary>
    internal static class TwsControllerExtensions
    {
        /// <summary>
        /// Ensures the connection is active.
        /// </summary>
        /// <param name="tws">The TwsController instance to use to check if the connection
        /// is active.</param>
        /// <param name="token">The token to check for canceling the connection.</param>
        /// <exception cref="TimeoutException">If the task times out before a connection
        /// is established.</exception>
        /// <exception cref="TaskCanceledException">If the task is canceled via
        /// <paramref name="token"/>.</exception>
        public static async Task EnsureConnectedAsync(
            this ITwsControllerBase tws, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            try
            {
                await tws.EnsureConnectedAsync().WaitAsync(token);
                token.ThrowIfCancellationRequested();
            }
            catch (TaskCanceledException e) when (!token.IsCancellationRequested)
            {
                throw new TimeoutException("Connection timed out", e);
            }
        }
    }
}
