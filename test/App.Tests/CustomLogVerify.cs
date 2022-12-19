using System;
using Moq;
using Serilog;

namespace EFR.NetworkObservability.NetObsStatsGenerator.Test
{
  internal static class CustomLogVerify
  {
    public static void ShouldLogError(this Mock<ILogger> mockLogger, string message)
    {
      mockLogger.Verify(logger => logger.Error(It.Is<string>(s => s.Contains(message))), Times.AtLeastOnce);
    }

    public static void ShouldLogExceptionError<T>(this Mock<ILogger> mockLogger, string message) where T : Exception
    {
      mockLogger.Verify(logger => logger.Error(It.Is<T>(e => e.Message.Contains(message)), It.IsAny<string>()), Times.AtLeastOnce);
    }

    public static void ShouldLogInfo(this Mock<ILogger> mockLogger, string message)
    {
      mockLogger.Verify(logger => logger.Information(It.Is<string>(s => s.Contains(message))), Times.AtLeastOnce);
    }

    public static void ShouldLogDebug(this Mock<ILogger> mockLogger, string message)
    {
      mockLogger.Verify(logger => logger.Debug(It.Is<string>(s => s.Contains(message))), Times.AtLeastOnce);
    }
  }
}
