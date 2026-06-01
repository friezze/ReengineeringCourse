using NetArchTest.Rules;
using NUnit.Framework;
using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests
{
    public class ArchitectureTests
    {
        [Test]
        public void Messages_ShouldNotDependOn_Networking()
        {
            // Arrange
            var result = Types.InAssembly(typeof(TcpClientWrapper).Assembly)
                .That()
                .ResideInNamespace("NetSdrClientApp.Messages")
                .ShouldNot()
                .HaveDependencyOn("NetSdrClientApp.Networking")
                .GetResult();

            // Assert
            Assert.That(result.IsSuccessful, Is.True, "Messages namespace has a prohibited dependency on Networking.");
        }
    }
}