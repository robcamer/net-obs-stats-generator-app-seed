using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;

namespace EFR.NetworkObservability.NetObsStatsGenerator;
/// <summary>
/// Implementation class for db connection factory
/// </summary>
[ExcludeFromCodeCoverage]
public class SqlConnectionFactory : IDbConnectionFactory
{
	private readonly string dbConnectionString;

	/// <summary>
	/// default constructor
	/// </summary>
	/// <param name="dbConnectionString">db connection string</param>
	public SqlConnectionFactory(string dbConnectionString)
	{
		this.dbConnectionString = dbConnectionString;
	}

	/// <summary>
	/// Creates sql connection
	/// </summary>
	/// <returns>Connection object</returns>
	public IDbConnection CreateConnection()
	{
		return new SqlConnection(this.dbConnectionString);
	}
}