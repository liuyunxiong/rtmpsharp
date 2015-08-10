using RtmpSharp.IO;
using System;

namespace RtmpSharp.Net
{
    class StatusCode
    {
        public const string CallFailed = "NetConnection.Call.Failed";
    }

    class StatusAsObject : AsObject
    {
        private Exception exception;

        public StatusAsObject(Exception exception)
        {
            // todo: complete AsObject member initialization
            this.exception = exception;
        }

        public StatusAsObject(string code, string level, string description, object application, ObjectEncoding objectEncoding)
        {
            this["code"] = code;
            this["level"] = level;
            this["description"] = description;
            this["application"] = application;
            this["objectEncoding"] = (double)objectEncoding;
        }

        public StatusAsObject(string code, string level, string description)
        {
            this["code"] = code;
            this["level"] = level;
            this["description"] = description;
        }
    }
}
