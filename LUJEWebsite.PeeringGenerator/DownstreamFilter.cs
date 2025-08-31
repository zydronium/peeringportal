using LUJEWebsite.Library.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using LUJEWebsite.Library.Utils;

namespace LUJEWebsite.PeeringGenerator
{
	internal class DownstreamFilter
	{
		public static void Run(Config config, List<PeeringEntry> downstreamList, MySqlConnection luje_conn, Dictionary<string, List<string>> filterReady)
		{
			MySqlCommand luje_cmd = new MySqlCommand();
			foreach (var peer in downstreamList)
			{
				luje_cmd = new MySqlCommand(@"select downstream_prefixes_addr, downstream_prefixes_type from downstream_prefixes where downstream_prefixes_downstream_id = @downstream_id and downstream_prefixes_deleted = false;", luje_conn);
				luje_cmd.Parameters.AddWithValue("@downstream_id", Convert.ToInt32(peer.downstream_id));
				luje_cmd.Prepare();
				MySqlDataReader luje_rdr = luje_cmd.ExecuteReader();

				var prefixList = new List<PrefixEntry>();

				int ipv4Count = 0;
				int ipv6Count = 0;
				while (luje_rdr.Read())
				{
					PrefixEntry entry = new PrefixEntry
					{
						prefix = luje_rdr["downstream_prefixes_addr"].ToString(),
						type = luje_rdr["downstream_prefixes_type"].ToString()
					};
					prefixList.Add(entry);
					if(luje_rdr["downstream_prefixes_type"].ToString() == "4")
					{
						ipv4Count++;
					}
					else
					{
						ipv6Count++;
					}
				}
				luje_rdr.Close();

				string asn = "AS" + peer.asn;
				Console.WriteLine($"working on BIRD config for asn {peer.asn}");
				//ASN: asn
				//description: name
				//as-set: asset

				luje_cmd = new MySqlCommand(@"select downstream_ips_addr_our, downstream_ips_addr_peer, downstream_ips_router, downstream_ips_type from downstream_ips where downstream_ips_downstream_id = @downstream_id and downstream_ips_deleted = false;", luje_conn);
				luje_cmd.Parameters.AddWithValue("@downstream_id", Convert.ToInt32(peer.downstream_id));
				luje_cmd.Prepare();
				luje_rdr = luje_cmd.ExecuteReader();

				var IPaddresses = new ArrayList();

				while (luje_rdr.Read())
				{
					string routerName = luje_rdr["downstream_ips_router"].ToString();

					Console.WriteLine($"found downstream {luje_rdr["downstream_ips_addr_peer"].ToString()} must deploy on {routerName}");

					var router = config.RouterMapping[routerName];
					string filename = $"{Configuration.RoutefiltersLocation}/downstream_{router.Fqdn}.{router.Vendor}.ipv{luje_rdr["downstream_ips_type"].ToString()}.config";
					bool gracefullShutdown = false;
					if (router.GracefulShutdown)
					{
						gracefullShutdown = true;
					}
					if (!filterReady[$"{routerName}{luje_rdr["downstream_ips_type"].ToString()}"].Contains($"AS{peer.asn}"))
					{
						GenerateFilter("downstream", filename, peer.downstream_id, Convert.ToInt32(luje_rdr["downstream_ips_type"]), prefixList);

						filterReady[$"{routerName}{luje_rdr["downstream_ips_type"].ToString()}"].Add($"AS{peer.asn}");
					}

					string neighboorName = $"downstream_AS{peer.asn}_ipv{luje_rdr["downstream_ips_type"].ToString()}_{Utils.GetNeighborName(luje_rdr["downstream_ips_addr_peer"].ToString())}";

					GenerateSession("downstream", peer.defaultroute, filename, luje_rdr["downstream_ips_addr_our"].ToString(), Convert.ToInt32(luje_rdr["downstream_ips_type"]), peer.downstream_id, neighboorName, peer.name, luje_rdr["downstream_ips_addr_peer"].ToString(), peer.asn, gracefullShutdown);
				}
				luje_rdr.Close();
			}

			//set probes to deployed
			luje_cmd = new MySqlCommand("update downstream_ips set downstream_ips_deployed = true, downstream_ips_modified = NOW() where downstream_ips_deployed = false and downstream_ips_deleted = false;", luje_conn);
			luje_cmd.ExecuteNonQuery();
		}

