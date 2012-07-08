using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Switchboard.Server.Connection;

namespace Switchboard.Server
{
    public class SwitchboardContext
    {
        private static long contextCounter;
        public long ContextId { get; private set; }

        public InboundConnection InboundConnection { get; private set; }
        public OutboundConnection OutboundConnection { get; private set; }

        private Timer CheckForDisconnectTimer;

        public SwitchboardContext(InboundConnection client)
        {
            this.InboundConnection = client;
            this.ContextId = Interlocked.Increment(ref contextCounter);
            this.CheckForDisconnectTimer = new Timer(CheckForDisconnect, null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        }

        private void CheckForDisconnect(object state)
        {
            // TODO
        }

        public Task<OutboundConnection> OpenSecureOutboundConnectionAsync(IPEndPoint endPoint, string targetHost)
        {
            return OpenOutboundConnectionAsync(endPoint, true, (ep) => new SecureOutboundConnection(targetHost, ep));
        }

        public Task<OutboundConnection> OpenOutboundConnectionAsync(IPEndPoint endPoint)
        {
            return OpenOutboundConnectionAsync(endPoint, false, (ep) => new OutboundConnection(ep));
        }

        private async Task<OutboundConnection> OpenOutboundConnectionAsync<T>(IPEndPoint endPoint, bool secure, Func<IPEndPoint, T> connectionFactory) where T: OutboundConnection
        {
            if (this.OutboundConnection != null)
            {
                if (!this.OutboundConnection.RemoteEndPoint.Equals(endPoint))
                {
                    Debug.WriteLine("{0}: Current outbound connection is for {1}, can't reuse for {2}", InboundConnection.RemoteEndPoint, this.OutboundConnection.RemoteEndPoint, endPoint);
                    this.OutboundConnection.Close();
                    this.OutboundConnection = null;
                }
                else if (this.OutboundConnection.IsSecure != secure)
                {
                    Debug.WriteLine("{0}: Current outbound connection {0} secure, can't reuse", InboundConnection.RemoteEndPoint, this.OutboundConnection.IsSecure ? "is" : "is not");
                    this.OutboundConnection.Close();
                    this.OutboundConnection = null;
                }
                else
                {
                    if (this.OutboundConnection.IsConnected)
                    {
                        Debug.WriteLine("{0}: Reusing outbound connection to {1}", InboundConnection.RemoteEndPoint, this.OutboundConnection.RemoteEndPoint);
                        return this.OutboundConnection;
                    }
                    else
                    {
                        Debug.WriteLine("{0}: Detected stale outbound connection, recreating", InboundConnection.RemoteEndPoint, this.OutboundConnection.RemoteEndPoint);
                        this.OutboundConnection.Close();
                        this.OutboundConnection = null;
                    }
                }
            }

            var conn = connectionFactory(endPoint);

            await conn.OpenAsync().ConfigureAwait(false);

            Debug.WriteLine("{0}: Outbound connection to {1} established", InboundConnection.RemoteEndPoint, conn.RemoteEndPoint);

            this.OutboundConnection = conn;

            return conn;
        }

        public async Task<OutboundConnection> OpenOutboundConnectionAsync(Task<OutboundConnection> openTask)
        {
            var conn = await openTask.ConfigureAwait(false);

            this.OutboundConnection = conn;

            return conn;
        }

        internal void Close()
        {
            if (this.InboundConnection.IsConnected)
                this.InboundConnection.Close();

            if (this.OutboundConnection != null && this.OutboundConnection.IsConnected)
                this.OutboundConnection.Close();
        }

        internal void Dispose()
        {
            this.Close();
        }
    }
}
