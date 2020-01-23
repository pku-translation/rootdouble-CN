using System;

namespace CSYetiTools
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
    }

    /*
    class OpCode_7B : StringCode
    {
        private int _arg1;

        public OpCode_7B() : base(0x7B) { }

        public override int ArgLength => throw new NotImplementedException();

        public override byte[] ArgsToBytes()
        {
            throw new NotImplementedException();
        }

        protected override string ArgsToString()
        {
            throw new NotImplementedException();
        }

        protected override void Read(BinaryReader reader)
        {
            throw new NotImplementedException();
        }
    }
    */
}