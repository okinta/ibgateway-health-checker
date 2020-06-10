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
        /// <param name="millisecondsTimeout">The total milliseconds to wait before
        /// throwing a TimeoutException.</param>
        /// <param name="token">The optional token to check for canceling the
        /// connection.</param>
        /// <exception cref="TimeoutException">If <paramref name="millisecondsTimeout"/>
        /// passes without the connection establishing.</exception>
        /// <exception cref="TaskCanceledException">If the task is canceled via
        /// <paramref name="token"/>.</exception>
        public static async Task EnsureConnectedAsync(
            this ITwsControllerBase tws, int millisecondsTimeout,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            var timeoutToken = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeoutToken.CancelAfter(millisecondsTimeout);

            try
            {
                await tws.EnsureConnectedAsync().WaitAsync(token);
                token.ThrowIfCancellationRequested();
            }
            catch (TaskCanceledException e) when (timeoutToken.IsCancellationRequested)
            {
                throw new TimeoutException(
                    $"Timed out after {millisecondsTimeout} milliseconds", e);
            }
        }
    }
}
