using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LUJEWebsite.PeeringGenerator.Models
{
	internal class RipeDatabaseSingleObject
	{
		public string type { get; set; }
		public RipeDatabaseSource source { get; set; }
		public RipeDatabaseAttributes attributes { get; set; }
	}
}
