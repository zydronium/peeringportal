﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LUJEWebsite.PeeringGenerator
{
    internal class Configuration
    {
        public static string DBPath
        {
            get
            {
                string server = Environment.GetEnvironmentVariable("DATABASE_SERVER");
                string user = Environment.GetEnvironmentVariable("DATABASE_USER");
                string password = Environment.GetEnvironmentVariable("DATABASE_PASSWORD");
                string name = Environment.GetEnvironmentVariable("DATABASE_NAME");
                return String.Format("Server={0};Database={1};Uid={2};Pwd={3};", server, name, user, password);
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
		public static string PfxPassword
		{
			get
			{
				return Environment.GetEnvironmentVariable("PFX_PASSWORD");
			}
		}
		public static string RoutefiltersLocation
		{
			get
			{
				return Environment.GetEnvironmentVariable("ROUTEFILTERS_LOCATION");
			}
		}
	}
}
