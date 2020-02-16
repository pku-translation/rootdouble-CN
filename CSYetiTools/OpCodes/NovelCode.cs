namespace CSYetiTools.OpCodes
{
    public class NovelCode : FixedLengthStringCode
    {
        // Short1: may be flags
        // Short2: may be voice index, -1 indicates no voice
        public NovelCode() : base(0x86) { }

        protected override string ArgsToString()
            => "<Novel> " + base.ArgsToString();
    }
}
