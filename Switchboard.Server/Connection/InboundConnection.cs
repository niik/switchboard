using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Switchboard.Server.Utils;

namespace Switchboard.Server.Connection
{
    public class InboundConnection : SwitchboardConnection
    {
        private static long connectionCounter;
        protected static readonly Encoding headerEncoding = Encoding.GetEncoding("us-ascii");
        public override bool IsSecure { get { return false; } }

        public long ConnectionId;

        protected TcpClient connection;
        protected NetworkStream networkStream;

        public bool IsConnected
        {
            get
            {
                if (!connection.Connected)
                    return false;

                try
                {
                    return !(connection.Client.Poll(1, SelectMode.SelectRead) && connection.Client.Available == 0);
                }
                catch (SocketException) { return false; }
                //return connection.Connected; 
            }
        }

        public IPEndPoint RemoteEndPoint { get; private set; }

        public InboundConnection(TcpClient connection)
        {
            this.connection = connection;
            this.networkStream = connection.GetStream();
            this.ConnectionId = Interlocked.Increment(ref connectionCounter);
            this.RemoteEndPoint = (IPEndPoint)connection.Client.RemoteEndPoint;
        }

        public virtual Task OpenAsync()
        {
            return this.OpenAsync(CancellationToken.None);
        }

        public virtual Task OpenAsync(CancellationToken ct)
        {
            return Task.FromResult<VoidTypeStruct>(default(VoidTypeStruct));
        }

        protected virtual Stream GetReadStream()
        {
            return this.networkStream;
        }

        protected virtual Stream GetWriteStream()
        {
            return this.networkStream;
        }


        public Task<SwitchboardRequest> ReadRequestAsync()
        {
            return ReadRequestAsync(CancellationToken.None);
        }

        public Task<SwitchboardRequest> ReadRequestAsync(CancellationToken ct)
        {
            var requestParser = new SwitchboardRequestParser();

            return requestParser.ParseAsync(this, this.GetReadStream());
        }

        public async Task WriteResponseAsync(SwitchboardResponse response)
        {
            await WriteResponseAsync(response, CancellationToken.None)
                .ConfigureAwait(false);
        }

        public async Task WriteResponseAsync(SwitchboardResponse response, CancellationToken ct)
        {
            var ms = new MemoryStream();
            var sw = new StreamWriter(ms, headerEncoding);

            sw.NewLine = "\r\n";
            sw.WriteLine("HTTP/{0} {1} {2}", response.ProtocolVersion, response.StatusCode, response.StatusDescription);

            for (int i = 0; i < response.Headers.Count; i++)
                sw.WriteLine("{0}: {1}", response.Headers.GetKey(i), response.Headers.Get(i));

            sw.WriteLine();
            sw.Flush();

            var writeStream = this.GetWriteStream();

            await writeStream.WriteAsync(ms.GetBuffer(), 0, (int)ms.Length).ConfigureAwait(false);
            Debug.WriteLine("{0}: Wrote headers ({1}b)", this.RemoteEndPoint, ms.Length);

            if (response.ResponseBody != null && response.ResponseBody.CanRead)
            {
                byte[] buffer = new byte[8192];
                int read;
                long written = 0;

                while ((read = await response.ResponseBody.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                {
                    written += read;
                    Debug.WriteLine("{0}: Read {1:N0} bytes from response body", this.RemoteEndPoint, read);
                    await writeStream.WriteAsync(buffer, 0, read).ConfigureAwait(false);
                    Debug.WriteLine("{0}: Wrote {1:N0} bytes to client", this.RemoteEndPoint, read);
                }

                Debug.WriteLine("{0}: Wrote response body ({1:N0} bytes) to client", this.RemoteEndPoint, written);

            }

            await writeStream.FlushAsync().ConfigureAwait(false);
        }

        public void Close()
        {
            connection.Close();
        }

    }
}
