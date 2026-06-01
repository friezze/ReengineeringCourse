using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace EchoTcpServerTests
{
    public class EchoServerTests
    {
        private Mock<ITcpListenerWrapper> _listenerMock;
        private Mock<ITcpClientWrapper> _clientMock;
        private EchoServer _server;

        [SetUp]
        public void Setup()
        {
            _listenerMock = new Mock<ITcpListenerWrapper>();
            _clientMock = new Mock<ITcpClientWrapper>();
            _server = new EchoServer(_listenerMock.Object);
        }

        [Test]
        public async Task HandleClientAsync_EchoesData_Correctly()
        {
            // Arrange
            byte[] inputData = new byte[] { 1, 2, 3, 4, 5 };
            var memoryStream = new MemoryStream();
            memoryStream.Write(inputData, 0, inputData.Length);
            memoryStream.Position = 0; 

            _clientMock.Setup(c => c.GetStream()).Returns(memoryStream);
            var cts = new CancellationTokenSource();

            // Act
            await _server.HandleClientAsync(_clientMock.Object, cts.Token);

            // Assert
            _clientMock.Verify(c => c.GetStream(), Times.Once);
            _clientMock.Verify(c => c.Close(), Times.Once);

            Assert.That(memoryStream.Length, Is.EqualTo(inputData.Length * 2));

            memoryStream.Position = inputData.Length;
            byte[] echoedData = new byte[inputData.Length];
            memoryStream.Read(echoedData, 0, echoedData.Length);

            Assert.That(echoedData, Is.EqualTo(inputData));
        }

        [Test]
        public void Stop_CancelsTokenAndStopsListener()
        {
            // Act
            _server.Stop();

            // Assert
            _listenerMock.Verify(l => l.Stop(), Times.Once);
        }
        
        [Test]
        public async Task StartAsync_AcceptsClients_UntilCancelled()
        {
            // Arrange
            var tcs = new TaskCompletionSource<ITcpClientWrapper>();
            _listenerMock.SetupSequence(l => l.AcceptTcpClientAsync())
                .ReturnsAsync(_clientMock.Object)
                .Returns(tcs.Task); 

            // Act
            var startTask = _server.StartAsync();
            await Task.Delay(50);
            
            _server.Stop();
            tcs.SetCanceled(); 

            // Assert
            _listenerMock.Verify(l => l.Start(), Times.Once);
            _listenerMock.Verify(l => l.AcceptTcpClientAsync(), Times.AtLeastOnce);
        }
    }
}