using HtmlAgilityPack;

namespace DataCenter.Handlers.HtmlCheckerUtils
{
	public interface IHtmlDataFinder
	{
		string get(HtmlNode node);
	}
}