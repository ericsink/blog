using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace blog
{
    public class pre
    {
        public static string crunch(Dictionary<string, Item> items, string html)
        {
            var t = html;

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

                    t = t.Replace(m.Value, other_path);
                }
            }

            return t;
        }

    }
}
