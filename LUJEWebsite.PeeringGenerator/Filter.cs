using LUJEWebsite.Library.Models;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
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
using LUJEWebsite.Library.Utils;

namespace LUJEWebsite.PeeringGenerator
{
	internal class Filter
    {
        static public void Run(Config config)
		{
			Dictionary<string, List<string>> filterReady = new Dictionary<string, List<string>>();
			
            foreach (var router in config.RouterMapping)
            {
				string filename4 = $"{Configuration.RoutefiltersLocation}/peering_{router.Value.Fqdn}.{router.Value.Vendor}.ipv4.config";
				File.WriteAllText(filename4, "");
				string filename6 = $"{Configuration.RoutefiltersLocation}/peering_{router.Value.Fqdn}.{router.Value.Vendor}.ipv6.config";
				File.WriteAllText(filename6, "");
				filename4 = $"{Configuration.RoutefiltersLocation}/upstream_{router.Value.Fqdn}.{router.Value.Vendor}.ipv4.config";
				File.WriteAllText(filename4, "");
				filename6 = $"{Configuration.RoutefiltersLocation}/upstream_{router.Value.Fqdn}.{router.Value.Vendor}.ipv6.config";
				File.WriteAllText(filename6, "");
				filename4 = $"{Configuration.RoutefiltersLocation}/downstream_{router.Value.Fqdn}.{router.Value.Vendor}.ipv4.config";
				File.WriteAllText(filename4, "");
				filename6 = $"{Configuration.RoutefiltersLocation}/downstream_{router.Value.Fqdn}.{router.Value.Vendor}.ipv6.config";
				File.WriteAllText(filename6, "");
				filename4 = $"{Configuration.RoutefiltersLocation}/probe_{router.Value.Fqdn}.{router.Value.Vendor}.ipv4.config";
				File.WriteAllText(filename4, "");
				filename6 = $"{Configuration.RoutefiltersLocation}/probe_{router.Value.Fqdn}.{router.Value.Vendor}.ipv6.config";
				File.WriteAllText(filename6, "");
				filename4 = $"{Configuration.RoutefiltersLocation}/exportfilter.ipv4.config";
				File.WriteAllText(filename4, "");
				filename6 = $"{Configuration.RoutefiltersLocation}/exportfilter.ipv6.config";
				File.WriteAllText(filename6, "");
				filterReady.Add($"{router.Key}4", new List<string>());
				filterReady.Add($"{router.Key}6", new List<string>());
			}

            MySqlConnection luje_conn = new MySqlConnection(Configuration.DBPath);
            luje_conn.Open();
			MySqlCommand luje_cmd = new MySqlCommand("select peering.peering_peeringdb_id, name, asn, irr_as_set from peering INNER JOIN peeringdb_network ON peeringdb_network.id = peering_peeringdb_id where peering_active = true and peering_deleted = false order by cast(peering_asn as integer) asc;", luje_conn);
			MySqlDataReader luje_rdr = luje_cmd.ExecuteReader();

			var peeringList = new List<PeeringEntry>();

			while(luje_rdr.Read())
	        {
				PeeringEntry entry = new PeeringEntry
				{
					peering_peeringdb_id = luje_rdr["peering_peeringdb_id"].ToString(),
					upstream_id = "",
					asn = luje_rdr["asn"].ToString(),
					name = luje_rdr["name"].ToString(),
					irr_as_set = luje_rdr["irr_as_set"].ToString()
				};
				peeringList.Add(entry);
			}
			luje_rdr.Close();

			luje_cmd = new MySqlCommand("select upstream_name, upstream_asn, upstream_id, upstream_no_importexport from upstream where upstream_enabled = true and upstream_deleted = false order by cast(upstream_asn as integer) asc;", luje_conn);
			luje_rdr = luje_cmd.ExecuteReader();

			var upstreamList = new List<PeeringEntry>();

			while (luje_rdr.Read())
			{
				PeeringEntry entry = new PeeringEntry
				{
					peering_peeringdb_id = "",
					upstream_id = luje_rdr["upstream_id"].ToString(),
					asn = luje_rdr["upstream_asn"].ToString(),
					name = luje_rdr["upstream_name"].ToString(),
					no_importexport = Convert.ToBoolean(luje_rdr["upstream_no_importexport"]),
					irr_as_set = ""
				};
				upstreamList.Add(entry);
			}
			luje_rdr.Close();

			luje_cmd = new MySqlCommand("select downstream_name, downstream_asn, downstream_id, downstream_defaultroute from downstream where downstream_enabled = true and downstream_deleted = false order by cast(downstream_asn as integer) asc;", luje_conn);
			luje_rdr = luje_cmd.ExecuteReader();

			var downstreamList = new List<PeeringEntry>();

			while (luje_rdr.Read())
			{
				PeeringEntry entry = new PeeringEntry
				{
					peering_peeringdb_id = "",
					downstream_id = luje_rdr["downstream_id"].ToString(),
					asn = luje_rdr["downstream_asn"].ToString(),
					name = luje_rdr["downstream_name"].ToString(),
					defaultroute = Convert.ToBoolean(luje_rdr["downstream_defaultroute"]),
					irr_as_set = ""
				};
				downstreamList.Add(entry);
			}
			luje_rdr.Close();

			if (config.Irr)
			{
				var bgpqFilterList = new List<PeeringEntry>();
				PeeringEntry entryExtra = new PeeringEntry
				{
					peering_peeringdb_id = "",
					upstream_id = "",
					asn = Configuration.PortalOwnerAsn,
					name = "",
					irr_as_set = Configuration.PortalExport
				};
				bgpqFilterList.Add(entryExtra);
				bgpqFilterList.AddRange(new List<PeeringEntry>(peeringList));

				Parallel.ForEach(bgpqFilterList, peer =>
				{
					Console.WriteLine($"working on BGPQ4 filter for asn {peer.asn}");
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
						asset = $"AS{peer.asn}";
					}

					//BGP IRR filter generation

					var bgpq = new BgpqGenerator(luje_conn, peer.asn, asset, "NTTCOM,INTERNAL,RADB,RIPE,ALTDB,BELL,LEVEL3,APNIC,JPIRR,ARIN,BBOI,TC,AFRINIC,RPKI,REGISTROBR", "rr.ntt.net");
					bgpq.GenerateFilters();

					Console.WriteLine($"finished working on BGPQ4 filter for asn {peer.asn}");
				});
			}

			PeeringFilter.Run(config, peeringList, luje_conn, filterReady);

			UpstreamFilter.Run(config, upstreamList, luje_conn, filterReady);

			DownstreamFilter.Run(config, downstreamList, luje_conn, filterReady);

			ExportFilter.Run(config, luje_conn);

			ProbeFilter.Run(config, luje_conn);

			luje_conn.Close();

			//deploy filters
			Parallel.ForEach(config.RouterMapping, router =>
			{
				Console.WriteLine($"Sync filters to router {router.Value.Fqdn}");
				var processStartInfo = new ProcessStartInfo
				{
					FileName = "rsync",
					Arguments = $"-avH --delete {Configuration.RoutefiltersLocation}/ root@{router.Value.Hostname}:/etc/bird/filters/",
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
					Arguments = $"root@{router.Value.Hostname} \"chown -R bird:bird /etc/bird; /usr/sbin/birdc configure\"",
					RedirectStandardOutput = true,
					UseShellExecute = false
				};
				using (var process = Process.Start(processStartInfo))
				{
					Console.WriteLine(process.StandardOutput.ReadToEnd());
				}
			});
		}
    }
}
