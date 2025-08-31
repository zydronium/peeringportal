using LUJEWebsite.Library.Models;
using LUJEWebsite.Library.Utils;
using LUJEWebsite.PeeringApi.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace LUJEWebsite.PeeringApi.Payload
{
	public class NetworkPage
	{
		public static async Task<ListSessionsResponse> NetworkPageAsync(int asn, int? maxResults, string nextToken)
		{
			ListSessionsResponse response = new ListSessionsResponse();
			response.Sessions = new List<Session>();
			string configFile = await System.IO.File.ReadAllTextAsync("peeringconfig.json");
			Config config = JsonConvert.DeserializeObject<Config>(configFile);

			HttpClient client = new HttpClient();

			string asnString = "AS" + asn;
			MySqlConnection luje_conn = new MySqlConnection(Configuration.DBPath);
			await luje_conn.OpenAsync();

			MySqlCommand luje_cmd = new MySqlCommand("select peeringdb_network_ixlan.ixlan_id from peeringdb_network_ixlan where asn = @portalowner;", luje_conn);
			luje_cmd.Parameters.AddWithValue("@portalowner", Convert.ToInt32(Configuration.PortalOwnerAsn));
			await luje_cmd.PrepareAsync();
			MySqlDataReader luje_rdr = (MySqlDataReader)await luje_cmd.ExecuteReaderAsync();
			var ixlanlist = new ArrayList();
			while (await luje_rdr.ReadAsync())
			{
				if (!ixlanlist.Contains(luje_rdr["ixlan_id"].ToString()))
				{
					ixlanlist.Add(luje_rdr["ixlan_id"].ToString());
				}
			}
			await luje_rdr.CloseAsync();

			luje_cmd = new MySqlCommand("select peering_active, peering_deployed from peering where peering_asn = @peering_asn and peering_deleted = false;", luje_conn);
			luje_cmd.Parameters.AddWithValue("@peering_asn", $"{asn}");
			await luje_cmd.PrepareAsync();
			luje_rdr = (MySqlDataReader)await luje_cmd.ExecuteReaderAsync();

			bool knownnetwork = false;
			if (await luje_rdr.ReadAsync())
			{
				knownnetwork = true;
			}
			await luje_rdr.CloseAsync();

			luje_cmd = new MySqlCommand(@"select peeringdb_network_ixlan.id, peeringdb_ix.name, peeringdb_network_ixlan.ixlan_id, peeringdb_network_ixlan.net_id, peeringdb_network_ixlan.ipaddr4, peeringdb_network_ixlan.ipaddr6, owner_peeringdb_network_ixlan.id as ownerid, owner_peeringdb_network_ixlan.ipaddr4 as owneripaddr4, owner_peeringdb_network_ixlan.ipaddr6 as owneripaddr6, peeringdb_ix.id as ix_id
from peeringdb_network_ixlan
inner join peeringdb_ixlan on peeringdb_ixlan.id = peeringdb_network_ixlan.ixlan_id and peeringdb_network_ixlan.status = 'ok'
inner join peeringdb_network_ixlan as owner_peeringdb_network_ixlan on peeringdb_ixlan.id = owner_peeringdb_network_ixlan.ixlan_id and owner_peeringdb_network_ixlan.asn = @ownerasn and owner_peeringdb_network_ixlan.status = 'ok'
inner join peeringdb_ix on peeringdb_ix.id = peeringdb_ixlan.ix_id
where peeringdb_network_ixlan.asn = @asn;", luje_conn);
			luje_cmd.Parameters.AddWithValue("@ownerasn", Convert.ToInt32(Configuration.PortalOwnerAsn));
			luje_cmd.Parameters.AddWithValue("@asn", asn);
			await luje_cmd.PrepareAsync();
			luje_rdr = (MySqlDataReader)await luje_cmd.ExecuteReaderAsync();

			var networkList = new List<NetworkEntry>();

			while (await luje_rdr.ReadAsync())
			{
				NetworkEntry entry = new NetworkEntry
				{
					id = luje_rdr["id"].ToString(),
					name = luje_rdr["name"].ToString(),
					ixlan_id = luje_rdr["ixlan_id"].ToString(),
					ix_id = luje_rdr["ix_id"].ToString(),
					net_id = luje_rdr["net_id"].ToString(),
					ipaddr4 = luje_rdr["ipaddr4"].ToString(),
					ipaddr6 = luje_rdr["ipaddr6"].ToString(),
					ownerid = luje_rdr["ownerid"].ToString(),
					owneripaddr4 = luje_rdr["owneripaddr4"].ToString(),
					owneripaddr6 = luje_rdr["owneripaddr6"].ToString()
				};
				networkList.Add(entry);
			}
			await luje_rdr.CloseAsync();

			foreach (var entry in networkList)
			{
				if (ixlanlist.Contains(entry.ixlan_id))
				{
					if (entry.ipaddr4 != null && entry.ipaddr4 != "")
					{
						string status = "Not configured";
						bool requestable = true;
						Guid requestId = Guid.Empty;
						if (knownnetwork)
						{
							luje_cmd = new MySqlCommand("select peering_ips_active, peering_ips_rejected, peering_ips_deployed, peering_ips_request_id from peering_ips inner join peering on peering.peering_id = peering_ips_peering_id and peering_deleted = false where peering_ips_peeringdb_lanid = @lanid and peering_ips_peeringdb_addrid = @addrid and peering_ips_peeringdb_oaddrid = @oaddrid and peering_ips_type = 4 and peering_asn = @peering_asn and peering_ips_deleted = false;", luje_conn);
							luje_cmd.Parameters.AddWithValue("@peering_asn", $"{asn}");
							luje_cmd.Parameters.AddWithValue("@addrid", Convert.ToInt32(entry.id));
							luje_cmd.Parameters.AddWithValue("@oaddrid", Convert.ToInt32(entry.ownerid));
							luje_cmd.Parameters.AddWithValue("@lanid", Convert.ToInt32(entry.ixlan_id));
							await luje_cmd.PrepareAsync();
							luje_rdr = (MySqlDataReader)await luje_cmd.ExecuteReaderAsync();

							if (await luje_rdr.ReadAsync())
							{
								requestable = false;
								requestId = Guid.Parse(luje_rdr["peering_ips_request_id"].ToString());
								if (Convert.ToBoolean(luje_rdr["peering_ips_active"]) == false && Convert.ToBoolean(luje_rdr["peering_ips_rejected"]) == false)
								{
									status = "Pending approval";
								}
								else if (Convert.ToBoolean(luje_rdr["peering_ips_active"]) == false && Convert.ToBoolean(luje_rdr["peering_ips_rejected"]) == true)
								{
									status = "Rejected";
								}
								else if (Convert.ToBoolean(luje_rdr["peering_ips_active"]) == true && Convert.ToBoolean(luje_rdr["peering_ips_deployed"]) == false)
								{
									status = "Pending deployment";
								}
								else if (Convert.ToBoolean(luje_rdr["peering_ips_active"]) == true && Convert.ToBoolean(luje_rdr["peering_ips_deployed"]) == true)
								{
									var lan = config.LanMapping[entry.ixlan_id];
									var matchingRouter = lan.Routers.FirstOrDefault(item => item.IPAddresses.Contains(entry.owneripaddr4));
									if (matchingRouter != null)
									{
										try
										{
											var neighboorName = $"peer_AS{asn}_{lan.Name}_ipv4_{Utils.GetNeighborName(entry.ipaddr4)}";
											var hostname = config.RouterMapping[matchingRouter.Name].Hostname;
											var birdlgOutput = await Utils.GetBirdProtocolStatus(hostname, neighboorName);
											status = Utils.GetInfoValue(birdlgOutput);
										}
										catch (Exception e)
										{
											status = "Deployed";
										}
									}
									else
									{
										status = "Deployed";
									}
								}
							}
							await luje_rdr.CloseAsync();
						}

						Location location = new Location();
						location.Id = $"pdb:ix:{entry.ix_id}";
						location.Type = LocationType.PublicEnum;

						Session session = new Session();
						session.SessionId = $"{asn}-{entry.ixlan_id}-{entry.id}-{entry.ownerid}-4";
						session.RequestId = requestId == Guid.Empty ? null : requestId.ToString();
						session.LocalAsn = asn;
						session.LocalIp = $"{entry.ipaddr4}";
						session.PeerAsn = Convert.ToInt32(Configuration.PortalOwnerAsn);
						session.PeerIp = $"{entry.owneripaddr4}";
						session.PeerType = "public";
						session.Status = $"{status}";
						session.Location = location;
						response.Sessions.Add(session);
					}
					if (entry.ipaddr6 != null && entry.ipaddr6.ToString() != "")
					{
						if (entry.owneripaddr6.ToString() == "")
						{
							continue;
						}
						string status = "Not configured";
						Guid requestId = Guid.Empty;
						bool requestable = true;
						if (knownnetwork)
						{
							luje_cmd = new MySqlCommand("select peering_ips_active, peering_ips_rejected, peering_ips_deployed, peering_ips_request_id from peering_ips inner join peering on peering.peering_id = peering_ips_peering_id and peering_deleted = false where peering_ips_peeringdb_lanid = @lanid and peering_ips_peeringdb_addrid = @addrid and peering_ips_peeringdb_oaddrid = @oaddrid and peering_ips_type = 6 and peering_asn = @peering_asn and peering_ips_deleted = false;", luje_conn);
							luje_cmd.Parameters.AddWithValue("@peering_asn", $"{asn}");
							luje_cmd.Parameters.AddWithValue("@addrid", Convert.ToInt32(entry.id));
							luje_cmd.Parameters.AddWithValue("@oaddrid", Convert.ToInt32(entry.ownerid));
							luje_cmd.Parameters.AddWithValue("@lanid", Convert.ToInt32(entry.ixlan_id));
							await luje_cmd.PrepareAsync();
							luje_rdr = (MySqlDataReader)await luje_cmd.ExecuteReaderAsync();

							if (await luje_rdr.ReadAsync())
							{
								requestable = false;
								requestId = Guid.Parse(luje_rdr["peering_ips_request_id"].ToString());
								if (Convert.ToBoolean(luje_rdr["peering_ips_active"]) == false && Convert.ToBoolean(luje_rdr["peering_ips_rejected"]) == false)
								{
									status = "Pending approval";
								}
								else if (Convert.ToBoolean(luje_rdr["peering_ips_active"]) == false && Convert.ToBoolean(luje_rdr["peering_ips_rejected"]) == true)
								{
									status = "Rejected";
								}
								else if (Convert.ToBoolean(luje_rdr["peering_ips_active"]) == true && Convert.ToBoolean(luje_rdr["peering_ips_deployed"]) == false)
								{
									status = "Pending deployment";
								}
								else if (Convert.ToBoolean(luje_rdr["peering_ips_active"]) == true && Convert.ToBoolean(luje_rdr["peering_ips_deployed"]) == true)
								{
									var lan = config.LanMapping[entry.ixlan_id];
									var matchingRouter = lan.Routers.FirstOrDefault(item => item.IPAddresses.Contains(entry.owneripaddr6));
									if (matchingRouter != null)
									{
										try
										{
											var neighboorName = $"peer_AS{asn}_{lan.Name}_ipv6_{Utils.GetNeighborName(entry.ipaddr6)}";
											var hostname = config.RouterMapping[matchingRouter.Name].Hostname;
											var birdlgOutput = await Utils.GetBirdProtocolStatus(hostname, neighboorName);
											status = Utils.GetInfoValue(birdlgOutput);
										}
										catch (Exception e)
										{
											status = "Deployed";
										}
									}
									else
									{
										status = "Deployed";
									}
								}
							}
							await luje_rdr.CloseAsync();
						}

						Location location = new Location();
						location.Id = $"pdb:ix:{entry.ix_id}";
						location.Type = LocationType.PublicEnum;

						Session session = new Session();
						session.SessionId = $"{asn}-{entry.ixlan_id}-{entry.id}-{entry.ownerid}-6";
						session.RequestId = requestId == Guid.Empty ? null : requestId.ToString();
						session.LocalAsn = asn;
						session.LocalIp = $"{entry.ipaddr6}";
						session.PeerAsn = Convert.ToInt32(Configuration.PortalOwnerAsn);
						session.PeerIp = $"{entry.owneripaddr6}";
						session.PeerType = "public";
						session.Status = $"{status}";
						session.Location = location;
						response.Sessions.Add(session);
					}
				}
			}
			await luje_conn.CloseAsync();
			return response;
		}
	}
}
