using System;
using System.Collections.Generic;
using System.Text;

namespace Robot.Core.Common
{
	public class Log
	{

		public interface IMarkProvider
		{
			string GetMark();
		}

		public enum Level
		{
			Trace = 0,
			Debug = 1,
			Info = 2,
			Warning = 3,
			Error = 4,
			Fatal = 5
		}

		public static readonly Log Instance;

		static Log()
		{
			Instance = new Log("unnamed-logger");
		}

		private List<IMarkProvider> markProviders = new List<IMarkProvider>();

		public string Name { get; }

		public Log(string name)
		{
			if (name == null) {
				throw new ArgumentNullException(nameof(name));
			}

			Name = name;
		}

#region Shortcuts

		public void Debug(string message, params object[] param)
		{
			Write(Level.Debug, message, param);
		}

		public void Info(string message, params object[] param)
		{
			Write(Level.Info, message, param);
		}

		public void Warning(string message, params object[] param)
		{
			Write(Level.Warning, message, param);
		}

		public void Error(string message, params object[] param)
		{
			Write(Level.Error, message, param);
		}

		public void Error(string message, Exception exception)
		{
			var exp = exception?.Message ?? "null exception";
			Write(Level.Error, "{0} :: {1}", message, exp);
		}

		public void Fatal(string message, params object[] param)
		{
			Write(Level.Fatal, message, param);
		}

		public void Fatal(string message, Exception exception)
		{
			var exp = exception?.Message ?? "null exception";
			Write(Level.Fatal, "{0} :: {1}", message, exp);
		}

#endregion

		public void Write(Level level, string message, params object[] param)
		{
			Write(level, string.Format(message, param));
		}

		public void Write(Level level, string message)
		{
			var marks = new StringBuilder(DateTime.Now.ToString("HH':'mm':'ss'.'fff"));
			foreach (var provider in markProviders) {
				marks.Append("|").Append(provider.GetMark());
			}

			System.Diagnostics.Debug.WriteLine(
				"[{0}|{1}] {2}: {3}",
				marks, GetLevelString(level), Name, message
			);
			Console.WriteLine(
				"[{0}|{1}] {2}: {3}",
				marks, GetLevelString(level), Name, message
			);
		}

		public void RegProvider(IMarkProvider provider)
		{
			markProviders.Add(provider);
		}

		private string GetLevelString(Level level)
		{
			switch (level) {
				case Level.Debug:
					return "dbg";
				case Level.Error:
					return "err";
				case Level.Fatal:
					return "ftl";
				case Level.Info:
					return "inf";
				case Level.Trace:
					return "trc";
				case Level.Warning:
					return "wrn";
				default:
					return level.ToString();
			}
		}
	}
}
