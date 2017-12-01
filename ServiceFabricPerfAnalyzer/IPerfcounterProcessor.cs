using System.Diagnostics;

namespace ServiceFabricPerfAnalyzer
{
	public interface IPerfcounterProcessor
	{
		string Category { get; set; }
		string Counter { get; set; }

		float Calculate(CounterSample last, CounterSample sample);
		string GetInstanceName(string instance);
	}
}