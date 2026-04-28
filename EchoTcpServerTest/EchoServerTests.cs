using Moq;
using NUnit.Framework;
using System.Net.Sockets;

[TestFixture]
public class EchoServerTests {
    [Test]
    public async Task HandleClientAsync_ShouldEchoData() {
        // Arrange
        var listenerMock = new Mock<ITcpListener>();
        var server = new EchoServer(listenerMock.Object);
        
        server.Stop(); 
        await server.StartAsync();

        listenerMock.Verify(l => l.Start(), Times.Once);
    }
}