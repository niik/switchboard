using System;
using System.IO;
using System.Threading.Tasks;
using Switchboard.Server.Utils;
using Switchboard.Server.Utils.HttpParser;

namespace Switchboard.Server
{
    internal class SwitchboardResponseParser
    {
        private sealed class ParseDelegate : IHttpResponseHandler
        {
            public bool headerComplete;
            public SwitchboardResponse response = new SwitchboardResponse();
            public ArraySegment<byte> responseBodyStart;

            public void OnResponseBegin() { }

            public void OnStatusLine(Version protocolVersion, int statusCode, string statusDescription)
            {
                response.ProtocolVersion = protocolVersion;
                response.StatusCode = statusCode;
                response.StatusDescription = statusDescription;
            }

            public void OnHeader(string name, string value)
            {
                response.Headers.Add(name, value);
            }

            public void OnEntityStart()
            {
            }

            public void OnHeadersEnd()
            {
                this.headerComplete = true;
            }

            public void OnEntityData(byte[] buffer, int offset, int count)
            {
                this.responseBodyStart = new ArraySegment<byte>(buffer, offset, count);
            }

            public void OnEntityEnd()
            {
            }

            public void OnResponseEnd()
            {
            }
        }

        public SwitchboardResponseParser()
        {
        }

        public async Task<SwitchboardResponse> ParseAsync(Stream stream)
        {
            var del = new ParseDelegate();
            var parser = new HttpResponseParser(del);

            int read;
            byte[] buffer = new byte[8192];

            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                parser.Execute(buffer, 0, read);

                if (del.headerComplete)
                    break;
            }

            if (!del.headerComplete)
                throw new FormatException("Parse error in response");

            var response = del.response;
            int cl = response.ContentLength;

            if (cl > 0)
            {
                if (del.responseBodyStart.Count > 0)
                {
                    response.ResponseBody = new MaxReadStream(new StartAvailableStream(del.responseBodyStart, stream), cl);
                }
                else
                {
                    response.ResponseBody = new MaxReadStream(stream, cl);
                }
            }
            else if (response.Headers["Transfer-Encoding"] == "chunked")
            {
                if (response.Headers["Connection"] == "close")
                {
                    if (del.responseBodyStart.Count > 0)
                    {
                        response.ResponseBody = new StartAvailableStream(del.responseBodyStart, stream);
                    }
                    else
                    {
                        response.ResponseBody = stream;
                    }
                }
                else
                {
                    if (del.responseBodyStart.Count > 0)
                    {
                        response.ResponseBody = new ChunkedStream(new StartAvailableStream(del.responseBodyStart, stream));
                    }
                    else
                    {
                        response.ResponseBody = new ChunkedStream(stream);
                    }
                }
            }

            return response;
        }
    }
}
