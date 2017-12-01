using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Fabric.Query;
using System.Linq;
using System.Reactive.Linq;

namespace ServiceFabricPerfAnalyzer
{
	[PerfCounterProcessor("Fabric")]
	public class SFperfcounterProcessor : IPerfcounterProcessor
	{
		public string Category { get; set; }
		public string Counter { get; set; }

		static Lazy<Dictionary<String, (Application app, Service service, Partition partition)>> clusterInfo = new Lazy<Dictionary<String, (Application app, Service service, Partition partition)>>(() =>
		{
			FabricClient client = new FabricClient();
			var partitions = from app in client.QueryManager.GetApplicationListAsync().Result
							 from service in client.QueryManager.GetServiceListAsync(app.ApplicationName).Result
							 from partition in client.QueryManager.GetPartitionListAsync(service.ServiceName).Result
							 select (app, service, partition);
			return partitions.ToDictionary(k => k.partition.PartitionInformation.Id.ToString(), v => v);
		});

		public string GetInstanceName(string instance)
		{
			var res = instance;
			switch (Category)
			{
				case "Service Fabric Actor":
					res = clusterInfo.Value[instance.Split('_')[0]].service.ServiceName.AbsolutePath.Substring(1);
					break;
				case "Service Fabric Actor Method":
					res = clusterInfo.Value[instance.Split('_')[2]].service.ServiceName.AbsolutePath.Substring(1);
					res = res + "." + instance.Split('_')[0];
					break;
				case "Service Fabric Service":
					res = clusterInfo.Value[instance.Split('_')[0]].service.ServiceName.AbsolutePath.Substring(1);
					break;
				case "Service Fabric Service Method":
					res = clusterInfo.Value[instance.Split('_')[2]].service.ServiceName.AbsolutePath.Substring(1);
					res = res + "." + instance.Split('_')[0];
					break;
				default:
					break;
			}
			return res;
		}

		public float Calculate(CounterSample last, CounterSample sample)
		{
			return (float)((sample.BaseValue - last.BaseValue) * TimeSpan.FromTicks(sample.TimeStamp100nSec - last.TimeStamp100nSec).TotalSeconds);
		}
	}
}
