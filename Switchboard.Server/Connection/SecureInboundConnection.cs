using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Switchboard.Server.Connection
{
    public class SecureInboundConnection : InboundConnection
    {
        private X509Certificate certificate;
        protected SslStream SslStream { get; private set; }
        public override bool IsSecure { get { return true; } }

        public SecureInboundConnection(TcpClient client, X509Certificate certificate)
            : base(client)
        {
            this.certificate = certificate;
        }

        public override async Task OpenAsync(System.Threading.CancellationToken ct)
        {
            await base.OpenAsync(ct);

            this.SslStream = CreateSslStream(base.networkStream);

            await this.SslStream.AuthenticateAsServerAsync(certificate);
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
