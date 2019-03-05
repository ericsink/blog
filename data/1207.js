content += '<p>This page serves as the table of contents for&nbsp;my series of articles entitled "Marketing for Geeks".&nbsp; The central theme here is that if we demystify marketing, it can be competently done by technical people.&nbsp; The series is still being written, with new articles coming soon to an RSS feed near you.</p>';

content += '<p>In most <a href="Small_ISV_Defined.html">small ISVs</a>, it\'s important for at least some of the&nbsp;developers to have an understanding of basic marketing.&nbsp;&nbsp;However, most&nbsp;geeks tend to shy away from marketing, citing their lack of creativity and graphic design skills.&nbsp; But these are typically not the differentiators which determine whether marketing is competent or not.&nbsp; Marketing efforts tend to succeed or fail on their strategy, not on their artwork.&nbsp; In fact, many teams can improve their marketing simply by realizing that marketing, like software development, has two distinct phases.</p>';

content += '<p><font face="Arial,Helvetica,sansserif" size="4">The Two Phases of Marketing</font></p>';

content += '<p>When we build software, we typically have a design phase, followed by an implementation phase.&nbsp; In the design phase, we carefully figure out exactly what we want to do.&nbsp; In the implementation phase, we do it.</p>';

content += '<p>Likewise, marketing has a strategic phase, followed by a&nbsp;communication phase.</p>';

content += '<ul> <li>The strategic phase is analogous to the design phase of building software.&nbsp; (In fact, they are related and must usually be done together.)<br /><br /> </li> <li>The communication phase is analogous to the implementation phase of building software.&nbsp; We call this set of activities "marketing communications", or "marcomm" for short.  </li> </ul>';

content += '<p>I find it interesting that although marketing people and technical people often think they have nothing in common, both groups naturally try to weasel out of doing their first phase.&nbsp; Maverick programmers don\'t want to write specs and do design.&nbsp; They simply want to write code.&nbsp; Similarly, marketing people&nbsp;often prefer&nbsp;to plunge headfirst into creating messages, taglines and ad campaigns.&nbsp; In either case, skipping the first phase will get you the instant gratification of visible results, but you\'ll&nbsp;have all kinds of trouble down the road.</p>';

content += '<p><font face="Arial,Helvetica,sansserif" size="4">Articles about Strategy</font></p>';

var local_items = find_by_keyword_match(site, ['(mfg_strategy)']);
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

content += '<p style="MARGIN-RIGHT: 0px"><font face="Arial,Helvetica,sansserif" size="4">Articles about Marcomm</font></p>';

var local_items = find_by_keyword_match(site, ['(mfg_marcomm)']);
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

