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
	internal class UpstreamFilter
	{
		public static void Run(Config config, List<PeeringEntry> upstreamList, MySqlConnection luje_conn, Dictionary<string, List<string>> filterReady)
		{
			MySqlCommand luje_cmd = new MySqlCommand();
			foreach (var peer in upstreamList)
			{
				luje_cmd = new MySqlCommand(@"select upstream_prefixes_addr, upstream_prefixes_type from upstream_prefixes where upstream_prefixes_upstream_id = @upstream_id and upstream_prefixes_deleted = false;", luje_conn);
				luje_cmd.Parameters.AddWithValue("@upstream_id", Convert.ToInt32(peer.upstream_id));
				luje_cmd.Prepare();
				MySqlDataReader luje_rdr = luje_cmd.ExecuteReader();

				var prefixList = new List<PrefixEntry>();

				int ipv4Count = 0;
				int ipv6Count = 0;
				while (luje_rdr.Read())
				{
					PrefixEntry entry = new PrefixEntry
					{
						prefix = luje_rdr["upstream_prefixes_addr"].ToString(),
						type = luje_rdr["upstream_prefixes_type"].ToString()
					};
					prefixList.Add(entry);
					if(luje_rdr["upstream_prefixes_type"].ToString() == "4")
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

				luje_cmd = new MySqlCommand(@"select upstream_ips_addr_our, upstream_ips_addr_peer, upstream_ips_router, upstream_ips_type from upstream_ips where upstream_ips_upstream_id = @upstream_id and upstream_ips_deleted = false;", luje_conn);
				luje_cmd.Parameters.AddWithValue("@upstream_id", Convert.ToInt32(peer.upstream_id));
				luje_cmd.Prepare();
				luje_rdr = luje_cmd.ExecuteReader();

				var IPaddresses = new ArrayList();

				while (luje_rdr.Read())
				{
					bool customFilter = false;
					if (ipv4Count > 0 || ipv6Count > 0)
					{
						customFilter = true;
					}

					string routerName = luje_rdr["upstream_ips_router"].ToString();

					Console.WriteLine($"found upstream {luje_rdr["upstream_ips_addr_peer"].ToString()} must deploy on {routerName}");

					var router = config.RouterMapping[routerName];
					string filename = $"{Configuration.RoutefiltersLocation}/upstream_{router.Fqdn}.{router.Vendor}.ipv{luje_rdr["upstream_ips_type"].ToString()}.config";
					bool gracefullShutdown = false;
					if (router.GracefulShutdown)
					{
						gracefullShutdown = true;
					}
					if (customFilter && !filterReady[$"{routerName}{luje_rdr["upstream_ips_type"].ToString()}"].Contains($"AS{peer.asn}"))
					{
						GenerateFilter("upstream", filename, peer.upstream_id, Convert.ToInt32(luje_rdr["upstream_ips_type"]), prefixList);

						filterReady[$"{routerName}{luje_rdr["upstream_ips_type"].ToString()}"].Add($"AS{peer.asn}");
					}

					string neighboorName = $"upstream_AS{peer.asn}_ipv{luje_rdr["upstream_ips_type"].ToString()}_{Utils.GetNeighborName(luje_rdr["upstream_ips_addr_peer"].ToString())}";

					GenerateSession("upstream", customFilter, filename, luje_rdr["upstream_ips_addr_our"].ToString(), Convert.ToInt32(luje_rdr["upstream_ips_type"]), peer.upstream_id, neighboorName, peer.name, luje_rdr["upstream_ips_addr_peer"].ToString(), peer.asn, gracefullShutdown, peer.no_importexport);
				}
				luje_rdr.Close();
			}

			//set probes to deployed
			luje_cmd = new MySqlCommand("update upstream_ips set upstream_ips_deployed = true, upstream_ips_modified = NOW() where upstream_ips_deployed = false and upstream_ips_deleted = false;", luje_conn);
			luje_cmd.ExecuteNonQuery();
		}

		private static void GenerateFilter(string sessionType, string FileName, string upstream_id, int afi, List<PrefixEntry> prefixList)
		{
			var filterBuilder = new StringBuilder();
			filterBuilder.Append($@"filter ebgp_export_ipv{afi}_{upstream_id} {{").AppendLine();
			filterBuilder.Append($@"    if ( ebgp_is_owned_by_me_ipv{afi}() ) then accept;").AppendLine();
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
			filterBuilder.Append($@"    ]) then accept;").AppendLine();
			filterBuilder.Append($@"    reject;").AppendLine();
			filterBuilder.Append($@"}}").AppendLine().AppendLine();
			File.AppendAllText(FileName, filterBuilder.ToString());
		}

		private static void GenerateSession(string sessionType, bool CustomFilter, string FileName, string IPAddress, int afi, string upstream_id, string NeighboorName, string Name, string IpAddress, string ASN, bool GracefulShutdown, bool no_importexport)
		{
			var sessionBuilder = new StringBuilder();
			sessionBuilder.Append($@"protocol bgp {NeighboorName} {{
    description ""{Name}"";
    neighbor {IpAddress} as {ASN};
    source address {IPAddress};
    local role customer;
    #include ""../ebgp_state.conf"";
    local as {Configuration.PortalOwnerAsn};
    ipv{afi} {{
        next hop self;
        import keep filtered;").AppendLine();

			if (no_importexport)
			{
				sessionBuilder.Append($@"        import none;").AppendLine();
			}
			else
			{
				sessionBuilder.Append($@"        import where transit_import_ipv{ afi} ({ upstream_id});").AppendLine();
			}

			if (no_importexport)
			{
				sessionBuilder.Append($@"        export none;").AppendLine();
			}
			else if(CustomFilter)
			{
				sessionBuilder.Append($@"        export filter ebgp_export_ipv{afi}_{upstream_id};").AppendLine();
			}
			else
			{
				sessionBuilder.Append($@"        export filter exportfilter_ipv{afi}_global;").AppendLine();
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
