namespace CSYetiTools.VnScripts
{
    public class ExtraDialogCode : DynamicLengthStringCode
    {
        // if Short1 == -1, it's used as normal dialog
        // else if Short1 == 0x0D, it's character name
        // else it's special string

        protected override string CodeName => "extra-dialog";

        public bool IsDialog
            => Short1 == -1;

        public bool IsCharacter
            => Short1 == 0x0D;

        protected override void DumpArgs(System.IO.TextWriter writer)
        {
            writer.Write(IsCharacter ? " <Character>" : " <ExDialog>");
            base.DumpArgs(writer);
        }
    }
}
