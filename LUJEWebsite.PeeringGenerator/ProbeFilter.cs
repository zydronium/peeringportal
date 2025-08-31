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
	internal class ProbeFilter
	{
		public static void Run(Config config, MySqlConnection luje_conn)
		{
			MySqlCommand luje_cmd = new MySqlCommand(@"select probes_addr_our, probes_addr_peer, probes_router, probes_type, probes_asn, probes_name from probes where probes_deleted = false;", luje_conn);
			MySqlDataReader luje_rdr = luje_cmd.ExecuteReader();

			while (luje_rdr.Read())
			{
				string routerName = luje_rdr["probes_router"].ToString();

				Console.WriteLine($"found probe {luje_rdr["probes_addr_peer"].ToString()} must deploy on {routerName}");

				var router = config.RouterMapping[routerName];
				string filename = $"{Configuration.RoutefiltersLocation}/probe_{router.Fqdn}.{router.Vendor}.ipv{luje_rdr["probes_type"].ToString()}.config";

				string neighboorName = $"probe_AS{luje_rdr["probes_asn"].ToString()}_ipv{luje_rdr["probes_type"].ToString()}_{Utils.GetNeighborName(luje_rdr["probes_addr_peer"].ToString())}";

				GenerateSession("probe", filename, luje_rdr["probes_addr_our"].ToString(), Convert.ToInt32(luje_rdr["probes_type"]), neighboorName, luje_rdr["probes_name"].ToString(), luje_rdr["probes_addr_peer"].ToString(), luje_rdr["probes_asn"].ToString());
			}

			luje_rdr.Close();

			//set probes to deployed
			luje_cmd = new MySqlCommand("update probes set probes_deployed = true, probes_modified = NOW() where probes_deployed = false and probes_deleted = false;", luje_conn);
			luje_cmd.ExecuteNonQuery();
		}

		private static void GenerateSession(string sessionType, string FileName, string IPAddress, int afi, string NeighboorName, string Name, string IpAddress, string ASN)
		{
			var sessionBuilder = new StringBuilder();
			sessionBuilder.Append($@"protocol bgp {NeighboorName} {{
    description ""{Name}"";
    neighbor {IpAddress} as {ASN};
    source address {IPAddress};
    local role provider;
    multihop 99;
    #include ""../ebgp_state.conf"";
    local as {Configuration.PortalOwnerAsn};
    ipv{afi} {{
        next hop self;
        export all;
        import none;
    }};
}}").AppendLine().AppendLine();

			File.AppendAllText(FileName, sessionBuilder.ToString());
		}
	}
}
