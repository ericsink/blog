
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace blog
{

public static class csfun
{
    static DateTime parse_date(string s)
    {
        var twoparts = s.Split(' ');
        var dateparts = twoparts[0].Split('-');
        var timeparts = twoparts[1].Split(':');
        var year = int.Parse(dateparts[0]);
        var month = int.Parse(dateparts[1]);
        var day = int.Parse(dateparts[2]);
        var hour = int.Parse(timeparts[0]);
        var min = int.Parse(timeparts[1]);
        var sec = int.Parse(timeparts[2]);

        var d = new DateTime(year, month, day, hour, min, sec, 0);

        return d;
    }

    static string format_date_rss(string s)
    {
        //<pubDate>{{{loop.datefiled:format='ddd, dd MMM yyyy HH:mm:ss CST'}}}</pubDate>
        var d = parse_date(s);
        return d.ToString("ddd, dd MMM yyyy HH:mm:ss CST");
    }

    static string format_date(string s)
    {
        var d = parse_date(s);
        return d.ToString("dddd, d MMMM yyyy");
    }

    static Dictionary<string, Item> find_by_keyword_match(Site site, IList<string> keywords)
    {
		return site.items
			.Where(kv => kv.Value.type == "html")
			.Where(kv => kv.Value.usetemplate)
			.Where(kv => (kv.Value.keywords != null) && keywords.Any(s => kv.Value.keywords.IndexOf(s) >= 0))
			.ToDictionary(
				kv => kv.Key,
				kv => kv.Value
				);
    }

    static Dictionary<string, Item> find_by_keyword_block(Site site, IList<string> keywords)
    {
		return site.items
			.Where(kv => kv.Value.type == "html")
			.Where(kv => kv.Value.usetemplate)
			.Where(kv => (kv.Value.keywords == null) || keywords.All(s => kv.Value.keywords.IndexOf(s) < 0))
			.ToDictionary(
				kv => kv.Key,
				kv => kv.Value
				);
    }

    static string do_1055(string dir_data, Site site, string my_path)
    {
        var content = "";
        content += "<?xml version=\"1.0\" encoding=\"iso-8859-1\" ?>";
        content += "<rss version=\"2.0\">";
        content += "<channel>";
        content += "<title>{{{site.title}}}</title>";
        content += "<link>http://www.ericsink.com/</link>";
        content += "<description>{{{site.tagline}}}</description>";
        content += "<copyright>{{{site.copyright}}}</copyright>";
        content += "<generator>mine</generator>";

        var sofar = 0;
        var local_items = find_by_keyword_block(site, new string[] { "(hide)", "(sol)", "(cornsharp)", "(ignoretoc)" });
        foreach (var kv in
            local_items
                .OrderByDescending(x => parse_date(x.Value.datefiled))
            )
        {
            var local_id = kv.Key;
            var local_it = kv.Value;

            string my_content;
            try
            {
                my_content = File.ReadAllText(Path.Combine(dir_data, local_id + ".html"));
            }
            catch
            {
                my_content = null;
            }

            if (local_it.active && (my_content != null))
            {
                var local_path = "/" + fsfun.get_path(site, local_it);
                var local_link = "http://www.ericsink.com/" + fsfun.make_link(my_path, local_path);

                content += "<item>";
                content += "<title>";
                content += local_it.title;
                content += "</title>";
                content += "<guid>";
                content += local_link;
                content += "</guid>";
                content += "<link>";
                content += local_link;
                content += "</link>";
                content += "<pubDate>";
                content += format_date_rss(local_it.datefiled);
                //<pubDate>{{{loop.datefiled:format="ddd, dd MMM yyyy HH:mm:ss CST"}}}</pubDate>
                content += "</pubDate>";
                content += "<description>";
                content += "<![CDATA[";
                content += my_content;
                content += "]]>";
                content += "</description>";
                content += "</item>";

                sofar++;
                if (sofar >= 10)
                {
                    break;
                }
            }
        }

        content += "</channel>";
        content += "</rss>";

        return content;
    }

    static string do_1150(Site site, string my_path)
    {
        var content = "";

        var local_items = find_by_keyword_match(site, new string[] { "(dev)" });
        foreach (var kv in
            local_items
                .OrderByDescending(x => parse_date(x.Value.datefiled))
            )
        {
            var local_id = kv.Key;
            var local_it = kv.Value;
            var local_path = "/" + fsfun.get_path(site, local_it);
            var local_link = fsfun.make_link(my_path, local_path);

            content += "<span class=\"DayPageArticleTitle\"><a href=\"";
            content += local_link;
            content += "\">";
            content += local_it.title;
            content += "</a></span>";
            content += " (<span class=ArticleDate>";
            content += format_date(local_it.datefiled);
            content += "</span>)";
            content += "<blockquote>";
            if (local_it.teaser != null)
            {
                content += local_it.teaser;
            }
            content += "</blockquote>";
        }

        return content;
    }

    static string do_1159(Site site, string my_path)
    {
        var content = "";

        content += "<p>The following articles originally appeared on the MSDN website.&nbsp; From October 2003 to April 2005, I wrote a column there called \"The Business of Software\".&nbsp; Thanks to <a href=\"http://www.sellsbrothers.com/\">Chris Sells</a> for giving me the opportunity to write for MSDN, and to Maurine Bryan for doing a great job with the editing.</p> <hr /> <br />";

        var local_items = find_by_keyword_match(site, new string[] { "(bos)" });
        foreach (var kv in
            local_items
                .OrderByDescending(x => parse_date(x.Value.datefiled))
            )
        {
            var local_id = kv.Key;
            var local_it = kv.Value;
            var local_path = "/" + fsfun.get_path(site, local_it);
            var local_link = fsfun.make_link(my_path, local_path);

            content += "<span class=\"DayPageArticleTitle\"><a href=\"";
            content += local_link;
            content += "\">";
            content += local_it.title;
            content += "</a></span>";
            content += " (<span class=ArticleDate>";
            content += format_date(local_it.datefiled);
            content += "</span>)";
            content += "<blockquote>";
            if (local_it.teaser != null)
            {
                content += local_it.teaser;
            }
            content += "</blockquote>";
        }

        return content;
    }

    static string do_1182(string dir_data, Site site, string my_path)
    {
        var content = "";


        {
            content += "</td></tr>";
            var sofar = 0;
            var local_items = find_by_keyword_block(site, new string[] { "(hide)", "(sol)", "(cornsharp)", "(ignoretoc)" });
            foreach (var kv in
                local_items
                    .OrderByDescending(x => parse_date(x.Value.datefiled))
                )
            {
                var local_id = kv.Key;
                var local_it = kv.Value;

                string my_content;
                try
                {
                    my_content = File.ReadAllText(Path.Combine(dir_data, local_id + ".html"));
                }
                catch
                {
                    my_content = null;
                }

                if (local_it.active && (my_content != null))
                {
                    var local_path = "/" + fsfun.get_path(site, local_it);
                    var local_link = fsfun.make_link(my_path, local_path);

                    content += "<tr><td><span align=\"right\" class=ArticleDate>";
                    content += format_date(local_it.datefiled);
                    content += "</span><br><a class=\"ArticleTitleGreen\" href=\"";
                    content += local_link;
                    content += "\">";
                    content += local_it.title;
                    content += "</a><br><br></td></tr><tr><td>";
                    content += my_content;
                    content += "<P> </td></tr> <tr><td>&nbsp;</td></tr>";

                    sofar++;

                    if (sofar >= 10)
                    {
                        break;
                    }
                }
            }
            content += "<tr><td bgcolor=\"white\">";
        }


        return content;
    }

    static string do_1205(Site site, string my_path)
    {
        var content = "";


        content += "<P>I love to play cards. I\'ve spent many hours sitting around a kitchen table playing pinochle, euchre or spades.</p><P>But I think my favorite card game is bridge. More specifically, the variant of bridge which fascinates me is called \"duplicate\". The basic idea of duplicate bridge is that your score is a function of how well you play your cards as compared to how the other teams played the exact same cards. <P>Just to be clear, let me repeat: In duplicate bridge, you are playing the same cards as your opponents. The luck of the deal is basically eliminated. <P>You have 13 cards in your hand, so there are 13 \"tricks\" available to win. If you are dealt excellent cards, there is no particular reason to get excited. Yes, your cards will take lots of tricks, but that\'s not the point. The issue is whether you take as many tricks as the other people who play those exact same cards. If you take nine tricks but somebody else finds a way to take ten, you lose. <P>Duplicate bridge is a brutal game. Every small mistake can be very costly.&nbsp; I do like to go to the local bridge club sometimes, but I&nbsp;usually end up in last place. At the end of the evening, I review each hand and figure out what went wrong.&nbsp; Even though I am terribly bad at this game, I still enjoy it because every game is such a learning experience. <P>I often wonder what other pursuits would be like if they had to operate under the same rules:&nbsp; Resources and context do not change -- the only variable is the ability of the person managing those resources. <P>These questions become particularly interesting to me when asked in the field of software product management. For a given piece of technology or code, what would happen if somebody else were managing it? <P><LI>If I were managing the <A href=\"http://www.borland.com/delphi/\">Delphi</A> product instead of Borland, could I do a better job? <LI>If <A href=\"http://www.joelonsoftware.com/\">Joel Spolsky</A> were managing Vault instead of me, would the product have more users? <LI>If Sun were to hand the management of <A href=\"http://java.sun.com/\">Java</A> over to a committee of monkeys, would it be more successful?&nbsp; :-) <P>Alas, these hypothetical fantasies are not going to happen. That\'s unfortunate. If ISVs had to play duplicate, we would all quickly learn a lot. First, the sheer volume of our stupid mistakes would be exposed, and we would quickly learn how very bad we all are at product management. And after that, we would start learning the fine points.&nbsp; Instead of just chalking up every failure to the fault of \"bad marketing\", we would review each decision and figure out exactly where and when we played the wrong card.</P><P>We can\'t play duplicate with our shrinkwrap products, but we can learn the fine points of marketing. Marketing is not some vague and fuzzy realm where only luck matters. There are principles which can be learned and applied. <P>Al Ries and Jack Trout&nbsp;refer to these principles as&nbsp;\"laws\". Their book, entitled \"<A href=\"http://www.amazon.com/exec/obidos/ASIN/0887306667/sawdust08-20\">The 22 Immutable Laws of Marketing</A>\" is one of my favorites. And I couldn\'t help but notice that there are exactly 22 weekdays in the month of June. So...</P><P>During the month of June,&nbsp;I plan to&nbsp;post a brief blurb each weekday.&nbsp; For each of the 22 laws, I will summarize the main point and draw a connection to the software industry.&nbsp; My entries will not be complete discussions of the topic.&nbsp;&nbsp;Interested readers should read the book&nbsp;and follow along. </P>";

        content += "<UL>";

        var local_items = find_by_keyword_match(site, new string[] { "(law)" });
        foreach (var kv in
            local_items
                .OrderBy(x => x.Key)
            )
        {
            var local_id = kv.Key;
            var local_it = kv.Value;
            var local_path = "/" + fsfun.get_path(site, local_it);
            var local_link = fsfun.make_link(my_path, local_path);

            content += "<li><a href=\"";
            content += local_link;
            content += "\">";
            content += local_it.title;
            content += "</a></li>";
        }
        content += "</UL>";

        content += "<P>For those who prefer to read this series of postings in printed form, a <A href=\"{{{link:id=\'1043\'}}}\">PDF version</A> is available.</P>";

        content += "<br/>";


        return content;
    }

    static string do_1207(Site site, string my_path)
    {
        var content = "";


        content += "<p>This page serves as the table of contents for&nbsp;my series of articles entitled \"Marketing for Geeks\".&nbsp; The central theme here is that if we demystify marketing, it can be competently done by technical people.&nbsp; The series is still being written, with new articles coming soon to an RSS feed near you.</p>";

        content += "<p>In most <a href=\"Small_ISV_Defined.html\">small ISVs</a>, it\'s important for at least some of the&nbsp;developers to have an understanding of basic marketing.&nbsp;&nbsp;However, most&nbsp;geeks tend to shy away from marketing, citing their lack of creativity and graphic design skills.&nbsp; But these are typically not the differentiators which determine whether marketing is competent or not.&nbsp; Marketing efforts tend to succeed or fail on their strategy, not on their artwork.&nbsp; In fact, many teams can improve their marketing simply by realizing that marketing, like software development, has two distinct phases.</p>";

        content += "<p><font face=\"Arial,Helvetica,sansserif\" size=\"4\">The Two Phases of Marketing</font></p>";

        content += "<p>When we build software, we typically have a design phase, followed by an implementation phase.&nbsp; In the design phase, we carefully figure out exactly what we want to do.&nbsp; In the implementation phase, we do it.</p>";

        content += "<p>Likewise, marketing has a strategic phase, followed by a&nbsp;communication phase.</p>";

        content += "<ul> <li>The strategic phase is analogous to the design phase of building software.&nbsp; (In fact, they are related and must usually be done together.)<br /><br /> </li> <li>The communication phase is analogous to the implementation phase of building software.&nbsp; We call this set of activities \"marketing communications\", or \"marcomm\" for short.  </li> </ul>";

        content += "<p>I find it interesting that although marketing people and technical people often think they have nothing in common, both groups naturally try to weasel out of doing their first phase.&nbsp; Maverick programmers don\'t want to write specs and do design.&nbsp; They simply want to write code.&nbsp; Similarly, marketing people&nbsp;often prefer&nbsp;to plunge headfirst into creating messages, taglines and ad campaigns.&nbsp; In either case, skipping the first phase will get you the instant gratification of visible results, but you\'ll&nbsp;have all kinds of trouble down the road.</p>";

        content += "<p><font face=\"Arial,Helvetica,sansserif\" size=\"4\">Articles about Strategy</font></p>";

        {
            var local_items = find_by_keyword_match(site, new string[] { "(mfg_strategy)" });
            foreach (var kv in
                local_items
                    .OrderByDescending(x => parse_date(x.Value.datefiled))
                )
            {
                var local_id = kv.Key;
                var local_it = kv.Value;
                var local_path = "/" + fsfun.get_path(site, local_it);
                var local_link = fsfun.make_link(my_path, local_path);

                content += "<span class=\"DayPageArticleTitle\"><a href=\"";
                content += local_link;
                content += "\">";
                content += local_it.title;
                content += "</a></span>";
                content += " (<span class=ArticleDate>";
                content += format_date(local_it.datefiled);
                content += "</span>)";
                content += "<blockquote>";
                if (local_it.teaser != null)
                {
                    content += local_it.teaser;
                }
                content += "</blockquote>";
            }
        }

        content += "<p style=\"MARGIN-RIGHT: 0px\"><font face=\"Arial,Helvetica,sansserif\" size=\"4\">Articles about Marcomm</font></p>";

        {
            var local_items = find_by_keyword_match(site, new string[] { "(mfg_marcomm)" });
            foreach (var kv in
                local_items
                    .OrderByDescending(x => parse_date(x.Value.datefiled))
                )
            {
                var local_id = kv.Key;
                var local_it = kv.Value;
                var local_path = "/" + fsfun.get_path(site, local_it);
                var local_link = fsfun.make_link(my_path, local_path);

                content += "<span class=\"DayPageArticleTitle\"><a href=\"";
                content += local_link;
                content += "\">";
                content += local_it.title;
                content += "</a></span>";
                content += " (<span class=ArticleDate>";
                content += format_date(local_it.datefiled);
                content += "</span>)";
                content += "<blockquote>";
                if (local_it.teaser != null)
                {
                    content += local_it.teaser;
                }
                content += "</blockquote>";
            }
        }

        return content;
    }

    static string do_1616(Site site, string my_path)
    {
        var content = "";


        var local_items = find_by_keyword_match(site, new string[] { "(laughs)" });
        foreach (var kv in
            local_items
                .OrderByDescending(x => parse_date(x.Value.datefiled))
            )
        {
            var local_id = kv.Key;
            var local_it = kv.Value;
            var local_path = "/" + fsfun.get_path(site, local_it);
            var local_link = fsfun.make_link(my_path, local_path);

            content += "<span class=\"DayPageArticleTitle\"><a href=\"";
            content += local_link;
            content += "\">";
            content += local_it.title;
            content += "</a></span>";
            content += " (<span class=ArticleDate>";
            content += format_date(local_it.datefiled);
            content += "</span>)";
            content += "<blockquote>";
            if (local_it.teaser != null)
            {
                content += local_it.teaser;
            }
            content += "</blockquote>";
        }


        return content;
    }

    static string do_3052(string dir_data, Site site, string my_path)
    {
        var content = "";



        content += "<p>This is a series of articles about using SQLite when developing mobile apps.  The target audience is folks who are coming from the world of SQL Server and .NET.</p>";

        content += "<h2>Table of Contents</h2>";

        content += "<p><i>Anything 'not written yet' is subject to change.  Entries are listed in a somewhat logical order for their content, roughly proceeding from 'big picture stuff' to 'technical details', but I am not promising to actually write them in that order.</i>  <tt>:-)</tt></p>";

        content += "<UL>";

        var local_items = find_by_keyword_match(site, new string[] { "(mssql_to_sqlite)" });
        foreach (var kv in
            local_items
                .OrderBy(x => x.Key)
            )
        {
            var local_id = kv.Key;
            var local_it = kv.Value;
            string my_content;
            try
            {
                my_content = File.ReadAllText(Path.Combine(dir_data, local_id + ".html"));
            }
            catch
            {
                my_content = null;
            }

            if (local_it.active)
            {
                if (my_content != null)
                {
                    var local_path = "/" + fsfun.get_path(site, local_it);
                    var local_link = fsfun.make_link(my_path, local_path);

                    content += "<li><p><a href=\"";
                    content += local_link;
                    content += "\">";
                    content += local_it.title;
                    content += "</a></p></li>";
                    if (local_it.teaser != null)
                    {
                        content += "<blockquote>";
                        content += local_it.teaser;
                        content += "</blockquote>";
                    }
                }
                else
                {
                    content += "<li><p>(not written yet) ";
                    content += local_it.title;
                    content += "</p></li>";
                    if (local_it.teaser != null)
                    {
                        content += "<blockquote>";
                        content += local_it.teaser;
                        content += "</blockquote>";
                    }
                }
            }
        }
        content += "</UL>";

        content += "<p>&nbsp;</p>";


        return content;
    }

    public static string do_js(string dir_data, Site site, string id, string my_path)
    {
        switch (id)
        {
            case "1055":
                return do_1055(dir_data, site, my_path);
            case "1150":
                return do_1150(site, my_path);
            case "1159":
                return do_1159(site, my_path);
            case "1182":
                return do_1182(dir_data, site, my_path);
            case "1205":
                return do_1205(site, my_path);
            case "1207":
                return do_1207(site, my_path);
            case "1616":
                return do_1616(site, my_path);
            case "3052":
                return do_3052(dir_data, site, my_path);
            default:
                System.Console.WriteLine("TODO");
                return "";
        }
    }

}

public class blog
{

    public static string NewId_hex()
    {
        string id = Guid
            .NewGuid()
            .ToString()
            .Replace("{", "")
            .Replace("}", "")
            .Replace("-", "");
        return id;
    }

    static string crunch(Site site, string id, string html, string content)
    {
        var t = html;
        var page = site.items[id];
        var my_path = "/" + fsfun.get_path(site, page);

        if (content != null)
        {
            t = t.Replace("{{{page.content}}}", content);
        }

        if (page.title != null)
        {
            t = t.Replace("{{{page.title}}}", page.title);
            t = t.Replace("{{{article.title}}}", "<p class=\"ArticleDate\" align=right>" + page.datefiled + "</p><h1>" + page.title + "</h1>");
        }
        else
        {
            t = t.Replace("{{{page.title}}}", site.title);
            t = t.Replace("{{{article.title}}}", "");
        }

        t = t.Replace("{{{haloscan}}}", "");
        t = t.Replace("{{{site.title}}}", site.title);
        t = t.Replace("{{{site.copyright}}}", site.copyright);
        t = t.Replace("{{{site.tagline}}}", site.tagline);

        var regx = new Regex(@"{{{link:id='(?<id>\d+)'}}}");
        var links = regx.Matches(t);
        if (links != null)
        {
            foreach (Match m in links)
            {
                var link_id = m.Groups["id"].Value;
                var link_it = site.items[link_id];
                var other_path = "/" + fsfun.get_path(site, link_it);

                var str_link = fsfun.make_link(my_path, other_path);

                t = t.Replace(m.Value, str_link);
            }
        }

        return t;
    }


    static string get_content(string dir_data, string id)
    {
        try
        {
            return File.ReadAllText(Path.Combine(dir_data, id + ".html"));
        }
        catch
        {
            return null;
        }
    }

    public static void Main()
    {
		var dir_data = Path.Combine("..", "data");
        var site = JsonConvert.DeserializeObject<Site>(File.ReadAllText(Path.Combine(dir_data, "esbma.json")));
        var dest = "pub_" + NewId_hex();

		var template = File.ReadAllText(Path.Combine(dir_data, "template.html"));

        Directory.CreateDirectory(dest);

        foreach (var kv in site.items)
        {
            var id = kv.Key;
            var it = kv.Value;

            var path = dest + "/" + fsfun.get_path(site, it);
            var my_path = "/" + fsfun.get_path(site, it);

            if (it.active)
            {
                if ("dir" == it.type)
                {
                    Directory.CreateDirectory(path);
                }
                else if ("blob" == it.type)
                {
                    File.Copy(Path.Combine(dir_data, id + ".bin"), path);
                }
                else if ("js" == it.type)
                {
                    var js = File.ReadAllText(Path.Combine(dir_data, id + ".js"));
                    var content = csfun.do_js(dir_data, site, id, my_path);
                    if (it.usetemplate)
                    {
                        var crunched = crunch(site, id, template, content);
                        File.WriteAllText(path, crunched);
                    }
                    else
                    {
                        var crunched = crunch(site, id, content, null);
                        File.WriteAllText(path, crunched);
                    }
                }
                else if ("html" == it.type)
                {
                    if (it.usetemplate)
                    {
                        var content = get_content(dir_data, id);
                        if (content != null)
                        {
                            var crunched = crunch(site, id, template, content);
                            File.WriteAllText(path, crunched);
                        }
                    }
                    else
                    {
                        var content = File.ReadAllText(Path.Combine(dir_data, id + ".html"));
                        var crunched = crunch(site, id, content, null);
                        File.WriteAllText(path, crunched);
                    }
                }
                else if ("xml" == it.type)
                {
                }
                else
                {
                    throw new Exception();
                }
            }
        }
    }

}

}

