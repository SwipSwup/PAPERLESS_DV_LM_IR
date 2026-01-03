using BL.Messaging;
using Core.Configuration;
using Core.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Tests.Unit.BL.Messaging
{
    [TestFixture]
    public class RabbitMqProducerTests
    {
        [Test]
        public void Constructor_ShouldThrow_WhenConnectionFails()
        {
            // Arrange
            RabbitMqSettings settings = new RabbitMqSettings
            {
                Host = "invalid-host",
                Port = 5672,
                User = "guest",
                Password = "guest",
                QueueName = "test-queue"
            };

            // Act & Assert
            // Since we can't mock the connection factory, this will try to connect and likely fail.
            // This verifies that the service attempts connection and handles failure by wrapping in MessagingException (as seen in code).
            Mock<ILogger<RabbitMqProducer>> mockLogger = new Mock<ILogger<RabbitMqProducer>>();
            Assert.Throws<MessagingException>(() => new RabbitMqProducer(settings, mockLogger.Object));
        }
    }
}
