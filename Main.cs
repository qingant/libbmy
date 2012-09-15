using System;
using System.Collections.Generic;
using System.Net;
using System.Web;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace BMY
{

	public static class ListUtils
	{
		public static T ReversedIndex<T> (this List<T> self, int index)
		{
			return self [self.Count - index - 1];
		}
	}
	class MainClass
	{
		public static void Main (string[] args)
		{
			List<int> numbers = new List<int>{1,2,3};
			
			var filtered = from int n in numbers 
				    where n % 2 == 1
					select n;
			foreach (int n in filtered) {
				Console.WriteLine (n);
			}
			BMYClient cli = new BMYClient ("Guest", "");
			var uri = cli.login ();
			var page = cli.getMain (uri);
			Console.WriteLine ("Main Page");
			Console.WriteLine (page);

			page = cli.getNav (page);
			Console.WriteLine ("Nav Page");
			Console.WriteLine (page);

			var sec = "电脑应用";
			var blist = cli.getBoardList (sec);
			BBSBoard board = blist [3];
			foreach (var p in blist) {
				Console.WriteLine ("{0},{1}", p.boardID, p.boardName);
			}
			board.topics.Refresh (cli);
			Console.WriteLine (board.topics);
			var top = board.topics.topicList.ReversedIndex (0);
			top.Refresh (cli);
			top.articleList.ReversedIndex (0) .reply (cli, "Test Mono C# libbmy Article Reply");
			//board.post (cli, "Test From Mono C#", "Just Test");
			//			filtered.ForEach(Console.WriteLine);
			Console.WriteLine ("Hello World!");
		}
	}
}
