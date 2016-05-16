using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DataCenter.Handlers.HtmlCheckerUtils;
using HtmlAgilityPack;

namespace DataCenter.Handlers
{
	public class HtmlChecker : IDisposable
	{
		private readonly Module _module;
		private readonly Mutex _checkMutex;
		private readonly List<HtmlCheck> _checks;
		private readonly IDisposable _interval;

		public HtmlChecker(Module module)
		{
			_module = module;
			module.Engine.SetValue("HtmlChecker", new Action<dynamic>(Init));
			module.Engine.SetValue("CreateHtmlNodeDataFinder", new Func<string, HtmlNodeDataFinder>(s => new HtmlNodeDataFinder(s)));
			module.Engine.SetValue("CreateAttributeDataFinder", new Func<string, AttributeDataFinder>(s => new AttributeDataFinder(s)));
			_checks = new List<HtmlCheck>();

			_interval = Utils.SetInterval(Update, 1000);

			_checkMutex = new Mutex();
		}

		private void Update()
		{
			_checkMutex.WaitOne();
			foreach (HtmlCheck check in _checks)
			{
				if (check.LastTriggered.AddSeconds(check.IntervalInSeconds) < DateTime.Now)
				{
					TriggerCheck(check);
				}
			}
			_checkMutex.ReleaseMutex();
		}

		private void TriggerCheck(HtmlCheck check)
		{
			check.LastTriggered = DateTime.Now;
			try
			{
				string body = Utils.GetBodyFromUrl(check.Url);
				if (string.IsNullOrEmpty(body)) return;

				HtmlDocument document = new HtmlDocument();
				document.LoadHtml(body);
				HtmlNode node = document.DocumentNode.SelectSingleNode("//" + check.Selector);

				string comparison = check.CheckElement.get(node);
				if (comparison != check.CheckElementValue)
				{
					_module.Emit(check.EventName, null, new
					{
						responseData = check.ResponseData.Select(attribute => attribute.get(node)).ToArray()
					});

					check.CheckElementValue = comparison;
				}
			}
			catch (Exception ex)
			{
				_module.Database.Save("error", new
				{
					message = "HTML checker failed",
					check = check,
					error = ex.Message,
					time = DateTime.Now
				});
			}
		}

		~HtmlChecker()
		{
			_interval.Dispose();
		}

		private void Init(dynamic arg)
		{
			IDictionary<string, object> obj = arg;
			HtmlCheck check = new HtmlCheck
			{
				LastTriggered = DateTime.MinValue
			};

			foreach (KeyValuePair<string, object> key in obj)
			{
				if(key.Key == "url") { check.Url = key.Value.ToString(); continue; }
				if(key.Key == "selector") { check.Selector = key.Value.ToString(); continue; }
				if(key.Key == "interval") { check.IntervalInSeconds = Convert.ToInt32(key.Value); continue; }
				if(key.Key == "eventName") { check.EventName = key.Value.ToString(); continue; }
				if (key.Key == "check")
				{
					IDictionary<string, object> child = key.Value as IDictionary<string, object>;
					if (child != null)
					{
						check.CheckElementValue = Convert.ToString(child["equals"]);
						check.CheckElement = child["element"] as IHtmlDataFinder;
					}
					continue;
				}

				if (key.Key == "responseData")
				{
					object[] child = key.Value as object[];
					if (child != null)
					{
						foreach (object c in child)
						{
							IHtmlDataFinder finder = c as IHtmlDataFinder;
							if (finder == null) continue;
							check.ResponseData.Add(finder);
						}
					}
				}
			}

			_checkMutex.WaitOne();
			_checks.Add(check);
			_checkMutex.ReleaseMutex();
		}

		private class HtmlCheck
		{
			public string Url { get; set; }
			public string Selector { get; set; }
			public IHtmlDataFinder CheckElement { get; set; }
			public string CheckElementValue { get; set; }

			public List<IHtmlDataFinder> ResponseData { get; } = new List<IHtmlDataFinder>();
			public int IntervalInSeconds { get; set; }

			public DateTime LastTriggered { get; set; }
			public string EventName { get; set; }
		}

		public void Dispose()
		{
			_interval.Dispose();
		}
	}

}
