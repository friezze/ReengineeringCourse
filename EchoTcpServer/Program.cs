using System.Net;
using System.Net.Sockets;

public interface ITcpListener {
    void Start();
    void Stop();
    Task<TcpClient> AcceptTcpClientAsync(CancellationToken token);
}

public class TcpListenerWrapper : ITcpListener {
    private readonly TcpListener _listener;
    public TcpListenerWrapper(int port) => _listener = new TcpListener(IPAddress.Any, port);
    public void Start() => _listener.Start();
    public void Stop() => _listener.Stop();
    public Task<TcpClient> AcceptTcpClientAsync(CancellationToken token) => _listener.AcceptTcpClientAsync(token);
}

public class EchoServer {
    private readonly ITcpListener _listener;
    private readonly CancellationTokenSource _cts = new();

    public EchoServer(ITcpListener listener) => _listener = listener;

    public async Task StartAsync() {
        _listener.Start();
        try {
            while (!_cts.Token.IsCancellationRequested) {
                var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                _ = HandleClientAsync(client);
            }
        } catch (OperationCanceledException) { }
    }

    public async Task HandleClientAsync(TcpClient client) {
        using (client)
        using (var stream = client.GetStream()) {
            byte[] buffer = new byte[1024];
            int read = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (read > 0) await stream.WriteAsync(buffer.AsMemory(0, read));
        }
    }

    public void Stop() => _cts.Cancel();
}