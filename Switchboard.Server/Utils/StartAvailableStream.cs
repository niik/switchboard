using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Switchboard.Server.Utils
{
    /// <summary>
    /// Used internally by request/response parsers to merge a piece of a buffer and the
    /// rest of the request/response stream.
    /// </summary>
    internal class StartAvailableStream : Stream
    {
        private Stream stream;

        private bool inStream;
        private MemoryStream buffer;

        public StartAvailableStream(ArraySegment<byte> startBuffer, Stream continuationStream)
            : this(startBuffer.Array, startBuffer.Offset, startBuffer.Count, continuationStream)
        {
        }

        public StartAvailableStream(byte[] startBuffer, int offset, int count, Stream continuationStream)
        {
            this.buffer = new MemoryStream(startBuffer, offset, count);
            this.stream = continuationStream;
        }

        public override bool CanRead
        {
            get { return !inStream || stream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void Flush()
        {
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<VoidTypeStruct>(default(VoidTypeStruct));
        }

        public override long Length
        {
            get { return this.buffer.Length + this.stream.Length; }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!inStream && this.buffer.Position == this.buffer.Length)
                inStream = true;

            if (inStream)
                return this.stream.Read(buffer, offset, count);
            else
                return this.buffer.Read(buffer, offset, count);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            if (!inStream && this.buffer.Position == this.buffer.Length)
                inStream = true;

            if (inStream)
            {
                return stream.BeginRead(buffer, offset, count, callback, state);
            }
            else
            {
                return this.buffer.BeginRead(buffer, offset, count, callback, state);
            }
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            if (inStream)
            {
                return stream.EndRead(asyncResult);
            }
            else
            {
                return this.buffer.EndRead(asyncResult);
            }
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (!inStream && this.buffer.Position == this.buffer.Length)
                inStream = true;

            if (inStream)
                return stream.ReadAsync(buffer, offset, count);
            else
                return this.buffer.ReadAsync(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
