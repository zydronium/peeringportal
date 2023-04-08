namespace LUJEWebsite.Mailer
{
	internal class Program
	{
		static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				//main engine cycle
				Console.WriteLine("Running a mailing batch");
				Mailer.run();
				Console.WriteLine("Finished");
			}
			else
			{
				//docker container goes into this state by default, just to keep docker-compose happy
				//and don't make the ticks go haywire with an update
				while (true)
				{
					Console.WriteLine("Start sleep");
					int timer = 1000 * 60 * 60;
					Thread.Sleep(timer);
					Console.WriteLine("Cycle");
				}
			}
		}
	}
}