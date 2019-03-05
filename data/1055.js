content += '<?xml version="1.0" encoding="iso-8859-1" ?>';
content += '<rss version="2.0">';
content += '<channel>';
content += '<title>{{{site.title}}}</title>';
content += '<link>http://www.ericsink.com/</link>';
content += '<description>{{{site.tagline}}}</description>';
content += '<copyright>{{{site.copyright}}}</copyright>';
content += '<generator>mine</generator>';

var local_items = find_by_keyword_block(site, ['(hide)','(sol)','(cornsharp)','(ignoretoc)']);
sort_items_datefiled_desc(local_items);
var sofar = 0;
for (var local_i=0; local_i<local_items.length; local_i++)
{
    var local_it = local_items[local_i];

    var my_content = null;
    try
    {
        my_content = sg.file.read("data/" + local_it.id + ".html");
    }
    catch (e)
    {
        my_content = null;
    }

    if (local_it.active && my_content)
    {
        var local_path = "/" + get_path(site, local_it);
        var local_link = "http://www.ericsink.com/" + make_link(my_path, local_path);

        content += '<item>';
        content += '<title>';
        content += local_it.title;
        content += '</title>';
        content += '<guid>';
        content += local_link;
        content += '</guid>';
        content += '<link>';
        content += local_link;
        content += '</link>';
        content += '<pubDate>';
        content += format_date_rss(local_it.datefiled);
    //<pubDate>{{{loop.datefiled:format='ddd, dd MMM yyyy HH:mm:ss CST'}}}</pubDate>
        content += '</pubDate>';
        content += '<description>';
        content += '<![CDATA[';
        content += my_content;
        content += ']]>';
        content += '</description>';
        content += '</item>';

        sofar++;
        if (sofar >= 10)
        {
            break;
        }
    }
}

content += '</channel>';
content += '</rss>';

