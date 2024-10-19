using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LUJEWebsite.PeeringGenerator.Models
{
	internal class LanRouterEntry
	{
		public string Name { get; set; }
		public List<string> IPAddresses { get; set; }
	}
}
