
load("date.js");

function parse_date(s)
{
    var twoparts = s.split(' ');
    var dateparts = twoparts[0].split('-');
    var timeparts = twoparts[1].split(':');
    var year = parseInt(dateparts[0], 10);
    var month = parseInt(dateparts[1], 10);
    var day = parseInt(dateparts[2], 10);
    var hour = parseInt(timeparts[0], 10);
    var min = parseInt(timeparts[1], 10);
    var sec = parseInt(timeparts[2], 10);

    var d = new Date(year, month - 1, day, hour, min, sec, 0);

    return d;
}

function sortfunc_id_asc(a, b)
{
    var ida = parseInt(a.id, 10);
    var idb = parseInt(b.id, 10);

    if (ida > idb)
    {
        return 1;
    }
    else if (idb > ida)
    {
        return -1;
    }
    else
    {
        return 0;
    }
}

function sortfunc_datefiled_desc(a, b)
{
    var da = parse_date(a.datefiled);
    var db = parse_date(b.datefiled);

    if (da > db)
    {
        return -1;
    }
    else if (db > da)
    {
        return 1;
    }
    else
    {
        return 0;
    }
}

function format_date_rss(s)
{
    //<pubDate>{{{loop.datefiled:format='ddd, dd MMM yyyy HH:mm:ss CST'}}}</pubDate>
    var d = parse_date(s);
    return d.toString('ddd, dd MMM yyyy HH:mm:ss CST');
}

function format_date(s)
{
    var d = parse_date(s);
    return d.toString('dddd, d MMMM yyyy');
}

function sort_items_datefiled_desc(items)
{
    items.sort(sortfunc_datefiled_desc);
}

function sort_items_id_asc(items)
{
    items.sort(sortfunc_id_asc);
}

function find_by_keyword_match(site, keywords)
{
    var result = [];

    for (var id in site.items)
    {
        var it  = site.items[id];

        if ("html" != it.type)
        {
            continue;
        }

        if (!it.usetemplate)
        {
            continue;
        }

        if (it.keywords)
        {
            for (var i=0; i<keywords.length; i++)
            {
                if (0 <= it.keywords.indexOf(keywords[i]))
                {
                    result.push(it);
                    break;
                }
            }
        }
    }

    return result;
}

function find_by_keyword_block(site, keywords)
{
    var result = [];

    for (var id in site.items)
    {
        var it  = site.items[id];

        if ("html" != it.type)
        {
            continue;
        }

        if (!it.usetemplate)
        {
            continue;
        }

        if (it.keywords)
        {
            var ok = true;
            for (var i=0; i<keywords.length; i++)
            {
                if (0 <= it.keywords.indexOf(keywords[i]))
                {
                    ok = false;
                    break;
                }
            }
            if (ok)
            {
                result.push(it);
            }
        }
        else
        {
            result.push(it);
        }
    }

    return result;
}

function path_combine(a, b)
{
    if (a.length == 0)
    {
        return b;
    }

    if (b.substr(0,1) == "/")
    {
        return b;
    }

    if (a.substr(a.length-1, 1) == "/")
    {
        return a + b;
    }

    return a + "/" + b;
}

function make_link(myPath, otherPath)
{
    var myParts = myPath.split('/');
    var otherParts = otherPath.split('/');

    // TODO this feels like a hack
    if (myPath == otherPath)
    {
        return myParts[myParts.length-1];
    }

    var ndx = 0;
    while (
        (ndx < myParts.length)
        && (ndx < otherParts.length)
        && (myParts[ndx] == otherParts[ndx])
        )
    {
        ndx++;
    }

    var result = "";

    var i = ndx;
    while (i < (myParts.length-1))
    {
        result = path_combine(result, "..");
        i++;
    }

    while (ndx < (otherParts.length))
    {
        result = path_combine(result, otherParts[ndx]);
        ndx++;
    }

    return result;
}

function get_path(site, it)
{
    if (it.parentid)
    {
        var pit = site.items[it.parentid];

        return get_path(site, pit) + "/" + it.pubname;
    }
    else
    {
        return it.pubname;
    }
}

function get_content(site, id)
{
    try
    {
        return sg.file.read("data/" + id + ".html");
    }
    catch (e)
    {
        return null;
    }
}

function crunch(site, id, html, content)
{
    var t = html;
    var page = site.items[id];
    var my_path = "/" + get_path(site, page);

    if (content)
    {
        t = t.replace("{{{page.content}}}", content);
    }

    if (page.title)
    {
        t = t.replace(/{{{page.title}}}/g, page.title);
        t = t.replace(/{{{article.title}}}/g, '<p class="ArticleDate" align=right>' + page.datefiled + '</p><h1>' + page.title + '</h1>');
    }
    else
    {
        t = t.replace(/{{{page.title}}}/g, site.title);
        t = t.replace(/{{{article.title}}}/g, "");
    }

    t = t.replace(/{{{haloscan}}}/g, "");
    t = t.replace(/{{{site.title}}}/g, site.title);
    t = t.replace(/{{{site.copyright}}}/g, site.copyright);
    t = t.replace(/{{{site.tagline}}}/g, site.tagline);

    var links = t.match(/{{{link:id='\d+'}}}/g);
    if (links)
    {
        for (var i=0; i<links.length; i++)
        {
            var link_id = links[i].match(/\d+/);
            var link_it = site.items[link_id];
            var other_path = "/" + get_path(site, link_it);

            var str_link = make_link(my_path, other_path);

            t = t.replace(links[i], str_link);
        }
    }

    return t;
}

var site = sg.from_json(sg.file.read("data/esbma.json"));
for (var id in site.items)
{
    site.items[id].id = id;
}
var dest = "pub_" + sg.gid().substr(1,8);

sg.fs.mkdir(dest);

for (var id in site.items)
{
    var it = site.items[id];
    var path = dest + "/" + get_path(site, it);
    var my_path = "/" + get_path(site, it);

    if (it.active)
    {
        if ("dir" == it.type)
        {
            sg.fs.mkdir(path);
        }
        else if ("blob" == it.type)
        {
            //sg.exec("cp", "data/" + id + ".bin", path);
            print("cp " + "data/" + id + ".bin " + path);
        }
        else if ("js" == it.type)
        {
            var js = sg.file.read("data/" + id + ".js");
            var content = "";
            eval(js);
            if (it.usetemplate)
            {
                var html = sg.file.read("data/template.html");
                var crunched = crunch(site, id, html, content);
                sg.file.write(path, crunched);
            }
            else
            {
                var crunched = crunch(site, id, content, null);
                sg.file.write(path, crunched);
            }
        }
        else if ("html" == it.type)
        {
            if (it.usetemplate)
            {
                var html = sg.file.read("data/template.html");
                var content = get_content(site, id);
                if (content)
                {
                    var crunched = crunch(site, id, html, content);
                    sg.file.write(path, crunched);
                }
            }
            else
            {
                var content = sg.file.read("data/" + id + ".html");
                var crunched = crunch(site, id, content, null);
                sg.file.write(path, crunched);
            }
        }
        else if ("xml" == it.type)
        {
        }
        else
        {
            throw it.type;
        }
    }
}

