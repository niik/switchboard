using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Switchboard.Server.Utils
{
    /// <summary>
    /// Simple wrapping stream which prevents reading more than the specified maximum length.
    /// Also prevents seeking. Support sync and async reads.
    /// </summary>
    internal class MaxReadStream : RedirectingStream
    {
        private class EmptyAsyncResult : IAsyncResult
        {
            public object AsyncState { get; set; }
            public WaitHandle AsyncWaitHandle { get; set; }
            public bool CompletedSynchronously { get { return true; } }
            public bool IsCompleted { get { return true; } }
        }

        int read = 0;
        int maxLength;

        private int Left { get { return maxLength - read; } }

        public MaxReadStream(Stream innerStream, int maxLength)
            : base(innerStream)
        {
            this.maxLength = maxLength;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int left = this.Left;

            if (left <= 0)
                return 0;

            if (count > left)
                count = left;

            int c = base.Read(buffer, offset, count);
            read += c;

            return c;
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            int left = this.Left;

            if (left <= 0)
            {
                var ar = new EmptyAsyncResult();
                ar.AsyncState = state;
                ar.AsyncWaitHandle = new ManualResetEvent(true);

                callback(ar);
                return ar;
            }

            if (count > left)
                count = left;

            return base.BeginRead(buffer, offset, count, callback, state);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int left = this.Left;

            if (left <= 0)
                return 0;

            if (count > left)
                count = left;

            int c = await base.ReadAsync(buffer, offset, count, cancellationToken);

            this.read += c;

            return c;
        }

        public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[bufferSize];

            int c;

            while ((c = await this.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await destination.WriteAsync(buffer, 0, c);
                cancellationToken.ThrowIfCancellationRequested();
            }

        }

        public override bool CanRead
        {
            get
            {
                return this.Left > 0;
            }
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            if (asyncResult is EmptyAsyncResult)
                return 0;

            int c = base.EndRead(asyncResult);
            read += c;

            return c;
        }

        public override int ReadByte()
        {
            if (Left > 0)
            {
                read++;
                return base.ReadByte();
            }
            else
            {
                throw new EndOfStreamException();
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }
    }
}
