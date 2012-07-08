using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Switchboard.Server;

namespace Switchboard.ConsoleHost
{
    /// <summary>
    /// Sample implementation of a reverse proxy. Streams requests and responses (no buffering).
    /// No support for location header rewriting.
    /// </summary>
    public class SimpleReverseProxyHandler : ISwitchboardRequestHandler
    {
        private Uri backendUri;

        public bool RewriteHost { get; set; }
        public bool AddForwardedForHeader { get; set; }

        public SimpleReverseProxyHandler(string backendUri)
            : this(new Uri(backendUri))
        {
        }

        public SimpleReverseProxyHandler(Uri backendUri)
        {
            this.backendUri = backendUri;

            this.RewriteHost = true;
            this.AddForwardedForHeader = true;
        }

        public async Task<SwitchboardResponse> GetResponseAsync(SwitchboardContext context, SwitchboardRequest request)
        {
            var originalHost = request.Headers["Host"];

            if (this.RewriteHost)
                request.Headers["Host"] = this.backendUri.Host + (this.backendUri.IsDefaultPort ? string.Empty : ":" + this.backendUri.Port);

            if (this.AddForwardedForHeader)
                SetForwardedForHeader(context, request);

            var sw = Stopwatch.StartNew();

            IPAddress ip;

            if(this.backendUri.HostNameType == UriHostNameType.IPv4) {
                ip = IPAddress.Parse(this.backendUri.Host);
            }
            else {
                var ipAddresses = await Dns.GetHostAddressesAsync(this.backendUri.Host);
                ip = ipAddresses[0];
            }

            var backendEp = new IPEndPoint(ip, this.backendUri.Port);

            Debug.WriteLine("{0}: Resolved upstream server to {1} in {2}ms, opening connection", context.InboundConnection.RemoteEndPoint, backendEp, sw.Elapsed.TotalMilliseconds);

            if (this.backendUri.Scheme != "https")
                await context.OpenOutboundConnectionAsync(backendEp);
            else
                await context.OpenSecureOutboundConnectionAsync(backendEp, this.backendUri.Host);

            Debug.WriteLine("{0}: Outbound connection established, sending request", context.InboundConnection.RemoteEndPoint);
            sw.Restart();
            await context.OutboundConnection.WriteRequestAsync(request);
            Debug.WriteLine("{0}: Handler sent request in {1}ms", context.InboundConnection.RemoteEndPoint, sw.Elapsed.TotalMilliseconds);

            var response = await context.OutboundConnection.ReadResponseAsync();

            return response;
        }

        private void SetForwardedForHeader(SwitchboardContext context, SwitchboardRequest request)
        {
            string remoteAddress = context.InboundConnection.RemoteEndPoint.Address.ToString();
            string currentForwardedFor = request.Headers["X-Forwarded-For"];

            request.Headers["X-Forwarded-For"] = string.IsNullOrEmpty(currentForwardedFor) ? remoteAddress : currentForwardedFor + ", " + remoteAddress;
        }
    }
}
