{
    content += "</td></tr>";
    var local_items = find_by_keyword_block(site, ['(hide)','(sol)','(cornsharp)','(ignoretoc)']);
    sort_items_datefiled_desc(local_items);
    var sofar = 0;
    var local_i = 0;
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
            var local_link = make_link(my_path, local_path);

            content += '<tr><td><span align="right" class=ArticleDate>';
            content += format_date(local_it.datefiled);
            content += '</span><br><a class="ArticleTitleGreen" href="';
            content += local_link;
            content += '">';
            content += local_it.title;
            content += '</a><br><br></td></tr><tr><td>';
            content += my_content;
            content += '<P> </td></tr> <tr><td>&nbsp;</td></tr>';

            sofar++;

            if (sofar >= 10)
            {
                break;
            }
        }
    }
    content += '<tr><td bgcolor="white">';
}

