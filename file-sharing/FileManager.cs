using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace file_sharing
{
    static class FileManager
    {
        private const int FILE_BUFFER_SIZE = 1_048_576;
        public static void ReceiveFile(byte[] fileNameData, NetworkStream networkStream)
        {
            int fileNameLength = fileNameData[9];
            byte[] fileSizeBytes = new byte[sizeof(long)];
            Array.Copy(fileNameData, 1, fileSizeBytes, 0, sizeof(long));
            long fileSize = BitConverter.ToInt64(fileSizeBytes, 0);
            byte[] fileNameBytes = new byte[fileNameLength];
            Array.Copy(fileNameData, 10, fileNameBytes, 0, fileNameLength);
            string fileName = Encoding.UTF8.GetString(fileNameBytes);
            byte[] fileBuffer = new byte[FILE_BUFFER_SIZE];
            int recBytes;
            int totalReceived = 0;
            do
            {
                int bytesToRead = fileBuffer.Length;
                if (fileSize - totalReceived < FILE_BUFFER_SIZE)
                {
                    bytesToRead = (int)(fileSize - totalReceived);
                }
                recBytes = networkStream.Read(fileBuffer, 0, bytesToRead);
                WriteToFile(fileName, fileBuffer, recBytes);
                totalReceived += recBytes;
            } while (totalReceived < fileSize);
        }

        private static void WriteToFile(string fileName, byte[] data, int count)
        {
            CreateDirectory();
            string fullPath = Path.Combine("storage", fileName);
            try
            {
                using (FileStream fileStream = new FileStream(fullPath, FileMode.Append, FileAccess.Write))
                {
                    fileStream.Write(data, 0, count);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static void CreateDirectory()
        {
            if (!Directory.Exists("storage"))
            {
                Directory.CreateDirectory("storage");
            }
        }
    }
}
