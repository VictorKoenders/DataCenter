using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace DataCenter
{
    public static class Utils
    {
        public static IDisposable SetTimeout(Action action, int durationInMilliseconds)
        {
            System.Timers.Timer timer = new System.Timers.Timer(durationInMilliseconds);
            timer.Elapsed += (source, e) =>
            {
                action();
                timer.Dispose();
            };

            timer.AutoReset = false;
            timer.Enabled = true;
            return timer;
        }

	    public static IDisposable SetInterval(Action action, int durationInMilliseconds)
		{
			System.Timers.Timer timer = new System.Timers.Timer(durationInMilliseconds);
			timer.Elapsed += (source, e) =>
			{
				action();
			};

			timer.AutoReset = true;
			timer.Enabled = true;
			return timer;
		}

		public static string GetBodyFromUrl(string url)
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();

			if (response.StatusCode == HttpStatusCode.OK)
			{
				Stream receiveStream = response.GetResponseStream();
				if (receiveStream == null) return null;

				StreamReader readStream = response.CharacterSet == null 
					? new StreamReader(receiveStream) 
					: new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));

				string data = readStream.ReadToEnd();

				response.Close();
				readStream.Close();
				return data;
			}
			response.Close();
			return null;
		}

        private static readonly Regex FormatRegex = new Regex("\\{([^}]+)\\}", RegexOptions.Compiled);

        public static string FormatString(string format, ExpandoObject args)
        {
            IDictionary<string, object> a = args;
            return FormatRegex.Replace(format, match => a[match.Groups[1].Value].ToString());
        }

        public static bool FormatMatches(string format, string text)
        {
            bool result = new Regex("^" + FormatRegex.Replace(format, "(.*)")
                .Replace("/", "\\/")
                .Replace("?", "\\?")
                + "$").IsMatch(text);
            return result;
        }

        public static object ParseFormatString(string format, string text)
        {
            ExpandoObject result = new ExpandoObject();
            MatchCollection formatMatches = FormatRegex.Matches(format);
            MatchCollection resultMatches = new Regex("^" + FormatRegex.Replace(format, "(.*)")
                .Replace("/", "\\/")
                .Replace("?", "\\?") 
                + "$").Matches(text);

            for (int i = 0; i < formatMatches.Count && i + 1 < resultMatches[0].Groups.Count; i++)
            {
                string key = formatMatches[i].Groups[1].Value;
                string value = resultMatches[0].Groups[1 + i].Value;

                ((IDictionary<string, object>) result).Add(key, value);
            }

            return result;
        }
	}
}