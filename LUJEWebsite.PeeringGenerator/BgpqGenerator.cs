using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LUJEWebsite.Library.Utils;

namespace LUJEWebsite.PeeringGenerator
{
    internal class BgpqGenerator
    {
		private readonly string asnumber;
        private readonly string peering_peeringdb_id;
		private readonly string as_set;
        private readonly string irr_order;
        private readonly string irr_source_host;
        private readonly MySqlConnection conn;

        public BgpqGenerator(MySqlConnection Conn, string asnumber, string as_set, string irr_order, string irr_source_host)
        {
			this.asnumber = asnumber;
			this.as_set = as_set;
            this.irr_order = irr_order;
            this.irr_source_host = irr_source_host;
			this.conn = Conn;
		}

		private void RunBgpq4(string filename, int v, string as_set, string vendor, string[] flags, string subterm)
        {
            string stanza_name = $"AUTOFILTER_AS{asnumber}_IPv{v}{subterm}";

            var argumentsList = new List<string>
            {
                "-h", irr_source_host,
                "-S", irr_order,
                "-A",
                "-l", stanza_name
            };

            if (v == 6)
            {
                argumentsList.Add("-6");
            }
            else if (v == 4)
            {
                argumentsList.Add("-4");
            }

            if (!string.IsNullOrEmpty(vendor))
            {
                argumentsList.Add($"-{vendor}");
            }

            if (flags != null)
            {
                argumentsList.AddRange(flags);
            }

            argumentsList.Add(as_set);

            using (var writer = new StreamWriter(filename))
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "bgpq4",
                    Arguments = string.Join(" ", argumentsList),
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };

