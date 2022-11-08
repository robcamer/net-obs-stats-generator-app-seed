using System.Data;

namespace EFR.NetworkObservability.NetObsStatsGenerator;
/// <summary>
/// Interface for db connection factory
/// </summary>
public interface IDbConnectionFactory
{
	/// <summary>
	/// Creates the connection
	/// </summary>
	IDbConnection CreateConnection();
}