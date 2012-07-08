/*
 * Copyright (c) 2008-2009 Markus Olsson
 * var mail = string.Join(".", new string[] {"j", "markus", "olsson"}) + string.Concat('@', "gmail.com");
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this 
 * software and associated documentation files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use, copy, modify, merge, publish, 
 * distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING 
 * BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Switchboard.Server.Utils
{
    /// <summary>
    /// An implementation of a Stream that transparently redirects all 
    /// stream-related method calls to the supplied inner stream. Makes
    /// it easy to implement the subset of stream functionality required
    /// for your stream.
    /// </summary>
    internal abstract class RedirectingStream : Stream
    {
        protected readonly Stream innerStream;

        public RedirectingStream(Stream innerStream)
        {
            this.innerStream = innerStream;
        }

        public override bool CanRead { get { return this.innerStream.CanRead; } }

        public override bool CanSeek { get { return this.innerStream.CanSeek; } }

        public override bool CanWrite { get { return this.innerStream.CanWrite; } }

        public override void Flush()
        {
            this.innerStream.Flush();
        }

        public override long Length
        {
            get { return this.innerStream.Length; }
        }

        public override long Position
        {
            get { return this.innerStream.Position; }
            set { this.innerStream.Position = value; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.innerStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            this.innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.innerStream.Write(buffer, offset, count);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return this.innerStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return this.innerStream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void Close()
        {
            this.innerStream.Close();
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, System.Threading.CancellationToken cancellationToken)
        {
            return this.innerStream.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        public override bool CanTimeout
        {
            get
            {
                return this.innerStream.CanTimeout;
            }
        }

        public override System.Runtime.Remoting.ObjRef CreateObjRef(Type requestedType)
        {
            throw new NotSupportedException();
        }

        [Obsolete]
        protected override System.Threading.WaitHandle CreateWaitHandle()
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            this.innerStream.Dispose();
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return this.innerStream.EndRead(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            this.innerStream.EndWrite(asyncResult);
        }

        public override bool Equals(object obj)
        {
            return this.innerStream.Equals(obj);
        }

        public override Task FlushAsync(System.Threading.CancellationToken cancellationToken)
        {
            return this.innerStream.FlushAsync(cancellationToken);
        }

        public override int GetHashCode()
        {
            return this.innerStream.GetHashCode();
        }

        public override object InitializeLifetimeService()
        {
            throw new NotSupportedException();
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken)
        {
            return this.innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override int ReadByte()
        {
            return this.innerStream.ReadByte();
        }

        public override int ReadTimeout
        {
            get
            {
                return this.innerStream.ReadTimeout;
            }
            set
            {
                this.innerStream.ReadTimeout = value;
            }
        }

        public override string ToString()
        {
            return this.innerStream.ToString();
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken)
        {
            return this.innerStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override void WriteByte(byte value)
        {
            this.innerStream.WriteByte(value);
        }

        public override int WriteTimeout
        {
            get
            {
                return this.innerStream.WriteTimeout;
            }
            set
            {
                this.innerStream.WriteTimeout = value;
            }
        }

    }
}
