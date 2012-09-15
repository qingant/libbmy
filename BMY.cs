using System;
using System.Collections.Generic;
using System.Net;
using System.Web;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace BMY
{
	public class Article
	{
		public string artToken{ get; private set; }
		public string artTitle{ get; private set; }
		public string artText{ get; private set; }
		public string artRef { get; private set; }
		private Topic artTopic{ get; set; }
		public Article (Topic top, string tok, string t)
		{
			artTopic = top;
			artTitle = t;
			artToken = tok;
			var m = Regex.Match (artToken, @"&F=(.+)$");
			artRef = m.Groups [1].Value;
		}
		private void fetchText (BMYClient client)
		{
			if (artText != null) {
				return;
			}
			var url = string.Format ("{0}{1}{2}", BMYClient.firstURL, client.bbsToken, artToken);
			var page = client.Client.GetSrc (url, "GBK");
			var match = Regex.Match (page, @"(发信人:.+)本文链接", RegexOptions.Multiline);
			if (match.Success) {
				var text = match.Groups [1].Value;
				text = text.Replace ("<br>", "\n");
				text = Regex.Replace (text, @"<[^>]+>", "");
				artText = text;
			}
		}
		public bool reply (BMYClient client, string text, string title = null, string qmd = "---\nFrom Mono C# Client")
		{
			if (title == null) {
				if (artTitle.Substring (0, 3) != "Re:") {
					title = "Re: " + artTitle;
				} else {
					title = artTitle;
				}
			}
			var url = string.Format ("{0}{1}bbssnd?board={2}&th={3}&ref={4}",
			                         BMYClient.firstURL, 
			                         client.bbsToken, 
			                         artTopic.boardID,
			                         artTopic.topicID,
			                         artRef);
			Console.WriteLine (url);
			var data = string.Format ("title={0}&text={1}", title, text + "\n" + qmd);
			var rsp = client.Client.PostData (url, data, "GBK", "GBK");
			Console.WriteLine ("Response:");
			Console.WriteLine (rsp);

			return true;
		}

	}
	public class Topic
	{
		public string topicID{ get; private set; }
		public string topicTitle{ get; private set; }
		public string boardID { get; private set; }
		public List<Article> articleList{ get; private set; }
		public BBSBoard boardHandle { get; private set; }
		public Topic (BBSBoard bn, string id, string title)
		{
			boardHandle = bn;
			boardID = boardHandle.boardID;
			topicTitle = title;
			topicID = id;
			
			articleList = new List<Article> ();
		}
		public void Refresh (BMYClient client)
		{
			if (articleList.Count != 0) {
				articleList.Clear ();
				
			}
			fetchArticles (client);
		}
		private void fetchArticles (BMYClient client)
		{
			var url = string.Format ("{0}{1}tfind?B={2}&th={3}", BMYClient.firstURL, client.bbsToken, boardID, topicID);
			var page = client.Client.GetSrc (url);
			Console.WriteLine (page);
			var matchs = Regex.Matches (page, @"<a href=(con\?B=[^>]+)>([^<]+)</a>", RegexOptions.Multiline);
			foreach (Match m in matchs) {
				articleList.Add (new Article (this, m.Groups [1].Value, m.Groups [2].Value));
			}
		}

	}
	public class Topics
	{
		public BBSBoard boardHandle{ get; private set; }
		public List<Topic> topicList { 
			get;
			private set; 
		}
		private string nextURL{ get; set; }
		private string preURL{ get; set; }
		public Topics (BBSBoard b)
		{
			boardHandle = b;
			topicList = new List<Topic> ();
		}
		private string getBoardPage (BMYClient client)
		{
			var url = string.Format ("{0}{1}tdoc?B={2}", BMYClient.firstURL, client.bbsToken, boardHandle.boardID);
			var page = client.Client.GetSrc (url);
			return page;
		}
		private void fetchList (BMYClient client)
		{
			var page = getBoardPage (client);
			Console.WriteLine (page);
			var matchs = Regex.Matches (page, "<a href=[^>]*&th=([^>\"]+)>([^<]+)</a>", RegexOptions.Multiline);
			foreach (Match m in matchs) {
				topicList.Add (new Topic (boardHandle, m.Groups [1].Value, m.Groups [2].Value));
			}
		}
		public void Refresh (BMYClient client)
		{
			if (topicList.Count != 0) {
				topicList.Clear ();
			}
			
			fetchList (client);
		}
		public override string ToString ()
		{
			var titles = from Topic t in this.topicList
				select t.topicTitle;
			var res = string.Empty;
			foreach (var i in titles) {
				res += (i + "\n");
			}
			return res;
		}
	}
	public class BBSBoard
	{
		public string boardID { get; private set; }
		public string boardName { get; private set; }
		public Topics topics { get; private set; }
		
		public BBSBoard (string id, string name)
		{
			boardID = id;
			boardName = name;
			topics = new Topics (this);
		}
		public bool post (BMYClient client, string title, string text, string qmd = "---\nFrom Mono C#\n")
		{
			var url = string.Format ("{0}{1}bbssnd?board={2}&th=-1", BMYClient.firstURL, client.bbsToken, boardID);
			var data = string.Format ("title={0}&text=\n{1}", title, text + "\n" + qmd);
			var rsp = client.Client.PostData (url, data, "GBK", "GBK");
			Console.WriteLine (rsp);
			return true;
		}

	}
	public class BMYClient
	{
		public static string firstURL = "http://bbs.xjtu.edu.cn/";
		static string sectionURL = "boa?secstr={0}";
		static Dictionary<string, string> secList = new Dictionary<string, string>
		{
			{"交通大学","1"},
			{"开发技术","2"},
			{"电脑应用","3"},
			{"学术科学","4"},
			{"社会科学","5"},
			{"文学艺术","6"},
			{"知性感性","7"},
			{"体育运动","8"},
			{"休闲音乐","9"},
			{"游戏天地","G"},
			{"新闻信息","N"},
			{"乡音乡情", "H"},
			{"校务信息", "A"},
			{"俱乐部区", "C"},
		};
		public string passWord { get; set; }
		public string userName { get; set; }
		public bool   isLogin  { get; private set; }
		public string bbsToken { get; private set; }
		public HttpClient Client { get; private set; }
		public BMYClient (string user, string pass)
		{
			userName = user;
			passWord = pass;
			Client = new HttpClient ();
		}
		public string login ()
		{
			
			Client.Headers ["User-Agent"] = "Mozilla/4.0 (Compatible; Windows NT 5.1; MSIE 6.0) " +
				"(compatible; MSIE 6.0; Windows NT 5.1; " +
				".NET CLR 1.1.4322; .NET CLR 2.0.50727)";
			Console.WriteLine (firstURL);
			
			string loginToken = string.Format ("{0}={1}&{2}={3}", "id", userName, "pw", passWord);
			Console.WriteLine (loginToken);
			string result = Client.PostData (firstURL + "BMY/bbslogin", loginToken, "ASCII", "GBK");
			Console.WriteLine ("Login:");
			Console.WriteLine (result);
			Match match = Regex.Match (result, @" url=(.+/)");
			if (match.Success) {
				var uri = match.Groups [1].Value;
				Console.WriteLine (uri);
				bbsToken = uri;
				isLogin = true;
				return uri;
			} else {
				throw new Exception ("Login Failed");
				
			}
		}
		public string getMain (string token)
		{
			var uri = firstURL + token;
			return Client.GetSrc (uri, "GBK");
		}
		public string getNav (string page)
		{
			Match match = Regex.Match (page, @"src=(bbsleft.+) ");
			var navToken = match.Groups [1].Value;
			var navURL = firstURL + bbsToken + navToken;
			return Client.GetSrc (navURL, "GBK");
			
		}
		private string getSecURL (string token)
		{
			var url = string.Format ("{0}{1}{2}", firstURL, bbsToken, string.Format (sectionURL, token));
			Console.WriteLine (url);
			return url;
		}
		private string getSecPage (string token)
		{
			var url = getSecURL (token);
			return Client.GetSrc (url);
		}
		private string getBoardURL (string token)
		{
			var url = string.Format ("{0}{1}{2}", firstURL, bbsToken, token);
			return url;
		}
		
		private List<BBSBoard> parserBoardList (string page)
		{
			var dict = new List<BBSBoard> ();
			//var pattern = new Regex ("<a href=(.*)>(.*)</a>");
			var matchs = Regex.Matches (page, @"<a href=home\?B=([^>]*)>([^<]+)</a>", RegexOptions.Multiline);
			
			foreach (Match m in matchs) {
				//Console.WriteLine (m);
				if (!Regex.IsMatch (m.Groups [2].Value, @"[a-zA-Z0-9 ]+")) {
					dict.Add (new BBSBoard (m.Groups [1].Value, m.Groups [2].Value));
				}
				
			}
			return dict;
			
		}
		
		public List<BBSBoard> getBoardList (string secName)
		{
			var secNo = secList [secName];
			var page = getSecPage (secNo);
			Console.WriteLine ("Sec:\n {0}{1}\n{2}", secName, secNo, page);
			return parserBoardList (page);
		}
		
	}
}

