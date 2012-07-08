using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Switchboard.Server.Utils.HttpParser
{
    /// <summary>
    /// Simple and naive http response parser. No support for chunked transfers or
    /// line folding in headers. Should probably not be used to read responses from non-friendly
    /// servers yet.
    /// 
    /// TODO:
    ///  * Proper chunked transfer support
    ///  * Proper support for RFC tokens, adhere to the BNF
    ///  * Hardening against maliciously crafted responses.
    ///  * 
    /// </summary>
    public class HttpResponseParser
    {
        private bool inEntityData;
        private bool inHeaders;

        private bool hasEntityData;

        private bool hasStarted;
        private bool isCompleted;

        private byte[] parseBuffer;
        private int parseBufferWritten;

        private IHttpResponseHandler handler;

        private int contentLength = -1;
        private int entityDataWritten = 0;

        public HttpResponseParser(IHttpResponseHandler handler)
        {
            this.handler = handler;
            this.parseBuffer = new byte[64 * 1024];
        }

        public void Execute(byte[] buffer, int offset, int count)
        {
            if (isCompleted)
                throw new InvalidOperationException("Parser is done");

            if (!hasStarted)
            {
                inHeaders = true;
                hasStarted = true;

                this.handler.OnResponseBegin();
            }

            if (!inHeaders)
            {
                if (!hasEntityData)
                {
                    this.isCompleted = true;
                    this.handler.OnResponseEnd();
                    return;
                }

                if (!inEntityData)
                {
                    inEntityData = true;
                    handler.OnEntityStart();
                }

                if (count > 0)
                {
                    this.handler.OnEntityData(buffer, offset, count);
                    this.entityDataWritten += count;
                }

                if (count == 0 || this.entityDataWritten == this.contentLength)
                {
                    inEntityData = false;
                    isCompleted = true;
                    this.handler.OnEntityEnd();
                    this.handler.OnResponseEnd();
                }

                return;
            }

            int bufferLeft = parseBuffer.Length - parseBufferWritten;

            if (bufferLeft <= 0)
                throw new FormatException("Response headers exceeded maximum allowed length");

            if (count > bufferLeft)
            {
                this.Execute(buffer, offset, bufferLeft);
                this.Execute(buffer, offset + bufferLeft, count - bufferLeft);

                return;
            }

            Array.Copy(buffer, offset, parseBuffer, parseBufferWritten, count);
            parseBufferWritten += count;

            int endOfHeaders = IndexOf(parseBuffer, 0, parseBufferWritten, 13, 10, 13, 10);

            if (endOfHeaders >= 0)
            {
                ParseHeaders(parseBuffer, 0, endOfHeaders + 4);

                this.inHeaders = false;

                if (endOfHeaders + 4 < parseBufferWritten)
                    this.Execute(parseBuffer, endOfHeaders + 4, parseBufferWritten - (endOfHeaders + 4));
                else
                {
                    if (!hasEntityData)
                    {
                        this.isCompleted = true;
                        this.handler.OnResponseEnd();
                        return;
                    }
                }
            }
        }

        private void ParseHeaders(byte[] buffer, int offset, int count)
        {
            using (var ms = new MemoryStream(buffer, offset, count))
            using (var sr = new StreamReader(ms, Encoding.GetEncoding("us-ascii")))
            {
                ParseStatusLine(sr.ReadLine());

                string line;

                while (!string.IsNullOrEmpty(line = sr.ReadLine()))
                    ParseHeaderLine(line);

                this.handler.OnHeadersEnd();

                hasEntityData = this.contentLength > 0 || chunkedTransfer;
            }
        }

        private static Regex StatusLineRegex = new Regex(@"^HTTP/(?<version>\d\.\d) (?<statusCode>\d{3}) (?<statusDescription>.*)");
        private bool chunkedTransfer;

        private void ParseStatusLine(string line)
        {
            if (line == null)
                throw new ArgumentNullException("line");

            var m = StatusLineRegex.Match(line);

            if (!m.Success)
                throw new FormatException("Malformed status line");

            var version = m.Groups["version"].Value;

            if (version != "1.1" && version != "1.0")
                throw new FormatException("Unknown http version");

            int statusCode = int.Parse(m.Groups["statusCode"].Value);
            string statusDescription = m.Groups["statusDescription"].Value;

            this.handler.OnStatusLine(new Version(version), statusCode, statusDescription);
        }

        private void ParseHeaderLine(string line)
        {
            var parts = line.Split(new[] { ':' }, 2);

            if (parts.Length != 2)
                throw new FormatException("Malformed header line");

            parts[1] = parts[1].Trim();

            if (parts[0] == "Content-Length")
            {
                int cl;
                if (int.TryParse(parts[1].Trim(), out cl))
                    this.contentLength = cl;
            }
            else if (parts[0] == "Transfer-Encoding")
            {
                if (parts[1] == "chunked")
                    this.chunkedTransfer = true;
            }

            this.handler.OnHeader(parts[0], parts[1]);
        }

        private int IndexOf(byte[] buffer, int offset, int count, params byte[] elements)
        {
            for (int i = offset; i < offset + count; i++)
            {
                int j = 0;
                for (; j < elements.Length && i + j < offset + count && buffer[i + j] == elements[j]; j++) ;

                if (j == elements.Length)
                    return i;
            }

            return -1;
        }
    }

}
