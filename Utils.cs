using System;
using System.IO;
using System.Net;
using System.Text;

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
	}
}