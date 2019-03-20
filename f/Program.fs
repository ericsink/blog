// Learn more about F# at http://fsharp.org

open System
open System.IO
open System.Collections.Generic
open System.Text
open System.Linq
open System.Text.RegularExpressions;

open AngleSharp.Html.Parser
open AngleSharp.Html
open AngleSharp.Html.Dom.Events
open AngleSharp.Html.Dom
open AngleSharp.Dom

// ok so this is fairly ridiculous...
let my_get_front_matter src =
    match util.fm.get_front_matter src with
    | (Some front_matter, html) -> (front_matter, html)
    | (None, html) -> (null, html)

// TODO mv to util
let path_combine (a :string) (b :string) =
    if (a.Length = 0) then
        b
    elif (b.Substring(0, 1) = "/") then
        b
    elif (a.Substring(a.Length - 1, 1) = "/") then
        a + b
    else
        a + "/" + b;

let links = Dictionary<string,HashSet<string>>()
let all_items = HashSet<string>()
let all_keywords = HashSet<string>()

let add_link (from :string) (dest :string) =
    if not (links.ContainsKey(from)) then
        links.Add(from, HashSet<string>())
    links.[from].Add(dest) |> ignore

let dump (from :string) (path :string) =
    if path <> null then
        if (path.StartsWith("http:")) then
            ()
        elif (path.StartsWith("https:")) then
            ()
        elif (path.StartsWith("ftp:")) then
            ()
        elif (path.StartsWith("mailto:")) then
            ()
        //elif (path.StartsWith("/")) then
            //()
        elif (path.StartsWith("#")) then
            ()
        else
            //printfn "    %s" path
            add_link from path

let rec find_links (from :string) (n: INode) =
    if (n :? IHtmlImageElement) then
        let a = n :?> IHtmlImageElement
        let path = a.GetAttribute("src")
        dump from path

    if (n :? IHtmlAnchorElement) then
        let a = n :?> IHtmlAnchorElement
        let path = a.GetAttribute("href")
        dump from path

    if n.HasChildNodes then
        for sub in n.ChildNodes do
            find_links from sub

let do_file_links (url_dir :string) (f :string) =
    let name = Path.GetFileName(f)
    let src = File.ReadAllText(f)
    let (front_matter, html) = my_get_front_matter src
    let url_path = path_combine url_dir name
    //printfn "%s" url_path
    let parser = HtmlParser()
    let dom = parser.ParseDocument("<html><body></body></html>")
    let nodes = parser.ParseFragment(html, dom.Body)
    for n in nodes do
        find_links url_path n

// <p><span class="ArticleTitle">Crabby is as crabby does</span><br><span class="ArticleDate">10 Oct 2003</span></p>

let do_file_old_item (f :string) =
    let dir = Path.GetDirectoryName(f)
    let name = Path.GetFileName(f)
    let src = File.ReadAllText(f)
    let (front_matter, html) = my_get_front_matter src
    let expr = """<p><span class="ArticleTitle">(?<title>[^<]*)</span><br><span class="ArticleDate">(?<date>[^<]+)</span></p>"""
    let regx = Regex(expr)
    let a = regx.Matches(html);
    if (a <> null) && (a.Count = 1) then
        let m = a.First()
        let title = m.Groups.["title"].Value.Trim()
        let date = m.Groups.["date"].Value
        //printfn "%s is %s: %s" name date title
        front_matter.["layout"] <- "post"
        let fixed_title =
            if title.Length = 0 then
                sprintf "Blog entry on %s" date
            else
                title
        front_matter.["title"] <- fixed_title
        let d = DateTime.Parse(date)
        let date_formatted = d.ToString("yyyy-MM-dd") + " 12:00:00"
        front_matter.["date"] <- date_formatted
        let new_html = html.Replace(m.Value, "")
        util.fm.write_with_front_matter f front_matter new_html

let just_count (html :string) (s :string) =
    let regx = Regex(s)
    let a = regx.Matches(html);
    if a <> null then
        a.Count
    else
        0

let do_file_dedup (f :string) =
    let dir = Path.GetDirectoryName(f)
    let name = Path.GetFileName(f)
    let src = File.ReadAllText(f)
    let (front_matter, html) = my_get_front_matter src
    let count_blurbs = just_count html "ArticleBlurbCell"
    let expr = """<tr class="ArticleBlurbCell"><td colspan="3">&nbsp;<a name="(?<id>[0-9]+)" href="(?<href>[^"]+)">"""
    let regx = Regex(expr)
    let a = regx.Matches(html);
    if (a <> null) && (a.Count > 0) then
        printfn "%s claims to contain %d items" name a.Count
        if a.Count <> count_blurbs then
            printfn "    but count_blurbs is %d" count_blurbs
        let mutable total_other_len = 0
        for m in a do
            let id = m.Groups.["id"].Value
            let other_name = m.Groups.["href"].Value
            let other_path = Path.Combine(dir, other_name)
            if (File.Exists(other_path)) then
                let other = File.ReadAllText(other_path)
                let len_other = other.Length
                total_other_len <- total_other_len + len_other
                printfn "    %s" other_name
            else
                printfn "    %s not found" other_name
        let len_src = src.Length
        let len_diff = len_src - total_other_len
        let abs_diff = Math.Abs(len_diff)
        let each = abs_diff / count_blurbs
        printfn "    ---- len_diff %d (%d each)" abs_diff each
        if each > 275 then
            printfn "    big"
    else
        printfn "%s has no dup" name

