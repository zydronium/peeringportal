using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LUJEWebsite.PeeringGenerator.Models
{
	internal class LanEntry
	{
		public string Name { get; set; }
		public List<LanRouterEntry> Routers { get; set; }
		public bool GracefulShutdown { get; set; }
		public bool AdminDownState { get; set; }
		public bool BlockImportExport { get; set; }
		public int? BgpLocalPref {  get; set; }
	}
}
