namespace LUJEWebsite.Mailer
{
	internal class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Running a cleaning batch");
			Cleaner.Run();
			Console.WriteLine("Running a mailing batch");
			Mailer.Run();
			Console.WriteLine("Finished");
		}
	}
}