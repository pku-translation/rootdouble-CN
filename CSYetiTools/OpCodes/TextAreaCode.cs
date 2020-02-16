using System;
using System.Linq;

namespace CSYetiTools.OpCodes
{
    public class TextAreaCode : FixedLengthCode
    {
        // [0] == 0A|0D|46|47
        // 

        private static byte[] Seq1 = { 0x0A, 0x00, 0x64, 0x00, 0x18, 0x00, 0x38, 0x04, 0x60, 0x00 }; // 0x0A, 100, 24, 1080, 96 (JP)

        private static byte[] Seq2 = { 0x0A, 0x00, 0x64, 0x00, 0x10, 0x00, 0x38, 0x04, 0x60, 0x00 }; // 0x0A, 100, 16, 1080, 96 (EN)

        public TextAreaCode() : base(0x68, 11) { }

        public bool IsTestTarget
            => _args.SequenceEqual(Seq1) || _args.SequenceEqual(Seq2);

        public void ChangeArgs(params byte[] args)
        {
            for (int i = 0; i < Math.Min(args.Length, _args.Length); ++i)
            {
                _args[i] = args[i];
            }
        }
    }
}