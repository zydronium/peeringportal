using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace LUJEWebsite.PeeringGenerator
{
	internal class ASNObjectGenerator
	{
		static public async Task RunAsync()
		{
			var xmlDocument =  new XDocument(
				new XDeclaration("1.0", "UTF-8", null),
				new XElement("whois-resources",
					new XElement("objects",
						new XElement("object",
							new XAttribute("type", "aut-num"),
							new XElement("source",
								new XAttribute("id", "ripe")
							),
							new XElement("attributes",
								new XElement("attribute",
									new XAttribute("name", "aut-num"),
									new XAttribute("value", $"AS{Configuration.PortalOwnerAsn}")
								),
								new XElement("attribute",
									new XAttribute("name", "as-name"),
									new XAttribute("value", "AS-LUJE")
								),
								new XElement("attribute",
									new XAttribute("name", "descr"),
									new XAttribute("value", "LUJE.net")
								),
								new XElement("attribute",
									new XAttribute("name", "org"),
									new XAttribute("value", "ORG-JL151-RIPE")
								),
								new XElement("attribute",
									new XAttribute("name", "sponsoring-org"),
									new XAttribute("value", "ORG-SB579-RIPE")
								),
								new XElement("attribute",
									new XAttribute("name", "remarks"),
									new XAttribute("value", "---")
								),
								new XElement("attribute",
									new XAttribute("name", "remarks"),
									new XAttribute("value", "Upstreams:")
								),
								new XElement("attribute",
									new XAttribute("name", "remarks"),
									new XAttribute("value", "Netwerkvereniging Coloclue")
								),
								new XElement("attribute",
									new XAttribute("name", "mp-import"),
									new XAttribute("value", "from AS8283 accept ANY")
								),
								new XElement("attribute",
									new XAttribute("name", "mp-export"),
									new XAttribute("value", $"to AS8283 announce {Configuration.PortalExport}")
								),
								new XElement("attribute",
									new XAttribute("name", "remarks"),
									new XAttribute("value", "IPng Networks GmbH")
								),
								new XElement("attribute",
									new XAttribute("name", "mp-import"),
									new XAttribute("value", "from AS8298 accept ANY")
								),
								new XElement("attribute",
									new XAttribute("name", "mp-export"),
									new XAttribute("value", $"to AS8298 announce {Configuration.PortalExport}")
								),
								new XElement("attribute",
									new XAttribute("name", "remarks"),
									new XAttribute("value", "FREETRANSIT")
								),
								new XElement("attribute",
									new XAttribute("name", "mp-import"),
									new XAttribute("value", "from AS41051 accept ANY")
								),
								new XElement("attribute",
									new XAttribute("name", "mp-export"),
									new XAttribute("value", $"to AS41051 announce {Configuration.PortalExport}")
								),
								new XElement("attribute",
									new XAttribute("name", "remarks"),
									new XAttribute("value", "NetOne NL")
								),
								new XElement("attribute",
									new XAttribute("name", "mp-import"),
									new XAttribute("value", "from AS200132 accept ANY")
								),
								new XElement("attribute",
									new XAttribute("name", "mp-export"),
									new XAttribute("value", $"to AS200132 announce {Configuration.PortalExport}")
								),
								new XElement("attribute",
									new XAttribute("name", "remarks"),
									new XAttribute("value", "---")
								),
								new XElement("attribute",
									new XAttribute("name", "remarks"),
									new XAttribute("value", "Peerings:")
								)
							)
						)
					)
			)
			);

			XElement attributesElement = xmlDocument.Root
			.Element("objects")
			.Element("object")
			.Element("attributes");


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

				attributesElement.Add(
					new XElement("attribute",
						new XAttribute("name", "remarks"),
						new XAttribute("value", luje_rdr["name"].ToString())
					),
					new XElement("attribute",
						new XAttribute("name", "mp-import"),
						new XAttribute("value", $"from AS{luje_rdr["asn"].ToString()} accept {asset}")
					),
					new XElement("attribute",
						new XAttribute("name", "mp-export"),
						new XAttribute("value", $"to AS{luje_rdr["asn"].ToString()} announce {Configuration.PortalExport}")
					)
				);

				PeeringEntry entry = new PeeringEntry
				{
					peering_peeringdb_id = luje_rdr["peering_peeringdb_id"].ToString(),
					asn = luje_rdr["asn"].ToString(),
					name = luje_rdr["name"].ToString(),
					irr_as_set = luje_rdr["irr_as_set"].ToString()
				};
				peeringList.Add(entry);
			}
			luje_rdr.Close();
			luje_conn.Close();

			attributesElement.Add(
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "---")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "Peering with LUJE.net go to: https://www.luje.net")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "---")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "Routing Policy")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "- All peering sessions are strictly filtered based on IRR data")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "- RPKI INVALID prefixes are rejected")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "---")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "BGP Communities")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "INFORMATIONAL COMMUNITIES:")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "==========================")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "Large        | Meaning (Informational)")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "-------------|-------------------------------------")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "212855:0:1   | peering routes")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "212855:0:2   | downstream routes")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "-------------+-------------------------------------")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "212855:5:1   | Accepted from peer because of valid IRR entry")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "212855:5:2   | Accepted from peer because of valid ROA")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "212855:5:3   | matching valid ROA exists")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "212855:5:4   | Accepted while RPKI invalid because it is added to our whitelist")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "-------------+-------------------------------------")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "212855:6:1   | received from Netwerkvereniging Coloclue")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "212855:6:2   | received from IPng Networks GmbH")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "212855:6:3   | received from FREETRANSIT")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "212855:6:4   | received from NetOne NL")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "-------------+-------------------------------------")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "Large        | Rejection Reasons")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "-------------+-------------------------------------")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "212855:7:1   | More specifics covering AS212855 space are considered hijacks")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "212855:7:2   | Somewhere in the AS_PATH a Bogon ASN is present (0, 23456, 64496..65534, 4200000000+)")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "212855:7:3   | The prefix is Bogon garbage (rfc1918, rfc4291 etc)")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "212855:7:4   | The prefix is an RPKI Invalid and as such rejected")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "212855:7:5   | The route's prefix length is unacceptable (too small or too large)")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "212855:7:6   | There is no IRR object that covers this route announcement")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "-------------+-------------------------------------")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "")
				),
				new XElement("attribute",
					new XAttribute("name", "remarks"),
					new XAttribute("value", "---")
				),
				new XElement("attribute",
					new XAttribute("name", "admin-c"),
					new XAttribute("value", "LUJE-RIPE")
				),
				new XElement("attribute",
					new XAttribute("name", "tech-c"),
					new XAttribute("value", "LUJE-RIPE")
				),
				new XElement("attribute",
					new XAttribute("name", "tech-c"),
					new XAttribute("value", "LH4244-RIPE")
				),
				new XElement("attribute",
					new XAttribute("name", "status"),
					new XAttribute("value", "ASSIGNED")
				),
				new XElement("attribute",
					new XAttribute("name", "mnt-by"),
					new XAttribute("value", "RIPE-NCC-END-MNT")
				),
				new XElement("attribute",
					new XAttribute("name", "mnt-by"),
					new XAttribute("value", "LUJENET-MNT")
				),
				new XElement("attribute",
					new XAttribute("name", "mnt-by"),
					new XAttribute("value", "LUJE-MNT")
				),
				new XElement("attribute",
					new XAttribute("name", "source"),
					new XAttribute("value", "RIPE")
				)
			);
			using (var handler = new HttpClientHandler())
			{
				handler.ClientCertificateOptions = ClientCertificateOption.Manual;
				handler.SslProtocols = SslProtocols.Tls12;
				handler.ClientCertificates.Add(new X509Certificate2("ripeauth.pfx", Configuration.PfxPassword));
				using (var client = new HttpClient(handler))
				{
					try
					{
						string ripeApiUrl = $"https://rest-cert.db.ripe.net/ripe/aut-num/AS{Configuration.PortalOwnerAsn}";
						var content = new StringContent(xmlDocument.ToString(), Encoding.UTF8, "application/xml");
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
