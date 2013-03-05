using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

class Program
{
	/// <summary>
	/// test the default log implementation
	/// </summary>
	static void Test1()
	{
		Log.Info("test1");
		Log.Info("log the message with default implementation.");
		
		var count = 10;
		Log.Debug("test append. count={0}\n", count);
		for(var i = 0; i < count; ++i)
			Log.Append("	test: {0}\n", i);

		Log.Error("test error.");
		Log.Flush();
	}

	/// <summary>
	/// test log file
	/// </summary>
	static void Test2()
	{
		Log.Info("test 2");
		Log.Info("log the message to file.");
		Log.Flush();

		Log.LogToFile("test.log");
		Log.Info("hello world.");
		Log.Flush();

		Log.LogToConsole();
		Log.Info("print 'test.log': {0}", File.ReadAllText("test.log"));
		File.Delete("test.log");
		Log.Flush();
	}

	/// <summary>
	/// customize the log output
	/// </summary>
	static void Test3()
	{
		Log.OnLogEvent += (threadName, level, time, content) =>
		{
			if(content == null)
				return false;
			switch(level)
			{
				case LogLevel.Info:
					Console.ForegroundColor = ConsoleColor.White;
					Console.WriteLine("[Info  {0}]  {1}", time.ToString("HH:mm:ss"), content);
					break;
				case LogLevel.Debug:
					Console.ForegroundColor = ConsoleColor.Gray;
					Console.WriteLine("[Debug {0}]  {1}", time.ToString("HH:mm:ss"), content);
					break;
				case LogLevel.Warning:
					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.WriteLine("[Warn  {0}]  {1}", time.ToString("HH:mm:ss"), content);
					break;
				case LogLevel.Error:
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("[Error {0}]  {1}", time.ToString("HH:mm:ss"), content);
					break;
			}
			return false;
		};

		Log.Info("some info...");
		Log.Warning("a warning...");
		Log.Error("an error...");
		Log.Append("now it's ok.");
		Log.Debug("some debug stuff...");
		Log.Flush();
	}
	

	static void Main(string[] args)
	{
		Test1();
		Test2();
		Test3();
	}
}