		private static void GenerateFilter(string sessionType, string FileName, string downstream_id, int afi, List<PrefixEntry> prefixList)
		{
			var filterBuilder = new StringBuilder();
			if (prefixList.Count > 0)
			{
				filterBuilder.Append($@"function downstream_extra_import_ipv{afi}_{downstream_id}() -> bool {{").AppendLine();
				filterBuilder.Append($@"    if (net ~ [").AppendLine();
				int i = 0;
				foreach (var prefix in prefixList)
				{
					if (prefix.type == afi.ToString())
					{
						if (i > 0)
						{
							filterBuilder.Append(',').AppendLine();
						}
						filterBuilder.Append($@"        {prefix.prefix}");
						i++;
					}
				}
				filterBuilder.AppendLine();
				filterBuilder.Append($@"    ]) then {{").AppendLine();
				filterBuilder.Append($@"        return true;").AppendLine();
				filterBuilder.Append($@"    }}").AppendLine();
				filterBuilder.Append($@"    return false;").AppendLine();
				filterBuilder.Append($@"}}").AppendLine().AppendLine();
			}

			filterBuilder.Append($@"filter downstream_import_ipv{afi}_{downstream_id} {{
    # https://tools.ietf.org/html/draft-ietf-grow-bgp-gshut
    allow_graceful_shutdown();

    if is_v4_rpki_invalid() && !downstream_extra_import_ipv{afi}_{downstream_id}() then {{
        reject;
    }}
    
    if is_aspa_invalid(true) then {{
        reject;
    }}

    accept;
}}").AppendLine().AppendLine();
			File.AppendAllText(FileName, filterBuilder.ToString());
		}

		private static void GenerateSession(string sessionType, bool defaultRoute, string FileName, string IPAddress, int afi, string downstream_id, string NeighboorName, string Name, string IpAddress, string ASN, bool GracefulShutdown)
		{
			var sessionBuilder = new StringBuilder();
			sessionBuilder.Append($@"protocol bgp {NeighboorName} {{
    default bgp_local_pref 500;
    description ""{Name}"";
    neighbor {IpAddress} as {ASN};
    source address {IPAddress};
    local role provider;
    #include ""../ebgp_state.conf"";
    local as {Configuration.PortalOwnerAsn};
    ipv{afi} {{
        next hop self;
        import keep filtered;
        import filter downstream_import_ipv{afi}_{downstream_id};").AppendLine();

			if (defaultRoute)
			{
				if (afi == 4)
				{
					sessionBuilder.Append($@"        export filter {{
            if net = 0.0.0.0/0 then accept;
            reject;
        }};").AppendLine();
				}
				else if (afi == 6)
				{
					sessionBuilder.Append($@"        export filter {{
            if net = ::/0 then accept;
            reject;
        }};").AppendLine();
				}
			}
			else
			{
				if (afi == 4)
				{
					sessionBuilder.Append($@"        export filter {{
            if net = 0.0.0.0/0 then reject;
            accept;
        }};").AppendLine();
				}
				else if (afi == 6)
				{
					sessionBuilder.Append($@"        export filter {{
            if net = ::/0 then reject;
            accept;
        }};").AppendLine();
				}
			}

			sessionBuilder.Append($@"    }};").AppendLine();

			if (GracefulShutdown)
			{
				sessionBuilder.Append($@"    default bgp_local_pref 0;").AppendLine();
			}
			sessionBuilder.Append($@"}}").AppendLine().AppendLine();

			File.AppendAllText(FileName, sessionBuilder.ToString());
		}
	}
}
