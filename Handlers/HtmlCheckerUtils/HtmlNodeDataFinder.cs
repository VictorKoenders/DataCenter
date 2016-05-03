using HtmlAgilityPack;

namespace DataCenter.Handlers.HtmlCheckerUtils
{
	public class HtmlNodeDataFinder : IHtmlDataFinder
	{
		private readonly string _relPath;
		private readonly IHtmlDataFinder _nextFinder;

		public HtmlNodeDataFinder(string relPath)
		{
			_relPath = relPath;
			_nextFinder = null;
		}
		public HtmlNodeDataFinder(string relPath, IHtmlDataFinder nextFinder)
		{
			_relPath = relPath;
			_nextFinder = nextFinder;
		}

		public string get(HtmlNode node)
		{
			HtmlNode nextNode = node.SelectSingleNode(_relPath);
			if (_nextFinder != null)
				return _nextFinder.get(nextNode);
			return nextNode.InnerText.Trim();
		}
	}
}
