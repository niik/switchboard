using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Switchboard.Server.Connection;

namespace Switchboard.Server
{
    public class SwitchboardServer
    {
        private ISwitchboardRequestHandler handler;
        private TcpListener server;
        private Task workTask;
        private bool stopping;
        private Timer connectivityTimer;

        public SwitchboardServer(IPEndPoint listenEp, ISwitchboardRequestHandler handler)
        {
            this.server = new TcpListener(listenEp);
            this.handler = handler;
        }

        public void Start()
        {
            this.server.Start();
            this.workTask = Run(CancellationToken.None);
        }

        private async Task Run(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                var client = await this.server.AcceptTcpClientAsync();

                var inbound = await CreateInboundConnection(client);
                await inbound.OpenAsync();

                Debug.WriteLine(string.Format("{0}: Connected", inbound.RemoteEndPoint));

                var context = new SwitchboardContext(inbound);

                HandleSession(context);
            }
        }

        protected virtual Task<InboundConnection> CreateInboundConnection(TcpClient client)
        {
            return Task.FromResult<InboundConnection>(new InboundConnection(client));
        }

        private async void HandleSession(SwitchboardContext context)
        {
            try
            {
                Debug.WriteLine("{0}: Starting session", context.InboundConnection.RemoteEndPoint);

                do
                {
                    var request = await context.InboundConnection.ReadRequestAsync().ConfigureAwait(false);

                    if (request == null)
                        return;

                    Debug.WriteLine(string.Format("{0}: Got {1} request for {2}", context.InboundConnection.RemoteEndPoint, request.Method, request.RequestUri));

                    var response = await handler.GetResponseAsync(context, request).ConfigureAwait(false);
                    Debug.WriteLine(string.Format("{0}: Got response from handler ({1})", context.InboundConnection.RemoteEndPoint, response.StatusCode));

                    await context.InboundConnection.WriteResponseAsync(response).ConfigureAwait(false);
                    Debug.WriteLine(string.Format("{0}: Wrote response to client", context.InboundConnection.RemoteEndPoint));

                    if (context.OutboundConnection != null && !context.OutboundConnection.IsConnected)
                        context.Close();

                } while (context.InboundConnection.IsConnected);
            }
            catch (Exception exc)
            {
                Debug.WriteLine(string.Format("{0}: Error: {1}", context.InboundConnection.RemoteEndPoint, exc.Message));
                context.Close();
                Debug.WriteLine(string.Format("{0}: Closed context", context.InboundConnection.RemoteEndPoint, exc.Message));
            }
            finally
            {
                context.Dispose();
            }
        }

        private Task<TcpClient> AcceptOneClient()
        {
            throw new NotImplementedException();
        }
    }
}
