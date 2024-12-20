using System.Collections.Generic;
using System.IO.Pipes;
using System.IO;
using System.Threading;
using System;
using System.Threading.Tasks;

namespace FUI.Editor
{
    internal class MessageServer : IDisposable
    {
        private const string PipeName = "FUICompilerMessage";
        private const int BufferSize = 1024;
        private readonly NamedPipeServerStream pipeServer;
        private readonly Queue<byte[]> sendQueue = new();
        private readonly CancellationTokenSource cts = new();
        private bool isRunning;

        public event Action<byte[]> MessageReceived;
        public event Action OnConnected;

        static MessageServer instance;
        public static MessageServer Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MessageServer();
                }
                return instance;
            }
        }

        MessageServer()
        {
            pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        }

        public async void Start()
        {
            if (isRunning)
            {
                return;
            }
            isRunning = true;

            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    // 等待客户端连接
                    await pipeServer.WaitForConnectionAsync(cts.Token);

                    // 启动读写任务
                    _ = ReadMessagesAsync();
                    _ = ProcessSendQueueAsync();

                    if (pipeServer.IsConnected)
                    {
                        OnConnected?.Invoke();
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常退出
            }
        }

        public void Send(byte[] message)
        {
            lock (sendQueue)
            {
                sendQueue.Enqueue(message);
            }
        }

        async Task ReadMessagesAsync()
        {
            var buffer = new byte[BufferSize];

            try
            {
                while (pipeServer.IsConnected && !cts.Token.IsCancellationRequested)
                {
                    var message = new List<byte>();
                    do
                    {
                        var bytesRead = await pipeServer.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                        if (bytesRead == 0)
                        {
                            break;
                        }

                        for (int i = 0; i < bytesRead; i++)
                        {
                            message.Add(buffer[i]);
                        }
                    }
                    while (!pipeServer.IsMessageComplete);

                    MessageReceived?.Invoke(message.ToArray());
                }
            }
            catch (IOException)
            {
                // 客户端断开连接
            }
            finally
            {
                if (pipeServer.IsConnected)
                {
                    pipeServer.Disconnect();
                }
            }
        }

        async Task ProcessSendQueueAsync()
        {
            try
            {
                while (pipeServer.IsConnected && !cts.Token.IsCancellationRequested)
                {
                    byte[]? message = null;

                    lock (sendQueue)
                    {
                        if (sendQueue.Count > 0)
                        {
                            message = sendQueue.Dequeue();
                        }
                    }

                    if (message != null)
                    {
                        await pipeServer.WriteAsync(message, 0, message.Length, cts.Token);
                        await pipeServer.FlushAsync(cts.Token);
                    }
                    else
                    {
                        await Task.Delay(100, cts.Token); // 避免空轮询
                    }
                }
            }
            catch (IOException)
            {
                // 客户端断开连接
            }
        }

        public void Stop()
        {
            isRunning = false;
            cts.Cancel();
        }

        public void Dispose()
        {
            Stop();
            cts.Dispose();
            pipeServer.Dispose();
        }
    }
}