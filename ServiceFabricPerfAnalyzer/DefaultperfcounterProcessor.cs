using System.Diagnostics;

namespace ServiceFabricPerfAnalyzer
{
	public class DefaultperfcounterProcessor : IPerfcounterProcessor
	{
		public string Category { get; set; }
		public string Counter { get; set; }

		public string GetInstanceName(string instance)
		{
			return instance;
		}

		public float Calculate(CounterSample last, CounterSample sample)
		{
			return CounterSample.Calculate(last, last);
		}
	}
}
