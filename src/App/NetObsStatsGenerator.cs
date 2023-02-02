using System.Data;
using EFR.NetworkObservability.Common.Exceptions;
using EFR.NetworkObservability.RabbitMQ;
using MassTransit;
using Serilog;

namespace EFR.NetworkObservability.NetObsStatsGenerator;
public class NetObsStatsGenerator : IConsumer<EventMetaDataMessage>
{
  private readonly ILogger logger;
  private readonly IDbConnectionFactory connectionFactory;

  /// <summary>
  /// Default constructor, Instantiates the NetObsStatsGenerator with required parameters
  /// </summary>
  /// <param name="logger">logger object for the class</param>
  /// <param name="connectionFactory">db connection factory instance</param>
  public NetObsStatsGenerator(ILogger logger, IDbConnectionFactory connectionFactory)
  {
    this.logger = logger;
    this.connectionFactory = connectionFactory;
  }

  /// <summary>
  /// Consumer API for the EventMetaDataMessage rabbit mq message
  /// </summary>
  /// <param name="context">EventMetaDataMessage rabbit mq messages</param>
  public async Task Consume(ConsumeContext<EventMetaDataMessage> context)
  {
    try
    {
      ArgumentNullException.ThrowIfNull(context, nameof(context));
      InvalidRabbitMQMessageException.ThrowIfMessageNull(context.Message);
      logger.Information($"EventMetaData rabbitmq message recieved");

      using (var connection = connectionFactory.CreateConnection())
      {
        connection.Open();
        CreatePacketsView(connection);
        CreateLookupProtocolTypes(connection);
      }

      await Task.Yield();
    }
    catch (InvalidRabbitMQMessageException e)
    {
      logger.Error(e, "The incoming RabbitMQ message is invalid!");
    }
    catch (ArgumentNullException e)
    {
      logger.Error(e, "The incoming RabbitMQ context is null!");
    }
    catch (Exception e)
    {
      logger.Error(e, "An unexpected exception occurred during NetObsStats Generator!");
    }
  }

  /// <summary>
  /// Create lookup table "ProtocolTypes" containing index to friendly name for standard protocols.
  /// </summary>
  private void CreateLookupProtocolTypes(IDbConnection conn)
  {
    using var command = conn.CreateCommand();

    string sql = @"IF EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ProtocolTypes') SELECT 1 ELSE SELECT 0";

    command!.CommandText = sql;
    command.ExecuteNonQuery();
    if (Convert.ToInt32(command.ExecuteScalar()) == 1) return;

    sql = @"CREATE TABLE [ProtocolTypes](
			[protocolTypeId] [tinyint] NOT NULL,
			[protocolType] [varchar](30) NOT NULL,
		CONSTRAINT [PK_ProtocolTypes] PRIMARY KEY CLUSTERED
		(
			[protocolTypeId] ASC
		)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
		) ON [PRIMARY];";

    command!.CommandText = sql;
    command.ExecuteNonQuery();

