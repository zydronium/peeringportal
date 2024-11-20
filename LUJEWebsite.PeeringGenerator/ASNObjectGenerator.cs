using LUJEWebsite.PeeringGenerator.Models;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace LUJEWebsite.PeeringGenerator
{
	internal class ASNObjectGenerator
	{
		static public async Task RunAsync()
		{
			var asnAttributes = new List<RipeDatabaseAttribute>
			{
				new RipeDatabaseAttribute { name = "aut-num", value = $"AS{Configuration.PortalOwnerAsn}" },
				new RipeDatabaseAttribute { name = "as-name", value = "AS-LUJE" },
				new RipeDatabaseAttribute { name = "descr", value = "LUJE.net" },
				new RipeDatabaseAttribute { name = "org", value = "ORG-JL151-RIPE" },
				new RipeDatabaseAttribute { name = "sponsoring-org", value = "ORG-NC22-RIPE" },
				new RipeDatabaseAttribute { name = "remarks", value = "---" },
				new RipeDatabaseAttribute { name = "remarks", value = "Upstreams:" },
				new RipeDatabaseAttribute { name = "remarks", value = "Netwerkvereniging Coloclue" },
				new RipeDatabaseAttribute { name = "mp-import", value = "from AS8283 accept ANY" },
				new RipeDatabaseAttribute { name = "mp-export", value = $"to AS8283 announce {Configuration.PortalExport}" },
				new RipeDatabaseAttribute { name = "remarks", value = "IPng Networks GmbH" },
				new RipeDatabaseAttribute { name = "mp-import", value = "from AS8298 accept ANY" },
				new RipeDatabaseAttribute { name = "mp-export", value = $"to AS8298 announce {Configuration.PortalExport}" },
				new RipeDatabaseAttribute { name = "remarks", value = "FREETRANSIT" },
				new RipeDatabaseAttribute { name = "mp-import", value = "from AS41051 accept ANY" },
				new RipeDatabaseAttribute { name = "mp-export", value = $"to AS41051 announce {Configuration.PortalExport}" },
				new RipeDatabaseAttribute { name = "remarks", value = "NetOne NL" },
				new RipeDatabaseAttribute { name = "mp-import", value = "from AS200132 accept ANY" },
				new RipeDatabaseAttribute { name = "mp-export", value = $"to AS200132 announce {Configuration.PortalExport}" },
				new RipeDatabaseAttribute { name = "remarks", value = "---" },
				new RipeDatabaseAttribute { name = "remarks", value = "Peerings:" }
			};

			NpgsqlConnection luje_conn = new NpgsqlConnection(Configuration.DBPath);
			luje_conn.Open();
			NpgsqlCommand luje_cmd = new NpgsqlCommand("select peering.peering_peeringdb_id, name, asn, irr_as_set from peering INNER JOIN peeringdb_network ON peeringdb_network.id = peering_peeringdb_id where peering_active = true and peering_deleted = false order by cast(peering_asn as integer) asc;", luje_conn);
			NpgsqlDataReader luje_rdr = luje_cmd.ExecuteReader();

			var peeringList = new List<PeeringEntry>();

			while (luje_rdr.Read())
			{
				string asset = "";
				if (luje_rdr["irr_as_set"].ToString() != "")
				{
					var assetBuilder = new StringBuilder();
					asset = luje_rdr["irr_as_set"].ToString();
					String[] spearator = { " " };
					String[] assetlist = asset.Split(spearator, StringSplitOptions.RemoveEmptyEntries);
					foreach (string assetPart in assetlist)
					{
						String[] spearatorPart = { "::" };
						String[] assetlistPart = assetPart.Split(spearatorPart, StringSplitOptions.RemoveEmptyEntries);
						if (assetlistPart.Count() == 1)
						{
							assetBuilder.Append(assetlistPart[0]);
							assetBuilder.Append(" ");
						}
						else
						{
							assetBuilder.Append(assetlistPart[1]);
							assetBuilder.Append(" ");
						}
					}

					asset = assetBuilder.ToString().Trim();
				}
				else
				{
					asset = luje_rdr["asn"].ToString();
				}

				asnAttributes.AddRange(new List<RipeDatabaseAttribute>
				{
					new RipeDatabaseAttribute { name = "remarks", value = luje_rdr["name"].ToString() },
					new RipeDatabaseAttribute { name = "mp-import", value = $"from AS{luje_rdr["asn"].ToString()} accept {asset}" },
					new RipeDatabaseAttribute { name = "mp-export", value = $"to AS{luje_rdr["asn"].ToString()} announce {Configuration.PortalExport}" }
				});
			}
			luje_rdr.Close();
			luje_conn.Close();

			asnAttributes.AddRange(new List<RipeDatabaseAttribute>
			{
				new RipeDatabaseAttribute { name = "remarks", value = "---" },
				new RipeDatabaseAttribute { name = "remarks", value = "Peering with LUJE.net go to: https://www.luje.net" },
				new RipeDatabaseAttribute { name = "remarks", value = "---" },
				new RipeDatabaseAttribute { name = "remarks", value = "Routing Policy" },
				new RipeDatabaseAttribute { name = "remarks", value = "" },
				new RipeDatabaseAttribute { name = "remarks", value = "- All peering sessions are strictly filtered based on IRR data" },
				new RipeDatabaseAttribute { name = "remarks", value = "- RPKI INVALID prefixes are rejected" },
				new RipeDatabaseAttribute { name = "remarks", value = "---" },
				new RipeDatabaseAttribute { name = "remarks", value = "BGP Communities" },
				new RipeDatabaseAttribute { name = "remarks", value = "" },
				new RipeDatabaseAttribute { name = "remarks", value = "INFORMATIONAL COMMUNITIES:" },
				new RipeDatabaseAttribute { name = "remarks", value = "==========================" },
				new RipeDatabaseAttribute { name = "remarks", value = "Large        | Meaning (Informational)" },
				new RipeDatabaseAttribute { name = "remarks", value = "-------------|-------------------------------------" },
				new RipeDatabaseAttribute { name = "remarks", value = "212855:0:1   | peering routes" },
				new RipeDatabaseAttribute { name = "remarks", value = "212855:0:2   | downstream routes" },
				new RipeDatabaseAttribute { name = "remarks", value = "-------------+-------------------------------------" },
				new RipeDatabaseAttribute { name = "remarks", value = "212855:5:1   | Accepted from peer because of valid IRR entry" },
				new RipeDatabaseAttribute { name = "remarks", value = "212855:5:2   | Accepted from peer because of valid ROA" },
				new RipeDatabaseAttribute { name = "remarks", value = "212855:5:3   | matching valid ROA exists" },
				new RipeDatabaseAttribute { name = "remarks", value = "212855:5:4   | Accepted while RPKI invalid because it is added to our whitelist" },
				new RipeDatabaseAttribute { name = "remarks", value = "-------------+-------------------------------------" },
				new RipeDatabaseAttribute { name = "remarks", value = "212855:6:1   | received from Netwerkvereniging Coloclue" },
				new RipeDatabaseAttribute { name = "remarks", value = "212855:6:2   | received from IPng Networks GmbH" },
				new RipeDatabaseAttribute { name = "remarks", value = "212855:6:3   | received from FREETRANSIT" },
				new RipeDatabaseAttribute { name = "remarks", value = "212855:6:4   | received from NetOne NL" },
				new RipeDatabaseAttribute { name = "remarks", value = "-------------+-------------------------------------" },
				new RipeDatabaseAttribute { name = "remarks", value = "" },
				new RipeDatabaseAttribute { name = "remarks", value = "Large        | Rejection Reasons" },
				new RipeDatabaseAttribute { name = "remarks", value = "-------------+-------------------------------------" },
				new RipeDatabaseAttribute { name = "remarks", value = "212855:7:1   | More specifics covering AS212855 space are considered hijacks" },
				new RipeDatabaseAttribute { name = "remarks", value = "212855:7:2   | Somewhere in the AS_PATH a Bogon ASN is present (0, 23456, 64496..65534, 4200000000+)" },
				new RipeDatabaseAttribute { name = "remarks", value = "212855:7:3   | The prefix is Bogon garbage (rfc1918, rfc4291 etc)" },
				new RipeDatabaseAttribute { name = "remarks", value = "212855:7:4   | The prefix is an RPKI Invalid and as such rejected" },
				new RipeDatabaseAttribute { name = "remarks", value = "212855:7:5   | The route's prefix length is unacceptable (too small or too large)" },
				new RipeDatabaseAttribute { name = "remarks", value = "212855:7:6   | There is no IRR object that covers this route announcement" },
				new RipeDatabaseAttribute { name = "remarks", value = "-------------+-------------------------------------" },
				new RipeDatabaseAttribute { name = "remarks", value = "" },
				new RipeDatabaseAttribute { name = "remarks", value = "---" },
				new RipeDatabaseAttribute { name = "admin-c", value = "LUJE-RIPE" },
				new RipeDatabaseAttribute { name = "tech-c", value = "LUJE-RIPE" },
				new RipeDatabaseAttribute { name = "mnt-by", value = "LUJE-MNT" },
				new RipeDatabaseAttribute { name = "mnt-by", value = "RIPE-NCC-END-MNT" },
				new RipeDatabaseAttribute { name = "status", value = "ASSIGNED" },
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
							type = "aut-num",
							source = new RipeDatabaseSource { id = "RIPE" },
							attributes = new RipeDatabaseAttributes
							{
								attribute = asnAttributes
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
						string ripeApiUrl = $"https://rest-cert.db.ripe.net/ripe/aut-num/AS{Configuration.PortalOwnerAsn}";
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
