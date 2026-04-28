using System.Net.Sockets;
using System.Text;

namespace NetSdrClientApp.Networking
{
    public class TcpClientWrapper : ITcpClient
    {
        private string _host;
        private int _port;
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cts;

        public bool Connected => _tcpClient != null && _tcpClient.Connected && _stream != null;
        public event EventHandler<byte[]>? MessageReceived;

        public TcpClientWrapper(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public void Connect()
        {
            if (Connected) return;
            _tcpClient = new TcpClient();
            try
            {
                _cts = new CancellationTokenSource();
                _tcpClient.Connect(_host, _port);
                _stream = _tcpClient.GetStream();
                _ = StartListeningAsync();
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        public void Disconnect()
        {
            if (!Connected) return;
            _cts?.Cancel();
            _stream?.Close();
            _tcpClient?.Close();
            _cts = null;
            _tcpClient = null;
            _stream = null;
        }

        public async Task SendMessageAsync(byte[] data)
        {
            if (!Connected || _stream == null || !_stream.CanWrite)
                throw new InvalidOperationException("Not connected");

            await _stream.WriteAsync(data, 0, data.Length);
        }

        public Task SendMessageAsync(string str) => SendMessageAsync(Encoding.UTF8.GetBytes(str));

        private async Task StartListeningAsync()
        {
            if (!Connected || _stream == null) return;
            try
            {
                while (_cts != null && !_cts.Token.IsCancellationRequested)
                {
                    byte[] buffer = new byte[8194];
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, _cts.Token);
                    if (bytesRead > 0) MessageReceived?.Invoke(this, buffer.AsSpan(0, bytesRead).ToArray());
                }
            }
            catch { /* ignore */ }
        }
    }
}