using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CsYetiTools.FileTypes
{
    public class Hca
    {
        public static byte[] FileTag = { (byte)'H', (byte)'C', (byte)'A', 0x00 };
    }
}