using System;

namespace RtmpSharp.Net
{
    // we use this for responding to invoke requests. but this version of rtmp-sharp doesn't include functionality for
    // handling invocation requests, and is thus currently unused
    class StatusAsObject : AsObject
    {
        readonly Exception exception;

        public StatusAsObject(Exception exception)
            => this.exception = exception;

        public StatusAsObject(string code, string level, string description, object application, ObjectEncoding encoding)
        {
            this["code"]           = code;
            this["level"]          = level;
            this["description"]    = description;
            this["application"]    = application;
            this["objectEncoding"] = (double)encoding;
        }

        public StatusAsObject(string code, string level, string description)
        {
            this["code"]           = code;
            this["level"]          = level;
            this["description"]    = description;
        }


        public static class Codes
        {
            public const string CallFailed = "NetConnection.Call.Failed";
        }
    }
}
