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
using System.Diagnostics.Metrics;

namespace LUJEWebsite.PeeringGenerator
{
	internal class ExportFilter
	{
		public static void Run(Config config, MySqlConnection luje_conn)
		{
			MySqlCommand luje_cmd = new MySqlCommand();
			luje_cmd = new MySqlCommand(@"select downstream_prefixes_addr, downstream_prefixes_type from downstream_prefixes where downstream_prefixes_global = true and downstream_prefixes_deleted = false;", luje_conn);
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
			}
			luje_rdr.Close();

			luje_cmd = new MySqlCommand(@"select @ownerasn as asn
										   union 
                                           select downstream_asn as asn from downstream where downstream_public = true and downstream_deleted = false
										   union
                                           select downstream_asns_asn as asn from downstream_asns where downstream_asns_deleted = false
                                           group by asn;", luje_conn);
			luje_cmd.Parameters.AddWithValue("@ownerasn", Convert.ToInt32(Configuration.PortalOwnerAsn));
			luje_cmd.Prepare();
			luje_rdr = luje_cmd.ExecuteReader();

			var asnList = new List<int>();
			while (luje_rdr.Read())
			{
				asnList.Add(Convert.ToInt32(luje_rdr["asn"]));
			}
			luje_rdr.Close();

			int[] afiList = new int[] { 4, 6 };

			foreach(int afi in afiList)
			{
				string filename = $"{Configuration.RoutefiltersLocation}/exportfilter.ipv{afi}.config";


				GenerateFilter(filename, afi, prefixList, asnList);
			}

			
		}

		private static void GenerateFilter(string FileName, int afi, List<PrefixEntry> prefixList, List<int> asnList)
		{
			var filterBuilder = new StringBuilder();

			filterBuilder.Append($@"define ALLOWED_ORIGINS_ipv{afi} = [").AppendLine();
			int i = 0;
			foreach (var asn in asnList)
			{
				if (i > 0)
				{
					filterBuilder.Append(',').AppendLine();
				}
				filterBuilder.Append($@"    {asn}");
				i++;
			}
			filterBuilder.AppendLine();
			filterBuilder.Append($@"];").AppendLine().AppendLine();

			filterBuilder.Append($@"function exportfilter_extra_ipv{afi}_global() -> bool {{").AppendLine();
			filterBuilder.Append($@"    if (net ~ [").AppendLine();
			i = 0;
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

			filterBuilder.Append($@"filter exportfilter_ipv{afi}_global {{
    # Accept all routes originated by your static_global4 protocol directly
    if (proto = ""static_global_ipv{afi}"") then {{
        accept;
    }}

    # Check that the prefix is originated by allowed ASNs
    if !(bgp_path.last ~ ALLOWED_ORIGINS_ipv{afi}) then {{
        reject;
    }}

    if is_v4_rpki_invalid() || !exportfilter_extra_ipv{afi}_global() then {{
        reject;
    }}
    
    if is_aspa_invalid(true) then {{
        reject;
    }}

    accept;
}}").AppendLine();
			File.AppendAllText(FileName, filterBuilder.ToString());
		}
	}
}
