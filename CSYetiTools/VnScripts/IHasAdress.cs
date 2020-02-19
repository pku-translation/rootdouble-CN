using System.Collections.Generic;

namespace CsYetiTools.VnScripts
{
    public interface IHasAddress
    {
        IEnumerable<CodeAddressData> GetAddresses();
    }
}