    sql = @"
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (0, N'HOPOPT');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (1, N'ICMP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (2, N'IGMP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (3, N'GGP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (4, N'IPv4');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (5, N'ST');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (6, N'TCP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (7, N'CBT');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (8, N'EGP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (9, N'IGP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (10, N'BBN-RCC-MON');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (11, N'NVP-II');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (12, N'PUP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (13, N'ARGUS (deprecated)');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (14, N'EMCON');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (15, N'XNET');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (16, N'CHAOS');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (17, N'UDP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (18, N'MUX');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (19, N'DCN-MEAS');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (20, N'HMP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (21, N'PRM');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (22, N'XNS-IDP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (23, N'TRUNK-1');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (24, N'TRUNK-2');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (25, N'LEAF-1');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (26, N'LEAF-2');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (27, N'RDP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (28, N'IRTP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (29, N'ISO-TP4');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (30, N'NETBLT');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (31, N'MFE-NSP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (32, N'MERIT-INP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (33, N'DCCP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (34, N'3PC');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (35, N'IDPR');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (36, N'XTP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (37, N'DDP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (38, N'IDPR-CMTP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (39, N'TP++');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (40, N'IL');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (41, N'IPv6');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (42, N'SDRP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (43, N'IPv6-Route');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (44, N'IPv6-Frag');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (45, N'IDRP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (46, N'RSVP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (47, N'GRE');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (48, N'DSR');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (49, N'BNA');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (50, N'ESP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (51, N'AH');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (52, N'I-NLSP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (53, N'SWIPE (deprecated)');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (54, N'NARP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (55, N'MOBILE');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (56, N'TLSP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (57, N'SKIP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (58, N'IPv6-ICMP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (59, N'IPv6-NoNxt');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (60, N'IPv6-Opts');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (61, N'Any host internal protocol');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (62, N'CFTP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (63, N'Any local network');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (64, N'SAT-EXPAK');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (65, N'KRYPTOLAN');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (66, N'RVD');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (67, N'IPPC');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (68, N'Any distributed file system');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (69, N'SAT-MON');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (70, N'VISA');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (71, N'IPCV');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (72, N'CPNX');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (73, N'CPHB');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (74, N'WSN');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (75, N'PVP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (76, N'BR-SAT-MON');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (77, N'SUN-ND');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (78, N'WB-MON');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (79, N'WB-EXPAK');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (80, N'ISO-IP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (81, N'VMTP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (82, N'SECURE-VMTP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (83, N'VINES');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (84, N'TTP-IPTM');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (85, N'NSFNET-IGP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (86, N'DGP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (87, N'TCF');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (88, N'EIGRP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (89, N'OSPFIGP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (90, N'Sprite-RPC');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (91, N'LARP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (92, N'MTP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (93, N'AX.25');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (94, N'IPIP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (95, N'MICP (deprecated)');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (96, N'SCC-SP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (97, N'ETHERIP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (98, N'ENCAP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (99, N'Any private encryption scheme');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (100, N'GMTP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (101, N'IFMP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (102, N'PNNI');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (103, N'PIM');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (104, N'ARIS');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (105, N'SCPS');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (106, N'QNX');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (107, N'A/N');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (108, N'IPComp');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (109, N'SNP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (110, N'Compaq-Peer');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (111, N'IPX-in-IP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (112, N'VRRP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (113, N'PGM');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (114, N'Any 0-hop protocol');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (115, N'L2TP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (116, N'DDX');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (117, N'IATP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (118, N'STP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (119, N'SRP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (120, N'UTI');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (121, N'SMP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (122, N'SM (deprecated)');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (123, N'PTP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (124, N'ISIS over IPv4');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (125, N'FIRE');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (126, N'CRTP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (127, N'CRUDP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (128, N'SSCOPMCE');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (129, N'IPLT');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (130, N'SPS');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (131, N'PIPE');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (132, N'SCTP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (133, N'FC');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (134, N'RSVP-E2E-IGNORE');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (135, N'Mobility Header');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (136, N'UDPLite');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (137, N'MPLS-in-IP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (138, N'manet');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (139, N'HIP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (140, N'Shim6');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (141, N'WESP');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (142, N'ROHC');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (143, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (144, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (145, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (146, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (147, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (148, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (149, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (150, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (151, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (152, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (153, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (154, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (155, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (156, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (157, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (158, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (159, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (160, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (161, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (162, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (163, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (164, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (165, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (166, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (167, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (168, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (169, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (170, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (171, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (172, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (173, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (174, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (175, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (176, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (177, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (178, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (179, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (180, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (181, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (182, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (183, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (184, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (185, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (186, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (187, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (188, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (189, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (190, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (191, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (192, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (193, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (194, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (195, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (196, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (197, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (198, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (199, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (200, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (201, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (202, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (203, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (204, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (205, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (206, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (207, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (208, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (209, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (210, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (211, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (212, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (213, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (214, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (215, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (216, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (217, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (218, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (219, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (220, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (221, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (222, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (223, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (224, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (225, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (226, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (227, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (228, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (229, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (230, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (231, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (232, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (233, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (234, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (235, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (236, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (237, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (238, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (239, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (240, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (241, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (242, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (243, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (244, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (245, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (246, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (247, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (248, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (249, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (250, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (251, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (252, N'Unassigned');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (253, N'Experimentation');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (254, N'Experimentation');
		INSERT  [ProtocolTypes] ([protocolTypeId], [protocolType]) VALUES (255, N'Reserved');";

    command!.CommandText = sql;
    command.ExecuteNonQuery();
    logger.Information("Created ProtocolTypes lookup table");
  }

  /// <summary>
  /// Create PacketsView view plus add its indexes.
  /// </summary>
  private void CreatePacketsView(IDbConnection conn)
  {
    using var command = conn.CreateCommand();

    string sql = @"IF EXISTS(SELECT * FROM SYS.VIEWS WHERE NAME = 'PacketsView') SELECT 1 ELSE SELECT 0";

    command!.CommandText = sql;
    command.ExecuteNonQuery();
    if (Convert.ToInt32(command.ExecuteScalar()) == 1) return;

    sql = @"
			CREATE VIEW [dbo].[PacketsView] WITH SCHEMABINDING AS
				SELECT I.packetID, M.collectorName as Collector, I.timestamp, I.ipPacketSize, I.sourceIP, I.destinationIP, I.typeOfService, I.protocol, I.sourcePort, I.destinationPort, I.julianDay
				FROM [dbo].PacketIndices I, [dbo].PcapMetaData M
				WHERE I.pcapFileProcessingLogID = M.pcapFileProcessingLogID AND (I.sourceIP NOT LIKE '%:%' OR I.destinationIP NOT LIKE '%:%')";

    command!.CommandText = sql;
    command.ExecuteNonQuery();

    sql = @"
			CREATE UNIQUE CLUSTERED INDEX [PacketsViewIndex] ON [dbo].[PacketsView]
			(
				[packetID] ASC
			)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]";

    command!.CommandText = sql;
    command.ExecuteNonQuery();

    sql = @"
			CREATE NONCLUSTERED INDEX [IX_NonClusteredIndex_Collector] ON [dbo].[PacketsView]
			(
				[Collector] ASC
			)
			INCLUDE([Timestamp]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 90, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]";

    command!.CommandText = sql;
    command.ExecuteNonQuery();
    logger.Information("Created PacketsView");
  }
}
