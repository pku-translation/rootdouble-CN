namespace CSYetiTools.OpCodes
{
    public class ExtraDialogCode : DynamicLengthStringCode
    {
        // if Short1 == -1, it's used as normal dialog
        // else if Short1 == 0x0D, it's character name
        // else it's special string
        public ExtraDialogCode() : base(0x47) { }

        public bool IsDialog
            => Short1 == -1;

        public bool IsCharacter
            => Short1 == 0x0D;

        protected override string ArgsToString(bool noString)
            => (IsCharacter ? "<Character> " : "<ExDialog> ") + base.ArgsToString(noString);
    }
}