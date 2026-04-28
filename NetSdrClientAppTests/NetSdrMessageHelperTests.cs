using NetSdrClientApp.Messages;

namespace NetSdrClientAppTests
{
    public class NetSdrMessageHelperTests
    {
        [Test]
        public void GetControlItemMessageTest()
        {
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverState;
            int parametersLength = 10;
            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, new byte[parametersLength]);

            var headerBytes = msg.Take(2);
            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);

            Assert.That(msg.Length, Is.EqualTo(parametersLength + 4));
            Assert.That(actualType, Is.EqualTo(type));
        }

        [Test]
        public void GetDataItemMessageTest()
        {
            var type = NetSdrMessageHelper.MsgTypes.DataItem2;
            int parametersLength = 10;
            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(type, new byte[parametersLength]);

            Assert.That(msg.Length, Is.EqualTo(parametersLength + 2));
        }

        [Test]
        public void GetControlItemMessage_SmallPayloadTest()
        {
            var type = NetSdrMessageHelper.MsgTypes.SetControlItem;
            var code = NetSdrMessageHelper.ControlItemCodes.RFFilter;
            byte[] parameters = { 0x01 };
            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, parameters);

            Assert.That(msg.Length, Is.EqualTo(5));
        }

        [Test]
        public void GetControlItemMessage_EmptyParametersTest()
        {
            var type = NetSdrMessageHelper.MsgTypes.SetControlItem;
            var code = NetSdrMessageHelper.ControlItemCodes.ADModes;
            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, Array.Empty<byte>());

            Assert.That(msg.Length, Is.EqualTo(4));
        }
    }
}
