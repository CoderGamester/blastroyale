using System;
using System.Globalization;
using System.IO;
using System.Linq;
using FirstLight.Game.Utils;
using UnityEngine;

namespace FirstLight.Game.TestCases
{
	/// <summary>Writes device benchmarks and test results to a local CSV artifact.</summary>
	public sealed class TestResultWriter
	{
		private readonly string _path = Path.Combine(Application.persistentDataPath, "play-test-results.csv");

		public void WriteHeaders()
		{
			File.WriteAllText(_path, "timestamp,event,result,message,location\n");
		}

		public void AppendGeneralInfo(string testCase) => Write("test", "", testCase + "; build=" + VersionUtils.BuildNumber, "");

		public void AppendEvent(string eventName, string location) => Write("event", "", eventName, location);

		public void AppendResult(bool succeeded, string message) => Write("result", succeeded.ToString(), message, "");

		public void AppendException(string message) => Write("exception", "false", message, "");

		public void AppendBenchmark(BenchmarkCollector.TimeFrameCollected benchmark, string location)
		{
			if (benchmark.fps.Count == 0 || benchmark.memory.Count == 0) return;
			var message = string.Format(CultureInfo.InvariantCulture,
				"minFps={0}; avgFps={1:F1}; allocatedMiB={2}; reservedMiB={3}; monoUsedMiB={4}; monoHeapMiB={5}",
				benchmark.fps.Min(), benchmark.fps.Average(),
				benchmark.memory.Max(m => m.AllocatedUsed), benchmark.memory.Max(m => m.AllocatedTotal),
				benchmark.memory.Max(m => m.MonoUsed), benchmark.memory.Max(m => m.MonoTotal));
			Write("benchmark", "", message, location);
		}

		public void EarlyQuitApp()
		{
			if (!Application.isEditor) Application.Quit(1);
		}

		private void Write(string eventName, string result, string message, string location)
		{
			File.AppendAllText(_path, string.Join(",", new[]
			{
				Escape(DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)), Escape(eventName),
				Escape(result), Escape(message), Escape(location)
			}) + "\n");
		}

		private static string Escape(string value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
	}
}