				using (var process = Process.Start(processStartInfo))
                {
                    writer.Write(process.StandardOutput.ReadToEnd());
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        Console.WriteLine($"ERROR: bgpq4 returned non-zero for existing filename {filename}: {process.ExitCode}");
                    }
                    else
                    {
                        Console.WriteLine($"bgpq4 IPv{v} filters created: {filename}");
                    }
				}
			}
		}

        private void parseSaveFilter(string filename, int v)
        {
			var newPrefixes = ParseIrrPrefixes(filename);
			Console.WriteLine("prefix list");
			foreach (var prefix in newPrefixes)
			{
				Console.WriteLine(prefix);
			}

			// Get existing prefixes from the database
			var existingPrefixes = new List<string>();
			var prefixIdMap = new Dictionary<string, int>();

			using (var command = new MySqlCommand("SELECT peering_acl_id, peering_acl_prefix FROM peering_acl WHERE peering_acl_peeringdb_id = @PeeringdbId AND peering_acl_asn = @Asn AND peering_acl_afi = @Afi AND peering_acl_deleted = false", conn))
			{
				command.Parameters.AddWithValue("@PeeringdbId", peering_peeringdb_id);
				command.Parameters.AddWithValue("@Asn", asnumber);
				command.Parameters.AddWithValue("@Afi", v);

				Console.WriteLine($"SELECT peering_acl_id, peering_acl_prefix FROM peering_acl WHERE peering_acl_peeringdb_id = {peering_peeringdb_id} AND peering_acl_asn = {asnumber} AND peering_acl_afi = {v} AND peering_acl_deleted = false");

				using (var reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						var prefix = reader["peering_acl_prefix"].ToString();
						var id = Convert.ToInt32(reader["peering_acl_id"]);
						existingPrefixes.Add(prefix);
						prefixIdMap[prefix] = id;
					}
				}
			}
			Console.WriteLine("prefix list existing");
			foreach (var prefix in existingPrefixes)
			{
				Console.WriteLine(prefix);
			}

			// Determine prefixes to add and delete
			var prefixesToAdd = newPrefixes.Except(existingPrefixes).ToList();
			var prefixesToDelete = existingPrefixes.Except(newPrefixes).ToList();
			Console.WriteLine("prefix list to add");
			foreach (var prefix in prefixesToAdd)
			{
				Console.WriteLine(prefix);
			}
			Console.WriteLine("prefix list to delete");
			foreach (var prefix in prefixesToDelete)
			{
				Console.WriteLine(prefix);
			}

			using (var transaction = conn.BeginTransaction())
			{
				// Soft delete prefixes no longer in the file
				if (prefixesToDelete.Count > 0)
				{
					using (var deleteCommand = new MySqlCommand("UPDATE peering_acl SET peering_acl_deleted = 1, peering_acl_modified = NOW() WHERE peering_acl_id = @Id AND peering_acl_afi = @Afi AND peering_acl_deleted = false", conn, transaction))
					{
						deleteCommand.Parameters.Add("@Id");
						deleteCommand.Parameters.AddWithValue("@Afi", v);

						foreach (var prefix in prefixesToDelete)
						{
							deleteCommand.Parameters["@Id"].Value = prefixIdMap[prefix];
							deleteCommand.ExecuteNonQuery();
						}
					}
				}

				// Add new prefixes from the file
				if (prefixesToAdd.Count > 0)
				{
					using (var insertCommand = new MySqlCommand("INSERT INTO peering_acl (peering_acl_peeringdb_id, peering_acl_asn, peering_acl_afi, peering_acl_prefix, peering_acl_created, peering_acl_modified, peering_acl_deleted) VALUES (@PeeringdbId, @Asn, @Afi, @Prefix, NOW(), NOW(), false)", conn, transaction))
					{
						insertCommand.Parameters.AddWithValue("@PeeringdbId", Convert.ToInt32(peering_peeringdb_id));
						insertCommand.Parameters.AddWithValue("@Asn", asnumber);
						insertCommand.Parameters.Add("@Prefix");
                        insertCommand.Parameters.AddWithValue("@Afi", v);

						foreach (var prefix in prefixesToAdd)
						{
							insertCommand.Parameters["@Prefix"].Value = prefix;
							insertCommand.ExecuteNonQuery();
						}
					}
				}

				transaction.Commit();
			}
		}

		private static List<string> ParseIrrPrefixes(string filePath)
		{
			var prefixes = new List<string>();
			var lines = File.ReadAllLines(filePath);

			foreach (var line in lines)
			{
				var trimmedLine = line.Trim();
				if (IsIpAddressOrPrefix(trimmedLine))
				{
					prefixes.Add(trimmedLine);
				}
			}

			return prefixes;
		}

		private static bool IsIpAddressOrPrefix(string line)
		{
			if (string.IsNullOrWhiteSpace(line))
			{
				return false;
			}

			var parts = line.Split('/');
			if (parts.Length > 2)
			{
				return false;
			}

			try
			{
				IPAddress.Parse(parts[0]);
				if (parts.Length == 2 && (!int.TryParse(parts[1], out int prefixLength) || prefixLength < 0 || prefixLength > (parts[0].Contains(":") ? 128 : 32)))
				{
					return false;
				}

				return true;
			}
			catch
			{
				return false;
			}
		}

		public void GenerateFilters()
		{
			if (as_set == "ANY")
			{
				return;
			}
			
			// BIRD IPv4 and IPv6
			foreach (var v in new[] { 4, 6 })
            {
                string filename = $"{Configuration.RoutefiltersLocation}/AS{asnumber}.prefixset.bird.ipv{v}";

                if (File.Exists(filename))
                {
                    if (DateTime.Now - File.GetLastWriteTime(filename) > TimeSpan.FromMinutes(5))
					{
						RunBgpq4(filename, v, as_set, "b", null, "");
						//parseSaveFilter(filename, v);
						Console.WriteLine($"bird ipv{v} refreshed: {filename}");
                    }
                    else
                    {
                        Console.WriteLine($"bird ipv{v} cached: {filename}");
                    }
                }
                else
                {
                    RunBgpq4(filename, v, as_set, "b", null, "");
					//parseSaveFilter(filename, v);
					Console.WriteLine($"bird ipv{v} created: {filename}");
                }
            }
        }
    }
}
