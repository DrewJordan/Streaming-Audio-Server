using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NAudio.Wave;

namespace AudioServer
{
    class Program
    {
        static int exceptioncount = 0;
        static int lastcount = 0;
        static void Main(string[] args)
        {
            TcpListener serverSocket = new TcpListener(IPAddress.Any, 49133);
            int requestCount = 0;
            TcpClient client = default(TcpClient);
            serverSocket.Start();
            Console.WriteLine(" >> Server Started");
            client = serverSocket.AcceptTcpClient();
            Console.WriteLine(" >> Accept connection from client");
            requestCount = 0;

            while ((true))
            {
                try
                {
                    if (exceptioncount > lastcount)
                    {
                        //client.GetStream().Close();
                        client.Close();
                        lastcount++;
                    }
                    requestCount = requestCount + 1;
                    if (!client.Connected)
                    {
                        if (serverSocket.Pending())
                        {
                            client = serverSocket.AcceptTcpClient();
                            Console.WriteLine(" >> Accept connection from client");
                        }
                        else
                            continue;
                    }
                    //if (client.)
                    NetworkStream networkStream = client.GetStream();
                    byte[] bytesFrom = new byte[65536];
                    int read = networkStream.Read(bytesFrom, 0, (int)client.ReceiveBufferSize);
                    if (read == 0)
                        continue;
                    string dataFromClient = Encoding.ASCII.GetString(bytesFrom);
                    dataFromClient = dataFromClient.Substring(0, dataFromClient.IndexOf("$"));
                    Console.WriteLine(" >> Data from client - " + dataFromClient);
                    //string response = HandleRequest(dataFromClient);
                    //string serverResponse = "Last Message from client" + dataFromClient;
                    Byte[] sendBytes = HandleRequest(dataFromClient);
                    networkStream.Write(sendBytes, 0, sendBytes.Length);
                    networkStream.Flush();
                    //Console.WriteLine(" >> " + serverResponse);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    exceptioncount++;
                }
            }

            client.Close();
            serverSocket.Stop();
            Console.WriteLine(" >> exit");
            Console.ReadLine();
        }

        private static byte[] HandleRequest(string request)
        {
            switch (request.Substring(0,2))
            {
                case "ls":
                    return ListDirectory(request.Substring(2));
                case "pl":
                    return PlayAudioFile(request.Substring(2));
                case "gl":
                    return GetLengthOfFile(request.Substring(2));
            }

            return null;
        }

        private static byte[] GetLengthOfFile(string substring)
        {
            Stream inputStream = new MemoryStream(File.ReadAllBytes(substring));
            Stream outputStream = new MemoryStream();

            using (WaveStream waveStream = WaveFormatConversionStream.CreatePcmStream(new Mp3FileReader(inputStream)))
            using (WaveFileWriter waveFileWriter = new WaveFileWriter(outputStream, waveStream.WaveFormat))
            {
                return Encoding.ASCII.GetBytes(waveStream.Length.ToString());
            }
            //byte[] ret = File.ReadAllBytes(substring);
            FileInfo f = new FileInfo(substring);
            Console.WriteLine(f.Length);
            return Encoding.ASCII.GetBytes( f.Length.ToString());
        }

        private static byte[] PlayAudioFile(string substring)
        {
            //return File.ReadAllBytes(substring);
            Console.WriteLine("Playing...");
            Stream inputStream = new MemoryStream(File.ReadAllBytes(substring));
            inputStream.Seek(0, SeekOrigin.Begin);
            Stream outputStream = new MemoryStream();
            byte[] bytes = new byte[inputStream.Length];
            string path = @"C:\temp";
            string wavFile = @"C:\temp\tempWav.wav";

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            if (File.Exists(wavFile))
                File.Delete(wavFile);

            using (var reader = new Mp3FileReader(substring))
            {
                using (var writer = new WaveFileWriter(wavFile, new WaveFormat()))
                {
                    var buf = new byte[4096];
                    for (; ; )
                    {
                        var cnt = reader.Read(buf, 0, buf.Length);
                        if (cnt == 0) break;
                        writer.Write(buf, 0, cnt);
                    }
                }
            }

            //SoundPlayer p = new SoundPlayer(wavFile);
            //p.Play();
            return File.ReadAllBytes(wavFile);
        }

        public static byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[input.Length];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        private static byte[] ListDirectory(string directory)
        {
            IEnumerable<string> fileList;
            try
            {
                fileList = Directory.EnumerateFileSystemEntries(directory);
            }
            catch (AccessViolationException ex)
            {
               return Encoding.UTF8.GetBytes("ex" + ex.Message);
            }
            StringBuilder sb = new StringBuilder();

            foreach (var file in fileList)
            {
                sb.Append(file + ";");
            }

            var x = Encoding.ASCII.GetBytes(sb.ToString());
            return x;
        }
    }
}
