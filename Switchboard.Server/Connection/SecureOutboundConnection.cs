using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;

namespace Switchboard.Server.Connection
{
    public class SecureOutboundConnection : OutboundConnection
    {
        public string TargetHost { get; set; }
        protected SslStream SslStream { get; private set; }
        public override bool IsSecure { get { return true; } }

        public SecureOutboundConnection(string targetHost, IPEndPoint ep)
            : base(ep)
        {
            this.TargetHost = targetHost;
        }

        public override async Task OpenAsync(System.Threading.CancellationToken ct)
        {
            await base.OpenAsync(ct);

            this.SslStream = CreateSslStream(base.networkStream);

            await this.SslStream.AuthenticateAsClientAsync(this.TargetHost);
        }

        protected virtual SslStream CreateSslStream(Stream innerStream)
        {
            return new SslStream(base.networkStream, leaveInnerStreamOpen: true);
        }

        protected override Stream GetWriteStream()
        {
            return this.SslStream;
        }

        protected override Stream GetReadStream()
        {
            return this.SslStream;
        }
    }
}
