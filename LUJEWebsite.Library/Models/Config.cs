using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LUJEWebsite.Library.Models
{
	public class Config
    {
        public Dictionary<string, string> Passwords { get; set; }
        public Dictionary<string, LanEntry> LanMapping { get; set; }
        public Dictionary<string, RouterEntry> RouterMapping { get; set; }
        public bool Rpki { get; set; }
		public bool Irr { get; set; }
	}
}
