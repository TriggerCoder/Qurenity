using System.Text;
public static class ByteReader
{
	public static int ReadInt32(byte[] data, ref int pointer)
	{
		return (int)(data[pointer++] |
				(int)data[pointer++] << 8 |
				(int)data[pointer++] << 16 |
				(int)data[pointer++] << 24);
	}

	public static int ReadInt16(byte[] data, ref int pointer)
	{
		return (int)(data[pointer++] |
				(int)data[pointer++] << 8);

	}

	public static short ReadShort(byte[] data, ref int pointer)
	{
		return (short)(data[pointer++] |
				(short)data[pointer++] << 8);

	}

	public static ushort LittleReadShort(byte[] data, ref int pointer)
	{
		return (ushort)(data[pointer++] << 8 |
			(ushort)data[pointer++]);

	}
	public static string ReadName8(byte[] data, ref int pointer)
	{
		return Encoding.ASCII.GetString(new byte[]
		{
			data[pointer++],
			data[pointer++],
			data[pointer++],
			data[pointer++],
			data[pointer++],
			data[pointer++],
			data[pointer++],
			data[pointer++]
		}).TrimEnd('\0').ToUpper();
	}

	public static string Md5Sum(byte[] bytes)
	{
		// encrypt bytes
		System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
		byte[] hashBytes = md5.ComputeHash(bytes);

		// Convert the encrypted bytes back to a string (base 16)
		string hashString = "";

		for (int i = 0; i < hashBytes.Length; i++)
		{
			hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');
		}
		return hashString.PadLeft(32, '0');
	}

	public static string Adler32(byte[] bytes)
	{
		const int MOD_ADLER = 65521;
		int a = 1, b = 0;
		string checkSum = "";

		for (int i = 0; i < bytes.Length; ++i)
		{
			a = (a + bytes[i]) % MOD_ADLER;
			b = (b + a) % MOD_ADLER;
		}
		checkSum = System.Convert.ToString(((b << 16) | a), 16);
		return checkSum.PadLeft(8, '0');
	}
}
