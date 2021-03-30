using System.Collections.Generic;

namespace CSYetiTools.VnScripts
{
    public interface IHasAddress
    {
        IEnumerable<LabelReference> GetAddresses();
    }
}
