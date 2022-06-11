using System.Security.Cryptography;

namespace Worker;
public class Work
{
    public static byte[] ComputeSha512(byte[] buffer, int iterations)
    {
        var shaM = new SHA512Managed();
        var output = shaM.ComputeHash(buffer);
        for (var i = 0; i < iterations - 1; i++)
        {
            output = shaM.ComputeHash(output);
        }
        return output;
    }
}