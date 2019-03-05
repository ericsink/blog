content += '<p>Note:  This series of articles provides an introduction to source control using "centralized" version control tools such as SourceGear Vault.  You may also be interested in my book, "Version Control by Example".  Its content is somewhat more oriented toward "decentralized" version control tools.  Its home page is <a href="http://www.ericsink.com/vcbe">http://www.ericsink.com/vcbe</a>.</p> <hr/> <blockquote>';

var local_items = find_by_keyword_match(site, ['(scm)']);
sort_items_id_asc(local_items);
for (var local_i=0; local_i<local_items.length; local_i++)
{
    var local_it = local_items[local_i];
    var local_path = "/" + get_path(site, local_it);
    var local_link = make_link(my_path, local_path);

    content += '<span class="DayPageArticleTitle"><a href="';
    content += local_link;
    content += '">';
    content += local_it.title;
    content += '</a></span><br /><br />';
    content += local_it.teaser;
    content += '<br /><br />';
}
content += '</blockquote> <p> </p>';

