using System;
using CSYetiTools.Base.IO;

namespace CSYetiTools.VnScripts;

public class ScriptArgument
{
    private byte _b1;
    private byte _b2;

    public ScriptArgument()
    { }

    public ScriptArgument(IBinaryStream stream)
    {
        _b1 = stream.ReadByte();
        _b2 = stream.ReadByte();
    }

    public void WriteTo(IBinaryStream stream)
    {
        stream.Write(_b1);
        stream.Write(_b2);
    }

    public bool IsConst => _b2 != 0x80;

    public int ConstValue
    {
        get
        {
            if (_b2 == 0x80) throw new InvalidOperationException();
            return (short)((_b2 << 8) | _b1);
        }
    }

    public override string ToString()
    {
        if (_b2 == 0x80) {
            if (_b1 >= 0x80) {
                return "var-" + (_b1 - 0x80);
            }
            else {
                return "flag-" + _b1;
            }
        }
        return ConstValue.ToString();
    }
}
