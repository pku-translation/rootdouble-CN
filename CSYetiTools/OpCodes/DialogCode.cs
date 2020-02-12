namespace CSYetiTools.OpCodes
{
    public class DialogCode : FixedLengthStringCode
    {
        public DialogCode() : base(0x45) { }

        protected override string ArgsToString(bool noString)
        {
            if (noString) return "<Dialog>";
            else return "<Dialog> " + base.ArgsToString(noString);
        }
    }
}
