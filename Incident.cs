using Newtonsoft.Json;
using RestSharp;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace IbGatewayHealthChecker
{
    /// <summary>
    /// Describes methods to manage a pageable incident.
    /// </summary>
    internal class Incident
    {
        /// <summary>
        /// Gets the description of the incident.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the ID of the incident.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the title of the incident.
        /// </summary>
        public string Title { get; }

        private const string PagerTreeUrlBase =
            "https://api.pagertree.com/integration/int_{0}";
        private RestClient Client { get; }

        /// <summary>
        /// Instantiates a new incident.
        /// </summary>
        /// <param name="pagerTreeIntId">The PagerTree integration ID to notify.</param>
        /// <param name="title">The title of the incident.</param>
        /// <param name="description">The description of the incident.</param>
        public Incident(string pagerTreeIntId, string title, string description)
        {
            if (string.IsNullOrEmpty(pagerTreeIntId))
                throw new ArgumentNullException(
                    nameof(pagerTreeIntId), "pagerTreeIntId must be provided");

            if (string.IsNullOrEmpty(title))
                throw new ArgumentNullException(
                    nameof(title), "title must be provided");

            if (string.IsNullOrEmpty(description))
                throw new ArgumentNullException(
                    nameof(description), "description must be provided");

            Client = new RestClient(string.Format(PagerTreeUrlBase, pagerTreeIntId));
            Description = description;
            Id = Guid.NewGuid().ToString();
            Title = title;
        }

        /// <summary>
        /// Notifies PagerTree of the incident.
        /// </summary>
        /// <param name="token">The token to check for canceling the async request.</param>
        public async Task Notify(CancellationToken token = default)
        {
            await Client.PostAsync<string>(CreateRequest(
                new Dictionary<string, string>
                {
                    {"event_type", "create"},
                    {"Id", Id},
                    {"Title", Title},
                    {"Description", Description}
                }), token);
        }

        /// <summary>
        /// Notifies PagerTree that the incident has been resolved.
        /// </summary>
        /// <param name="token">The token to check for canceling the async request.</param>
        public async Task Resolve(CancellationToken token = default)
        {
            await Client.PostAsync<string>(CreateRequest(
                new Dictionary<string, string>
                {
                    {"event_type", "resolve"},
                    {"Id", Id},
                }), token);
        }

        /// <summary>
        /// Creates a new IRestRequest instance for a PagerTree integration incident.
        /// </summary>
        /// <param name="data">The collection of arguments to send as JSON via POST to
        /// PagerTree.</param>
        /// <returns>The instantiated IRestRequest instance.</returns>
        private static IRestRequest CreateRequest(IDictionary<string, string> data)
        {
            var request = new RestRequest(Method.POST) { RequestFormat = DataFormat.Json };
            var json = JsonConvert.SerializeObject(data);
            request.AddParameter(
                "application/json; charset=utf-8", json, ParameterType.RequestBody);
            return request;
        }
    }
}
