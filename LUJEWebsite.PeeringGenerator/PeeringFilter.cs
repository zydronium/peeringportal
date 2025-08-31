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
using Microsoft.Extensions.Primitives;
using LUJEWebsite.Library.Utils;

namespace LUJEWebsite.PeeringGenerator
{
	internal class PeeringFilter
	{
		public static void Run(Config config, List<PeeringEntry> peeringList, MySqlConnection luje_conn, Dictionary<string, List<string>> filterReady)
		{
			MySqlCommand luje_cmd;
			foreach (var peer in peeringList)
			{
				string asn = "AS" + peer.asn;
				Console.WriteLine($"working on BIRD config for asn {peer.asn}");
				//ASN: asn
				//description: name
				//as-set: asset

				luje_cmd = new MySqlCommand(@"select peering.peering_aspa_upstream, peering.peering_role_overwrite, peering_ips.peering_ips_id, peering_ips.peering_ips_peeringdb_lanid, peering_ips.peering_ips_peeringdb_addrid, peering_ips.peering_ips_type, peering.peering_peeringdb_id, peeringdb_network_ixlan.id, peeringdb_ix.name as ix_name, peeringdb_network_ixlan.ixlan_id, peeringdb_network_ixlan.net_id, peeringdb_network_ixlan.ipaddr4, peeringdb_network_ixlan.ipaddr6, peeringdb_network.asn, peeringdb_network.name, peeringdb_network.info_prefixes4, peeringdb_network.info_prefixes6, owner_peeringdb_network_ixlan.ipaddr4 as owneripaddr4, owner_peeringdb_network_ixlan.ipaddr6 as owneripaddr6
from peering_ips
inner join peering on peering.peering_id = peering_ips.peering_ips_peering_id and peering.peering_deleted = false
inner join peeringdb_network ON peeringdb_network.id = peering_peeringdb_id
INNER JOIN peeringdb_network_ixlan on peeringdb_network.id = peeringdb_network_ixlan.net_id and peeringdb_network_ixlan.id = peering_ips_peeringdb_addrid and peeringdb_network_ixlan.ixlan_id = peering_ips_peeringdb_lanid
inner join peeringdb_ixlan on peeringdb_ixlan.id = peeringdb_network_ixlan.ixlan_id
inner join peeringdb_ix on peeringdb_ix.id = peeringdb_ixlan.ix_id
inner join peeringdb_network_ixlan as owner_peeringdb_network_ixlan on peeringdb_ixlan.id = owner_peeringdb_network_ixlan.ixlan_id and owner_peeringdb_network_ixlan.id = peering_ips_peeringdb_oaddrid
where peering.peering_peeringdb_id = @peer and peering_ips_active = true and peering_ips_deleted = false
ORDER BY peeringdb_network.asn ASC;", luje_conn);
				luje_cmd.Parameters.AddWithValue("@peer", Convert.ToInt32(peer.peering_peeringdb_id));
				luje_cmd.Prepare();
				MySqlDataReader luje_rdr = luje_cmd.ExecuteReader();

				var IPaddresses = new ArrayList();

				while (luje_rdr.Read())
				{
					var lan = config.LanMapping[luje_rdr["peering_ips_peeringdb_lanid"].ToString()];
					string addr = "";
					if (luje_rdr["peering_ips_type"].ToString() == "4")
					{
						if(
							!(luje_rdr["owneripaddr4"].ToString().StartsWith("185.1.203.") && luje_rdr["ipaddr4"].ToString().StartsWith("185.1.160."))
							&&
							!(luje_rdr["owneripaddr4"].ToString().StartsWith("185.1.160.") && luje_rdr["ipaddr4"].ToString().StartsWith("185.1.203."))
							&& luje_rdr["ipaddr4"].ToString() != "" && luje_rdr["owneripaddr4"].ToString() != ""
						)
						{
							Console.WriteLine($"found peer {luje_rdr["ipaddr4"].ToString()} in IXP {lan.Name} with localpref {lan.BgpLocalPref}");

							foreach (var item in lan.Routers)
							{
								foreach (var routerAddress in item.IPAddresses)
								{
									if (luje_rdr["owneripaddr4"].ToString() == routerAddress)
									{
										Console.WriteLine($"must deploy on {item.Name}");
										var router = config.RouterMapping[item.Name];
										string filename = $"{Configuration.RoutefiltersLocation}/peering_{router.Fqdn}.{router.Vendor}.ipv4.config";
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
										if (!filterReady[$"{item.Name}4"].Contains($"peer_AS{luje_rdr["asn"].ToString()}"))
										{
											GenerateFilter(config, "peer", filename, 4, $"AS{luje_rdr["asn"].ToString()}", gracefullShutdown, "peer", config.Rpki, Convert.ToBoolean(luje_rdr["peering_aspa_upstream"]));
											filterReady[$"{item.Name}4"].Add($"peer_AS{luje_rdr["asn"].ToString()}");
										}

										string neighboorName = $"peer_AS{luje_rdr["asn"].ToString()}_{lan.Name}_ipv4_{Utils.GetNeighborName(luje_rdr["ipaddr4"].ToString())}";
										int? limit = null;
										if (luje_rdr["info_prefixes4"] != null && luje_rdr["info_prefixes4"].ToString() != "")
										{
											limit = Convert.ToInt32(luje_rdr["info_prefixes4"]);
										}
										else
										{
											limit = 0;
										}

										string? password = null;
										if (config.Passwords.ContainsKey($"AS{luje_rdr["asn"].ToString()}"))
										{
											password = config.Passwords[$"AS{luje_rdr["asn"].ToString()}"];
										}

										GenerateSession(luje_rdr["peering_role_overwrite"].ToString(), "peer", filename, luje_rdr["owneripaddr4"].ToString(), 4, null, neighboorName, luje_rdr["name"].ToString(), luje_rdr["ipaddr4"].ToString(), luje_rdr["asn"].ToString(), lan.BgpLocalPref, limit, blockImportExport, password, adminDownState, gracefullShutdown);
									}
								}
							}
						}
					}
					else if (luje_rdr["peering_ips_type"].ToString() == "6")
					{
						if(luje_rdr["ipaddr6"].ToString() != "" && luje_rdr["owneripaddr6"].ToString() != "")
						{
							Console.WriteLine($"found peer {luje_rdr["ipaddr6"].ToString()} in IXP {lan.Name} with localpref {lan.BgpLocalPref}");

							foreach (var item in lan.Routers)
							{
								foreach (var routerAddress in item.IPAddresses)
								{
									if (luje_rdr["owneripaddr6"].ToString() == routerAddress)
									{
										Console.WriteLine($"must deploy on {item.Name}");
										var router = config.RouterMapping[item.Name];
										string filename = $"{Configuration.RoutefiltersLocation}/peering_{router.Fqdn}.{router.Vendor}.ipv6.config";
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
										if (!filterReady[$"{item.Name}6"].Contains($"peer_AS{luje_rdr["asn"].ToString()}"))
										{
											GenerateFilter(config, "peer", filename, 6, $"AS{luje_rdr["asn"].ToString()}", gracefullShutdown, "peer", config.Rpki, Convert.ToBoolean(luje_rdr["peering_aspa_upstream"]));

											filterReady[$"{item.Name}6"].Add($"peer_AS{luje_rdr["asn"].ToString()}");
										}

										string neighboorName = $"peer_AS{luje_rdr["asn"].ToString()}_{lan.Name}_ipv6_{Utils.GetNeighborName(luje_rdr["ipaddr6"].ToString())}";
										int? limit = null;
										if (luje_rdr["info_prefixes6"] != null && luje_rdr["info_prefixes6"].ToString() != "")
										{
											limit = Convert.ToInt32(luje_rdr["info_prefixes6"]);
										}
										else
										{
											limit = 0;
										}

											string? password = null;
										if (config.Passwords.ContainsKey($"AS{luje_rdr["asn"].ToString()}"))
										{
											password = config.Passwords[$"AS{luje_rdr["asn"].ToString()}"];
										}

										GenerateSession(luje_rdr["peering_role_overwrite"].ToString(), "peer", filename, luje_rdr["owneripaddr6"].ToString(), 6, null, neighboorName, luje_rdr["name"].ToString(), luje_rdr["ipaddr6"].ToString(), luje_rdr["asn"].ToString(), lan.BgpLocalPref, limit, blockImportExport, password, adminDownState, gracefullShutdown);
									}
								}
							}
						}
					}
				}
				luje_rdr.Close();

				luje_cmd = new MySqlCommand(@"select peering.peering_aspa_upstream, peering.peering_role_overwrite, peering_ips_extra.peering_ips_extra_addr, peering_ips_extra.peering_ips_extra_multihop, peeringdb_network.asn, peeringdb_network.name, peeringdb_network.info_prefixes4, peeringdb_network.info_prefixes6
from peering_ips_extra 
inner join peering on peering.peering_id = peering_ips_extra.peering_ips_extra_peering_id and peering.peering_deleted = false
inner join peeringdb_network ON peeringdb_network.id = peering_peeringdb_id
where peering.peering_peeringdb_id = @peer and peering_ips_extra_active = true and peering_ips_extra_deleted = false;", luje_conn);
				luje_cmd.Parameters.AddWithValue("@peer", Convert.ToInt32(peer.peering_peeringdb_id));
				luje_cmd.Prepare();
				luje_rdr = luje_cmd.ExecuteReader();

				while (luje_rdr.Read())
				{

					IPAddress ipAddress = IPAddress.Parse(luje_rdr["peering_ips_extra_addr"].ToString());
					foreach (var router in config.RouterMapping)
					{
						foreach (var subnet in router.Value.ExtraPrefix)
						{
							if (ipAddress.AddressFamily == AddressFamily.InterNetwork && subnet.Afi == 4)
							{
								if (Utils.IsInSubnet(ipAddress, subnet.Subnet))
								{
									Console.WriteLine($"found peer {luje_rdr["peering_ips_extra_addr"].ToString()} in IXP {subnet.Name} with localpref {subnet.BgpLocalPref}");
									Console.WriteLine($"must deploy on {string.Join(",", router.Key)}");

									string multihop = luje_rdr["peering_ips_extra_multihop"].ToString();

                                    string filename = $"{Configuration.RoutefiltersLocation}/peering_{router.Value.Fqdn}.{router.Value.Vendor}.ipv4.config";
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
									if (!filterReady[$"{router.Key}4"].Contains($"peer_AS{luje_rdr["asn"].ToString()}"))
									{
										GenerateFilter(config, "peer", filename, 4, $"AS{luje_rdr["asn"].ToString()}", gracefullShutdown, "peer", config.Rpki, Convert.ToBoolean(luje_rdr["peering_aspa_upstream"]));
										filterReady[$"{router.Key}4"].Add($"peer_AS{luje_rdr["asn"].ToString()}");
									}

									string neighboorName = $"peer_AS{luje_rdr["asn"].ToString()}_{subnet.Name}_ipv4_{Utils.GetNeighborName(luje_rdr["peering_ips_extra_addr"].ToString())}";
									int? limit = null;
									if (!string.IsNullOrEmpty(luje_rdr["info_prefixes4"].ToString()))
									{
										limit = 10;
									}
									else
									{
										limit = Convert.ToInt32(luje_rdr["info_prefixes4"]);
									}

									string? password = null;
									if (config.Passwords.ContainsKey($"AS{luje_rdr["asn"].ToString()}"))
									{
										password = config.Passwords[$"AS{luje_rdr["asn"].ToString()}"];
									}

									GenerateSession(luje_rdr["peering_role_overwrite"].ToString(), "peer", filename, subnet.IPAddress, 4, multihop, neighboorName, luje_rdr["name"].ToString(), luje_rdr["peering_ips_extra_addr"].ToString(), luje_rdr["asn"].ToString(), subnet.BgpLocalPref, limit, blockImportExport, password, adminDownState, gracefullShutdown);
									continue;
								}
							}
							else if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6 && subnet.Afi == 6)
							{
								if (Utils.IsInSubnet(ipAddress, subnet.Subnet))
                                {
                                    Console.WriteLine($"found peer {luje_rdr["peering_ips_extra_addr"].ToString()} in IXP {subnet.Name} with localpref {subnet.BgpLocalPref}");
                                    Console.WriteLine($"must deploy on {string.Join(",", router.Key)}");

                                    string multihop = luje_rdr["peering_ips_extra_multihop"].ToString();

                                    string filename = $"{Configuration.RoutefiltersLocation}/peering_{router.Value.Fqdn}.{router.Value.Vendor}.ipv6.config";
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
									if (!filterReady[$"{router.Key}6"].Contains($"peer_AS{luje_rdr["asn"].ToString()}"))
									{
										GenerateFilter(config, "peer", filename, 6, $"AS{luje_rdr["asn"].ToString()}", gracefullShutdown, "peer", config.Rpki, Convert.ToBoolean(luje_rdr["peering_aspa_upstream"]));
										filterReady[$"{router.Key}6"].Add($"peer_AS{luje_rdr["asn"].ToString()}");
									}

									string neighboorName = $"peer_AS{luje_rdr["asn"].ToString()}_{subnet.Name}_ipv6_{Utils.GetNeighborName(luje_rdr["peering_ips_extra_addr"].ToString())}";
									int? limit = null;
									if (string.IsNullOrEmpty(luje_rdr["info_prefixes6"].ToString()))
									{
										limit = 10;
									}
									else
									{
										limit = Convert.ToInt32(luje_rdr["info_prefixes6"]);
									}

									string? password = null;
									if (config.Passwords.ContainsKey($"AS{luje_rdr["asn"].ToString()}"))
									{
										password = config.Passwords[$"AS{luje_rdr["asn"].ToString()}"];
									}

									GenerateSession(luje_rdr["peering_role_overwrite"].ToString(), "peer", filename, subnet.IPAddress, 6, multihop, neighboorName, luje_rdr["name"].ToString(), luje_rdr["peering_ips_extra_addr"].ToString(), luje_rdr["asn"].ToString(), subnet.BgpLocalPref, limit, blockImportExport, password, adminDownState, gracefullShutdown);
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
			luje_cmd = new MySqlCommand("update peering set peering_deployed = true, peering_modified = NOW() where peering_active = true and peering_deployed = false and peering_deleted = false;", luje_conn);
			luje_cmd.ExecuteNonQuery();

			luje_cmd = new MySqlCommand("update peering_ips set peering_ips_deployed = true, peering_ips_modified = NOW() where peering_ips_active = true and peering_ips_deployed = false and peering_ips_deleted = false;", luje_conn);
			luje_cmd.ExecuteNonQuery();

			luje_cmd = new MySqlCommand("update peering_ips_extra set peering_ips_extra_deployed = true, peering_ips_extra_modified = NOW() where peering_ips_extra_active = true and peering_ips_extra_deployed = false and peering_ips_extra_deleted = false;", luje_conn);
			luje_cmd.ExecuteNonQuery();
		}

		private static void GenerateFilter(Config config, string sessionType, string FileName, int afi, string ASN, bool GracefulShutdown, string Type, bool Rpki, bool AspaUpstream)
		{
			var filterBuilder = new StringBuilder();
			filterBuilder.Append($@"filter {sessionType}_in_{ASN}_ipv{afi}
prefix set AUTOFILTER_{ASN}_IPv{afi};
{{
    # ignore bogon AS_PATHs
    if is_bogon_aspath() then {{
        #print ""Reject: bogon AS_PATH: "", net, "" "", bgp_path;
        bgp_large_community.add(({Configuration.PortalOwnerAsn}, rejected_route, r_bogon_aspath));
        reject;
    }}

    if ( is_luje_more_specific_ipv{afi}() ) then {{
        #print ""Reject: {Configuration.PortalOwnerAsn} more specific: "", net, "" "", bgp_path, "" filter: peer_in_{ASN}_ipv{afi}"";
        bgp_large_community.add(({Configuration.PortalOwnerAsn}, rejected_route, r_luje_morespecific));
        reject;
    }}
	
    if ( is_bogon_ipv{afi}() ) then {{
        #print ""Reject: bogon: "", net, "" "", bgp_path, "" filter: peer_in_AS{ASN}_ipv{afi}"";
        bgp_large_community.add(({Configuration.PortalOwnerAsn}, rejected_route, r_bogon_prefix));
        reject;
    }}

    if ! is_acceptable_size_ipv{afi}() then {{
        #print ""Reject: too large or too small: "", net, "" "", bgp_path, "" filter: peer_in_AS{ASN}_ipv{afi}"";
        bgp_large_community.add(({Configuration.PortalOwnerAsn}, rejected_route, r_unacceptable_pfxlen));
        reject;
    }}

    # https://tools.ietf.org/html/rfc8326
    allow_graceful_shutdown();").AppendLine().AppendLine();

			if (GracefulShutdown)
			{
				filterBuilder.Append(@"    bgp_community.add((65535, 0));
	bgp_local_pref = 0;").AppendLine().AppendLine();
			}

			if (Type == "peer")
			{
				filterBuilder.Append($@"    # Scrub BGP Communities (RFC 7454 Section 11) from peering partners
    bgp_large_community.delete([({Configuration.PortalOwnerAsn}, *, *)]);
    # Scrub BLACKHOLE Community from peering partners
    bgp_community.delete((65535, 666));
    bgp_large_community.add(({Configuration.PortalOwnerAsn}, 0, 1)); /* mark peering routes as peering routes */").AppendLine().AppendLine();
			}

			if (Rpki)
			{
				filterBuilder.Append($@"    if is_v{afi}_rpki_invalid() then {{
        reject;
    }}").AppendLine().AppendLine()
					.Append($@"    if is_aspa_invalid(")
					.Append(AspaUpstream ? "true" : "false")
					.Append($@") then {{
        reject;
    }}").AppendLine().AppendLine();
			}

			if(config.Irr) {
				filterBuilder.Append($@"    include ""{ASN}.prefixset.bird.ipv{afi}"";
    if (net ~ AUTOFILTER_{ASN}_IPv{afi}");

				if (Rpki)
				{
					filterBuilder.Append($@" || ({Configuration.PortalOwnerAsn}, 5, 2) ~ bgp_large_community");
				}


				filterBuilder.Append($@") then {{
        bgp_med = 0;

        # classifications
        if (net ~ AUTOFILTER_{ASN}_IPv{afi}) then {{
            bgp_large_community.add(({Configuration.PortalOwnerAsn}, 5, 1));
        }}
        accept;
    }}
    #print ""Reject: No IRR? "", net, "" "", bgp_path, "" filter: peer_in_{ASN}_ipv{afi}"";
    bgp_large_community.add(({Configuration.PortalOwnerAsn}, rejected_route, r_no_irr));
    reject;").AppendLine();
			}
			else
			{
				filterBuilder.Append("    accept;").AppendLine().AppendLine();
			}
			filterBuilder.Append("}").AppendLine().AppendLine();

			File.AppendAllText(FileName, filterBuilder.ToString());
		}

		private static void GenerateSession(string bgprole, string sessionType, string FileName, string IPAddress, int afi, string multihop, string NeighboorName, string Name, string IpAddress, string ASN, int? BgpLocalPref, int? Limit, bool BlockImportExport, string? Password, bool AdminDownState, bool GracefulShutdown)
		{
			if(string.IsNullOrEmpty(bgprole))
			{
				bgprole = "peer";
			}
			var sessionBuilder = new StringBuilder();
			sessionBuilder.Append($@"protocol bgp {NeighboorName} {{
    description ""{Name}"";
    neighbor {IpAddress} as {ASN};
    source address {IPAddress};
    local role {bgprole};
    #include ""../ebgp_state.conf"";
    local as {Configuration.PortalOwnerAsn};").AppendLine();
			if(!string.IsNullOrEmpty(multihop))
            {
                sessionBuilder.Append($@"    multihop {multihop};").AppendLine();
            }
            if (BgpLocalPref != null)
			{
				sessionBuilder.Append($@"    default bgp_local_pref {BgpLocalPref};").AppendLine();
			}
			sessionBuilder.Append($@"    ipv{afi} {{
        next hop self;").AppendLine();
			if (Limit != null && Limit >= 0)
			{
				sessionBuilder.Append($@"        receive limit {Limit} action restart;").AppendLine();
			}
			sessionBuilder.Append($@"        import keep filtered;").AppendLine();
			if (BlockImportExport)
			{
				sessionBuilder.Append($@"        import none;").AppendLine();
			}
			else
			{
				sessionBuilder.Append($@"        import filter {sessionType}_in_AS{ASN}_ipv{afi};").AppendLine();
			}
			if (BlockImportExport)
			{
				sessionBuilder.Append($@"        export none;").AppendLine();
			}
			else
			{
				sessionBuilder.Append($@"        export filter exportfilter_ipv{afi}_global;").AppendLine();
			}

			sessionBuilder.Append($@"    }};").AppendLine();
			if (Password != null)
			{
				sessionBuilder.Append($@"    authentication md5;").AppendLine();
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
