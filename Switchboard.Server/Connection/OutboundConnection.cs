using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Switchboard.Server.Connection
{
    public class OutboundConnection : SwitchboardConnection
    {
        protected static readonly Encoding headerEncoding = Encoding.GetEncoding("us-ascii");

        public IPEndPoint RemoteEndPoint { get; private set; }
        public override bool IsSecure { get { return false; } }

        public bool IsConnected
        {
            get { return connection.Connected; }
        }

        protected TcpClient connection;
        protected NetworkStream networkStream;

        public OutboundConnection(IPEndPoint endPoint)
        {
            this.RemoteEndPoint = endPoint;
            this.connection = new TcpClient();
        }

        public Task OpenAsync()
        {
            return OpenAsync(CancellationToken.None);
        }

        public virtual async Task OpenAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            await this.connection.ConnectAsync(this.RemoteEndPoint.Address, this.RemoteEndPoint.Port);

            this.networkStream = this.connection.GetStream();
        }

        public Task WriteRequestAsync(SwitchboardRequest request)
        {
            return WriteRequestAsync(request, CancellationToken.None);
        }

        public async Task WriteRequestAsync(SwitchboardRequest request, CancellationToken ct)
        {
            var writeStream = this.GetWriteStream();

            var ms = new MemoryStream();
            var sw = new StreamWriter(ms, headerEncoding);

            sw.NewLine = "\r\n";
            sw.WriteLine("{0} {1} HTTP/1.{2}", request.Method, request.RequestUri, request.ProtocolVersion.Minor);

            for (int i = 0; i < request.Headers.Count; i++)
                sw.WriteLine("{0}: {1}", request.Headers.GetKey(i), request.Headers.Get(i));

            sw.WriteLine();
            sw.Flush();

            await writeStream.WriteAsync(ms.GetBuffer(), 0, (int)ms.Length)
                .ConfigureAwait(continueOnCapturedContext: false);

            if (request.RequestBody != null)
            {
                await request.RequestBody.CopyToAsync(writeStream)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }

            await writeStream.FlushAsync()
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        protected virtual Stream GetWriteStream()
        {
            return this.networkStream;
        }

        protected virtual Stream GetReadStream()
        {
            return this.networkStream;
        }

        public Task<SwitchboardResponse> ReadResponseAsync()
        {
            var parser = new SwitchboardResponseParser();
            return parser.ParseAsync(this.GetReadStream());
        }

        public void Close()
        {
            connection.Close();
        }
    }
}
