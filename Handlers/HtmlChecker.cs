﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;
using DataCenter.Handlers.HtmlCheckerUtils;
using HtmlAgilityPack;
using Jint.Native;

namespace DataCenter.Handlers
{
	public class HtmlChecker
	{
		private readonly Module _module;
		private Mutex _checkMutex;
		private List<HtmlCheck> _checks;
		private IDisposable _interval;

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
			HtmlCheck check = new HtmlCheck();
			check.LastTriggered = DateTime.MinValue;

			foreach (var key in obj)
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
					continue;
				}
			}

			_checkMutex.WaitOne();
			_checks.Add(check);
			_checkMutex.ReleaseMutex();
		}

		public class HtmlCheck
		{
			public string Url { get; set; }
			public string Selector { get; set; }
			public IHtmlDataFinder CheckElement { get; set; }
			public string CheckElementValue { get; set; }

			public List<IHtmlDataFinder> ResponseData { get; set; } = new List<IHtmlDataFinder>();
			public int IntervalInSeconds { get; set; }

			public DateTime LastTriggered { get; set; }
			public string EventName { get; set; }
		}
	}

}