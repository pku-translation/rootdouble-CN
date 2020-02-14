namespace CSYetiTools.OpCodes
{
    public class ExtraDialogCode : DynamicLengthStringCode
    {
        // if Short1 == -1, it's used as normal dialog
        // else, it's character name
        public ExtraDialogCode() : base(0x47) { }

        public bool IsDialog
            => Short1 == -1;

        public bool IsCharacter
            => Short1 != -1;

        protected override string ArgsToString(bool noString)
            => (IsCharacter ? "<Character> " : "<Dialog> ") + base.ArgsToString(noString);
    }
}