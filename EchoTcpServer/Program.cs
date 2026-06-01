using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public interface ITcpListenerWrapper
{
    void Start();
    void Stop();
    Task<ITcpClientWrapper> AcceptTcpClientAsync();
}

public interface ITcpClientWrapper : IDisposable
{
    Stream GetStream();
    void Close();
}

public class TcpListenerWrapper : ITcpListenerWrapper
{
    private readonly TcpListener _listener;
    public TcpListenerWrapper(IPAddress localaddr, int port) { _listener = new TcpListener(localaddr, port); }
    public void Start() => _listener.Start();
    public void Stop() => _listener.Stop();
    public async Task<ITcpClientWrapper> AcceptTcpClientAsync() => new TcpClientWrapper(await _listener.AcceptTcpClientAsync());
}

public class TcpClientWrapper : ITcpClientWrapper
{
    private readonly TcpClient _client;
    public TcpClientWrapper(TcpClient client) { _client = client; }
    public Stream GetStream() => _client.GetStream();
    public void Close() => _client.Close();
    public void Dispose() => _client.Dispose();
}

public class EchoServer
{
    private readonly ITcpListenerWrapper _listener;
    private CancellationTokenSource _cancellationTokenSource;

    public EchoServer(ITcpListenerWrapper listener)
    {
        _listener = listener;
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public async Task StartAsync()
    {
        _listener.Start();
        Console.WriteLine("Server started.");

        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                ITcpClientWrapper client = await _listener.AcceptTcpClientAsync();
                Console.WriteLine("Client connected.");

                _ = Task.Run(() => HandleClientAsync(client, _cancellationTokenSource.Token));
            }
            catch (Exception ex) when (ex is ObjectDisposedException || ex is OperationCanceledException)
            {
                break;
            }
        }
        Console.WriteLine("Server shutdown.");
    }

    public async Task HandleClientAsync(ITcpClientWrapper client, CancellationToken token)
    {
        using (client)
        {
            try
            {
                using (Stream stream = client.GetStream())
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead;

                    while (!token.IsCancellationRequested && (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                    {
                        await stream.WriteAsync(buffer, 0, bytesRead, token);
                        Console.WriteLine($"Echoed {bytesRead} bytes to the client.");
                    }
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                client.Close();
                Console.WriteLine("Client disconnected.");
            }
        }
    }

    public void Stop()
    {
        _cancellationTokenSource.Cancel();
        _listener.Stop();
        _cancellationTokenSource.Dispose();
        Console.WriteLine("Server stopped.");
    }
}

public class Program
{
    public static async Task Main(string[] args)
    {
        var listener = new TcpListenerWrapper(IPAddress.Any, 5000);
        EchoServer server = new EchoServer(listener);

        _ = Task.Run(() => server.StartAsync());

        string host = "127.0.0.1"; 
        int port = 60000;          
        int intervalMilliseconds = 5000; 

        using (var sender = new UdpTimedSender(host, port))
        {
            Console.WriteLine("Press any key to stop sending...");
            sender.StartSending(intervalMilliseconds);

            Console.WriteLine("Press 'q' to quit...");
            while (Console.ReadKey(intercept: true).Key != ConsoleKey.Q)
            {
            }

            sender.StopSending();
            server.Stop();
            Console.WriteLine("Sender stopped.");
        }
    }
}

public class UdpTimedSender : IDisposable
{
    private readonly string _host;
    private readonly int _port;
    private readonly UdpClient _udpClient;
    private Timer _timer;

    public UdpTimedSender(string host, int port)
    {
        _host = host;
        _port = port;
        _udpClient = new UdpClient();
    }

    public void StartSending(int intervalMilliseconds)
    {
        if (_timer != null)
            throw new InvalidOperationException("Sender is already running.");

        _timer = new Timer(SendMessageCallback, null, 0, intervalMilliseconds);
    }

    ushort i = 0;

    private void SendMessageCallback(object state)
    {
        try
        {
            Random rnd = new Random();
            byte[] samples = new byte[1024];
            rnd.NextBytes(samples);
            i++;

            byte[] msg = (new byte[] { 0x04, 0x84 }).Concat(BitConverter.GetBytes(i)).Concat(samples).ToArray();
            var endpoint = new IPEndPoint(IPAddress.Parse(_host), _port);

            _udpClient.Send(msg, msg.Length, endpoint);
            Console.WriteLine($"Message sent to {_host}:{_port} ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
        }
    }

    public void StopSending()
    {
        _timer?.Dispose();
        _timer = null;
    }

    public void Dispose()
    {
        StopSending();
        _udpClient.Dispose();
    }
}