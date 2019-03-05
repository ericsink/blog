content += '<p>The following articles originally appeared on the MSDN website.&nbsp; From October 2003 to April 2005, I wrote a column there called "The Business of Software".&nbsp; Thanks to <a href="http://www.sellsbrothers.com/">Chris Sells</a> for giving me the opportunity to write for MSDN, and to Maurine Bryan for doing a great job with the editing.</p> <hr /> <br />';

var local_items = find_by_keyword_match(site, ['(bos)']);
sort_items_datefiled_desc(local_items);
for (var local_i=0; local_i<local_items.length; local_i++)
{
    var local_it = local_items[local_i];
    var local_path = "/" + get_path(site, local_it);
    var local_link = make_link(my_path, local_path);

    content += '<span class="DayPageArticleTitle"><a href="';
    content += local_link;
    content += '">';
    content += local_it.title;
    content += '</a></span>';
    content += ' (<span class=ArticleDate>';
    content += format_date(local_it.datefiled);
    content += '</span>)';
    content += '<blockquote>';
    if (local_it.teaser)
    {
        content += local_it.teaser;
    }
    content += '</blockquote>';
}

