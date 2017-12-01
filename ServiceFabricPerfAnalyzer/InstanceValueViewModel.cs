using ObservableDatabinding;
using System.Diagnostics;

namespace ServiceFabricPerfAnalyzer
{
	public class InstanceValueViewModel
	{
		public XamlSubject<string> Name { get; set; } = new XamlSubject<string>();
		public XamlSubject<float> Value { get; set; } = new XamlSubject<float>();
		public IPerfcounterProcessor Processor { get; }

		public CounterSample lastSample = CounterSample.Empty;

		public InstanceValueViewModel(string name, IPerfcounterProcessor processor)
		{
			Name.Value = processor.GetInstanceName(name);
			Processor = processor;
		}

		internal void Update(InstanceData i)
		{
			Value.Value = Processor.Calculate(lastSample, i.Sample);
			lastSample = i.Sample;
		}
	}
}
