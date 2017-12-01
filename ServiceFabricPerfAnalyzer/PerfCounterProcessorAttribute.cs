using System;

namespace ServiceFabricPerfAnalyzer
{
	public class PerfCounterProcessorAttribute : Attribute
	{
		public string Category { get; }

		public PerfCounterProcessorAttribute(string category)
		{
			Category = category;
		}
	}
}
