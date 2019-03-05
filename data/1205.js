content += '<P>I love to play cards. I\'ve spent many hours sitting around a kitchen table playing pinochle, euchre or spades.</p><P>But I think my favorite card game is bridge. More specifically, the variant of bridge which fascinates me is called "duplicate". The basic idea of duplicate bridge is that your score is a function of how well you play your cards as compared to how the other teams played the exact same cards. <P>Just to be clear, let me repeat: In duplicate bridge, you are playing the same cards as your opponents. The luck of the deal is basically eliminated. <P>You have 13 cards in your hand, so there are 13 "tricks" available to win. If you are dealt excellent cards, there is no particular reason to get excited. Yes, your cards will take lots of tricks, but that\'s not the point. The issue is whether you take as many tricks as the other people who play those exact same cards. If you take nine tricks but somebody else finds a way to take ten, you lose. <P>Duplicate bridge is a brutal game. Every small mistake can be very costly.&nbsp; I do like to go to the local bridge club sometimes, but I&nbsp;usually end up in last place. At the end of the evening, I review each hand and figure out what went wrong.&nbsp; Even though I am terribly bad at this game, I still enjoy it because every game is such a learning experience. <P>I often wonder what other pursuits would be like if they had to operate under the same rules:&nbsp; Resources and context do not change -- the only variable is the ability of the person managing those resources. <P>These questions become particularly interesting to me when asked in the field of software product management. For a given piece of technology or code, what would happen if somebody else were managing it? <P><LI>If I were managing the <A href="http://www.borland.com/delphi/">Delphi</A> product instead of Borland, could I do a better job? <LI>If <A href="http://www.joelonsoftware.com/">Joel Spolsky</A> were managing Vault instead of me, would the product have more users? <LI>If Sun were to hand the management of <A href="http://java.sun.com/">Java</A> over to a committee of monkeys, would it be more successful?&nbsp; :-) <P>Alas, these hypothetical fantasies are not going to happen. That\'s unfortunate. If ISVs had to play duplicate, we would all quickly learn a lot. First, the sheer volume of our stupid mistakes would be exposed, and we would quickly learn how very bad we all are at product management. And after that, we would start learning the fine points.&nbsp; Instead of just chalking up every failure to the fault of "bad marketing", we would review each decision and figure out exactly where and when we played the wrong card.</P><P>We can\'t play duplicate with our shrinkwrap products, but we can learn the fine points of marketing. Marketing is not some vague and fuzzy realm where only luck matters. There are principles which can be learned and applied. <P>Al Ries and Jack Trout&nbsp;refer to these principles as&nbsp;"laws". Their book, entitled "<A href="http://www.amazon.com/exec/obidos/ASIN/0887306667/sawdust08-20">The 22 Immutable Laws of Marketing</A>" is one of my favorites. And I couldn\'t help but notice that there are exactly 22 weekdays in the month of June. So...</P><P>During the month of June,&nbsp;I plan to&nbsp;post a brief blurb each weekday.&nbsp; For each of the 22 laws, I will summarize the main point and draw a connection to the software industry.&nbsp; My entries will not be complete discussions of the topic.&nbsp;&nbsp;Interested readers should read the book&nbsp;and follow along. </P>';

content += '<UL>';

var local_items = find_by_keyword_match(site, ['(law)']);
sort_items_id_asc(local_items);
for (var local_i=0; local_i<local_items.length; local_i++)
{
    var local_it = local_items[local_i];
    var local_path = "/" + get_path(site, local_it);
    var local_link = make_link(my_path, local_path);

    content += '<li><a href="';
    content += local_link;
    content += '">';
    content += local_it.title;
    content += '</a></li>';
}
content += '</UL>';

content += '<P>For those who prefer to read this series of postings in printed form, a <A href="{{{link:id=\'1043\'}}}">PDF version</A> is available.</P>';

content += '<br/>';

