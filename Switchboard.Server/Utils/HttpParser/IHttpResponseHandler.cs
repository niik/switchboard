using System;

namespace Switchboard.Server.Utils.HttpParser
{
    public interface IHttpResponseHandler
    {
        void OnResponseBegin();
        void OnStatusLine(Version protocolVersion, int statusCode, string statusDescription);
        void OnHeader(string name, string value);
        void OnHeadersEnd();
        void OnEntityStart();
        void OnEntityData(byte[] buffer, int offset, int count);
        void OnEntityEnd();
        void OnResponseEnd();
    }
}
