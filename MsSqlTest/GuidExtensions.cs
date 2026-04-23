namespace MsSqlTest;

public static class GuidExtensions
{
    public static bool IsV4(this Guid guid)
    {
        // 00000000-0000-F000-0000-000000000000.
        // return guid.Version == 7; .net 10
        
        Span<byte> bytes = stackalloc byte[16];
        guid.TryWriteBytes(bytes);

        return bytes[7] >>> 4 == 4;
    }
    
    public static bool IsV7(this Guid guid)
    {
        // 00000000-0000-F000-0000-000000000000.
        // return guid.Version == 7; .net 10
        
        Span<byte> bytes = stackalloc byte[16];
        guid.TryWriteBytes(bytes);

        return bytes[7] >>> 4 == 7;
    }
    
    public static bool IsV7SwappedForMsSql(this Guid guid)
    {
        Span<byte> bytes = stackalloc byte[16];
        guid.TryWriteBytes(bytes);

        return bytes[8] >>> 4 == 7;
    }

    public static Guid EnsureGuidV7(this Guid guid)
    {
        if (!guid.IsV7())
        {
            return SwapV7FromMsSqlServer(guid);
        }

        return guid;
    }
    
    public static Guid EnsureGuidV7SwappedToMsSql(this Guid guid)
    {
        if (!guid.IsV7SwappedForMsSql())
        {
            return SwapV7ToMsSqlServer(guid);
        }

        return guid;
    }

    private static void ReorderBytesForGuidInternalLayout(Span<byte> src)
    {
        var tmp0 = src[0];
        var tmp1 = src[1];
        var tmp2 = src[2];
        var tmp3 = src[3];
        src[0] = tmp3;
        src[1] = tmp2;
        src[2] = tmp1;
        src[3] = tmp0;
        var tmp4 = src[4];
        var tmp5 = src[5];
        src[4] = tmp5;
        src[5] = tmp4;
        var tmp6 = src[6];
        var tmp7 = src[7];
        src[6] = tmp7;
        src[7] = tmp6;
    }
    
    public static Guid SwapV7ToMsSqlServer(this Guid guidV7)
    {
        if (!guidV7.IsV7())
        {
            throw new ArgumentException("The specified GUID does not match 7 version.");
        }
        
        Span<byte> src = stackalloc byte[16];
        guidV7.TryWriteBytes(src);
        
        // src is not normal guid v7 now.
        // `Hex(src)` != `guidV7.ToString()`
        
        // reorder because of guid internal layout (unit Test3)
        ReorderBytesForGuidInternalLayout(src);
        
        // src is normal guid bytes now
        // `Hex(src)` == `guidV7.ToString()`
        
        Span<byte> dst = stackalloc byte[16];
        // reorder for SQL SERVER Sort order
        dst[0] = src[12];
        dst[1] = src[13];
        dst[2] = src[14];
        dst[3] = src[15];
        
        dst[4] = src[10];
        dst[5] = src[11];
        
        dst[6] = src[8];
        dst[7] = src[9];
        
        dst[8] = src[6];
        dst[9] = src[7];
        
        dst[10] = src[0];
        dst[11] = src[1];
        dst[12] = src[2];
        dst[13] = src[3];
        dst[14] = src[4];
        dst[15] = src[5];
        
        // dst is normal ms sql friendly guid bytes now.
        
        // reorder because of guid internal layout
        ReorderBytesForGuidInternalLayout(dst);
        
        // dst is not normal any guid bytes now.
        // dst is `new Guid(bytes)` friendly now.
        
        return new Guid(dst);
    }
    
    public static Guid SwapV7FromMsSqlServer(this Guid swappedGuidV7)
    {
        Span<byte> src = stackalloc byte[16];
        swappedGuidV7.TryWriteBytes(src);
        
        ReorderBytesForGuidInternalLayout(src);
        
        Span<byte> dst = stackalloc byte[16];
        
        // reorder from SQL SERVER Sort order
        dst[0] = src[10];
        dst[1] = src[11];
        dst[2] = src[12];
        dst[3] = src[13];
        
        dst[4] = src[14];
        dst[5] = src[15];
        
        dst[6] = src[8];
        dst[7] = src[9];
        
        dst[8] = src[6];
        dst[9] = src[7];
        
        dst[10] = src[4];
        dst[11] = src[5];
        
        dst[12] = src[0];
        dst[13] = src[1];
        dst[14] = src[2];
        dst[15] = src[3];


        ReorderBytesForGuidInternalLayout(dst);
        
        var newGuid = new Guid(dst);
        
        if (!newGuid.IsV7())
        {
            throw new ArgumentException("The result GUID does not match 7 version.");
        }
        
        return newGuid;
    }
    
}