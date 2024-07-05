using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LUJEWebsite.PeeringGenerator.Models
{
	internal class RouterEntry
	{
		public string Fqdn { get; set; }
		public string Hostname { get; set; }
		public string Ipv4 { get; set; }
		public string Ipv6 { get; set; }
		public string Vendor { get; set; }
		public bool GracefulShutdown { get; set; }
		public bool MaintenanceMode { get; set; }
		public List<ExtraPrefixEntry> ExtraPrefix { get; set; }
	}
}