using System.Diagnostics.CodeAnalysis;
using EFR.NetworkObservability.Common;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EFR.NetworkObservability.NetObsStatsGenerator;

/// <summary>
/// Initializes .NET Host and starts services
/// </summary>
[ExcludeFromCodeCoverage]
public class Program
{
	public static IHost CreateHost(string[] args)
		=> Host.CreateDefaultBuilder(args)
				.ConfigureServices((hostContext, services) =>
				{
					string eventDataQueue = Utils.GetEnvVar(Constants.EVENTDATA_PROCESS_QUEUE);
					string dbConnectionString = Utils.GetEnvVar(Constants.DB_CONNECTION_STRING);
					ushort rabbitMQPort = Utils.GetEnvVarOrDefault<ushort>(Constants.RABBITMQ_PORT, 5672);
					string rabbitMQHostname = Utils.GetEnvVar(Constants.RABBITMQ_HOSTNAME);
					string rabbitMQUsername = Utils.GetEnvVar(Constants.RABBITMQ_USERNAME);
					string rabbitMQPassword = Utils.GetEnvVar(Constants.RABBITMQ_PASSWORD);

					services.AddSingleton<NetObsStatsGenerator>();
					services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>(s => new SqlConnectionFactory(dbConnectionString));

					// MassTransit
					services.AddMassTransit(mtConfig =>
					{
						mtConfig.AddConsumer<NetObsStatsGenerator>();

						mtConfig.UsingRabbitMq((context, rabbitConfig) =>
						{
							rabbitConfig.Host(rabbitMQHostname, rabbitMQPort, "/", hostConfig =>
							{
								hostConfig.Username(rabbitMQUsername);
								hostConfig.Password(rabbitMQPassword);
							});


							rabbitConfig.ReceiveEndpoint(eventDataQueue, e =>
							{
								e.ConfigureConsumer<NetObsStatsGenerator>(context);
							});
						});
					});
				}).Build();

	public static async Task Main(string[] args) => await CreateHost(args).RunAsync();

}