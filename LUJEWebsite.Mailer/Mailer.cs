using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using MySqlConnector;

namespace LUJEWebsite.Mailer
{
	internal class Mailer
	{
		public static void run()
		{
			MySqlConnection peeringdb_conn = new MySqlConnection(Configuration.PeeringDBPath);
			peeringdb_conn.Open();
			MySqlConnection luje_conn = new MySqlConnection(Configuration.DBPath);
			luje_conn.Open();

			approval_notifications(luje_conn, peeringdb_conn);
			requester_notifications(luje_conn, peeringdb_conn);

			peeringdb_conn.Close();
			luje_conn.Close();
		}

		public static void approval_notifications(MySqlConnection luje_conn, MySqlConnection peeringdb_conn)
		{
			MySqlCommand luje_cmd = new MySqlCommand("select peering_ips.peering_ips_id, peering_ips.peering_ips_peeringdb_lanid, peering_ips.peering_ips_peeringdb_addrid, peering_ips.peering_ips_type, peering.peering_peeringdb_id from peering_ips inner join peering on peering.peering_id = peering_ips.peering_ips_peering_id and peering.peering_deleted = 0 where peering_ips_active = 0 and peering_ips_notified_approval = 0 and peering_ips_notified_skip = 0 and peering_ips_deleted = 0 order by peering.peering_asn asc;", luje_conn);
			MySqlDataReader luje_rdr = luje_cmd.ExecuteReader();

			var stringbBuilder = new StringBuilder();
			stringbBuilder.AppendLine("Dear Hostmaster,");
			stringbBuilder.AppendLine("");
			stringbBuilder.AppendLine("Here are the new peering requests pending approval:");

			bool notify = false;
			while (luje_rdr.Read())
			{
				notify = true;
				MySqlCommand peeringdb_cmd = new MySqlCommand("select peeringdb_network_ixlan.id, peeringdb_ix.name as ix_name, peeringdb_network_ixlan.ixlan_id, peeringdb_network_ixlan.net_id, peeringdb_network_ixlan.ipaddr4, peeringdb_network_ixlan.ipaddr6, peeringdb_network.asn, peeringdb_network.name from peeringdb_network_ixlan inner join peeringdb_network on peeringdb_network.id = peeringdb_network_ixlan.net_id inner join peeringdb_ixlan on peeringdb_ixlan.id = peeringdb_network_ixlan.ixlan_id inner join peeringdb_ix on peeringdb_ix.id = peeringdb_ixlan.ix_id where peeringdb_network.id = @peeringdb_id and peeringdb_network_ixlan.id = @addrid and peeringdb_network_ixlan.ixlan_id = @lanid;", peeringdb_conn);
				peeringdb_cmd.Prepare();
				peeringdb_cmd.Parameters.AddWithValue("@peeringdb_id", luje_rdr["peering_peeringdb_id"].ToString());
				peeringdb_cmd.Parameters.AddWithValue("@addrid", luje_rdr["peering_ips_peeringdb_addrid"].ToString());
				peeringdb_cmd.Parameters.AddWithValue("@lanid", luje_rdr["peering_ips_peeringdb_lanid"].ToString());

				MySqlDataReader peeringdb_rdr = peeringdb_cmd.ExecuteReader();
				if (peeringdb_rdr.Read())
				{
					string addr = "";
					string asn = "AS" + peeringdb_rdr["asn"].ToString();
					if (luje_rdr["peering_ips_type"].ToString() == "4")
					{
						addr = peeringdb_rdr["ipaddr4"].ToString();
					}
					else if (luje_rdr["peering_ips_type"].ToString() == "6")
					{
						addr = peeringdb_rdr["ipaddr6"].ToString();
					}

					stringbBuilder.Append("- ");
					stringbBuilder.Append(asn);
					stringbBuilder.Append(" ");
					stringbBuilder.Append(peeringdb_rdr["name"].ToString());
					stringbBuilder.Append(" ");
					stringbBuilder.Append(peeringdb_rdr["ix_name"].ToString());
					stringbBuilder.Append(" ");
					stringbBuilder.Append(addr);
					stringbBuilder.AppendLine("");
				}
				peeringdb_rdr.Close();
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

					SmtpServer.Credentials = new System.Net.NetworkCredential(Configuration.MailUser, "S92mXh65HKA5bqb7vN5DdqBs");
					SmtpServer.EnableSsl = true;

					SmtpServer.Send(mail);

					luje_cmd = new MySqlCommand("update peering_ips set peering_ips_notified_approval = '1', peering_ips_modified = NOW() where peering_ips_active = 0 and peering_ips_notified_approval = 0 and peering_ips_rejected = 0 and peering_ips_notified_skip = 0 and peering_ips_deleted = 0;", luje_conn);
					luje_cmd.ExecuteNonQuery();
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error during sending of mail");
				}
			}
		}
		public static void requester_notifications(MySqlConnection luje_conn, MySqlConnection peeringdb_conn)
		{
			MySqlCommand luje_cmd = new MySqlCommand("select peering.peering_id, peering.peering_peeringdb_id, peering_ips.peering_ips_notified_email from peering_ips inner join peering on peering.peering_id = peering_ips.peering_ips_peering_id and peering.peering_deleted = 0 where ((peering_ips_active = 1 and peering_ips_rejected = 0) or (peering_ips_active = 0 and peering_ips_rejected = 1)) and peering_ips_notified = 0 and peering_ips_notified_skip = 0 and peering_ips_deleted = 0 group by peering.peering_asn, peering_ips.peering_ips_notified_email, peering.peering_peeringdb_id;", luje_conn);
			MySqlDataReader luje_rdr = luje_cmd.ExecuteReader();

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
				MySqlCommand peeringdb_cmd = new MySqlCommand("select peeringdb_network.asn, peeringdb_network.name from peeringdb_network where peeringdb_network.id = @peeringdb_id;", peeringdb_conn);
				peeringdb_cmd.Prepare();
				peeringdb_cmd.Parameters.AddWithValue("@peeringdb_id", peer.peeringdb_id);

				string asn = "";
				string name = "";
				MySqlDataReader peeringdb_rdr = peeringdb_cmd.ExecuteReader();
				if (peeringdb_rdr.Read())
				{
					asn = "AS" + peeringdb_rdr["asn"].ToString();
					name = peeringdb_rdr["name"].ToString();
				}
				peeringdb_rdr.Close();

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
				luje_cmd = new MySqlCommand("select peering_ips.peering_ips_id, peering_ips.peering_ips_peeringdb_lanid, peering_ips.peering_ips_peeringdb_addrid, peering_ips.peering_ips_type, peering.peering_peeringdb_id, peering_ips.peering_ips_active, peering_ips.peering_ips_rejected from peering_ips inner join peering on peering.peering_id = peering_ips.peering_ips_peering_id and peering.peering_deleted = 0 where ((peering_ips_active = 1 and peering_ips_rejected = 0) or (peering_ips_active = 0 and peering_ips_rejected = 1)) and peering_ips_notified = 0 and peering_ips_notified_skip = 0 and peering_ips_deleted = 0 and peering_id = @peeringid order by peering.peering_asn asc;", luje_conn);
				luje_cmd.Prepare();
				luje_cmd.Parameters.AddWithValue("@peeringid", peer.id);
				luje_rdr = luje_cmd.ExecuteReader();
				while (luje_rdr.Read())
				{
					notify = true;
					peeringdb_cmd = new MySqlCommand("select peeringdb_network_ixlan.id, peeringdb_ix.name as ix_name, peeringdb_network_ixlan.ixlan_id, peeringdb_network_ixlan.net_id, peeringdb_network_ixlan.ipaddr4, peeringdb_network_ixlan.ipaddr6, peeringdb_network.asn, peeringdb_network.name from peeringdb_network_ixlan inner join peeringdb_network on peeringdb_network.id = peeringdb_network_ixlan.net_id inner join peeringdb_ixlan on peeringdb_ixlan.id = peeringdb_network_ixlan.ixlan_id inner join peeringdb_ix on peeringdb_ix.id = peeringdb_ixlan.ix_id where peeringdb_network.id = @peeringdb_id and peeringdb_network_ixlan.id = @addrid and peeringdb_network_ixlan.ixlan_id = @lanid;", peeringdb_conn);
					peeringdb_cmd.Prepare();
					peeringdb_cmd.Parameters.AddWithValue("@peeringdb_id", luje_rdr["peering_peeringdb_id"].ToString());
					peeringdb_cmd.Parameters.AddWithValue("@addrid", luje_rdr["peering_ips_peeringdb_addrid"].ToString());
					peeringdb_cmd.Parameters.AddWithValue("@lanid", luje_rdr["peering_ips_peeringdb_lanid"].ToString());

					peeringdb_rdr = peeringdb_cmd.ExecuteReader();
					if (peeringdb_rdr.Read())
					{
						string addr = "";
						if (luje_rdr["peering_ips_type"].ToString() == "4")
						{
							addr = peeringdb_rdr["ipaddr4"].ToString();
						}
						else if (luje_rdr["peering_ips_type"].ToString() == "6")
						{
							addr = peeringdb_rdr["ipaddr6"].ToString();
						}

						stringbBuilder.Append("- ");
						stringbBuilder.Append(peeringdb_rdr["ix_name"].ToString());
						stringbBuilder.Append(" ");
						stringbBuilder.Append(addr);
						if(Convert.ToBoolean(luje_rdr["peering_ips_active"]) && !Convert.ToBoolean(luje_rdr["peering_ips_rejected"]))
						{
							active = true;
							stringbBuilder.Append(" - Status: Approved, Pending deployment");
						}
						else if (!Convert.ToBoolean(luje_rdr["peering_ips_active"]) && Convert.ToBoolean(luje_rdr["peering_ips_rejected"]))
						{
							rejected = true;
							stringbBuilder.Append(" - Status: Rejected");
						}
						stringbBuilder.AppendLine("");
					}
					peeringdb_rdr.Close();
				}
				if(active)
				{
					stringbBuilder.AppendLine("");
					stringbBuilder.AppendLine("Your approved sessions will be automatically deployed during the next 24 hours.");
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

						SmtpServer.Credentials = new System.Net.NetworkCredential(Configuration.MailUser, Configuration.MailPassword);
						SmtpServer.EnableSsl = true;

						SmtpServer.Send(mail);

						luje_cmd = new MySqlCommand("update peering_ips set peering_ips_notified = '1', peering_ips_modified = NOW() where peering_ips_active = 1 and peering_ips_rejected = 0 and peering_ips_notified = 0 and peering_ips_notified_skip = 0 and peering_ips_deleted = 0 and peering_ips_peering_id = @id;", luje_conn);
						luje_cmd.Prepare();
						luje_cmd.Parameters.AddWithValue("@id", peer.id);
						luje_cmd.ExecuteNonQuery();

						luje_cmd = new MySqlCommand("update peering_ips set peering_ips_notified = '1', peering_ips_deleted = '1', peering_ips_modified = NOW() where peering_ips_active = 0 and peering_ips_rejected = 1 and peering_ips_notified = 0 and peering_ips_notified_skip = 0 and peering_ips_deleted = 0 and peering_ips_peering_id = @id;", luje_conn);
						luje_cmd.Prepare();
						luje_cmd.Parameters.AddWithValue("@id", peer.id);
						luje_cmd.ExecuteNonQuery();

						luje_cmd = new MySqlCommand("select peering.peering_id from peering_ips inner join peering on peering.peering_id = peering_ips.peering_ips_peering_id and peering.peering_deleted = 0 where peering_ips_deleted = 0 and peering_ips_rejected = 0 and peering.peering_id = @id;", luje_conn);
						luje_cmd.Prepare();
						luje_cmd.Parameters.AddWithValue("@id", peer.id);
						luje_rdr = luje_cmd.ExecuteReader();

						if (!luje_rdr.Read())
						{
							luje_rdr.Close();
							luje_cmd = new MySqlCommand("update peering set peering_deleted = '1', peering_modified = NOW() where peering_id = @id and peering_deleted = '0';", luje_conn);
							luje_cmd.Prepare();
							luje_cmd.Parameters.AddWithValue("@id", peer.id);
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
					}
				}
			}
		}
	}
}
