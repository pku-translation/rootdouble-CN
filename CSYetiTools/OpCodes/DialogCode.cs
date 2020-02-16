namespace CSYetiTools.OpCodes
{
    public class DialogCode : FixedLengthStringCode
    {
        /*************************************************************
            there are 3 types of 0x45 code, mostly type-1
            type-2 and type-3 only appears in chunk0124 (normal ending of route-A)

            1: Short1 == -1           Short2 may be dialog index

            2: Short1 == 0x000A       Short2 may be dialog index

            3: Short1 == 0x0046       Short2 == 0x6981

        *************************************************************/
        public DialogCode() : base(0x45) { }

        public bool IsIndexed
        {
            get
            {
                if (Short1 == 0x0046 && Short2 == 0x6981) return false;
                return Short2 != -1;
            }
        }

        protected override string ArgsToString()
        {
            return "<Dialog> " + base.ArgsToString();
        }
    }
}
