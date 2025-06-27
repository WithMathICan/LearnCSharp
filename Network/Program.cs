using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

class Program {
    // TCP Server
    private const int TCP_PORT = 12345;
    private const int UDP_PORT = 12346;
    static async Task RunTcpServerAsync() {
        try {
            TcpListener server = new TcpListener(IPAddress.Loopback, TCP_PORT);
            server.Start();
            Console.WriteLine($"[TCP Server] Running on port {TCP_PORT}...");

            using TcpClient client = await server.AcceptTcpClientAsync();
            Console.WriteLine("[TCP Server] Client connected!");

            using NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            Stopwatch stopwatch = Stopwatch.StartNew();

            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine($"[TCP Server] Received: {message}");

            await stream.WriteAsync(buffer, 0, bytesRead);
            Console.WriteLine("[TCP Server] Sent echo back to client.");

            stopwatch.Stop();
            double seconds = stopwatch.Elapsed.TotalSeconds;
            Console.WriteLine($"[TCP Server] Processed {bytesRead} bytes in {seconds:F6} seconds.");
            Console.WriteLine($"[TCP Server] Throughput: {(bytesRead / seconds / 1024 / 1024):F2} MB/s.");

            server.Stop();
        } catch (Exception ex) {
            Console.WriteLine($"[TCP Server] Error: {ex.Message}");
        }
    }

    // TCP Client
    static async Task RunTcpClientAsync() {
        try {
            // Small delay to ensure server is ready
            await Task.Delay(10);

            using TcpClient client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, TCP_PORT);
            Console.WriteLine("[TCP Client] Connected to server!");

            using NetworkStream stream = client.GetStream();
            string message = "Hello from TCP client!";
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            Stopwatch stopwatch = Stopwatch.StartNew();

            await stream.WriteAsync(buffer, 0, buffer.Length);
            Console.WriteLine($"[TCP Client] Sent: {message}");

            byte[] response = new byte[1024];
            int bytesRead = await stream.ReadAsync(response, 0, response.Length);
            string responseMessage = Encoding.UTF8.GetString(response, 0, bytesRead);
            Console.WriteLine($"[TCP Client] Received: {responseMessage}");

            stopwatch.Stop();
            double seconds = stopwatch.Elapsed.TotalSeconds;
            Console.WriteLine($"[TCP Client] Processed {bytesRead} bytes in {seconds:F6} seconds.");
            Console.WriteLine($"[TCP Client] Throughput: {(bytesRead / seconds / 1024 / 1024):F2} MB/s.");
        } catch (Exception ex) {
            Console.WriteLine($"[TCP Client] Error: {ex.Message}");
        }
    }

    static async Task RunUdpServerAsync() {
        try {
            using UdpClient server = new UdpClient(UDP_PORT);
            Console.WriteLine($"[UDP Server] Running on port {UDP_PORT}...");

            Stopwatch stopwatch = Stopwatch.StartNew();
            UdpReceiveResult result = await server.ReceiveAsync();
            string message = Encoding.UTF8.GetString(result.Buffer);
            Console.WriteLine($"[UDP Server] Received from {result.RemoteEndPoint}: {message}");

            await server.SendAsync(result.Buffer, result.Buffer.Length, result.RemoteEndPoint);
            Console.WriteLine("[UDP Server] Sent echo back to client.");

            stopwatch.Stop();
            double seconds = stopwatch.Elapsed.TotalSeconds;
            Console.WriteLine($"[UDP Server] Processed {result.Buffer.Length} bytes in {seconds:F6} seconds.");
            Console.WriteLine($"[UDP Server] Throughput: {(result.Buffer.Length / seconds / 1024 / 1024):F2} MB/s.");
        } catch (Exception ex) {
            Console.WriteLine($"[UDP Server] Error: {ex.Message}");
        }
    }

    // UDP Client
    static async Task RunUdpClientAsync() {
        try {
            // Small delay to ensure server is ready
            await Task.Delay(10);

            using UdpClient client = new UdpClient();
            IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Loopback, UDP_PORT);
            Console.WriteLine("[UDP Client] Sending to server...");

            string message = "Hello from UDP client!";
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            Stopwatch stopwatch = Stopwatch.StartNew();

            await client.SendAsync(buffer, buffer.Length, serverEndpoint);
            Console.WriteLine($"[UDP Client] Sent: {message}");

            UdpReceiveResult result = await client.ReceiveAsync();
            string responseMessage = Encoding.UTF8.GetString(result.Buffer);
            Console.WriteLine($"[UDP Client] Received from {result.RemoteEndPoint}: {responseMessage}");

            stopwatch.Stop();
            double seconds = stopwatch.Elapsed.TotalSeconds;
            Console.WriteLine($"[UDP Client] Processed {result.Buffer.Length} bytes in {seconds:F6} seconds.");
            Console.WriteLine($"[UDP Client] Throughput: {(result.Buffer.Length / seconds / 1024 / 1024):F2} MB/s.");
        } catch (Exception ex) {
            Console.WriteLine($"[UDP Client] Error: {ex.Message}");
        }
    }

    static async Task Main1(string[] args) {
        try {
            await Task.WhenAll(RunTcpServerAsync(), RunTcpClientAsync());
            await Task.WhenAll(RunUdpServerAsync(), RunUdpClientAsync());
            Console.WriteLine("Both TCP client and server have completed.");
        } catch (Exception ex) {
            Console.WriteLine($"[Main] Error: {ex.Message}");
        }
    }
}