using System.Collections.Generic;

namespace CsYetiTools.VnScripts
{
    public interface IHasAddress
    {
        void SetCodeIndices(IReadOnlyDictionary<int, OpCode> codeTable);
    }
}