namespace CsYetiTools.VnScripts
{
    public class ZeroCodeException : OpCodeParseException
    {
        public ZeroCode Code { get; }

        public ZeroCodeException(ZeroCode code, string message, string context) : base(message, context)
        {
            Code = code;
        }
    }
}
