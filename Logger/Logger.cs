using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;

/// <summary>
/// Log Level
/// </summary>
public enum LogLevel
{
	Info,
	Debug,
	Warning,
	Error,
	Fatal
};

/// <summary>
/// Logger module
/// </summary>
public static class Log
{
#if !_MONO
	[DllImport("Kernel32.dll")]
	static extern bool AllocConsole();

	[DllImport("Kernel32.dll")]
	static extern bool FreeConsole();
#endif
	/// <summary>
	/// Log event callback
	/// </summary>
	/// <param name="threadName">the thread name of the log issue</param>
	/// <param name="level">log level</param>
	/// <param name="content"></param>
	/// <returns>ture: write to log, false: don't log</returns>
	public delegate bool OnLog(string threadName, LogLevel level, DateTime time, string content);

	class LogContext
	{
		public LogLevel level;
		public string content;
		public DateTime time;
		public Thread thread;
		public volatile bool loging = false;
	}

	#region Implement
	static string logFileName = "output.log";

	static List<LogContext> _contexts = new List<LogContext>();
	static LocalDataStoreSlot ContextSlot = Thread.AllocateDataSlot();

	static bool[] _logEnable = { true, true, true, true, true };
	delegate void WriteLog(LogLevel level, string content);

	static void WriteLogConsole(LogLevel level, string content)
	{
		var head = "";
		switch(level)
		{
			case LogLevel.Info: head = "[Info] "; break;
			case LogLevel.Debug: head = "[Debug] "; break;
			case LogLevel.Error: head = "[Error] "; break;
			case LogLevel.Warning: head = "[Warning] "; break;
			case LogLevel.Fatal: head = "[Fatal] "; break;
		}
		Console.Write(head + content + "\n");
	}
	static void WriteLogFile(LogLevel level, string content)
	{
		var head = "";
		switch(level)
		{
			case LogLevel.Info: head = "[Info] "; break;
			case LogLevel.Debug: head = "[Debug] "; break;
			case LogLevel.Error: head = "[Error] "; break;
			case LogLevel.Warning: head = "[Warning] "; break;
			case LogLevel.Fatal: head = "[Fatal] "; break;
		}

		using(var file = File.AppendText(logFileName))
		{
			file.Write(head + content + "\n");
			file.Close();
		}
	}
	static WriteLog WriteLogFunc = WriteLogConsole;

	static void Write(LogContext context, LogLevel level, string content)
	{
		lock(_contexts)
		{
			//log can't re-entry
			if(context.loging)
			{
				WriteLogFunc(LogLevel.Error, "Log re-entry!");
				return;
			}
			context.loging = true;
			if(OnLogEvent != null && !OnLogEvent(context.thread.Name, context.level, context.time, context.content))
			{
				context.level = level;
				context.content = content;
				context.time = DateTime.Now;
				context.loging = false;
				return;
			}
			if(context.content != null)
				WriteLogFunc(context.level, context.content);

			context.content = content;
			context.level = level;
			context.time = DateTime.Now;
			context.loging = false;
		}
	}

	static void Write(LogLevel level, string content)
	{
		LogContext context = (LogContext)Thread.GetData(ContextSlot);
		if(context == null)
		{
			context = new LogContext();
			Thread.SetData(ContextSlot, context);
			context.thread = Thread.CurrentThread;
			lock(_contexts)
			{
				_contexts.Add(context);
			}
		}
		Write(context, level, content);
	}

	static void WriteAppend(string content)
	{
		LogContext context = (LogContext)Thread.GetData(ContextSlot);
		if(context == null)
			return;
		if(context.content == null)
			context.content = content;
		else
			context.content += content;
	}

	#endregion

	#region Method

	/// <summary>
	/// format the object with JSON string
	/// </summary>
	/// <param name="obj"></param>
	/// <returns></returns>
	public static string ObjToString(object obj)
	{
		if(obj == null)
			return "null";
		Type type = obj.GetType();

		if(type == typeof(bool))
		{
			return ((bool)obj) ? "true" : "false";
		}
		else if(type.IsEnum)
		{
			return "\"" + obj.ToString() + "\"";
		}
		else if(type.IsPrimitive)
		{
			return obj.ToString();
		}
		else if(type == typeof(string))
		{
			return "\"" + (string)obj + "\"";
		}
		else if(type == typeof(Guid))
		{
			return obj.ToString();
		}
		else if(type == typeof(DateTime))
		{
			return obj.ToString();
		}
		else
		{
			var sb = new StringBuilder();
			if(type.IsArray)
			{
				sb.Append("[");
				Array array = (Array)obj;
				for(int i = 0; i < array.Length; ++i)
				{
					sb.Append(ObjToString(array.GetValue(i)));
					if(i != array.Length - 1)
						sb.Append(", ");
				}
				sb.Append("]");
			}
			else if(type.IsValueType || type.IsClass)
			{
				sb.Append("{");
				FieldInfo[] fields = type.GetFields();
				for(int i = 0; i < fields.Length; ++i)
				{
					sb.Append(fields[i].Name);
					sb.Append(": ");
					sb.Append(ObjToString(fields[i].GetValue(obj)));
					if(i != fields.Length - 1)
						sb.Append(", ");
				}
				sb.Append("}");
			}
			return sb.ToString();
		}
	}

	/// <summary>
	/// Log the Info message
	/// </summary>
	/// <param name="format"></param>
	/// <param name="args"></param>
	public static void Info(string format, params object[] args)
	{
		if(!_logEnable[(int)LogLevel.Info])
			Write(LogLevel.Info, null);
		else
			Write(LogLevel.Info, string.Format(format, args));
	}

