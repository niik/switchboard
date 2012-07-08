using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Switchboard.Server.Utils;

namespace Switchboard.Server.Tests
{
    [TestClass]
    public class ChunkedStreamTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var ms = new MemoryStream();
            var sw = new StreamWriter(ms);
            sw.NewLine = "\r\n";

            var sizes = new[] { 10, 30, 10, 100, 20 };

            foreach (var size in sizes)
            {
                sw.WriteLine(size.ToString("X"));
                sw.WriteLine(new string('Y', size));
            }
            sw.Flush();

            ms.Seek(0, SeekOrigin.Begin);

            var chunkedStream = new ChunkedStream(ms);

            var ms2 = new MemoryStream();
            chunkedStream.CopyToAsync(ms2).Wait();

            Assert.AreEqual(ms.Length, ms2.Length);

            var buf1 = ms.ToArray();
            var buf2 = ms2.ToArray();

            for (int i = 0; i < ms.Length; i++)
                Assert.AreEqual(buf1[i], buf2[i]);
        }
    }
}
