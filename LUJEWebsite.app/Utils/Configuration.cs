namespace LUJEWebsite.app.Utils
{
    public class Configuration
    {
        public static string DBPath
        {
            get
            {
                string server = Environment.GetEnvironmentVariable("DATABASE_SERVER");
                string user = Environment.GetEnvironmentVariable("DATABASE_USER");
                string password = Environment.GetEnvironmentVariable("DATABASE_PASSWORD");
                string name = Environment.GetEnvironmentVariable("DATABASE_NAME");
                return String.Format("Server={0};Database={1};Uid={2};Pwd={3};Include Error Detail=true", server, name, user, password);
            }
        }
		public static string PortalHostname
		{
			get
			{
				return Environment.GetEnvironmentVariable("PORTAL_HOSTNAME");
			}
		}
		public static string PortalExport
		{
			get
			{
				return Environment.GetEnvironmentVariable("PORTAL_EXPORT");
			}
		}
		public static string PortalOwnerAsn
        {
            get
            {
                return Environment.GetEnvironmentVariable("PORTAL_OWNER_ASN");
            }
        }
        public static string PeeringDBClientID
        {
			get
			{
				return Environment.GetEnvironmentVariable("PEERINGDB_CLIENTID");
			}
		}
		public static string PeeringDBClientSecret
        {
			get
			{
				return Environment.GetEnvironmentVariable("PEERINGDB_CLIENTSECRET");
			}
        }
    }
}