	/// <summary>
	/// Log the Info message
	/// </summary>
	/// <param name="content"></param>
	public static void Info(string content)
	{
		if(!_logEnable[(int)LogLevel.Info])
			Write(LogLevel.Info, null);
		else
			Write(LogLevel.Info, content);
	}

	/// <summary>
	/// Log the Debug message
	/// </summary>
	/// <param name="format"></param>
	/// <param name="args"></param>
	public static void Debug(string format, params object[] args)
	{
		if(!_logEnable[(int)LogLevel.Debug])
			Write(LogLevel.Debug, null);
		else
			Write(LogLevel.Debug, string.Format(format, args));
	}

	/// <summary>
	/// Log the Debug message
	/// </summary>
	/// <param name="content"></param>
	public static void Debug(string content)
	{
		if(!_logEnable[(int)LogLevel.Debug])
			Write(LogLevel.Debug, null);
		else
			Write(LogLevel.Debug, content);
	}

	/// <summary>
	/// Log the error message
	/// </summary>
	/// <param name="format"></param>
	/// <param name="args"></param>
	public static void Error(string format, params object[] args)
	{
		if(!_logEnable[(int)LogLevel.Error])
			Write(LogLevel.Error, null);
		else
			Write(LogLevel.Error, string.Format(format, args));
	}

	/// <summary>
	/// Log the error message
	/// </summary>
	/// <param name="content"></param>
	public static void Error(string content)
	{
		if(!_logEnable[(int)LogLevel.Error])
			Write(LogLevel.Error, null);
		else
			Write(LogLevel.Error, content);
	}

	/// <summary>
	/// log the warning message
	/// </summary>
	/// <param name="format"></param>
	/// <param name="args"></param>
	public static void Warning(string format, params object[] args)
	{
		if(!_logEnable[(int)LogLevel.Warning])
			Write(LogLevel.Warning, null);
		else
			Write(LogLevel.Warning, string.Format(format, args));
	}

	/// <summary>
	/// log the warning message
	/// </summary>
	/// <param name="content"></param>
	public static void Warning(string content)
	{
		if(!_logEnable[(int)LogLevel.Warning])
			Write(LogLevel.Warning, null);
		else
			Write(LogLevel.Warning, content);
	}
	/// <summary>
	/// log the fatal message
	/// </summary>
	/// <param name="format"></param>
	/// <param name="args"></param>
	public static void Fatal(string format, params object[] args)
	{
		if(!_logEnable[(int)LogLevel.Fatal])
			Write(LogLevel.Fatal, null);
		else
			Write(LogLevel.Fatal, string.Format(format, args));
	}

	/// <summary>
	/// log the fatal message
	/// </summary>
	/// <param name="content"></param>
	public static void Fatal(string content)
	{
		if(!_logEnable[(int)LogLevel.Fatal])
			Write(LogLevel.Fatal, null);
		else
			Write(LogLevel.Fatal, content);
	}

	/// <summary>
	/// Append the message to the last log message
	/// </summary>
	/// <param name="format"></param>
	/// <param name="args"></param>
	public static void Append(string format, params object[] args)
	{
		LogContext context = (LogContext)Thread.GetData(ContextSlot);
		if(context == null)
			return;
		if(!_logEnable[(int)context.level])
			return;
		WriteAppend(string.Format(format, args));
	}

	/// <summary>
	/// Append the message to the last log message
	/// </summary>
	/// <param name="content">the content</param>
	public static void Append(string content)
	{
		LogContext context = (LogContext)Thread.GetData(ContextSlot);
		if(context == null)
			return;
		if(!_logEnable[(int)context.level])
			return;
		WriteAppend(content);
	}

	/// <summary>
	/// Flush the log to the output
	/// </summary>
	public static void Flush()
	{
		lock(_contexts)
		{
			foreach(var context in _contexts)
			{
				Write(context, context.level, null);
			}
		}
		if(OnFlush != null)
			OnFlush();
	}
	/// <summary>
	/// Enable the specified level of log
	/// </summary>
	/// <param name="log"></param>
	/// <param name="enable"></param>
	public static void LogEnable(LogLevel log, bool enable)
	{
		_logEnable[(int)log] = enable;
	}

	/// <summary>
	/// redirecte the log to file
	/// </summary>
	/// <param name="fileName"></param>
	public static void LogToFile(string fileName)
	{
		logFileName = fileName;
		WriteLogFunc = WriteLogFile;
	}
	/// <summary>
	/// redirecte the log to file
	/// </summary>
	public static void LogToFile()
	{
		WriteLogFunc = WriteLogFile;
	}
	/// <summary>
	/// redirecte the log to console window
	/// </summary>
	public static void LogToConsole()
	{
		WriteLogFunc = WriteLogConsole;
	}

#if !_MONO
	/// <summary>
	/// open the console window
	/// </summary>
	/// <remarks>
	/// if the application is not a console project, you need call this to show the console.
	/// </remarks>
	public static void OpenConsole()
	{
		AllocConsole();
	}

	/// <summary>
	/// close the console window
	/// </summary>
	public static void CloseConsole()
	{
		FreeConsole();
	}
#endif
	/// <summary>
	/// the log event filter
	/// </summary>
	public static event OnLog OnLogEvent;
	/// <summary>
	/// the flush event
	/// </summary>
	public static event Action OnFlush;
	#endregion
};
