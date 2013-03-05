using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

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
					Console.WriteLine("[{0}.Info  {1}]  {2}", threadName, time.ToString("HH:mm:ss"), content);
					break;
				case LogLevel.Debug:
					Console.ForegroundColor = ConsoleColor.Gray;
					Console.WriteLine("[{0}.Debug {1}]  {2}", threadName, time.ToString("HH:mm:ss"), content);
					break;
				case LogLevel.Warning:
					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.WriteLine("[{0}.Warn  {1}]  {2}", threadName, time.ToString("HH:mm:ss"), content);
					break;
				case LogLevel.Error:
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("[{0}.Error {1}]  {2}", threadName, time.ToString("HH:mm:ss"), content);
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

	static void CreateThread(int id)
	{
		var thread = new Thread(s =>
		{
			Thread.CurrentThread.Name = "Worker" + id;
			Log.Info("start");
			for(int i = 0; i < 5; ++i)
			{
				Log.Debug("do something...");
				Thread.Sleep(new Random().Next(100));
				Log.Append("ok");
			}
			Log.Info("end.");
			Log.Info("");
		});
		thread.Start();
	}

	/// <summary>
	/// test the muti-thread log
	/// </summary>
	static void Test4()
	{
		Thread.CurrentThread.Name = "Main";
		for(int i = 0; i < 10; ++i)
		{
			Log.Debug("start thread {0}...", i);
			Thread.Sleep(10);
			CreateThread(i);
			Log.Append("ok");
		}
		Thread.Sleep(500);
		Log.Info("finished.");
		Log.Flush();
	}

	static void Main(string[] args)
	{
		Test1();
		Test2();
		Test3();
		Test4();
	}
}
