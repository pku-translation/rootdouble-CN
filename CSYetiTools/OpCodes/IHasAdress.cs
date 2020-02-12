using System.Collections.Generic;

namespace CSYetiTools.OpCodes
{
    public interface IHasAddress
    {
        void SetCodeIndices(IReadOnlyDictionary<int, OpCode> codeTable);
    }
}