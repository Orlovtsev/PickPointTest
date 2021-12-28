using System;

namespace PickPointTest.DataProviders
{
    public class NotFoundDataException : Exception
    {
        public NotFoundDataException(string message) : base(message){}
        public NotFoundDataException(){}
    }
}