namespace CSYetiTools.OpCodes
{
    public class CharacterCode : DynamicLengthStringCode
    {
        public CharacterCode() : base(0x47) { }

        protected override string ArgsToString(bool noString)
            => "<Character> " + base.ArgsToString(noString);
    }
}