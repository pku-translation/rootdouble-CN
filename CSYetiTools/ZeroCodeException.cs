namespace CSYetiTools
{
    public class ZeroCodeException : OpCodeParseException
    {
        public ZeroCodeException(string message, string context) : base(message, context)
        {}
    }
}