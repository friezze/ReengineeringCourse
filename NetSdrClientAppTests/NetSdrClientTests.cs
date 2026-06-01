using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests;

public class NetSdrClientTests
{
    NetSdrClient _client;
    Mock<ITcpClient> _tcpMock;
    Mock<IUdpClient> _updMock;

    public NetSdrClientTests() { }

    [SetUp]
    public void Setup()
    {
        _tcpMock = new Mock<ITcpClient>();
        _tcpMock.Setup(tcp => tcp.Connect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
        });

        _tcpMock.Setup(tcp => tcp.Disconnect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
        });

        _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>())).Callback<byte[]>((bytes) =>
        {
            _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, bytes);
        });

        _updMock = new Mock<IUdpClient>();

        _client = new NetSdrClient(_tcpMock.Object, _updMock.Object);
    }

    [Test]
    public async Task ConnectAsyncTest()
    {
        //act
        await _client.ConnectAsync();

        //assert
        _tcpMock.Verify(tcp => tcp.Connect(), Times.Once);
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
    }

    [Test]
    public async Task DisconnectWithNoConnectionTest()
    {
        //act
        _client.Disconect();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task DisconnectTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        _client.Disconect();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task StartIQNoConnectionTest()
    {

        //act
        await _client.StartIQAsync();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        _tcpMock.VerifyGet(tcp => tcp.Connected, Times.AtLeastOnce);
    }

    [Test]
    public async Task StartIQTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        await _client.StartIQAsync();

        //assert
        //No exception thrown
        _updMock.Verify(udp => udp.StartListeningAsync(), Times.Once);
        Assert.That(_client.IQStarted, Is.True);
    }

    [Test]
    public async Task StopIQTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        await _client.StopIQAsync();

        //assert
        //No exception thrown
        _updMock.Verify(tcp => tcp.StopListening(), Times.Once);
        Assert.That(_client.IQStarted, Is.False);
    }

    //TODO: cover the rest of the NetSdrClient code here
    [Test]
    public async Task StartIQAsync_CalledTwice_ExecutesTwice()
    {
        // Arrange
        await ConnectAsyncTest();

        // Act
        await _client.StartIQAsync();
        await _client.StartIQAsync();

        // Assert
        _updMock.Verify(udp => udp.StartListeningAsync(), Times.Exactly(2));
    }

    [Test]
    public async Task StopIQAsync_CalledTwice_ExecutesTwice()
    {
        // Arrange
        await ConnectAsyncTest();

        // Act
        await _client.StopIQAsync();
        await _client.StopIQAsync();

        // Assert
        _updMock.Verify(udp => udp.StopListening(), Times.Exactly(2));
    }

    [Test]
    public void Disconect_CalledTwice_ExecutesTwice()
    {
        // Act
        _client.Disconect();
        _client.Disconect();

        // Assert
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Exactly(2));
    }

    [Test]
    public async Task StartIQAsync_NeverCalls_TcpDisconnect()
    {
        // Act
        await _client.StartIQAsync();

        // Assert
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Never);
    }

    [Test]
    public async Task StopIQAsync_NeverCalls_TcpDisconnect()
    {
        // Act
        await _client.StopIQAsync();

        // Assert
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Never);
    }
}
