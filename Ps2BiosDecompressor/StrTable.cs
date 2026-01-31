namespace Ps2BiosDecompressor;

public class StrTable
{
    
    public Dictionary<string, byte[]> DataTable { get; set; }
    
    public StrTable(byte[] buf)
    {
        var endTocOffset = 0x7FFFFFFF;
        var tocOffset = 0;
        var dataOffset = 0;
        DataTable = new Dictionary<string, byte[]>();
        if ((GetString(buf) != "RESET") && (GetString(buf) != "RESETB"))
        {
            throw new InvalidDataException("Passed data does not start with RESET string");
        }
        while (tocOffset < endTocOffset)
        {
            var label = GetString(buf.Skip(tocOffset).Take(10).ToArray());
            var length = BitConverter.ToInt32(buf.Skip(tocOffset + 0xC).Take(4).ToArray());

            if (label == "ROMDIR") {
                endTocOffset = length - 0x10;
            }

            if (label != "-")
            {
                DataTable.Add(label, buf.Skip(dataOffset).Take(length).ToArray());
            }

            tocOffset += 0x10;

            if (label == "RESET")
            {
                continue;
            }
            dataOffset += length;
            while (dataOffset % 0x10 != 0)
            {
                dataOffset++;
            }
            
        }
    }

    public static string GetString(byte[] raw)
    {
        var offset = 0;
        var output = "";
        while (raw[offset] != 0x00 && (offset < raw.Length - 1))
        {
            output += Convert.ToChar(raw[offset++]);
        }
        return output;
    }
}