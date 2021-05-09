using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace file_sharing
{
    static class FileManager
    {
        public const int FILE_BUFFER_SIZE = 5_048_576;
        private static SharingWindow sharingWindow = SharingWindow.GetInstance();
        public static void ReceiveFile(byte[] fileNameData, NetworkStream networkStream)
        {
            int fileNameLength = fileNameData[9];
            byte[] fileSizeBytes = new byte[sizeof(long)];
            Array.Copy(fileNameData, 1, fileSizeBytes, 0, sizeof(long));
            long fileSize = BitConverter.ToInt64(fileSizeBytes, 0);
            byte[] fileNameBytes = new byte[fileNameLength];
            Array.Copy(fileNameData, 10, fileNameBytes, 0, fileNameLength);
            string fileName = Encoding.UTF8.GetString(fileNameBytes);
            fileName = AvoidOverwriting(fileName);
            byte[] fileBuffer = new byte[FILE_BUFFER_SIZE];
            int recBytes;
            int totalReceived = 0;
            try
            {
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
                    double percentReceived = ((double)totalReceived) / fileSize;
                    int progress = Convert.ToInt32(percentReceived * 100);
                    SharingWindow.GetInstance().Dispatcher.Invoke(
                        () => SharingWindow.SetReceivingProgress(fileName, progress));
                } while (totalReceived < fileSize);
            } catch
            {
                MessageBox.Show("Connection was broken. File was not received", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            SharingWindow.GetInstance().Dispatcher.Invoke(
                    () => SharingWindow.ResetReceivingProggress());
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
