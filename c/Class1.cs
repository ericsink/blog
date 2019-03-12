using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace blog
{
    public class pre
    {
        public static string crunch(Dictionary<string, Item> items, string id, string html)
        {
            var t = html;
            var page = items[id];
            var my_path = "/" + fsfun.get_path(items, page);

            t = t.Replace("{{{haloscan}}}", "");

            var regx = new Regex(@"{{{link:id='(?<id>\d+)'}}}");
            var links = regx.Matches(t);
            if (links != null)
            {
                foreach (Match m in links)
                {
                    var link_id = m.Groups["id"].Value;
                    var link_it = items[link_id];
                    var other_path = "/" + fsfun.get_path(items, link_it);

                    var str_link = fsfun.make_link(my_path, other_path);

                    t = t.Replace(m.Value, str_link);
                }
            }

            return t;
        }

    }
}
