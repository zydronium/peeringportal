using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LUJEWebsite.Mailer
{
	internal class NetworkEntry
	{
		public string peering_ips_peering_id {  get; set; }
		public string peering_ips_id { get; set; }
		public string peering_ips_peeringdb_lanid { get; set; }
		public string peering_ips_peeringdb_addrid { get; set; }
		public string peering_ips_type { get; set; }
		public string peering_peeringdb_id { get; set; }
		public bool peering_ips_active { get; set; }
		public bool peering_ips_rejected { get; set; }

	}
}