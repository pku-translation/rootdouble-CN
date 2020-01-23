namespace CSYetiTools.OpCodes
{
    public class DialogCode : FixedLengthStringCode
    {
        public DialogCode() : base(0x45) { }

        protected override string ArgsToString()
            => "<Dialog> " + base.ArgsToString();
    }
}
