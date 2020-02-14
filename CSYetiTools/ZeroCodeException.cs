namespace CSYetiTools
{
    public class ZeroCodeException : OpCodeParseException
    {
        public OpCodes.ZeroCode Code { get; }

        public ZeroCodeException(OpCodes.ZeroCode code, string message, string context) : base(message, context)
        {
            Code = code;
        }
    }
}
