using System.Net;
using System.Net.Sockets;

public class UdpClientWrapper : IUdpClient
{
    private readonly IPEndPoint _localEndPoint;
    private CancellationTokenSource? _cts;
    private UdpClient? _udpClient;
    public event EventHandler<byte[]>? MessageReceived;

    public UdpClientWrapper(int port) => _localEndPoint = new IPEndPoint(IPAddress.Any, port);

    public async Task StartListeningAsync()
    {
        _cts = new CancellationTokenSource();
        try
        {
            _udpClient = new UdpClient(_localEndPoint);
            while (!_cts.Token.IsCancellationRequested)
            {
                UdpReceiveResult result = await _udpClient.ReceiveAsync(_cts.Token);
                MessageReceived?.Invoke(this, result.Buffer);
            }
        }
        catch { /* ignore */ }
    }

    public void StopListening()
    {
        _cts?.Cancel();
        _udpClient?.Close();
    }

    public void Exit() => StopListening();
}