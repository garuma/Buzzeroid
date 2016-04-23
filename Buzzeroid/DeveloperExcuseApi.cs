using System;
using System.Net.Http;
using System.Xml;
using System.Threading.Tasks;

namespace Buzzeroid
{
	public class DeveloperExcuseApi
	{
		const string Url = "http://developerexcuses.com/";

		HttpClient client;

		public async Task<string> GetNextExcuseAsync ()
		{
			if (client == null)
				client = new HttpClient ();

			var content = await client.GetStreamAsync (Url).ConfigureAwait (false);
			var settings = new XmlReaderSettings {
				Async = true,
				DtdProcessing = DtdProcessing.Ignore,
				CloseInput = true
			};
			using (var xmlReader = XmlReader.Create (content, settings)) {
				await xmlReader.MoveToContentAsync ();
				while (await xmlReader.ReadAsync ().ConfigureAwait (false)) {
					if (xmlReader.NodeType != XmlNodeType.Element)
						continue;
					if (xmlReader.LocalName == "a") {
						return await xmlReader.ReadElementContentAsStringAsync ().ConfigureAwait (false);
					}
				}

				return null;
			}
		}
	}
}

