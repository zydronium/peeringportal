using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;

namespace LUJEWebsite.Mailer
{
	internal class Mailer
	{
		public static void Run()
		{
			NpgsqlConnection luje_conn = new NpgsqlConnection(Configuration.DBPath);
			luje_conn.Open();

			approval_notifications(luje_conn);
			requester_notifications(luje_conn);

			luje_conn.Close();
		}

		public static void approval_notifications(NpgsqlConnection luje_conn)
		{
			NpgsqlCommand luje_cmd = new NpgsqlCommand("select peering_ips.peering_ips_peering_id, peering_ips.peering_ips_id, peering_ips.peering_ips_peeringdb_lanid, peering_ips.peering_ips_peeringdb_addrid, peering_ips.peering_ips_peeringdb_oaddrid, peering_ips.peering_ips_type, peering.peering_peeringdb_id from peering_ips inner join peering on peering.peering_id = peering_ips.peering_ips_peering_id and peering.peering_deleted = false where peering_ips_active = false and peering_ips_notified_approval = false and peering_ips_notified_skip = false and peering_ips_deleted = false order by peering.peering_asn asc;", luje_conn);
			NpgsqlDataReader luje_rdr = luje_cmd.ExecuteReader();
			var networkList = new List<NetworkEntry>();

			while (luje_rdr.Read())
			{
				NetworkEntry entry = new NetworkEntry
				{
					peering_ips_peering_id = luje_rdr["peering_ips_peering_id"].ToString(),
					peering_ips_id = luje_rdr["peering_ips_id"].ToString(),
					peering_ips_peeringdb_lanid = luje_rdr["peering_ips_peeringdb_lanid"].ToString(),
					peering_ips_peeringdb_addrid = luje_rdr["peering_ips_peeringdb_addrid"].ToString(),
					peering_ips_peeringdb_oaddrid = luje_rdr["peering_ips_peeringdb_oaddrid"].ToString(),
					peering_ips_type = luje_rdr["peering_ips_type"].ToString(),
					peering_peeringdb_id = luje_rdr["peering_peeringdb_id"].ToString()
				};

				networkList.Add(entry);
			}
			luje_rdr.Close();

			var stringbBuilder = new StringBuilder();
			stringbBuilder.AppendLine("Dear Hostmaster,");
			stringbBuilder.AppendLine("");
			stringbBuilder.AppendLine("Here are the new peering requests pending approval:");

			bool notify = false;
			foreach (var entry in networkList)
			{
				notify = true;
				luje_cmd = new NpgsqlCommand(@"select peeringdb_network_ixlan.id, peeringdb_ix.name as ix_name, peeringdb_network_ixlan.ixlan_id, peeringdb_network_ixlan.net_id, peeringdb_network_ixlan.ipaddr4, peeringdb_network_ixlan.ipaddr6, peeringdb_network.asn, peeringdb_network.name, owner_peeringdb_network_ixlan.ipaddr4 as owneripaddr4, owner_peeringdb_network_ixlan.ipaddr6 as owneripaddr6
from peeringdb_network_ixlan 
inner join peeringdb_network on peeringdb_network.id = peeringdb_network_ixlan.net_id 
inner join peeringdb_ixlan on peeringdb_ixlan.id = peeringdb_network_ixlan.ixlan_id 
inner join peeringdb_network_ixlan as owner_peeringdb_network_ixlan on peeringdb_ixlan.id = owner_peeringdb_network_ixlan.ixlan_id and owner_peeringdb_network_ixlan.id = @oaddrid
inner join peeringdb_ix on peeringdb_ix.id = peeringdb_ixlan.ix_id 
where peeringdb_network.id = @peeringdb_id and peeringdb_network_ixlan.id = @addrid and peeringdb_network_ixlan.ixlan_id = @lanid;", luje_conn);
				luje_cmd.Parameters.AddWithValue("@peeringdb_id", NpgsqlDbType.Integer, Convert.ToInt32(entry.peering_peeringdb_id));
				luje_cmd.Parameters.AddWithValue("@addrid", NpgsqlDbType.Integer, Convert.ToInt32(entry.peering_ips_peeringdb_addrid));
				luje_cmd.Parameters.AddWithValue("@oaddrid", NpgsqlDbType.Integer, Convert.ToInt32(entry.peering_ips_peeringdb_oaddrid));
				luje_cmd.Parameters.AddWithValue("@lanid", NpgsqlDbType.Integer, Convert.ToInt32(entry.peering_ips_peeringdb_lanid));
				luje_cmd.Prepare();
				luje_rdr = luje_cmd.ExecuteReader();
				if (luje_rdr.Read())
				{
					string addr = "";
					string owneraddr = "";
					string asn = "AS" + luje_rdr["asn"].ToString();
					if (entry.peering_ips_type == "4")
					{
						addr = luje_rdr["ipaddr4"].ToString();
						owneraddr = luje_rdr["owneripaddr4"].ToString();
					}
					else if (entry.peering_ips_type == "6")
					{
						addr = luje_rdr["ipaddr6"].ToString();
						owneraddr = luje_rdr["owneripaddr6"].ToString();
					}

					stringbBuilder.Append("- ");
					stringbBuilder.Append(asn);
					stringbBuilder.Append(" ");
					stringbBuilder.Append(luje_rdr["name"].ToString());
					stringbBuilder.Append(" ");
					stringbBuilder.Append(luje_rdr["ix_name"].ToString());
					stringbBuilder.AppendLine("");
					stringbBuilder.Append("  Peer IP Address: ");
					stringbBuilder.Append(addr);
					stringbBuilder.AppendLine("");
					stringbBuilder.Append("  Our IP Address: ");
					stringbBuilder.Append(owneraddr);
					stringbBuilder.AppendLine("");
				}
				luje_rdr.Close();
			}
			stringbBuilder.AppendLine("");
			stringbBuilder.AppendLine("Regards,");
			stringbBuilder.AppendLine("LUJE.net Peering Portal");
			luje_rdr.Close();

			if (notify)
			{
				try
				{
					MailMessage mail = new MailMessage();
					SmtpClient SmtpServer = new SmtpClient(Configuration.MailServer, Configuration.MailPort);

					mail.From = new MailAddress(Configuration.MailFrom);
					mail.To.Add(Configuration.MailFrom);
					mail.Subject = "Pending peering request approvals";
					mail.Body = stringbBuilder.ToString();
					mail.Headers.Add("Message-Id", String.Format("<{0}@{1}>", Guid.NewGuid().ToString(), Configuration.MailFrom.Split("@")[1]));

					SmtpServer.Credentials = new System.Net.NetworkCredential(Configuration.MailUser, Configuration.MailPassword);
					if (Configuration.MailTls == "True")
					{
						SmtpServer.EnableSsl = true;
					}

					SmtpServer.Send(mail);

					luje_cmd = new NpgsqlCommand("update peering_ips set peering_ips_notified_approval = true, peering_ips_modified = NOW() where peering_ips_active = false and peering_ips_notified_approval = false and peering_ips_rejected = false and peering_ips_notified_skip = false and peering_ips_deleted = false;", luje_conn);
					luje_cmd.ExecuteNonQuery();
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error during sending of mail");
				}
			}
		}
		public static void requester_notifications(NpgsqlConnection luje_conn)
		{
			NpgsqlCommand luje_cmd = new NpgsqlCommand("select peering.peering_id, peering.peering_peeringdb_id, peering_ips.peering_ips_notified_email from peering_ips inner join peering on peering.peering_id = peering_ips.peering_ips_peering_id and peering.peering_deleted = false where ((peering_ips_active = true and peering_ips_rejected = false) or (peering_ips_active = false and peering_ips_rejected = true)) and peering_ips_notified = false and peering_ips_notified_skip = false and peering_ips_deleted = false group by peering.peering_id, peering.peering_asn, peering_ips.peering_ips_notified_email, peering_peeringdb_id;", luje_conn);
			NpgsqlDataReader luje_rdr = luje_cmd.ExecuteReader();

			var peeringList = new ArrayList();

			while(luje_rdr.Read())
	{
				NotifyNetworkModel model = new NotifyNetworkModel();
				model.id = luje_rdr["peering_id"].ToString();
				model.peeringdb_id = luje_rdr["peering_peeringdb_id"].ToString();
				model.email = luje_rdr["peering_ips_notified_email"].ToString();
				peeringList.Add(model);
			}
			luje_rdr.Close();

			foreach (NotifyNetworkModel peer in peeringList)
			{
				luje_cmd = new NpgsqlCommand("select peeringdb_network.asn, peeringdb_network.name from peeringdb_network where peeringdb_network.id = @peeringdb_id;", luje_conn);
				luje_cmd.Parameters.AddWithValue("@peeringdb_id", NpgsqlDbType.Integer, Convert.ToInt32(peer.peeringdb_id));
				luje_cmd.Prepare();

				string asn = "";
				string name = "";
				luje_rdr = luje_cmd.ExecuteReader();
				if (luje_rdr.Read())
				{
					asn = "AS" + luje_rdr["asn"].ToString();
					name = luje_rdr["name"].ToString();
				}
				luje_rdr.Close();

				var stringbBuilder = new StringBuilder();
				stringbBuilder.Append("Dear ");
				stringbBuilder.Append(name);
				stringbBuilder.AppendLine(" Peering Team,");
				stringbBuilder.AppendLine("");
				stringbBuilder.AppendLine("The LUJE.net Peering Team has processed your requests.");
				stringbBuilder.AppendLine("");
				stringbBuilder.AppendLine("The Peering Team has decided the following:");

				bool notify = false;
				bool active = false;
				bool rejected = false;
				luje_cmd = new NpgsqlCommand("select peering_ips.peering_ips_peering_id, peering_ips.peering_ips_id, peering_ips.peering_ips_peeringdb_lanid, peering_ips.peering_ips_peeringdb_addrid, peering_ips.peering_ips_peeringdb_oaddrid, peering_ips.peering_ips_type, peering.peering_peeringdb_id, peering_ips_active, peering_ips_rejected from peering_ips inner join peering on peering.peering_id = peering_ips.peering_ips_peering_id and peering.peering_deleted = false where ((peering_ips_active = true and peering_ips_rejected = false) or (peering_ips_active = false and peering_ips_rejected = true)) and peering_ips_notified = false and peering_ips_notified_skip = false and peering_ips_deleted = false and peering_id = @peeringid order by peering.peering_asn asc;", luje_conn);
				luje_cmd.Parameters.AddWithValue("@peeringid", Convert.ToInt32(peer.id));
				luje_cmd.Prepare();
				luje_rdr = luje_cmd.ExecuteReader();
				var networkList = new List<NetworkEntry>();

				while (luje_rdr.Read())
				{
					NetworkEntry entry = new NetworkEntry
					{
						peering_ips_peering_id = luje_rdr["peering_ips_peering_id"].ToString(),
						peering_ips_id = luje_rdr["peering_ips_id"].ToString(),
						peering_ips_peeringdb_lanid = luje_rdr["peering_ips_peeringdb_lanid"].ToString(),
						peering_ips_peeringdb_addrid = luje_rdr["peering_ips_peeringdb_addrid"].ToString(),
						peering_ips_peeringdb_oaddrid = luje_rdr["peering_ips_peeringdb_oaddrid"].ToString(),
						peering_ips_type = luje_rdr["peering_ips_type"].ToString(),
						peering_peeringdb_id = luje_rdr["peering_peeringdb_id"].ToString(),
						peering_ips_active = Convert.ToBoolean(luje_rdr["peering_ips_active"]),
						peering_ips_rejected = Convert.ToBoolean(luje_rdr["peering_ips_rejected"])
					};

					networkList.Add(entry);
				}
				luje_rdr.Close();

				foreach (var entry in networkList)
				{
					notify = true;
					luje_cmd = new NpgsqlCommand(@"select peeringdb_network_ixlan.id, peeringdb_ix.name as ix_name, peeringdb_network_ixlan.ixlan_id, peeringdb_network_ixlan.net_id, peeringdb_network_ixlan.ipaddr4, peeringdb_network_ixlan.ipaddr6, peeringdb_network.asn, peeringdb_network.name, owner_peeringdb_network_ixlan.ipaddr4 as owneripaddr4, owner_peeringdb_network_ixlan.ipaddr6 as owneripaddr6
from peeringdb_network_ixlan
inner join peeringdb_network on peeringdb_network.id = peeringdb_network_ixlan.net_id 
inner join peeringdb_ixlan on peeringdb_ixlan.id = peeringdb_network_ixlan.ixlan_id 
inner join peeringdb_network_ixlan as owner_peeringdb_network_ixlan on peeringdb_ixlan.id = owner_peeringdb_network_ixlan.ixlan_id and owner_peeringdb_network_ixlan.id = @oaddrid
inner join peeringdb_ix on peeringdb_ix.id = peeringdb_ixlan.ix_id 
where peeringdb_network.id = @peeringdb_id and peeringdb_network_ixlan.id = @addrid and peeringdb_network_ixlan.ixlan_id = @lanid;", luje_conn);
					luje_cmd.Parameters.AddWithValue("@peeringdb_id", NpgsqlDbType.Integer, Convert.ToInt32(entry.peering_peeringdb_id));
					luje_cmd.Parameters.AddWithValue("@addrid", NpgsqlDbType.Integer, Convert.ToInt32(entry.peering_ips_peeringdb_addrid));
					luje_cmd.Parameters.AddWithValue("@oaddrid", NpgsqlDbType.Integer, Convert.ToInt32(entry.peering_ips_peeringdb_oaddrid));
					luje_cmd.Parameters.AddWithValue("@lanid", NpgsqlDbType.Integer, Convert.ToInt32(entry.peering_ips_peeringdb_lanid));
					luje_cmd.Prepare();
					luje_rdr = luje_cmd.ExecuteReader();
					if (luje_rdr.Read())
					{
						string addr = "";
						string owneraddr = "";
						if (entry.peering_ips_type == "4")
						{
							addr = luje_rdr["ipaddr4"].ToString();
							owneraddr = luje_rdr["owneripaddr4"].ToString();
						}
						else if (entry.peering_ips_type == "6")
						{
							addr = luje_rdr["ipaddr6"].ToString();
							owneraddr = luje_rdr["owneripaddr6"].ToString();
						}

						stringbBuilder.Append("- ");
						stringbBuilder.Append(luje_rdr["ix_name"].ToString());
						if(entry.peering_ips_active && !entry.peering_ips_rejected)
						{
							active = true;
							stringbBuilder.Append(" - Status: Approved, Pending deployment");
						}
						else if (!entry.peering_ips_active && entry.peering_ips_rejected)
						{
							rejected = true;
							stringbBuilder.Append(" - Status: Rejected");
						}
						stringbBuilder.AppendLine("");
						stringbBuilder.Append("  Your IP Address: ");
						stringbBuilder.Append(addr);
						stringbBuilder.AppendLine("");
						stringbBuilder.Append("  Our IP Address: ");
						stringbBuilder.Append(owneraddr);
						stringbBuilder.AppendLine("");
					}
					luje_rdr.Close();
				}
				if(active)
				{
					stringbBuilder.AppendLine("");
					stringbBuilder.AppendLine("General details:");
					stringbBuilder.Append("ASN: AS");
					stringbBuilder.AppendLine(Configuration.PortalOwnerAsn);
					stringbBuilder.Append("AS-SET: ");
					stringbBuilder.AppendLine(Configuration.PortalExport);
					stringbBuilder.AppendLine("");
					stringbBuilder.AppendLine("Your approved sessions will be automatically deployed within an hour.");
				}
				stringbBuilder.AppendLine("");
				stringbBuilder.AppendLine("Regards,");
				stringbBuilder.AppendLine("LUJE.net Peering Portal");
				luje_rdr.Close();

				if (notify)
				{
					try
					{
						MailMessage mail = new MailMessage();
						SmtpClient SmtpServer = new SmtpClient(Configuration.MailServer, Configuration.MailPort);

						mail.From = new MailAddress(Configuration.MailFrom);
						mail.To.Add(peer.email);
						mail.Bcc.Add(Configuration.MailFrom);
						mail.Subject = "Your peering requests with LUJE.net";
						mail.Body = stringbBuilder.ToString();
						mail.Headers.Add("Message-Id", String.Format("<{0}@{1}>", Guid.NewGuid().ToString(), Configuration.MailFrom.Split("@")[1]));

						SmtpServer.Credentials = new System.Net.NetworkCredential(Configuration.MailUser, Configuration.MailPassword);
						if(Configuration.MailTls == "True")
						{
							SmtpServer.EnableSsl = true;
						}

						SmtpServer.Send(mail);

						luje_cmd = new NpgsqlCommand("update peering_ips set peering_ips_notified = true, peering_ips_modified = NOW() where peering_ips_active = true and peering_ips_rejected = false and peering_ips_notified = false and peering_ips_notified_skip = false and peering_ips_deleted = false and peering_ips_peering_id = @id;", luje_conn);
						luje_cmd.Parameters.AddWithValue("@id", NpgsqlDbType.Integer, Convert.ToInt32(peer.id));
						luje_cmd.Prepare();
						luje_cmd.ExecuteNonQuery();

						luje_cmd = new NpgsqlCommand("update peering_ips set peering_ips_notified = true, peering_ips_deleted = true, peering_ips_modified = NOW() where peering_ips_active = false and peering_ips_rejected = true and peering_ips_notified = false and peering_ips_notified_skip = false and peering_ips_deleted = false and peering_ips_peering_id = @id;", luje_conn);
						luje_cmd.Parameters.AddWithValue("@id", NpgsqlDbType.Integer, Convert.ToInt32(peer.id));
						luje_cmd.Prepare();
						luje_cmd.ExecuteNonQuery();

						luje_cmd = new NpgsqlCommand("select peering.peering_id from peering_ips inner join peering on peering.peering_id = peering_ips.peering_ips_peering_id and peering.peering_deleted = false where peering_ips_deleted = false and peering_ips_rejected = false and peering.peering_id = @id;", luje_conn);
						luje_cmd.Parameters.AddWithValue("@id", NpgsqlDbType.Integer, Convert.ToInt32(peer.id));
						luje_cmd.Prepare();
						luje_rdr = luje_cmd.ExecuteReader();

						if (!luje_rdr.Read())
						{
							luje_rdr.Close();
							luje_cmd = new NpgsqlCommand("update peering set peering_deleted = true, peering_modified = NOW() where peering_id = @id and peering_deleted = false;", luje_conn);
							luje_cmd.Parameters.AddWithValue("@id", NpgsqlDbType.Integer, Convert.ToInt32(peer.id));
							luje_cmd.Prepare();
							luje_cmd.ExecuteNonQuery();
						}
						else
						{
							luje_rdr.Close();
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine("Error during sending of mail");
						Console.WriteLine(ex.Message);
					}
				}
			}
		}
	}
}
