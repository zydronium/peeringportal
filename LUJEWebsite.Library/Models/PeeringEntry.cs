namespace LUJEWebsite.Library.Models
{
	public class PeeringEntry
	{
		public string peering_peeringdb_id { get; set; }
		public string upstream_id { get; set; }
		public string downstream_id { get; set; }
		public string name { get; set; }
		public string asn { get; set; }
		public string irr_as_set { get; set; }
		public bool defaultroute { get; set; }
		public bool no_importexport { get; set; }
	}
}
