using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq.Expressions;
using EFR.NetworkObservability.RabbitMQ;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using static EFR.NetworkObservability.Common.Constants;

namespace EFR.NetworkObservability.NetObsStatsGenerator.Test;

[ExcludeFromCodeCoverage]
public class TestNetObsStatsGenerator : IDisposable
{

	private string tmpDir;
	private const long DEFAULT_JOB_PK = 123456789;
	private NetObsStatsGenerator netObsStatsGenerator;
	private Mock<ILogger<NetObsStatsGenerator>> mockLogger;
	private Mock<IDbConnectionFactory> connectionFactoryMock;
	private Mock<IDbConnection> connectionMock;
	private Mock<IDbCommand> commandMock;
	private Mock<ConsumeContext<EventMetaDataMessage>> mockConsumeContext;

	public TestNetObsStatsGenerator()
	{

		tmpDir = Path.GetTempPath();
		Environment.SetEnvironmentVariable(RABBITMQ_HOSTNAME, "localhost");
		Environment.SetEnvironmentVariable(RABBITMQ_PORT, "5672");
		Environment.SetEnvironmentVariable(RABBITMQ_USERNAME, "rabbit");
		Environment.SetEnvironmentVariable(RABBITMQ_PASSWORD, "rabbit");

		Environment.SetEnvironmentVariable(EVENTDATA_PROCESS_QUEUE, EVENTDATA_PROCESS_QUEUE);
		Environment.SetEnvironmentVariable(DB_CONNECTION_STRING, "test");

		// Instantiate Mocks
		mockLogger = new Mock<ILogger<NetObsStatsGenerator>>();

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

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		// do nothing
	}

	private EventMetaDataMessage BuildEventMetaDataMessage()
	{
		EventMetaDataMessage msg = new EventMetaDataMessage();
		msg.JulianDay = "145";
		msg.Ready = "true";
		msg.ReProcess = "false";
		msg.IntervalInSeconds = "300";
		return msg;
	}

	private EventMetaDataMessage BuildLastYearEventMetaDataMessage()
	{
		EventMetaDataMessage msg = new EventMetaDataMessage();
		msg.JulianDay = DateTime.UtcNow.AddDays(1).DayOfYear.ToString();
		msg.Ready = "true";
		msg.ReProcess = "false";
		msg.IntervalInSeconds = "300";
		return msg;
	}

	private EventMetaDataMessage BuildThisYearEventMetaDataMessage()
	{
		EventMetaDataMessage msg = new EventMetaDataMessage();
		msg.JulianDay = DateTime.UtcNow.DayOfYear.ToString();
		msg.Ready = "true";
		msg.ReProcess = "false";
		msg.IntervalInSeconds = "300";
		return msg;
	}

	[Fact]
	public async void TestConsumeThrowsExceptionWithNullMessage()
	{
		// Setup
		mockConsumeContext.Setup(c => c.Message).Returns((EventMetaDataMessage)null);

		// Test
		await netObsStatsGenerator.Consume(mockConsumeContext.Object);

		// Validate
		Expression<Func<object, Type, bool>> matcher = (object value, Type type) =>
			(value.ToString().StartsWith("The incoming RabbitMQ message is invalid")
				&& value.ToString().Contains("The incoming RabbitMQ message is empty"));
		TestUtils.VerifyLog(mockLogger, LogLevel.Error, matcher: matcher);
	}

	[Fact]
	public async void TestConsumeThrowsExceptionWithMissingJulianDay()
	{
		// Setup
		EventMetaDataMessage message = BuildEventMetaDataMessage();
		message.JulianDay = null;
		mockConsumeContext.Setup(c => c.Message).Returns(message);

		// Test
		await netObsStatsGenerator.Consume(mockConsumeContext.Object);

		// Validate
		Expression<Func<object, Type, bool>> matcher = (object value, Type type) =>
			(value.ToString().StartsWith("The incoming RabbitMQ message is invalid")
				&& value.ToString().Contains("The incoming RabbitMQ message does not contain a JulianDay"));
		TestUtils.VerifyLog(mockLogger, LogLevel.Error, matcher: matcher);
	}

	[Fact]
	public async void TestConsumeThrowsExceptionWithMissingIntervalInSeconds()
	{
		// Setup
		EventMetaDataMessage message = BuildEventMetaDataMessage();
		message.IntervalInSeconds = null;
		mockConsumeContext.Setup(c => c.Message).Returns(message);

		// Test
		await netObsStatsGenerator.Consume(mockConsumeContext.Object);

		// Validate
		Expression<Func<object, Type, bool>> matcher = (object value, Type type) =>
			(value.ToString().StartsWith("The incoming RabbitMQ message is invalid")
				&& value.ToString().Contains("The incoming RabbitMQ message does not contain a IntervalInSeconds"));
		TestUtils.VerifyLog(mockLogger, LogLevel.Error, matcher: matcher);
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
		TestUtils.VerifyLog(mockLogger, LogLevel.Debug, String.Format("PacketsView doesnt exists"));
		TestUtils.VerifyLog(mockLogger, LogLevel.Debug, String.Format("Created PacketsView"));

		//validate Tally and reportIntervals Creation
		TestUtils.VerifyLog(mockLogger, LogLevel.Debug, String.Format("Created ReportIntervals"));
		TestUtils.VerifyLog(mockLogger, LogLevel.Debug, String.Format("Created Tally"));

		// Validate insert to report intervals
		TestUtils.VerifyLog(mockLogger, LogLevel.Debug, String.Format("Successfully Inserted Data into ReportIntervals"));

	}

