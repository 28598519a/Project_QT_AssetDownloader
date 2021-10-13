using System;
using System.IO;

namespace Project_QT_AssetDL
{
    public static class qtxFile
    {
		public static byte[] Decrypt(ref byte[] data, int nStartOffset, int nSizeToDecrypt, char[] randomKey)
		{
			string text = "UR3Hcj7ndh8Ynot9";
			int num = 0;
			for (int i = nStartOffset; i < nStartOffset + nSizeToDecrypt; i++)
			{
				int index = num % text.Length;
				int num2 = num % randomKey.Length;
				byte b = data[i];
				b -= (byte)randomKey[num2];
				b ^= (byte)text[index];
				data[i] = b;
				num++;
			}

			return data;
		}

		public static bool IsKTXFile(byte[] source, int randomSizeSize)
		{
			char[] array = new char[]
			{
			'«',
			'K',
			'T',
			'X',
			' ',
			'1',
			'1',
			'»',
			'\r',
			'\n',
			'\u001a',
			'\n'
			};
			if (source.Length < array.Length)
			{
				return false;
			}
			bool result = true;
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] != (char)source[i + randomSizeSize])
				{
					result = false;
					break;
				}
			}
			return result;
		}

		public static byte[] GetKTXFile(byte[] source, int nRandomKeySize)
		{
			byte[] array = new byte[source.Length - nRandomKeySize];
			Buffer.BlockCopy(source, nRandomKeySize, array, 0, source.Length - nRandomKeySize);
			return array;
		}

		public enum ETC2_FMT
		{
			ETC2_RGB8 = 37492,
			ETC2_RGB8A1 = 37494,
			ETC2_RGBA8 = 37496
		}

		
		public static void KTX2PNG(string path)
		{
			byte[] source = File.ReadAllBytes(path);
			int startIndex = 28;
			int num = 36;
			int startIndex2 = num + 4;
			int startIndex3 = 56;
			int startIndex4 = 60;
			int num2 = BitConverter.ToInt32(source, startIndex);
			int num3 = BitConverter.ToInt32(source, num);
			int num4 = BitConverter.ToInt32(source, startIndex2);
			int num5 = BitConverter.ToInt32(source, startIndex3);
			int num6 = BitConverter.ToInt32(source, startIndex4);
			bool flag = num5 > 1;
			ETC2_FMT etc2_FMT = (ETC2_FMT)num2;
			int mode;
			if (etc2_FMT != ETC2_FMT.ETC2_RGBA8)
			{
				if (etc2_FMT != ETC2_FMT.ETC2_RGB8A1)
				{
					//ETC2_RGB -> DecompressETC2
					mode = 1;
				}
				else
				{
					//ETC2_RGBA1 -> DecompressETC2A1
					mode = 2;
				}
			}
			else
			{
				//ETC2_RGBA8 -> DecompressETC2A8
				mode = 3;
			}
			int num7 = 64 + num6 + 4;
			byte[] array = new byte[source.Length - num7];
			Buffer.BlockCopy(source, num7, array, 0, source.Length - num7);
			File.WriteAllBytes(path, array);

			EtcBitmap etcBitmap = new EtcBitmap();
			// num3 = width, num4 = height
			etcBitmap.ETC2PNG(path, num3, num4, mode);
		}
	}
}
