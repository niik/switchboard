﻿using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using HttpMachine;
using Switchboard.Server.Connection;
using Switchboard.Server.Utils;

namespace Switchboard.Server
{
    internal class SwitchboardRequestParser
    {
        private sealed class ParseDelegate : IHttpRequestParserDelegate
        {
            private string headerName;

            public SwitchboardRequest request = new SwitchboardRequest();
            public ArraySegment<byte> requestBodyStart;
            public bool complete;
            public bool headerComplete;

			public void OnBody(HttpParser parser, ArraySegment<byte> data) { requestBodyStart = data; }
			public void OnFragment(HttpParser parser, string fragment) { }
			public void OnHeaderName(HttpParser parser, string name) { headerName = name; }
			public void OnHeaderValue(HttpParser parser, string value) { request.Headers.Add(headerName, value); }
			public void OnHeadersEnd(HttpParser parser) { this.headerComplete = true; }
			public void OnMessageBegin(HttpParser parser) { }
			public void OnMessageEnd(HttpParser parser) { this.complete = true; }
			public void OnMethod(HttpParser parser, string method) { request.Method = method; }
			public void OnQueryString(HttpParser parser, string queryString) { }
			public void OnRequestUri(HttpParser parser, string requestUri) { request.RequestUri = requestUri; }

			#region IHttpRequestParserDelegate implementation

			public void OnPath (HttpParser parser, string path)
			{
				//throw new NotImplementedException ();
			}

			#endregion
        }

        public SwitchboardRequestParser()
        {
        }

        public async Task<SwitchboardRequest> ParseAsync(InboundConnection conn, Stream stream)
        {
            var del = new ParseDelegate();
            var parser = new HttpParser(del);

            int read;
            int readTotal = 0;
            byte[] buffer = new byte[8192];

            Debug.WriteLine(string.Format("{0}: RequestParser starting", conn.RemoteEndPoint));

            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
            {
                readTotal += read;

                if (parser.Execute(new ArraySegment<byte>(buffer, 0, read)) != read)
                    throw new FormatException("Parse error in request");

                if (del.headerComplete)
                    break;
            }

            Debug.WriteLine(string.Format("{0}: RequestParser read enough ({1} bytes)", conn.RemoteEndPoint, readTotal));

            if (readTotal == 0)
                return null;

            if (!del.headerComplete)
                throw new FormatException("Parse error in request");

            var request = del.request;

            request.ProtocolVersion = new Version(parser.MajorVersion, parser.MinorVersion);

            int cl = request.ContentLength;

            if (cl > 0)
            {
                if (del.requestBodyStart.Count > 0)
                {
                    request.RequestBody = new MaxReadStream(new StartAvailableStream(del.requestBodyStart, stream), cl);
                }
                else
                {
                    request.RequestBody = new MaxReadStream(stream, cl);
                }
            }

            return request;
        }
    }
}
