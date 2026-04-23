using MsSqlTest;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace MsSqlGuidV7SwapperTest;

internal class TestOutputHelperVoid : ITestOutputHelper
{
    public void WriteLine(string message)
    {
    }

    public void WriteLine(string format, params object[] args)
    {
    }
}

public class UnitTest1
{
    public static bool IsDebugLoggingEnabled = false;
    private readonly ITestOutputHelper _log;

    public UnitTest1(ITestOutputHelper log)
    {
        if (IsDebugLoggingEnabled)
        {
            _log = log;
        }
        else
        {
            _log = new TestOutputHelperVoid();
        }
    }
    
    string NormalizeShortGuidText(string guidText)
    {
        return guidText.ToUpper().Replace("-", "");
    }

    string ToNormalizedShortGuidText(Guid guid)
    {
        return NormalizeShortGuidText(guid.ToString());
    }

    /// <summary>
    /// bytesTryWriteBytes produces not normal guid and new Guid(bytes) handles not normal guid.
    /// guid.ToString() produces normal guid and new Guid(string) handles normal guid.
    /// </summary>
    [Fact]
    public void Test3()
    {
        for (int i = 0; i < 1_000_000; i++)
        {
            var originalNormalGuidV7Text = MsSqlTest.Program.GenerateUuidV7(); // it is normal guid
            _log.WriteLine($"{nameof(originalNormalGuidV7Text)}:\n{originalNormalGuidV7Text}");
            
            var originalNormalGuidV7 = new Guid(originalNormalGuidV7Text); // it is normal guid
            Assert.Equal(originalNormalGuidV7Text, ToNormalizedShortGuidText(originalNormalGuidV7));
            Assert.True(originalNormalGuidV7.IsV7());
            
            var tmp = new byte[16];
            originalNormalGuidV7.TryWriteBytes(tmp);

            var tmpGuidFromBytes = new Guid(tmp);
            Assert.Equal(originalNormalGuidV7, tmpGuidFromBytes);

            var tmpHex = NormalizeShortGuidText(Convert.ToHexString(tmp));
            Assert.NotEqual(originalNormalGuidV7Text, tmpHex);
            Assert.NotEqual(new Guid(tmp), new Guid(tmpHex));
        }
    }

    /// <summary>
    /// some asserts about Run3 and guidV7 to ms sql friendly reordering
    /// </summary>
    [Fact]
    public void Test2()
    {
        for (int i = 0; i < 1_000_000; i++)
        {
            var originalNormalGuidV7Text = MsSqlTest.Program.GenerateUuidV7(); // it is normal guid
            _log.WriteLine($"{nameof(originalNormalGuidV7Text)}:\n{originalNormalGuidV7Text}");
            
            var originalNormalGuidV7 = new Guid(originalNormalGuidV7Text); // it is normal guid
            Assert.Equal(originalNormalGuidV7Text, ToNormalizedShortGuidText(originalNormalGuidV7));
            Assert.True(originalNormalGuidV7.IsV7());

            var originalGuidV7RawBytes = new byte[16]; // it is not normal guid
            originalNormalGuidV7.TryWriteBytes(originalGuidV7RawBytes);

            var originalGuidV7RawBytesHex =
                NormalizeShortGuidText(Convert.ToHexString(originalGuidV7RawBytes)); // it is not normal guid
            _log.WriteLine($"{nameof(originalGuidV7RawBytesHex)}:\n{originalGuidV7RawBytesHex}");
            Assert.NotEqual(originalNormalGuidV7Text, originalGuidV7RawBytesHex);
            Assert.NotEqual(originalNormalGuidV7, new Guid(originalGuidV7RawBytesHex));

            var originalNormalGuidV7RawBytes = originalGuidV7RawBytes.ToArray(); // it will be normal guid after reorder
            {
                var src = originalNormalGuidV7RawBytes;
                // reorder because (Test3) bytesTryWriteBytes produces not normal guid and new Guid(bytes) handles not normal guid 

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
            var originalNormalGuidV7RawBytesHex =
                NormalizeShortGuidText(Convert.ToHexString(originalNormalGuidV7RawBytes)); // it is normal guid
            _log.WriteLine($"{nameof(originalNormalGuidV7RawBytesHex)}:\n{originalNormalGuidV7RawBytesHex}");
            Assert.Equal(originalNormalGuidV7Text, originalNormalGuidV7RawBytesHex);
            Assert.Equal(originalNormalGuidV7, new Guid(originalNormalGuidV7RawBytesHex));
            Assert.True(new Guid(originalNormalGuidV7RawBytesHex).IsV7());

            var msSqlGuidV7RawBytes = new byte[16];
            {
                var src = originalNormalGuidV7RawBytes;
                var dst = msSqlGuidV7RawBytes;
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
            }

            var msSqlGuidV7RawBytesHex =
                NormalizeShortGuidText(
                    Convert.ToHexString(msSqlGuidV7RawBytes)); // it is MS SQL friendly guid produced from normal
            _log.WriteLine($"{nameof(msSqlGuidV7RawBytesHex)}:\n{msSqlGuidV7RawBytesHex}");
            var msSqlGuidV7Guid = new Guid(msSqlGuidV7RawBytesHex); // it is MS SQL friendly guid produced from normal
            Assert.Equal(msSqlGuidV7RawBytesHex, ToNormalizedShortGuidText(msSqlGuidV7Guid));

            Assert.Equal(MsSqlTest.Program.ReorderUuid(originalNormalGuidV7Text),
                ToNormalizedShortGuidText(msSqlGuidV7Guid));
        }
    }

    /// <summary>
    /// some asserts about GuidExtensions
    /// </summary>
    [Fact]
    public void Test1()
    {
        for (int i = 0; i < 1_000_000; i++)
        {
            var originalGuidV7Text = MsSqlTest.Program.GenerateUuidV7();

            var originalGuidV7ReorderedText = MsSqlTest.Program.ReorderUuid(originalGuidV7Text);
            
            var originalGuidV7 = new Guid(originalGuidV7Text);
            Assert.True(originalGuidV7.IsV7());
            Assert.Equal(originalGuidV7Text, ToNormalizedShortGuidText(originalGuidV7));
            Assert.Equal(originalGuidV7, Guid.Parse(originalGuidV7Text));

            var guidV7SwappedToMsSqlServer = originalGuidV7.EnsureGuidV7SwappedToMsSql();
            Assert.True(guidV7SwappedToMsSqlServer.IsV7SwappedForMsSql());
            Assert.Equal(originalGuidV7ReorderedText, ToNormalizedShortGuidText(guidV7SwappedToMsSqlServer));

            var guidV7SwappedFromMsSqlServer = guidV7SwappedToMsSqlServer.EnsureGuidV7();
            Assert.True(guidV7SwappedFromMsSqlServer.IsV7());
            Assert.Equal(originalGuidV7, guidV7SwappedFromMsSqlServer);
            Assert.Equal(originalGuidV7Text, ToNormalizedShortGuidText(guidV7SwappedFromMsSqlServer));
        }
    }
}