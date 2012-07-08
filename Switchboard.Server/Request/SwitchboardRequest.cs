using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Switchboard.Server
{
    public class SwitchboardRequest
    {
        private static long requestCounter;

        public long RequestId { get; private set; }

        public Version ProtocolVersion { get; set; }

        public string Method { get; set; }

        public WebHeaderCollection Headers { get; set; }

        public string RequestUri { get; set; }

        public Stream RequestBody { get; set; }
        public bool IsRequestBuffered { get; private set; }

        public int ContentLength
        {
            get
            {
                var clHeader = Headers.Get("Content-Length");

                if (clHeader == null)
                    return 0;

                int cl;

                if (!int.TryParse(clHeader, out cl))
                    return 0;

                return cl;
            }
        }

        public SwitchboardRequest()
        {
            this.Headers = new WebHeaderCollection();
            this.RequestId = Interlocked.Increment(ref requestCounter);
        }

        public async Task CloseAsync()
        {
            if (this.ContentLength > 0 && this.RequestBody != null && this.RequestBody.CanRead)
            {
                var buf = new byte[8192];

                int c;

                while ((c = await this.RequestBody.ReadAsync(buf, 0, buf.Length).ConfigureAwait(false)) > 0)
                    continue;
            }
        }

        public async Task BufferRequestAsync()
        {
            if (IsRequestBuffered)
                return;

            if (this.RequestBody == null)
                return;

            var ms = new MemoryStream();

            await this.RequestBody.CopyToAsync(ms);

            this.RequestBody = ms;
            ms.Seek(0, SeekOrigin.Begin);

            this.IsRequestBuffered = true;
        }

    }
}
