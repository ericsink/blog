
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
                .OrderByDescending(x => fsfun.parse_date(x.Value.datefiled))
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
                content += fsfun.format_date_rss(local_it.datefiled);
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
                .OrderByDescending(x => fsfun.parse_date(x.Value.datefiled))
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
            content += fsfun.format_date(local_it.datefiled);
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
                .OrderByDescending(x => fsfun.parse_date(x.Value.datefiled))
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
            content += fsfun.format_date(local_it.datefiled);
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
                    .OrderByDescending(x => fsfun.parse_date(x.Value.datefiled))
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
                    content += fsfun.format_date(local_it.datefiled);
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
                    .OrderByDescending(x => fsfun.parse_date(x.Value.datefiled))
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
                content += fsfun.format_date(local_it.datefiled);
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
                    .OrderByDescending(x => fsfun.parse_date(x.Value.datefiled))
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
                content += fsfun.format_date(local_it.datefiled);
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
                .OrderByDescending(x => fsfun.parse_date(x.Value.datefiled))
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
            content += fsfun.format_date(local_it.datefiled);
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

