using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LUJEWebsite.Library.Utils
{
	public class Utils
	{
		public static string GetNeighborName(string peer)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(peer);
			using (SHA256 sha256 = SHA256.Create())
			{
				byte[] hashBytes = sha256.ComputeHash(bytes);
				string hashHex = BitConverter.ToString(hashBytes).Replace("-", ""); // Convert hash bytes to hexadecimal string
				BigInteger hashInt = BigInteger.Parse(hashHex, System.Globalization.NumberStyles.HexNumber); // Convert hexadecimal string to integer
				string base36 = ConvertToBase36(hashInt); // Convert integer to base 36 string
				string neighborName = base36.Substring(0, Math.Min(6, base36.Length)); // Take first 6 characters
				return neighborName;
			}
		}

		public static string ConvertToBase36(BigInteger value)
		{
			const string chars = "0123456789abcdefghijklmnopqrstuvwxyz";
			string result = string.Empty;

			value = BigInteger.Abs(value); // Take the absolute value

			do
			{
				result = chars[(int)(value % 36)] + result;
				value /= 36;
			} while (value != 0);

			return result;
		}

		public static string GenerateRandomString(string input, int length)
		{
			// Ensure length is positive
			if (length <= 0)
				throw new ArgumentException("Length must be a positive integer.", nameof(length));

			// Use input string to seed the random number generator
			Random rand = new Random(input.GetHashCode());

			// Characters to use for generating the random string
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

			// Generate the random string
			char[] randomString = new char[length];
			for (int i = 0; i < length; i++)
			{
				randomString[i] = chars[rand.Next(chars.Length)];
			}

			return new string(randomString);
		}

		public static bool IsInSubnet(IPAddress ipAddress, string cidrSubnet)
		{
			var parts = cidrSubnet.Split('/');
			var subnetAddress = IPAddress.Parse(parts[0]);
			var prefixLength = int.Parse(parts[1]);

			byte[] ipBytes = ipAddress.GetAddressBytes();
			byte[] subnetBytes = subnetAddress.GetAddressBytes();

			if (ipBytes.Length != subnetBytes.Length)
			{
				throw new ArgumentException("IP address and subnet address lengths do not match.");
			}

			if (ipAddress.AddressFamily != subnetAddress.AddressFamily)
			{
				throw new ArgumentException("IP address and subnet address have different address families.");
			}

			byte[] maskBytes = PrefixLengthToSubnetMask(prefixLength, ipAddress.AddressFamily);

			byte[] maskedIpBytes = new byte[ipBytes.Length];
			for (int i = 0; i < ipBytes.Length; i++)
			{
				maskedIpBytes[i] = (byte)(ipBytes[i] & maskBytes[i]);
			}

			for (int i = 0; i < ipBytes.Length; i++)
			{
				if (maskedIpBytes[i] != subnetBytes[i])
				{
					return false;
				}
			}

			return true;
		}

		public static byte[] PrefixLengthToSubnetMask(int prefixLength, AddressFamily addressFamily)
		{
			int maskLength = addressFamily == AddressFamily.InterNetwork ? 32 : 128;
			byte[] maskBytes = new byte[maskLength / 8];

			for (int i = 0; i < maskBytes.Length; i++)
			{
				int byteValue = Math.Min(prefixLength, 8);
				maskBytes[i] = (byte)(0xFF << (8 - byteValue));
				prefixLength -= byteValue;
			}

			return maskBytes;
		}

		public async static Task<string> GetBirdProtocolStatus(string hostname, string neighboorName)
		{
			string url = $"http://{hostname}:8000/bird?q=show protocols {neighboorName}";
			using (HttpClient client = new HttpClient())
			{
				HttpResponseMessage response = await client.GetAsync(url);
				response.EnsureSuccessStatusCode();
				string responseBody = await response.Content.ReadAsStringAsync();
				return responseBody;
			}
		}

		public static string GetInfoValue(string birdlgOutput)
		{
			// Regular expression to match the time or date and capture the subsequent text
			string pattern = @"\b(?:\d{2}:\d{2}:\d{2}\.\d{3}|\d{4}-\d{2}-\d{2})\s+(.+)$";
			var match = Regex.Match(birdlgOutput, pattern, RegexOptions.Multiline);

			if (match.Success)
			{
				// Return the captured group containing the text after the time or date
				return match.Groups[1].Value.Trim();
			}

			return string.Empty; // Return empty string if no match found
		}

		public static bool AreSameAddressFamily(string ip1, string ip2)
		{
			if (string.IsNullOrWhiteSpace(ip1) || string.IsNullOrWhiteSpace(ip2))
			{
				throw new ArgumentException("IP addresses cannot be null, empty, or whitespace.");
			}

			if (!IPAddress.TryParse(ip1, out var address1))
			{
				throw new ArgumentException($"Invalid IP address: {ip1}");
			}

			if (!IPAddress.TryParse(ip2, out var address2))
			{
				throw new ArgumentException($"Invalid IP address: {ip2}");
			}

			return address1.AddressFamily == address2.AddressFamily;
		}

		public static int GetAddressFamily(string ip)
		{
			if (string.IsNullOrWhiteSpace(ip))
			{
				throw new ArgumentException("IP address cannot be null, empty, or whitespace.");
			}

			if (!IPAddress.TryParse(ip, out var address))
			{
				throw new ArgumentException($"Invalid IP address: {ip}");
			}

			return address.AddressFamily switch
			{
				System.Net.Sockets.AddressFamily.InterNetwork => 4,  // IPv4
				System.Net.Sockets.AddressFamily.InterNetworkV6 => 6, // IPv6
				_ => -1 // Unknown/unsupported address family
			};
		}

		public static int GetAddressFamilyFromPrefix(string prefix)
		{
			if (string.IsNullOrWhiteSpace(prefix))
			{
				throw new ArgumentException("Prefix cannot be null, empty, or whitespace.");
			}

			// Split the prefix into IP address and subnet length parts
			var parts = prefix.Split('/');
			if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var ipAddress))
			{
				throw new ArgumentException($"Invalid IP prefix: {prefix}");
			}

			// Determine the address family based on the parsed IP address
			return ipAddress.AddressFamily switch
			{
				System.Net.Sockets.AddressFamily.InterNetwork => 4,  // IPv4
				System.Net.Sockets.AddressFamily.InterNetworkV6 => 6, // IPv6
				_ => -1 // Unknown/unsupported address family
			};
		}
	}
}
