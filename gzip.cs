using System;
using System.IO;
using System.IO.Compression;
using System.Collections;

namespace SOTSEdit
{
    class Gzip
    {
		public static byte[] read(string fileName)
		{
			using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
			using (BinaryReader reader = new BinaryReader(stream))
			{
				long fileSize = stream.Length;

				byte[] buffer = new byte[fileSize];
				int bytesRead = reader.Read(buffer, 0, System.Convert.ToInt32(fileSize));
				
				if (bytesRead != fileSize)
					throw new Exception("Couldn't read file" + fileName + ".");

				return buffer;
			}
		}

		public static void write(string fileName, byte[] BinData)
        {
            using (FileStream outFile = File.Create(fileName))
            {
                outFile.Write(BinData, 0, BinData.Length);
                outFile.Close();
            }

        }

        public static void Compress(string fileName, byte[] data)
        {
            FileStream stream = File.Create(fileName);
            GZipStream gzip = new GZipStream(stream, CompressionMode.Compress);
            gzip.Write(data, 0, data.Length);
            gzip.Close();
            stream.Close();
        }

        public static byte[] Decompress(string fileName)
        {
            using(System.IO.Stream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                return Decompress(stream);
        }

        public static byte[] Decompress(byte[] data)
        {
            using(System.IO.Stream stream = new MemoryStream(data))
                return Decompress(stream);
        }

        private static byte[] Decompress(System.IO.Stream stream)
        {
            using(GZipStream gzip = new GZipStream(stream, CompressionMode.Decompress))
            {
                Queue segments = new Queue();
                byte[] buffer = new byte[256 * 1024];

                int bytesWritten = 0;
                while(buffer.Length == (bytesWritten = gzip.Read(buffer, 0, buffer.Length)))
                {
                    segments.Enqueue(buffer);
                    buffer = new byte[buffer.Length];
                }

                byte[] output = new byte[bytesWritten + buffer.Length * segments.Count];
                int outputIndex = 0;
                foreach(byte[] b in segments)
                {
                    Buffer.BlockCopy(b, 0, output, outputIndex, buffer.Length);
                    outputIndex += buffer.Length;
                }
                Buffer.BlockCopy(buffer, 0, output, outputIndex, bytesWritten);

                segments.Clear();
                gzip.Close();
                stream.Close();
                return output;
            }
        }

        public static void writeCompressedHex(string fileName)
        {
            byte[] unc = read(fileName);
            Gzip.Compress(fileName + ".temp", unc);
            byte[] compressed = Gzip.read(fileName + ".temp");
            string res = "0x" + BitConverter.ToString(compressed).Replace("-",",0x");
            write(fileName + ".compressed", System.Text.Encoding.ASCII.GetBytes(res));
        }
    }
}
