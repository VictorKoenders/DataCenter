/*
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
*/

namespace Test
{
	class Program
	{
		static void Main(string[] args)
		{
			/*
			string urlToCheck = "http://www.crunchyroll.com/food-wars-shokugeki-no-soma";
			string targetElement = "a[contains(@class, 'episode')]";

			IHtmlDataFinder elementToCheck = new HtmlNodeDataFinder("span");
			string compareElementTo = "Episode 23";

			List<IHtmlDataFinder> attributesToReturn = new List<IHtmlDataFinder>
			{
				new AttributeDataFinder("href"),
				new HtmlNodeDataFinder("span")
			};
			string strToReturn = "New episode of Food Wars! {1} at http://www.crunchyroll.com{0}";

			HtmlDocument document = new HtmlDocument();
			document.LoadHtml(GetBodyFromUrl(urlToCheck));
			HtmlNodeCollection nodes = document.DocumentNode.SelectNodes("//" + targetElement);
			foreach (HtmlNode node in nodes)
			{
				string comparison = elementToCheck.get(node);
				if (comparison != compareElementTo)
				{
					object[] data = attributesToReturn.Select(attribute => attribute.get(node)).ToArray();
					string str = string.Format(strToReturn, data);
				}
				break;
			}*/
		}
		/*
		static string GetBodyFromUrl(string url)
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();

			if (response.StatusCode == HttpStatusCode.OK)
			{
				Stream receiveStream = response.GetResponseStream();
				StreamReader readStream = null;

				if (response.CharacterSet == null)
				{
					readStream = new StreamReader(receiveStream);
				}
				else
				{
					readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
				}

				string data = readStream.ReadToEnd();

				response.Close();
				readStream.Close();
				return data;
			}
			return null;
		}*/
	}
}
