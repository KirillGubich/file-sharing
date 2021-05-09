using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Windows;

namespace file_sharing
{
    static class FileManager
    {
        public const int FILE_BUFFER_SIZE = 5_048_576;
        private static SharingWindow sharingWindow = SharingWindow.GetInstance();

        public static void ReceiveFile(byte[] fileNameData, NetworkStream networkStream)
        {
            long fileSize = ExtractFileSize(fileNameData);
            string fileName = ExtractFileName(fileNameData);
            fileName = AvoidOverwriting(fileName);
            try
            {
                byte[] fileBuffer = new byte[FILE_BUFFER_SIZE];
                int recBytes;
                int totalReceived = 0;
                sharingWindow.Dispatcher.Invoke(() => SharingWindow.SetReceivingFile(fileName));
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
                    int progress = CountProgress(totalReceived, fileSize);
                    sharingWindow.Dispatcher.Invoke(() => SharingWindow.SetReceivingProgress(progress));
                } while (totalReceived < fileSize);
            }
            catch
            {
                MessageBox.Show("Connection was broken. File was not received", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            sharingWindow.Dispatcher.Invoke(() => SharingWindow.ResetReceivingProggress());
        }

        private static string ExtractFileName(byte[] fileNameData)
        {
            int fileNameLength = fileNameData[Messenger.FILE_NAME_LENGTH_INDEX];
            byte[] fileNameBytes = new byte[fileNameLength];
            Array.Copy(fileNameData, Messenger.FILE_NAME_BYTE_INDEX, fileNameBytes, 0, fileNameLength);
            return Encoding.UTF8.GetString(fileNameBytes);
        }

        private static long ExtractFileSize(byte[] fileNameData)
        {
            byte[] fileSizeBytes = new byte[sizeof(long)];
            Array.Copy(fileNameData, Messenger.FILE_SIZE_BYTE_INDEX, fileSizeBytes, 0, sizeof(long));
            return BitConverter.ToInt64(fileSizeBytes, 0);
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

        public static int CountProgress(long totalSent, long fileSize)
        {
            double percentReceived = ((double)totalSent) / fileSize;
            return Convert.ToInt32(percentReceived * 100);
        }

        private static string AvoidOverwriting(string fileName)
        {
            int i = 1;
            string updFileName = fileName;
            string fullPath = Path.Combine("storage", updFileName);
            while (File.Exists(fullPath))
            {
                updFileName = "(" + i + ")" + fileName;
                fullPath = Path.Combine("storage", updFileName);
                i++;

            }
            return updFileName;
        }
    }
}
