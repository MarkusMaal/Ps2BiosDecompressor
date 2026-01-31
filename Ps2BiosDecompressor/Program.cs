namespace Ps2BiosDecompressor;

internal abstract class Program
{
    private static unsafe void LzDecompress(byte[] dest, byte[] source, uint destSize)
    {
        fixed (byte* pDest = dest)
        fixed (byte* pSource = source)
        {
            var ptr = pDest;
            var src = pSource;

            uint flag = 0, count = 0, mask = 0;
            ushort shift = 0;
            
            while ((ptr - pDest) < destSize)
            {
                if (count == 0)
                {
                    count = 30;
                    flag = SwapUInt32(*(uint*)src);
                    src += 4;
                    mask = 0x3fffu >> (int)(flag & 3);
                    shift = (ushort)(14 - (flag & 3));
                }

                if ((flag & 0x80000000) != 0)
                {
                    var offSize = SwapUInt16(*(ushort*)src);
                    src += 2;
                    var offset = (ushort)((offSize & mask) + 1);
                    var length = (offSize >> shift) + 3;
                    for (var i = 0; i < length; i++)
                    {
                        *ptr = *(ptr - offset);
                        ptr++;
                    }
                }
                else
                {
                    *ptr = *src;
                    ptr++;
                    src++;
                }

                count--;
                flag <<= 1;
            }
        }
    }

    private static uint SwapUInt32(uint val)
    {
        return (val >> 24) |
               ((val >> 8) & 0x0000FF00) |
               ((val << 8) & 0x00FF0000) |
               (val << 24);
    }

    private static ushort SwapUInt16(ushort val)
    {
        return (ushort)((val >> 8) | (val << 8));
    }

    private static int Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: {0} <PS2 BIOS ROM file> <output dir>", AppDomain.CurrentDomain.FriendlyName);
            return -1;
        }

        var buf = File.ReadAllBytes(args[0]);
        var status = false;
        Console.WriteLine("Started search for data segments...");
        for (var i = 0; i < buf.Length; i += 0x10)
        {
            var data = buf.Skip(i).Take(0x10).ToArray();
            if (StrTable.GetString(data) != "RESET") continue;
            if (buf[i + 0xF] != 0x00) continue;
            data = buf.Skip(i).ToArray();
            Console.WriteLine($"Found data segment at offset 0x{i:X}");
            ParseData(data, args[1]);
            status = true;
            break;
        }

        if (status) return 0;
        Console.WriteLine("No data segments found.");
        return 1;

    }

    private static void ParseData(byte[] data, string outPath, bool subDir = false)
    {
        StrTable st = new(data);
        foreach (var kvp in st.DataTable)
        {
            switch (kvp.Key)
            {
                case "RESET":
                case "RESETB":
                case "ROMDIR":
                case "EXTINFO":
                    continue;
            }
            var uncompressedLen = BitConverter.ToUInt32(kvp.Value, 0);
                
            byte[] unpacked;
            if (!kvp.Value.Take(4).All(b => b is >= 32 and <= 127) && subDir)
            {
                Console.WriteLine($"\t\t|-> Decompressing {kvp.Key}");
                unpacked = new byte[uncompressedLen];
                LzDecompress(unpacked, kvp.Value.AsSpan(4).ToArray(), uncompressedLen);
            }
            else
            {
                Console.WriteLine($"\tExtracting {kvp.Key}");
                unpacked = kvp.Value;
            }
            var ext = "BIN";
            if (kvp.Key.Contains("STR")) ext = "TXT";
            if (kvp.Key.StartsWith("ICO")) ext = "ICO";
            if (kvp.Key.StartsWith("TEX")) ext = BitConverter.ToInt32(unpacked.Take(4).ToArray(), 0) == 16 ? "TIM" : "RAW";
            if (kvp.Key.StartsWith("SND") && kvp.Key.EndsWith('H')) ext = "HD";
            if (kvp.Key.StartsWith("SND") && kvp.Key.EndsWith('B')) ext = "BD";
            if (kvp.Key.StartsWith("SND") && kvp.Key.EndsWith('S')) ext = "SQ";
            if (unpacked.All(b => b is >= 32 and <= 127)) ext = "TXT";
            if (uncompressedLen == 0x464C457F) ext = "ELF";
            if (kvp.Key.EndsWith("IMAGE") || (StrTable.GetString(unpacked.Take(0x10).ToArray()).Length >= 5 && StrTable.GetString(unpacked.Take(0x10).ToArray())[..5] == "RESET" && (unpacked[9] == 0x00))) ext = "IMG";
            if (ext == "IMG")
            {
                if (!Directory.Exists(Path.Join(outPath, kvp.Key)))
                {
                    Directory.CreateDirectory(Path.Join(outPath, kvp.Key));
                }
                ParseData(unpacked, Path.Join(outPath, kvp.Key), true);
                continue;
            }
            File.WriteAllBytes(Path.Join(outPath, kvp.Key + "." + ext), unpacked);
        }
    }
}