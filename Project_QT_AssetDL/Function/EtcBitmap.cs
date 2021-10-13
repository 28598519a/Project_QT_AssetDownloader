using Etc;
using System;
using System.Drawing.Imaging;
using System.IO;

namespace Project_QT_AssetDL
{
	public class EtcBitmap
    {
        public void ETC2PNG(string path, int width, int height, int mode)
        {
			using (DirectBitmap bitmap = new DirectBitmap(width, height))
			{
				byte[] data = File.ReadAllBytes(path);
				EtcDecoder decoder = new EtcDecoder();
				switch (mode)
				{
					case 0:
						decoder.DecompressETC(data, width, height, bitmap.Bits);
						break;
					case 1:
						decoder.DecompressETC2(data, width, height, bitmap.Bits);
						break;
					case 2:
						decoder.DecompressETC2A1(data, width, height, bitmap.Bits);
						break;
					case 3:
						decoder.DecompressETC2A8(data, width, height, bitmap.Bits);
						break;
					case 4:
						decoder.DecompressEACRUnsigned(data, width, height, bitmap.Bits);
						break;
					case 5:
						decoder.DecompressEACRSigned(data, width, height, bitmap.Bits);
						break;
					case 6:
						decoder.DecompressEACRGUnsigned(data, width, height, bitmap.Bits);
						break;
					case 7:
						decoder.DecompressEACRGSigned(data, width, height, bitmap.Bits);
						break;

					default:
						throw new Exception(mode.ToString());
				}

				string dirPath = Path.GetDirectoryName(path);
				string fileName = Path.GetFileNameWithoutExtension(path);
				string outPath = Path.Combine(dirPath, fileName + ".png");
				bitmap.Bitmap.Save(outPath, ImageFormat.Png);
			}
		}
    }
}
