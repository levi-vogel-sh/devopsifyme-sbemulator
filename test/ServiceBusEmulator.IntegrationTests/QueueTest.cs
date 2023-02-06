using AutoFixture;
using Azure.Messaging.ServiceBus;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace ServiceBusEmulator.IntegrationTests
{
    [Collection(Consts.QueueCollection)]
    public class QueueTest : Base
    {
        [Fact]
        public async Task ThatMessageIsReceived()
        {
            string messageBody = Fixture.Create<string>();

            var sender = Client.CreateSender(Consts.TestQueueName);
            var receiver = Client.CreateReceiver(Consts.TestQueueName, new ServiceBusReceiverOptions
            {
                ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
            });

            await sender.SendMessageAsync(new ServiceBusMessage(messageBody));
            var receivedMessage = await receiver.ReceiveMessageAsync();
            var nextMessage = await receiver.PeekMessageAsync();

            Assert.Equal(messageBody, receivedMessage.Body.ToString());
            Assert.Null(nextMessage);
        }

        [Fact]
        public async Task ThatMessageIsConfirmed()
        {
            string messageBody = Fixture.Create<string>();

            var sender = Client.CreateSender(Consts.TestQueueName);
            await sender.SendMessageAsync(new ServiceBusMessage(messageBody));

            var receiver = Client.CreateReceiver(Consts.TestQueueName, new ServiceBusReceiverOptions
            {
                ReceiveMode = ServiceBusReceiveMode.PeekLock
            });

            // Abandon
            {
                var receivedMessage = await receiver.ReceiveMessageAsync();
                await receiver.AbandonMessageAsync(receivedMessage);
                var abandonedMessage = await receiver.PeekMessageAsync();

                Assert.Multiple(
                    () => Assert.Equal(messageBody, receivedMessage.Body.ToString()),
                    () => Assert.Equal(messageBody, abandonedMessage.Body.ToString())
                );
            }

            // Complete
            {
                var receivedMessage = await receiver.ReceiveMessageAsync();
                await receiver.CompleteMessageAsync(receivedMessage);
                var nextMessage = await receiver.PeekMessageAsync();

                Assert.Multiple(
                    () => Assert.Equal(messageBody, receivedMessage.Body.ToString()),
                    () => Assert.Null(nextMessage)
                );
            }
        }
    }
}