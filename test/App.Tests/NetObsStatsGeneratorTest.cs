using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using EFR.NetworkObservability.Common.Exceptions;
using EFR.NetworkObservability.RabbitMQ;
using MassTransit;
using Moq;
using Serilog;
using Xunit;
using static EFR.NetworkObservability.Common.Constants;

namespace EFR.NetworkObservability.NetObsStatsGenerator.Test;

[ExcludeFromCodeCoverage]
public class TestNetObsStatsGenerator
{
  private readonly NetObsStatsGenerator netObsStatsGenerator;
  private readonly Mock<ILogger> mockLogger;
  private readonly Mock<IDbConnectionFactory> connectionFactoryMock;
  private readonly Mock<IDbConnection> connectionMock;
  private readonly Mock<IDbCommand> commandMock;
  private readonly Mock<ConsumeContext<EventMetaDataMessage>> mockConsumeContext;

  public TestNetObsStatsGenerator()
  {
    Environment.SetEnvironmentVariable(RABBITMQ_HOSTNAME, "localhost");
    Environment.SetEnvironmentVariable(RABBITMQ_PORT, "5672");
    Environment.SetEnvironmentVariable(RABBITMQ_USERNAME, "rabbit");
    Environment.SetEnvironmentVariable(RABBITMQ_PASSWORD, "rabbit");

    Environment.SetEnvironmentVariable(EVENTDATA_PROCESS_QUEUE, EVENTDATA_PROCESS_QUEUE);
    Environment.SetEnvironmentVariable(DB_CONNECTION_STRING, "test");

    // Instantiate Mocks
    mockLogger = new Mock<ILogger>();

    // sql connection
    connectionMock = new Mock<IDbConnection>();
    connectionFactoryMock = new Mock<IDbConnectionFactory>();
    commandMock = new Mock<IDbCommand>();

    connectionFactoryMock.Setup(x => x.CreateConnection()).Returns(connectionMock.Object);
    connectionMock.Setup(x => x.CreateCommand()).Returns(commandMock.Object);

    // RabbitMQ Context Mocks
    mockConsumeContext = new Mock<ConsumeContext<EventMetaDataMessage>>();

    netObsStatsGenerator = new NetObsStatsGenerator(mockLogger.Object, connectionFactoryMock.Object);
  }

  private static EventMetaDataMessage BuildEventMetaDataMessage() => new()
  {
    JulianDay = "145",
    Ready = "true",
    ReProcess = "false",
    IntervalInSeconds = "300"
  };

  [Fact]
  public async void TestConsumeThrowsExceptionWithNullContext()
  {
    await netObsStatsGenerator.Consume(null);

    mockLogger.ShouldLogExceptionError<ArgumentNullException>("Value cannot be null. (Parameter 'context')");
  }

  [Fact]
  public async void TestConsumeThrowsExceptionWithNullMessage()
  {
    // Setup
    mockConsumeContext.Setup(c => c.Message).Returns((EventMetaDataMessage)null);

    // Test
    await netObsStatsGenerator.Consume(mockConsumeContext.Object);

    mockLogger.ShouldLogExceptionError<InvalidRabbitMQMessageException>("The incoming RabbitMQ message is empty");
  }

  [Fact]
  public async void TestConsumeSuccess()
  {
    // Setup
    EventMetaDataMessage message = BuildEventMetaDataMessage();
    mockConsumeContext.Setup(c => c.Message).Returns(message);

    // Test
    await netObsStatsGenerator.Consume(mockConsumeContext.Object);

    // validate openConnection
    Assert.NotNull(connectionMock);
    connectionMock.Verify(x => x.Open(), Times.Once());
    connectionFactoryMock.Verify(x => x.CreateConnection(), Times.Once());

    // validate PacketsView
    mockLogger.ShouldLogInfo("Created PacketsView");
    mockLogger.ShouldLogInfo("Created ProtocolTypes lookup table");

  }
}
