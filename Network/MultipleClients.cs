using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Network {
    class MultipleClients {
        // TCP Server: Handles three clients concurrently
        static async Task RunTcpServerAsync() {
            try {
                TcpListener server = new TcpListener(IPAddress.Loopback, 12345);
                server.Start();
                Console.WriteLine("[TCP Server] Running on port 12345...");

                // Store client tasks
                Task[] clientTasks = new Task[3];
                for (int i = 0; i < 3; i++) {
                    clientTasks[i] = HandleTcpClientAsync(await server.AcceptTcpClientAsync(), i + 1);
                }

                // Wait for all clients to complete
                await Task.WhenAll(clientTasks);
                Console.WriteLine("[TCP Server] All clients processed.");

                server.Stop();
            } catch (Exception ex) {
                Console.WriteLine($"[TCP Server] Error: {ex.Message}");
            }
        }

        // Handle individual TCP client
        static async Task HandleTcpClientAsync(TcpClient client, int clientId) {
            try {
                Console.WriteLine($"[TCP Server] Client {clientId} connected!");
                using NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[10000]; // 10KB
                long totalBytesReceived = 0;
                Stopwatch stopwatch = Stopwatch.StartNew();

                while (true) {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break; // Client closed connection
                    totalBytesReceived += bytesRead;
                    await stream.WriteAsync(buffer, 0, bytesRead);
                }

                stopwatch.Stop();
                double seconds = stopwatch.Elapsed.TotalSeconds;
                Console.WriteLine($"[TCP Server] Client {clientId}: Processed {totalBytesReceived} bytes in {seconds:F6} seconds, Throughput: {(seconds > 0 ? totalBytesReceived / seconds / 1024 / 1024 : 0):F2} MB/s.");
            } catch (Exception ex) {
                Console.WriteLine($"[TCP Server] Client {clientId} Error: {ex.Message}");
            } finally {
                client.Close();
            }
        }

        // TCP Client
        static async Task RunTcpClientAsync(int clientId, char dataChar) {
            try {
                await Task.Delay(100); // Ensure server is ready
                using TcpClient client = new TcpClient();
                await client.ConnectAsync(IPAddress.Loopback, 12345);
                Console.WriteLine($"[TCP Client {clientId}] Connected to server!");

                using NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[10000]; // 10KB
                Array.Fill(buffer, (byte)dataChar); // Unique data (e.g., 'A', 'B', 'C')
                long totalBytesSent = 0, totalBytesReceived = 0;
                Stopwatch stopwatch = Stopwatch.StartNew();

                for (int i = 0; i < 100; i++) // 1MB total (100 * 10KB)
                {
                    await stream.WriteAsync(buffer, 0, buffer.Length);
                    totalBytesSent += buffer.Length;
                }

                byte[] response = new byte[10000];
                while (totalBytesReceived < totalBytesSent) {
                    int bytesRead = await stream.ReadAsync(response, 0, response.Length);
                    if (bytesRead == 0) break;
                    totalBytesReceived += bytesRead;
                }

                stopwatch.Stop();
                double seconds = stopwatch.Elapsed.TotalSeconds;
                Console.WriteLine($"[TCP Client {clientId}] Sent {totalBytesSent} bytes, received {totalBytesReceived} bytes in {seconds:F6} seconds, Throughput: {(seconds > 0 ? totalBytesSent / seconds / 1024 / 1024 : 0):F2} MB/s.");
            } catch (Exception ex) {
                Console.WriteLine($"[TCP Client {clientId}] Error: {ex.Message}");
            }
        }

        static async Task RunUdpServerAsync() {
            try {
                using UdpClient server = new UdpClient(12346);
                Console.WriteLine("[UDP Server] Running on port 12346...");

                // Track bytes and timers per client endpoint
                ConcurrentDictionary<IPEndPoint, (long Bytes, Stopwatch Timer)> clientData = new ConcurrentDictionary<IPEndPoint, (long, Stopwatch)>();
                CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // 10s timeout

                try {
                    while (!cts.Token.IsCancellationRequested) {
                        var receiveTask = server.ReceiveAsync();
                        var completedTask = await Task.WhenAny(receiveTask, Task.Delay(Timeout.Infinite, cts.Token));
                        if (completedTask != receiveTask) break; // Timeout

                        UdpReceiveResult result = await receiveTask;
                        IPEndPoint clientEndpoint = result.RemoteEndPoint;

                        // Initialize client data if new
                        clientData.GetOrAdd(clientEndpoint, _ => (0, Stopwatch.StartNew()));

                        // Update bytes
                        long newBytes = clientData[clientEndpoint].Bytes + result.Buffer.Length;
                        clientData[clientEndpoint] = (newBytes, clientData[clientEndpoint].Timer);

                        // Echo back
                        await server.SendAsync(result.Buffer, result.Buffer.Length, clientEndpoint);

                        // Report if client is done
                        if (newBytes >= 1000000) {
                            var (bytes, timer) = clientData[clientEndpoint];
                            timer.Stop();
                            double seconds = timer.Elapsed.TotalSeconds;
                            Console.WriteLine($"[UDP Server] Client {clientEndpoint}: Processed {bytes} bytes in {seconds:F6} seconds, Throughput: {(seconds > 0 ? bytes / seconds / 1024 / 1024 : 0):F2} MB/s.");
                            clientData.TryRemove(clientEndpoint, out _);
                        }

                        // Debug: Log progress
                        Console.WriteLine($"[UDP Server] Received {result.Buffer.Length} bytes from {clientEndpoint}, Total clients: {clientData.Count}");
                    }
                } catch (OperationCanceledException) {
                    Console.WriteLine("[UDP Server] Timed out after 10 seconds.");
                }

                // Report any incomplete clients
                foreach (var (endpoint, (bytes, timer)) in clientData) {
                    timer.Stop();
                    double seconds = timer.Elapsed.TotalSeconds;
                    Console.WriteLine($"[UDP Server] Client {endpoint}: Incomplete, processed {bytes} bytes in {seconds:F6} seconds.");
                }
            } catch (Exception ex) {
                Console.WriteLine($"[UDP Server] Error: {ex.Message}");
            }
        }

        // UDP Client with timeout
        static async Task RunUdpClientAsync(int clientId, char dataChar) {
            try {
                await Task.Delay(100);
                using UdpClient client = new UdpClient();
                IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Loopback, 12346);
                Console.WriteLine($"[UDP Client {clientId}] Sending to server...");

                byte[] buffer = new byte[10000]; // 10KB
                Array.Fill(buffer, (byte)dataChar);
                long totalBytesSent = 0, totalBytesReceived = 0;
                Stopwatch stopwatch = Stopwatch.StartNew();
                CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                // Send 1MB
                for (int i = 0; i < 100; i++) {
                    await client.SendAsync(buffer, buffer.Length, serverEndpoint);
                    totalBytesSent += buffer.Length;
                }

                // Receive echoed data
                try {
                    while (totalBytesReceived < totalBytesSent && !cts.Token.IsCancellationRequested) {
                        var receiveTask = client.ReceiveAsync();
                        var completedTask = await Task.WhenAny(receiveTask, Task.Delay(Timeout.Infinite, cts.Token));
                        if (completedTask != receiveTask) break;

                        UdpReceiveResult result = await receiveTask;
                        if (result.RemoteEndPoint.Equals(serverEndpoint)) {
                            totalBytesReceived += result.Buffer.Length;
                            Console.WriteLine($"[UDP Client {clientId}] Received {result.Buffer.Length} bytes, Total: {totalBytesReceived}");
                        }
                    }
                } catch (OperationCanceledException) {
                    Console.WriteLine($"[UDP Client {clientId}] Receive timed out after 10 seconds.");
                }

                stopwatch.Stop();
                double seconds = stopwatch.Elapsed.TotalSeconds;
                Console.WriteLine($"[UDP Client {clientId}] Sent {totalBytesSent} bytes, received {totalBytesReceived} bytes in {seconds:F6} seconds, Throughput: {(seconds > 0 ? totalBytesSent / seconds / 1024 / 1024 : 0):F2} MB/s.");
            } catch (Exception ex) {
                Console.WriteLine($"[UDP Client {clientId}] Error: {ex.Message}");
            }
        }
        

        static async Task Main(string[] args) {
            try {
                await Task.WhenAll([
                    RunTcpServerAsync(),
                    RunTcpClientAsync(1, 'A'),
                    RunTcpClientAsync(2, 'B'),
                    RunTcpClientAsync(3, 'C'),
                ]);
                await Task.WhenAll([
                    RunUdpServerAsync(),
                    RunUdpClientAsync(1, 'A'),
                    RunUdpClientAsync(2, 'B'),
                    RunUdpClientAsync(3, 'C'),
                ]);

                Console.WriteLine("All TCP and UDP clients and servers have completed.");
            } catch (Exception ex) {
                Console.WriteLine($"[Main] Error: {ex.Message}");
            }
        }
    }
}