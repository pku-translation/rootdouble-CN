namespace CSYetiTools.OpCodes
{
    public class NovelCode : FixedLengthStringCode
    {
        public NovelCode() : base(0x86) { }

        protected override string ArgsToString(bool noString)
            => "<Novel> " + base.ArgsToString(noString);
    }
}
