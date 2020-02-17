using System;
using System.Text;

namespace CsYetiTools.VnScripts
{
    public class OpCodeParseException : Exception
    {
        public string ScriptContext { get; set; }

        public OpCodeParseException(string message, string context, Exception inner) : base(message, inner)
        {
            ScriptContext = context;
        }

        public OpCodeParseException(string message, string context) : base(message)
        {
            ScriptContext = context;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.AppendLine(Message);
            if (!string.IsNullOrWhiteSpace(ScriptContext))
            {
                builder.AppendLine("------ context --------");
                builder.AppendLine(ScriptContext);
                builder.AppendLine("-----------------------");
            }
            if (InnerException != null)
            {
                builder.AppendLine(InnerException.ToString());
            }
            return builder.ToString();
        }
    }
}