let do_file_parse f =
    let src = File.ReadAllText(f)
    let (front_matter, html) = my_get_front_matter src
    if front_matter <> null then
        let parser = HtmlParser()
        parser.Error.Add(
            fun s -> 
                let e = s :?> HtmlErrorEvent
                printfn "%d: %s" e.Code e.Message
            )
        let dom = parser.ParseDocument("<html><body></body></html>")
        let nodes = parser.ParseFragment(html, dom.Body)
        let sw = new StringWriter()
        let fmt = PrettyMarkupFormatter()
        for n in nodes do
            n.ToHtml(sw, fmt)
        let new_html = sw.ToString()
        util.fm.write_with_front_matter f front_matter new_html
    
let do_file_rel f =
    let src = File.ReadAllText(f)
    let (front_matter, html) = my_get_front_matter src
    if front_matter <> null then
        let new_html = html.Replace("""href="../entries/""", """href="/entries/""").Replace("""href="scm/""", """href="/scm/""").Replace("""href="../item_""", """href="/item_""").Replace("""href="../articles/""", """href="/articles/""")
        if new_html <> html then
            util.fm.write_with_front_matter f front_matter new_html
    
let do_file_rel2 f =
    let src = File.ReadAllText(f)
    let (front_matter, html) = my_get_front_matter src
    if front_matter <> null then
        let new_html = html.Replace("""href="item_1""", """href="/item_1""")
        if new_html <> html then
            util.fm.write_with_front_matter f front_matter new_html
    
let do_file_rel3 f =
    let src = File.ReadAllText(f)
    let (front_matter, html) = my_get_front_matter src
    if front_matter <> null then
        let new_html = html.Replace("""src="smiley.gif""", """src="/smiley.gif""")
        if new_html <> html then
            util.fm.write_with_front_matter f front_matter new_html
    
let do_file_find_keywords f =
    let src = File.ReadAllText(f)
    let (front_matter, html) = my_get_front_matter src
    if front_matter <> null then
        if (front_matter.ContainsKey("keywords")) then
            let s = front_matter.["keywords"].Split(' ')
            for k in s do
                let k2 = k.Trim()
                all_keywords.Add(k2) |> ignore

let fix_one_kw (s :string) (k : string) =
    let without = k.Replace("(", "").Replace(")", "")
    let mutable t = s
    t <- t.Replace(k, without)
    t


let fix_keywords (s :string) =
    let mutable t = s
    t <- fix_one_kw t "(ignoretoc)"
    t <- fix_one_kw t "(dev)"
    t <- fix_one_kw t "(mfg_strategy)"
    t <- fix_one_kw t "(book_marketing)"
    t <- fix_one_kw t "(cornsharp)"
    t <- fix_one_kw t "(laughs)"
    t <- fix_one_kw t "(xamarin)"
    t <- fix_one_kw t "(sourcegear)"
    t <- fix_one_kw t "(book_biz)"
    t <- fix_one_kw t "(book_dev)"
    t <- fix_one_kw t "(mfg_marcomm)"
    t <- fix_one_kw t "(topic)"
    t <- fix_one_kw t "(biz)"
    t <- fix_one_kw t "(bos)"
    t <- fix_one_kw t "(sol)"
    t <- fix_one_kw t "(fsharp)"
    t <- fix_one_kw t "(sqlite)"
    t <- fix_one_kw t "(dotnet)"
    t <- fix_one_kw t "(wpf)"
    t <- fix_one_kw t "(rust)"
    t <- fix_one_kw t "(sqlitepclraw)"
    t <- fix_one_kw t "(law)"
    t <- fix_one_kw t "(mssql_to_sqlite)"
    t <- fix_one_kw t "(scm)"
    t

let do_file_fix_keywords f =
    let src = File.ReadAllText(f)
    let (front_matter, html) = my_get_front_matter src
    if front_matter <> null then
        if (front_matter.ContainsKey("keywords")) then
            let kw = front_matter.["keywords"]
            let kw_fixed = fix_keywords kw
            front_matter.["keywords"] <- kw_fixed
        let html_fixed = fix_keywords html
        util.fm.write_with_front_matter f front_matter html_fixed

let do_file_url f =
    let src = File.ReadAllText(f)
    let (front_matter, html) = my_get_front_matter src
    if front_matter <> null then
        let new_html = html.Replace("software.ericsink.com", "ericsink.com").Replace("www.ericsink.com", "ericsink.com").Replace("http://ericsink.com", "https://ericsink.com").Replace("href=\"https://ericsink.com/", "href=\"/")
        if new_html <> html then
            util.fm.write_with_front_matter f front_matter new_html
    
let rec do_dir (url_dir :string) (from :string) =
    for f in (Directory.GetFiles(from)) do
        let name = Path.GetFileName(f)
        let url_path = path_combine url_dir name
        all_items.Add(url_path) |> ignore
        do_file_fix_keywords f

    for from_sub in (Directory.GetDirectories(from)) do
        let name = Path.GetFileName(from_sub)
        // TODO skip _layouts at every depth, or just at the top?
        if (name <> "_layouts") then
            let url_subdir = path_combine url_dir name
            do_dir url_subdir from_sub

[<EntryPoint>]
let main argv =
    let dir_src = argv.[0]
    do_dir "/" dir_src
    all_items.Add("/index.html") |> ignore
    for k in all_keywords do
        printfn "%s" k
    (*
    let all_links = HashSet<string>()
    for path in links.Keys do
        let h = links.[path]
        for a in h do
            all_links.Add(a) |> ignore
            if not (all_items.Contains(a)) then
                printfn "broken from %s to %s" path a
    *)
    (*
    for a in all_items do
        if not (all_links.Contains(a)) then
            printfn "never linked %s" a
    *)
    0 // return an integer exit code