	[Fact]
	public async void TestConsumeSuccessWithExistingView()
	{
		// Setup
		EventMetaDataMessage message = BuildEventMetaDataMessage();
		mockConsumeContext.Setup(c => c.Message).Returns(message);

		string sql = @"IF EXISTS(select * FROM sys.views where name = 'PacketsView') SELECT 1 ELSE SELECT 0";
		commandMock.Setup(x => x.CommandText).Returns(sql);
		commandMock.Setup(x => x.ExecuteScalar()).Returns(1);

		// Test
		await netObsStatsGenerator.Consume(mockConsumeContext.Object);

		// validate openConnection
		Assert.NotNull(connectionMock);
		connectionMock.Verify(x => x.Open(), Times.Once());
		connectionFactoryMock.Verify(x => x.CreateConnection(), Times.Once());

		// validate PacketsView
		TestUtils.VerifyLog(mockLogger, LogLevel.Debug, String.Format("PacketsView already exists"));
		TestUtils.VerifyLog(mockLogger, LogLevel.Debug, Times.Never(), "Created PacketsView");

	}


	[Fact]
	public async void TestConsumelastYearSuccess()
	{
		// Setup
		EventMetaDataMessage message = BuildLastYearEventMetaDataMessage();
		mockConsumeContext.Setup(c => c.Message).Returns(message);

		// Test
		await netObsStatsGenerator.Consume(mockConsumeContext.Object);

		// validate openConnection
		Assert.NotNull(connectionMock);
		connectionMock.Verify(x => x.Open(), Times.Once());
		connectionFactoryMock.Verify(x => x.CreateConnection(), Times.Once());

		// validate PacketsView
		TestUtils.VerifyLog(mockLogger, LogLevel.Debug, String.Format("PacketsView doesnt exists"));
		TestUtils.VerifyLog(mockLogger, LogLevel.Debug, String.Format("Created PacketsView"));

		//validate Tally and reportIntervals Creation
		TestUtils.VerifyLog(mockLogger, LogLevel.Debug, String.Format("Created ReportIntervals"));
		TestUtils.VerifyLog(mockLogger, LogLevel.Debug, String.Format("Created Tally"));

		// Validate insert to report intervals
		TestUtils.VerifyLog(mockLogger, LogLevel.Debug, String.Format("Successfully Inserted Data into ReportIntervals"));
	}

	[Fact]
	public async void TestConsumeThisYearSuccess()
	{
		// Setup
		EventMetaDataMessage message = BuildThisYearEventMetaDataMessage();
		mockConsumeContext.Setup(c => c.Message).Returns(message);

		// Test
		await netObsStatsGenerator.Consume(mockConsumeContext.Object);

		// validate openConnection
		Assert.NotNull(connectionMock);
		connectionMock.Verify(x => x.Open(), Times.Once());
		connectionFactoryMock.Verify(x => x.CreateConnection(), Times.Once());

		// validate PacketsView
		TestUtils.VerifyLog(mockLogger, LogLevel.Debug, String.Format("PacketsView doesnt exists"));
		TestUtils.VerifyLog(mockLogger, LogLevel.Debug, String.Format("Created PacketsView"));

		//validate Tally and reportIntervals Creation
		TestUtils.VerifyLog(mockLogger, LogLevel.Debug, String.Format("Created ReportIntervals"));
		TestUtils.VerifyLog(mockLogger, LogLevel.Debug, String.Format("Created Tally"));

		// Validate insert to report intervals
		TestUtils.VerifyLog(mockLogger, LogLevel.Debug, String.Format("Successfully Inserted Data into ReportIntervals"));
	}
	
}

public static class TestUtils
{
	public static void VerifyLog<T>(Mock<ILogger<T>> mockLogger, LogLevel logLevel, string? message = null, Expression<Func<object, Type, bool>>? matcher = null)
	{
		VerifyLog(mockLogger, logLevel, Times.Once(), message, matcher);
	}

	public static void VerifyLog<T>(Mock<ILogger<T>> mockLogger, LogLevel logLevel, Times times, string? message = null, Expression<Func<object, Type, bool>>? matcher = null)
	{
		if (matcher == null)
		{
			matcher = (value, type) => string.Equals(message, value.ToString(), StringComparison.InvariantCultureIgnoreCase);
		}

		mockLogger.Verify(
					x => x.Log(
						logLevel,
						It.IsAny<EventId>(),
						It.Is<It.IsAnyType>(matcher),
						It.IsAny<Exception>(),
						It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
					times);
	}
}