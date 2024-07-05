namespace LUJEWebsite.app.Model
{
	public class Peer
	{
		public string asn { get; set; }
		public string description { get; set; }
		public string import { get; set; }
		public string export { get; set; }
		public List<string> addresses { get; set; }
	}
}
