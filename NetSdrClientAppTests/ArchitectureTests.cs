using NetArchTest.Rules;
using NetSdrClientApp;
using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests
{
    public class ArchitectureTests
    {
        [Test]
        public void Networking_ShouldNot_DependOn_Messages()
        {
            var result = Types.InAssembly(typeof(NetSdrClient).Assembly)
                .That().ResideInNamespace("NetSdrClientApp.Networking")
                .ShouldNot().HaveDependencyOn("NetSdrClientApp.Messages")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True, "Networking layer must not depend on Messages layer directly!");
        }
    }
}