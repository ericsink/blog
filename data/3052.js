
content += '<p>This is a series of articles about using SQLite when developing mobile apps.  The target audience is folks who are coming from the world of SQL Server and .NET.</p>';

content += "<h2>Table of Contents</h2>";

content += "<p><i>Anything 'not written yet' is subject to change.  Entries are listed in a somewhat logical order for their content, roughly proceeding from 'big picture stuff' to 'technical details', but I am not promising to actually write them in that order.</i>  <tt>:-)</tt></p>";

content += '<UL>';

var local_items = find_by_keyword_match(site, ['(mssql_to_sqlite)']);
sort_items_id_asc(local_items);
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

    if (local_it.active)
    {
        if (my_content)
        {
            var local_path = "/" + get_path(site, local_it);
            var local_link = make_link(my_path, local_path);

            content += '<li><p><a href="';
            content += local_link;
            content += '">';
            content += local_it.title;
            content += '</a></p></li>';
            if (local_it.teaser)
            {
                content += '<blockquote>';
                content += local_it.teaser;
                content += '</blockquote>';
            }
        }
        else
        {
            content += '<li><p>(not written yet) ';
            content += local_it.title;
            content += '</p></li>';
            if (local_it.teaser)
            {
                content += '<blockquote>';
                content += local_it.teaser;
                content += '</blockquote>';
            }
        }
    }
}
content += '</UL>';

content += '<p>&nbsp;</p>';

