using System.Net;

namespace LUJEWebsite.Library.Models
{
	public class Profile
	{
		public int id { get; set; }
		public string given_name { get; set; }
		public string family_name { get; set; }
		public bool verified_user { get; set; }
		public string email { get; set; }
		public bool verified_email { get; set; }
		public string name { get; set; }
		public Network[] networks { get; set; }
	}
}
