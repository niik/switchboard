using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Switchboard.Server.Utils
{
    internal class ChunkedStream : Stream
    {
        private Stream innerStream;
        private bool inChunkHeader = true;
        private int chunkHeaderPosition;
        private bool inChunkHeaderLength = true;
        private int chunkLength;
        private int chunkRead;
        private bool done;
        private bool inChunkTrailingCrLf;
        private int chunkTrailingCrLfPosition;
        private bool inChunk;

        private int chunkLeft { get { return chunkLength - chunkRead; } }

        public override bool CanRead { get { return !this.done; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return false; } }

        public ChunkedStream(Stream innerStream)
        {
            this.innerStream = innerStream;
        }

        public override void Flush()
        {
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (this.done)
                return 0;

            count = Math.Min(OptimizeCount(count), count);

            int read = await this.innerStream.ReadAsync(buffer, offset, count, cancellationToken);

            this.Execute(buffer, offset, read);

            return read;
        }

        private int OptimizeCount(int count)
        {
            if (!inChunkHeader)
            {
                if (count > chunkLeft + 2)
                    count = chunkLeft + 2;
            }
            else
            {
                if (chunkHeaderPosition == 0)
                    count = 3;
                else
                {
                    if (inChunkHeaderLength)
                        count = 2;
                    else
                        count = chunkLength + 3;
                }
            }
            return count;
        }

        private void Execute(byte[] buffer, int offset, int count)
        {
            for (int i = offset; i < offset + count; i++)
            {
                if (this.done)
                    break;

                byte b = buffer[i];

                if (this.inChunkHeader)
                {
                    for (; i < offset + count; i++)
                    {
                        b = buffer[i];

                        if (this.inChunkHeaderLength)
                        {
                            if (b == 13)
                                this.inChunkHeaderLength = false;
                            else
                                this.chunkLength = (this.chunkLength << 4) + FromHex(b);
                            
                            this.chunkHeaderPosition++;
                        }
                        else
                        {
                            if (b != 10)
                                throw new FormatException("Malformed chunk header");

                            this.inChunkHeader = false;
                            this.inChunk = true;
                            this.chunkHeaderPosition = 0;

                            break;
                        }
                    }
                }
                else if (this.inChunkTrailingCrLf)
                {
                    if (this.chunkTrailingCrLfPosition == 0 && b != 13 || this.chunkTrailingCrLfPosition == 1 && b != 10)
                        throw new FormatException("Malformed chunk header");

                    if (this.chunkTrailingCrLfPosition == 1)
                    {
                        this.inChunkTrailingCrLf = false;
                        this.chunkTrailingCrLfPosition = 0;

                        this.inChunkHeader = true;
                        this.inChunkHeaderLength = true;

                        if (chunkLength == 0)
                            this.done = true;

                        this.chunkLength = 0;
                    }
                    else
                    {
                        this.chunkTrailingCrLfPosition++;
                    }

                }
                else if (this.inChunk)
                {
                    for (; i < offset + count; i++)
                    {
                        this.chunkRead++;

                        if (chunkRead == this.chunkLength)
                        {
                            this.inChunk = false;
                            this.inChunkTrailingCrLf = true;
                            this.chunkRead = 0;
                            break;
                        }
                    }
                }
            }
        }

        private int FromHex(byte b)
        {
            // 0-9
            if (b >= 48 && b <= 57)
                return b - 48;

            // A-F
            if (b >= 65 && b <= 70)
                return 10 + (b - 65);

            // a-f
            if (b >= 97 && b <= 102)
                return 10 + (b - 97);

            throw new FormatException("Not hex");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
