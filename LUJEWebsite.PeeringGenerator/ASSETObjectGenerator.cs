using LUJEWebsite.Library.Models;
using LUJEWebsite.Library.Utils;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace LUJEWebsite.PeeringGenerator
{
	internal class ASSETObjectGenerator
	{
		static public async Task RunAsync()
		{
			var assetAttributes = new List<RipeDatabaseAttribute>
			{
				new RipeDatabaseAttribute { name = "as-set", value = $"{Configuration.PortalExport}" },
				new RipeDatabaseAttribute { name = "members", value = $"AS{Configuration.PortalOwnerAsn}" }
			};

			MySqlConnection luje_conn = new MySqlConnection(Configuration.DBPath);
			luje_conn.Open();

			MySqlCommand luje_cmd = new MySqlCommand("select downstream_asn, downstream_asset from downstream where downstream_public = true and downstream_enabled = true and downstream_deleted = false order by downstream_id;", luje_conn);
			MySqlDataReader luje_rdr = luje_cmd.ExecuteReader();
			while (luje_rdr.Read())
			{
				if(string.IsNullOrEmpty(luje_rdr["downstream_asset"].ToString()))
				{
					assetAttributes.AddRange(new List<RipeDatabaseAttribute>
					{
						new RipeDatabaseAttribute { name = "members", value = $"AS{luje_rdr["downstream_asn"].ToString()}" }
					});
				}
				else
				{
					assetAttributes.AddRange(new List<RipeDatabaseAttribute>
					{
						new RipeDatabaseAttribute { name = "members", value = $"{luje_rdr["downstream_asset"].ToString()}" }
					});
				}
				
			}
			luje_rdr.Close();
			luje_conn.Close();

			assetAttributes.AddRange(new List<RipeDatabaseAttribute>
			{
				new RipeDatabaseAttribute { name = "admin-c", value = "LUJE-RIPE" },
				new RipeDatabaseAttribute { name = "tech-c", value = "LUJE-RIPE" },
				new RipeDatabaseAttribute { name = "mnt-by", value = "LUJE-MNT" },
				new RipeDatabaseAttribute { name = "source", value = "RIPE" }
			});

			var asnObject = new RipeDatabaseObject
			{
				objects = new RipeDatabaseObjects
				{
					@object = new List<RipeDatabaseSingleObject>
					{
						new RipeDatabaseSingleObject
						{
							type = "as-set",
							source = new RipeDatabaseSource { id = "RIPE" },
							attributes = new RipeDatabaseAttributes
							{
								attribute = assetAttributes
							}
						}
					}
				}
			};

			using (var handler = new HttpClientHandler())
			{
				handler.ClientCertificateOptions = ClientCertificateOption.Manual;
				handler.SslProtocols = SslProtocols.Tls12;
				handler.ClientCertificates.Add(new X509Certificate2("ripeauth.pfx", Configuration.PfxPassword));
				using (var client = new HttpClient(handler))
				{
					// Set the Accept header to request JSON
					client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

					try
					{
						string ripeApiUrl = $"https://rest-cert.db.ripe.net/ripe/as-set/{Configuration.PortalExport}";
						var jsonPayload = JsonSerializer.Serialize(asnObject, new JsonSerializerOptions { WriteIndented = true });
						var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
						var response = await client.PutAsync(ripeApiUrl, content);
						response.EnsureSuccessStatusCode();
					}
					catch (HttpRequestException e)
					{
						Console.WriteLine($"Request failed: {e.Message}");
					}
				}
			}
		}
	}
}
