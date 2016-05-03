using HtmlAgilityPack;

namespace DataCenter.Handlers.HtmlCheckerUtils
{
	public class AttributeDataFinder : IHtmlDataFinder
	{
		private readonly string _attributeName;

		public AttributeDataFinder(string attributeName)
		{
			_attributeName = attributeName;
		}

		public string get(HtmlNode node)
		{
			return node.Attributes[_attributeName].Value;
		}
	}
}