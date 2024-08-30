﻿using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace LUJEWebsite.Mailer
{
	internal class Cleaner
	{
		public static void Run()
		{
			NpgsqlConnection luje_conn = new NpgsqlConnection(Configuration.DBPath);
			luje_conn.Open();

			cleanup_peers(luje_conn);

			luje_conn.Close();
		}

		public static void cleanup_peers(NpgsqlConnection luje_conn)
		{
			NpgsqlCommand luje_cmd = new NpgsqlCommand("select peering_ips.peering_ips_peering_id, peering_ips.peering_ips_id, peering_ips.peering_ips_peeringdb_lanid, peering_ips.peering_ips_peeringdb_addrid, peering_ips.peering_ips_type, peering.peering_peeringdb_id from peering_ips inner join peering on peering.peering_id = peering_ips.peering_ips_peering_id and peering.peering_deleted = false where peering_ips_active = true and peering_ips_deleted = false order by peering_asn asc;", luje_conn);
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
					peering_ips_type = luje_rdr["peering_ips_type"].ToString(),
					peering_peeringdb_id = luje_rdr["peering_peeringdb_id"].ToString()
				};
				networkList.Add(entry);
			}
			luje_rdr.Close();

			List<PeerModel> peers = new List<PeerModel>();
            var stringbBuilder = new StringBuilder();
            stringbBuilder.AppendLine("Dear Hostmaster,");
            stringbBuilder.AppendLine("");
            stringbBuilder.AppendLine("Here are the peering sessions that have been cleaned up:");

            bool notify = false;
            bool remove = false;
			foreach (var entry in networkList)
			{
				luje_cmd = new NpgsqlCommand("select peeringdb_network_ixlan.id, peeringdb_ix.name as ix_name, peeringdb_network_ixlan.ixlan_id, peeringdb_network_ixlan.net_id, peeringdb_network_ixlan.ipaddr4, peeringdb_network_ixlan.ipaddr6, peeringdb_network.asn, peeringdb_network.name from peeringdb_network_ixlan inner join peeringdb_network on peeringdb_network.id = peeringdb_network_ixlan.net_id inner join peeringdb_ixlan on peeringdb_ixlan.id = peeringdb_network_ixlan.ixlan_id inner join peeringdb_ix on peeringdb_ix.id = peeringdb_ixlan.ix_id where peeringdb_network.id = @peeringdb_id and peeringdb_network_ixlan.id = @addrid and peeringdb_network_ixlan.ixlan_id = @lanid;", luje_conn);
				luje_cmd.Parameters.AddWithValue("@peeringdb_id", NpgsqlDbType.Integer, Convert.ToInt32(entry.peering_peeringdb_id));
				luje_cmd.Parameters.AddWithValue("@addrid", NpgsqlDbType.Integer, Convert.ToInt32(entry.peering_ips_peeringdb_addrid));
				luje_cmd.Parameters.AddWithValue("@lanid", NpgsqlDbType.Integer, Convert.ToInt32(entry.peering_ips_peeringdb_lanid));
				luje_cmd.Prepare();
				luje_rdr = luje_cmd.ExecuteReader();
				if (luje_rdr.Read())
                {
                    string addr = "";
                    string asn = "AS" + luje_rdr["asn"].ToString();
                    if (entry.peering_ips_type == "4")
                    {
                        addr = luje_rdr["ipaddr4"].ToString();
                    }
                    else if (entry.peering_ips_type == "6")
                    {
                        addr = luje_rdr["ipaddr6"].ToString();
                    }
                    stringbBuilder.Append("- ");
                    stringbBuilder.Append(asn);
                    stringbBuilder.Append(" ");
                    stringbBuilder.Append(luje_rdr["name"].ToString());
                    stringbBuilder.Append(" ");
                    stringbBuilder.Append(luje_rdr["ix_name"].ToString());
                    stringbBuilder.Append(" ");
                    stringbBuilder.Append(addr);
                    stringbBuilder.AppendLine("");

                    var peer = new PeerModel { Id = Convert.ToInt32(entry.peering_ips_id), PeeringID = Convert.ToInt32(entry.peering_ips_peering_id) };
					remove = true;
				}
				luje_rdr.Close();
			}
            stringbBuilder.AppendLine("");
            stringbBuilder.AppendLine("Regards,");
            stringbBuilder.AppendLine("LUJE.net Peering Portal");

            foreach (PeerModel peer in peers)
			{
				notify = true;
                luje_cmd = new NpgsqlCommand("update peering_ips set peering_ips_rejected = true, peering_ips_modified = NOW() where peering_ips_id = @id and peering_ips_deleted = false;", luje_conn);
				luje_cmd.Parameters.AddWithValue("@id", NpgsqlDbType.Integer, peer.Id);
				luje_cmd.Prepare();
				luje_cmd.ExecuteNonQuery();

				luje_cmd = new NpgsqlCommand("select peering.peering_id from peering_ips inner join peering on peering.peering_id = peering_ips.peering_ips_peering_id and peering.peering_deleted = false where peering_ips_deleted = false and peering_ips_rejected = false and peering.peering_id = @id;", luje_conn);
				luje_cmd.Parameters.AddWithValue("@id", NpgsqlDbType.Integer, peer.PeeringID);
				luje_cmd.Prepare();
				luje_rdr = luje_cmd.ExecuteReader();

				if (!luje_rdr.Read())
				{
					luje_rdr.Close();
					luje_cmd = new NpgsqlCommand("update peering set peering_deleted = true, peering_modified = NOW() where peering_id = @id and peering_deleted = false;", luje_conn);
					luje_cmd.Parameters.AddWithValue("@id", NpgsqlDbType.Integer, peer.PeeringID);
					luje_cmd.Prepare();
					luje_cmd.ExecuteNonQuery();
				}
				else
				{
					luje_rdr.Close();
				}
            }

            if (notify)
            {
                try
                {
                    MailMessage mail = new MailMessage();
                    SmtpClient SmtpServer = new SmtpClient(Configuration.MailServer, Configuration.MailPort);

                    mail.From = new MailAddress(Configuration.MailFrom);
                    mail.To.Add(Configuration.MailFrom);
                    mail.Subject = "Peering sessions removal";
                    mail.Body = stringbBuilder.ToString();
					mail.Headers.Add("Message-Id", String.Format("<{0}@{1}>", Guid.NewGuid().ToString(), Configuration.MailFrom.Split("@")[1]));

					SmtpServer.Credentials = new System.Net.NetworkCredential(Configuration.MailUser, Configuration.MailPassword);
					if (Configuration.MailTls == "True")
					{
						SmtpServer.EnableSsl = true;
					}

					SmtpServer.Send(mail);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error during sending of mail");
                }
            }
        }
	}
}