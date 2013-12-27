using System;

// astralfoxy:complete/exceptionaleventargs.cs
namespace Complete
{
    class ExceptionalEventArgs : EventArgs
    {
        public string Description;
        public Exception Exception;

        public ExceptionalEventArgs(string description)
        {
            Description = description;
        }

        public ExceptionalEventArgs(string description, Exception exception)
        {
            Description = description;
            Exception = exception;
        }
    }
}
