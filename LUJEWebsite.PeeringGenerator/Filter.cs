using LUJEWebsite.PeeringGenerator.Models;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace LUJEWebsite.PeeringGenerator
{
    internal class Filter
    {
        static public void Run(Config config)
		{
			Dictionary<string, List<string>> filterReady = new Dictionary<string, List<string>>();
			
            foreach (var router in config.RouterMapping)
            {
				string filename4 = $"{Configuration.RoutefiltersLocation}/{router.Value.Fqdn}.{router.Value.Vendor}.ipv4.config";
				File.WriteAllText(filename4, "");
				string filename6 = $"{Configuration.RoutefiltersLocation}/{router.Value.Fqdn}.{router.Value.Vendor}.ipv6.config";
				File.WriteAllText(filename6, "");
				filterReady.Add($"{router.Key}4", new List<string>());
				filterReady.Add($"{router.Key}6", new List<string>());
			}

            MySqlConnection luje_conn = new MySqlConnection(Configuration.DBPath);
            luje_conn.Open();
			MySqlCommand luje_cmd = new MySqlCommand("select peering.peering_peeringdb_id, name, asn, irr_as_set from peering INNER JOIN peeringdb.peeringdb_network ON peeringdb_network.id = peering_peeringdb_id where peering_active = 1 and peering_deleted = 0 order by cast(peering_asn as unsigned) asc;", luje_conn);
			MySqlDataReader luje_rdr = luje_cmd.ExecuteReader();

			var peeringList = new List<PeeringEntry>();

			while(luje_rdr.Read())
	        {
				PeeringEntry entry = new PeeringEntry
				{
					peering_peeringdb_id = luje_rdr["peering_peeringdb_id"].ToString(),
					asn = luje_rdr["asn"].ToString(),
					name = luje_rdr["name"].ToString(),
					irr_as_set = luje_rdr["irr_as_set"].ToString()
				};
				peeringList.Add(entry);
			}
			luje_rdr.Close();

			foreach (var peer in peeringList)
			{
				string asn = "AS" + peer.asn;
                string name = peer.name;
                string asset = "";
                if (peer.irr_as_set != "")
                {
                    var assetBuilder = new StringBuilder();
                    asset = peer.irr_as_set;
                    String[] spearator = { " " };
                    String[] assetlist = asset.Split(spearator, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string assetPart in assetlist)
                    {
                        String[] spearatorPart = { "::" };
                        String[] assetlistPart = assetPart.Split(spearatorPart, StringSplitOptions.RemoveEmptyEntries);
                        if (assetlistPart.Count() == 1)
                        {
                            assetBuilder.Append(assetlistPart[0]);
                            assetBuilder.Append(" ");
                        }
                        else
                        {
                            assetBuilder.Append(assetlistPart[1]);
                            assetBuilder.Append(" ");
                        }
                    }

                    asset = assetBuilder.ToString().Trim();
                }
                else
                {
                    asset = asn;
                }

                Console.WriteLine($"working on asn {asn}");

                //BGP IRR filter generation

                var bgpq = new BgpqGenerator(luje_conn, peer.asn, peer.peering_peeringdb_id, asn, asset, "NTTCOM,INTERNAL,RADB,RIPE,ALTDB,BELL,LEVEL3,APNIC,JPIRR,ARIN,BBOI,TC,AFRINIC,RPKI,REGISTROBR", "rr.ntt.net");
                bgpq.GenerateFilters();

				//ASN: asn
				//description: name
				//as-set: asset

				luje_cmd = new MySqlCommand(@"select peering_ips.peering_ips_id, peering_ips.peering_ips_peeringdb_lanid, peering_ips.peering_ips_peeringdb_addrid, peering_ips.peering_ips_type, peering.peering_peeringdb_id, peeringdb_network_ixlan.id, peeringdb_ix.name as ix_name, peeringdb_network_ixlan.ixlan_id, peeringdb_network_ixlan.net_id, peeringdb_network_ixlan.ipaddr4, peeringdb_network_ixlan.ipaddr6, peeringdb_network.asn, peeringdb_network.name, peeringdb_network.info_prefixes4, peeringdb_network.info_prefixes6
from peering_ips
inner join peering on peering.peering_id = peering_ips.peering_ips_peering_id and peering.peering_deleted = 0
inner join peeringdb.peeringdb_network ON peeringdb_network.id = peering_peeringdb_id
INNER JOIN peeringdb.peeringdb_network_ixlan on peeringdb_network.id = peeringdb_network_ixlan.net_id and peeringdb_network_ixlan.id = peering_ips_peeringdb_addrid and peeringdb_network_ixlan.ixlan_id = peering_ips_peeringdb_lanid
inner join peeringdb.peeringdb_ixlan on peeringdb_ixlan.id = peeringdb_network_ixlan.ixlan_id
inner join peeringdb.peeringdb_ix on peeringdb_ix.id = peeringdb_ixlan.ix_id
where peering.peering_peeringdb_id = @peer and peering_ips_active = 1 and peering_ips_deleted = 0
ORDER BY peeringdb_network.asn ASC;", luje_conn);
				luje_cmd.Prepare();
				luje_cmd.Parameters.AddWithValue("@peer", peer.peering_peeringdb_id);
				luje_rdr = luje_cmd.ExecuteReader();

				var IPaddresses = new ArrayList();

				while (luje_rdr.Read())
                {
                    var lan = config.LanMapping[luje_rdr["peering_ips_peeringdb_lanid"].ToString()];
                    string addr = "";
                    if (luje_rdr["peering_ips_type"].ToString() == "4")
					{
						Console.WriteLine($"found peer {luje_rdr["ipaddr4"].ToString()} in IXP {lan.Name} with localpref {lan.BgpLocalPref}");
						Console.WriteLine($"must deploy on {string.Join(",", lan.Routers)}");

						foreach (var item in lan.Routers) {
							var router = config.RouterMapping[item];
							string filename = $"{Configuration.RoutefiltersLocation}/{router.Fqdn}.{router.Vendor}.ipv4.config";
                            bool gracefullShutdown = false;
                            if(router.GracefulShutdown || lan.GracefulShutdown)
                            {
                                gracefullShutdown = true;
							}
							bool adminDownState = false;
							if (lan.AdminDownState)
							{
								adminDownState = true;
							}
							bool blockImportExport = false;
							if (lan.BlockImportExport)
							{
								blockImportExport = true;
							}
							if (!filterReady[$"{item}4"].Contains($"AS{luje_rdr["asn"].ToString()}"))
                            {
								GenerateFilter(filename, 4, $"AS{luje_rdr["asn"].ToString()}", gracefullShutdown, "peer", config.Rpki);
                                filterReady[$"{item}4"].Add($"AS{luje_rdr["asn"].ToString()}");
							}

							string neighboorName = $"peer_ipv4_AS{luje_rdr["asn"].ToString()}_{lan.Name}_{Utils.GetNeighborName(luje_rdr["ipaddr4"].ToString())}";
							int? limit = null;
							if (luje_rdr["info_prefixes4"] != null)
							{
								limit = Convert.ToInt32(luje_rdr["info_prefixes4"]);
							}

							string? password = null;
							if (config.Passwords.ContainsKey($"AS{luje_rdr["asn"].ToString()}"))
							{
								password = config.Passwords[$"AS{luje_rdr["asn"].ToString()}"];
							}

							GenerateSession(filename, 4, neighboorName, luje_rdr["name"].ToString(), luje_rdr["ipaddr4"].ToString(), luje_rdr["asn"].ToString(), lan.BgpLocalPref, limit, blockImportExport, false, password, adminDownState, gracefullShutdown);
						}
                    }
                    else if (luje_rdr["peering_ips_type"].ToString() == "6")
					{
						Console.WriteLine($"found peer {luje_rdr["ipaddr6"].ToString()} in IXP {lan.Name} with localpref {lan.BgpLocalPref}");
						Console.WriteLine($"must deploy on {string.Join(",", lan.Routers)}");

						foreach (var item in lan.Routers)
						{
							var router = config.RouterMapping[item];
							string filename = $"{Configuration.RoutefiltersLocation}/{router.Fqdn}.{router.Vendor}.ipv6.config";
							bool gracefullShutdown = false;
							if (router.GracefulShutdown || lan.GracefulShutdown)
							{
								gracefullShutdown = true;
							}
							bool adminDownState = false;
							if (lan.AdminDownState)
							{
								adminDownState = true;
							}
							bool blockImportExport = false;
							if (lan.BlockImportExport)
							{
								blockImportExport = true;
							}
							if (!filterReady[$"{item}6"].Contains($"AS{luje_rdr["asn"].ToString()}"))
							{
								GenerateFilter(filename, 6, $"AS{luje_rdr["asn"].ToString()}", gracefullShutdown, "peer", config.Rpki);

								filterReady[$"{item}6"].Add($"AS{luje_rdr["asn"].ToString()}");
							}

							string neighboorName = $"peer_ipv6_AS{luje_rdr["asn"].ToString()}_{lan.Name}_{Utils.GetNeighborName(luje_rdr["ipaddr6"].ToString())}";
                            int? limit = null;
                            if(luje_rdr["info_prefixes6"] != null)
                            {
                                limit = Convert.ToInt32(luje_rdr["info_prefixes6"]);
							}

                            string? password = null;
                            if(config.Passwords.ContainsKey($"AS{luje_rdr["asn"].ToString()}"))
                            {
                                password = config.Passwords[$"AS{luje_rdr["asn"].ToString()}"];
							}

							GenerateSession(filename, 6, neighboorName, luje_rdr["name"].ToString(), luje_rdr["ipaddr6"].ToString(), luje_rdr["asn"].ToString(), lan.BgpLocalPref, limit, blockImportExport, false, password, adminDownState, gracefullShutdown);
						}
                    }
                }
                luje_rdr.Close();

                luje_cmd = new MySqlCommand(@"select peering_ips_extra.peering_ips_extra_addr, peeringdb_network.asn, peeringdb_network.name, peeringdb_network.info_prefixes4, peeringdb_network.info_prefixes6
from peering_ips_extra 
inner join peering on peering.peering_id = peering_ips_extra.peering_ips_extra_peering_id and peering.peering_deleted = 0
inner join peeringdb.peeringdb_network ON peeringdb_network.id = peering_peeringdb_id
where peering.peering_peeringdb_id = @peer and peering_ips_extra_active = 1 and peering_ips_extra_deleted = 0;", luje_conn);
                luje_cmd.Prepare();
                luje_cmd.Parameters.AddWithValue("@peer", peer.peering_peeringdb_id);
                luje_rdr = luje_cmd.ExecuteReader();

                while (luje_rdr.Read())
                {

					IPAddress ipAddress = IPAddress.Parse(luje_rdr["peering_ips_extra_addr"].ToString());
					foreach(var router in config.RouterMapping)
					{
						foreach(var subnet in router.Value.ExtraPrefix)
						{
							if (ipAddress.AddressFamily == AddressFamily.InterNetwork && subnet.Afi == 4)
							{
								if (Utils.IsInSubnet(ipAddress, subnet.Subnet))
								{
									Console.WriteLine($"found peer {luje_rdr["peering_ips_extra_addr"].ToString()} in IXP {subnet.Name} with localpref {subnet.BgpLocalPref}");
									Console.WriteLine($"must deploy on {string.Join(",", router.Key)}");


									string filename = $"{Configuration.RoutefiltersLocation}/{router.Value.Fqdn}.{router.Value.Vendor}.ipv4.config";
									bool gracefullShutdown = false;
									if (router.Value.GracefulShutdown || subnet.GracefulShutdown)
									{
										gracefullShutdown = true;
									}
									bool adminDownState = false;
									if (subnet.AdminDownState)
									{
										adminDownState = true;
									}
									bool blockImportExport = false;
									if (subnet.BlockImportExport)
									{
										blockImportExport = true;
									}
									if (!filterReady[$"{router.Key}4"].Contains($"AS{luje_rdr["asn"].ToString()}"))
									{
										GenerateFilter(filename, 4, $"AS{luje_rdr["asn"].ToString()}", gracefullShutdown, "peer", config.Rpki);
										filterReady[$"{router.Key}4"].Add($"AS{luje_rdr["asn"].ToString()}");
									}

									string neighboorName = $"peer_ipv4_AS{luje_rdr["asn"].ToString()}_{subnet.Name}_{Utils.GetNeighborName(luje_rdr["peering_ips_extra_addr"].ToString())}";
									int? limit = null;
									if (luje_rdr["info_prefixes4"] != null)
									{
										limit = Convert.ToInt32(luje_rdr["info_prefixes4"]);
									}

									string? password = null;
									if (config.Passwords.ContainsKey($"AS{luje_rdr["asn"].ToString()}"))
									{
										password = config.Passwords[$"AS{luje_rdr["asn"].ToString()}"];
									}

									GenerateSession(filename, 4, neighboorName, luje_rdr["name"].ToString(), luje_rdr["peering_ips_extra_addr"].ToString(), luje_rdr["asn"].ToString(), subnet.BgpLocalPref, limit, blockImportExport, false, password, adminDownState, gracefullShutdown);
									continue;
								}
							}
							else if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6 && subnet.Afi == 6)
							{
								if (Utils.IsInSubnet(ipAddress, subnet.Subnet))
								{
									string filename = $"{Configuration.RoutefiltersLocation}/{router.Value.Fqdn}.{router.Value.Vendor}.ipv6.config";
									bool gracefullShutdown = false;
									if (router.Value.GracefulShutdown || subnet.GracefulShutdown)
									{
										gracefullShutdown = true;
									}
									bool adminDownState = false;
									if (subnet.AdminDownState)
									{
										adminDownState = true;
									}
									bool blockImportExport = false;
									if (subnet.BlockImportExport)
									{
										blockImportExport = true;
									}
									if (!filterReady[$"{router.Key}6"].Contains($"AS{luje_rdr["asn"].ToString()}"))
									{
										GenerateFilter(filename, 6, $"AS{luje_rdr["asn"].ToString()}", gracefullShutdown, "peer", config.Rpki);
										filterReady[$"{router.Key}6"].Add($"AS{luje_rdr["asn"].ToString()}");
									}

									string neighboorName = $"peer_ipv6_AS{luje_rdr["asn"].ToString()}_{subnet.Name}_{Utils.GetNeighborName(luje_rdr["peering_ips_extra_addr"].ToString())}";
									int? limit = null;
									if (luje_rdr["info_prefixes6"] != null)
									{
										limit = Convert.ToInt32(luje_rdr["info_prefixes4"]);
									}

									string? password = null;
									if (config.Passwords.ContainsKey($"AS{luje_rdr["asn"].ToString()}"))
									{
										password = config.Passwords[$"AS{luje_rdr["asn"].ToString()}"];
									}

									GenerateSession(filename, 6, neighboorName, luje_rdr["name"].ToString(), luje_rdr["peering_ips_extra_addr"].ToString(), luje_rdr["asn"].ToString(), subnet.BgpLocalPref, limit, blockImportExport, false, password, adminDownState, gracefullShutdown);
									continue;
								}
								
							}
						}
					}

					IPaddresses.Add(luje_rdr["peering_ips_extra_addr"].ToString());
                }
                luje_rdr.Close();
            }

			//set peering to deployed
			luje_cmd = new MySqlCommand("update peering set peering_deployed = '1', peering_modified = NOW() where peering_active = '1' and peering_deployed = '0' and peering_deleted = '0';", luje_conn);
			luje_cmd.ExecuteNonQuery();

			luje_cmd = new MySqlCommand("update peering_ips set peering_ips_deployed = '1', peering_ips_modified = NOW() where peering_ips_active = '1' and peering_ips_deployed = '0' and peering_ips_deleted = '0';", luje_conn);
			luje_cmd.ExecuteNonQuery();

			luje_cmd = new MySqlCommand("update peering_ips_extra set peering_ips_extra_deployed = '1', peering_ips_extra_modified = NOW() where peering_ips_extra_active = '1' and peering_ips_extra_deployed = '0' and peering_ips_extra_deleted = '0';", luje_conn);
			luje_cmd.ExecuteNonQuery();
			luje_conn.Close();

			//deploy filters
			foreach (var router in config.RouterMapping)
			{
				Console.WriteLine($"Sync filters to router {router.Value.Fqdn}");
				var processStartInfo = new ProcessStartInfo
				{
					FileName = "rsync",
					Arguments = $"-avH --delete {Configuration.RoutefiltersLocation}/ root@{router.Value.Hostname}:/etc/bird/peering/",
					RedirectStandardOutput = true,
					UseShellExecute = false
				};
				using (var process = Process.Start(processStartInfo))
				{
					Console.WriteLine(process.StandardOutput.ReadToEnd());
				}

				Console.WriteLine($"Reconfigure router {router.Value.Fqdn}");
				processStartInfo = new ProcessStartInfo
				{
					FileName = "ssh",
					Arguments = $"root@{router.Value.Hostname} \"chown -R bird:bird /etc/bird; /usr/sbin/birdc configure; /usr/local/bin/birdc6 configure\"",
					RedirectStandardOutput = true,
					UseShellExecute = false
				};
				using (var process = Process.Start(processStartInfo))
				{
					Console.WriteLine(process.StandardOutput.ReadToEnd());
				}
			}
		}

        private static void GenerateFilter(string FileName, int afi, string ASN, bool GracefulShutdown, string Type, bool Rpki)
        {
			var filterBuilder = new StringBuilder();
			filterBuilder.Append($@"filter peer_in_{ASN}_ipv{afi}
prefix set AUTOFILTER_{ASN}_IPv{afi};
{{
    # ignore bogon AS_PATHs
    if is_bogon_aspath() then {{
        #print ""Reject: bogon AS_PATH: "", net, "" "", bgp_path;
        bgp_large_community.add((212855, rejected_route, r_bogon_aspath));
        reject;
    }}

    if ( is_luje_more_specific() ) then {{
        #print ""Reject: 212855 more specific: "", net, "" "", bgp_path, "" filter: peer_in_{ASN}_ipv{afi}"";
        bgp_large_community.add((212855, rejected_route, r_luje_morespecific));
        reject;
    }}
	
    if ( is_bogon() ) then {{
        #print ""Reject: bogon: "", net, "" "", bgp_path, "" filter: peer_in_AS{ASN}_ipv{afi}"";
        bgp_large_community.add((212855, rejected_route, r_bogon_prefix));
        reject;
    }}

    if ! is_acceptable_size() then {{
        #print ""Reject: too large or too small: "", net, "" "", bgp_path, "" filter: peer_in_AS{ASN}_ipv{afi}"";
        bgp_large_community.add((212855, rejected_route, r_unacceptable_pfxlen));
        reject;
    }}

    # https://tools.ietf.org/html/rfc8326
    allow_graceful_shutdown();").AppendLine().AppendLine();

            if (GracefulShutdown)
            {
				filterBuilder.Append(@"    bgp_community.add((65535, 0));
	bgp_local_pref = 0;").AppendLine().AppendLine();
			}

            if(Type == "peer")
            {
				filterBuilder.Append(@"    # Scrub BGP Communities (RFC 7454 Section 11) from peering partners
    bgp_large_community.delete([(212855, *, *)]);
    # Scrub BLACKHOLE Community from peering partners
    bgp_community.delete((65535, 666));
    bgp_large_community.add((212855, 0, 1)); /* mark peering routes as peering routes */").AppendLine().AppendLine();
			}

            if(Rpki)
            {
				filterBuilder.Append($@"    if is_v{afi}_rpki_invalid() then {{
        reject;
    }}").AppendLine().AppendLine();
			}

            filterBuilder.Append($@"	include ""{ASN}.prefixset.bird.ipv{afi}"";
    if (net ~ AUTOFILTER_{ASN}_IPv{afi}");
            
            if(Rpki)
            {
                filterBuilder.Append($@" || (212855, 5, 2) ~ bgp_large_community");
            }


			filterBuilder.Append($@") then {{
        bgp_med = 0;").AppendLine().AppendLine();

			if (Type == "downstream")
			{
				filterBuilder.Append(@"        bgp_large_community.add((212855,0, 2));
        if (212855, 1, 50) ~ bgp_large_community then bgp_local_pref = 50;
        if (212855, 1, 150) ~ bgp_large_community then bgp_local_pref = 150;").AppendLine().AppendLine();
			}

			filterBuilder.Append($@"# classifications
		if (net ~ AUTOFILTER_{ASN}_IPv{afi}) then {{
            bgp_large_community.add((212855, 5, 1));
        }}
        accept;
    }}
    #print ""Reject: No IRR? "", net, "" "", bgp_path, "" filter: peer_in_{ASN}_ipv{afi}"";
    bgp_large_community.add((212855, rejected_route, r_no_irr));
    reject;
}}").AppendLine().AppendLine();

            File.AppendAllText(FileName, filterBuilder.ToString());
		}

        private static void GenerateSession(string FileName, int afi, string NeighboorName, string Name, string IpAddress, string ASN, int? BgpLocalPref, int? Limit, bool BlockImportExport, bool ExportFullTable, string? Password, bool AdminDownState, bool GracefulShutdown)
        {
			var sessionBuilder = new StringBuilder();
            sessionBuilder.Append($@"protocol bgp {NeighboorName} {{
    description ""{Name}"";
    neighbor {IpAddress} as {ASN};
    #include ""../ebgp_state.conf"";
    local as 212855;").AppendLine();
            if(BgpLocalPref != null)
            {
				sessionBuilder.Append($@"    default bgp_local_pref {BgpLocalPref};").AppendLine();
			}
            sessionBuilder.Append($@"    ipv{afi} {{
        next hop self;").AppendLine();
			if (Limit != null && Limit > 0)
			{
				sessionBuilder.Append($@"        receive limit {Limit} action restart;").AppendLine();
			}
			sessionBuilder.Append($@"        import keep filtered;").AppendLine();
            if(BlockImportExport)
            {
				sessionBuilder.Append($@"        import none;").AppendLine();
			}
            else
            {
				sessionBuilder.Append($@"        import filter peer_in_AS{ASN}_ipv{afi};").AppendLine();
			}
            if(BlockImportExport)
            {
				sessionBuilder.Append($@"        export none;").AppendLine();
			}
            else if (ExportFullTable)
			{
				sessionBuilder.Append($@"        export where full_table_export({ASN})").AppendLine();
			}
			else
			{
				sessionBuilder.Append($@"        #export where ebgp_peering_export({ASN});
        export filter ebgp_export;").AppendLine();
			}

			sessionBuilder.Append($@"    }};").AppendLine();
			if (Password != null)
			{
				sessionBuilder.Append($@"    password ""{Password}"";").AppendLine();
			}
			if (AdminDownState)
			{
				sessionBuilder.Append($@"    disabled;").AppendLine();
			}

			if (GracefulShutdown)
			{
				sessionBuilder.Append($@"    default bgp_local_pref 0;").AppendLine();
			}
			sessionBuilder.Append($@"}}").AppendLine().AppendLine();

			File.AppendAllText(FileName, sessionBuilder.ToString());
		}
    }
}
