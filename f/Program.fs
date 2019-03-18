// Learn more about F# at http://fsharp.org

open System
open System.IO
open System.Collections.Generic
open System.Text
open System.Linq
open System.Text.RegularExpressions;

open AngleSharp.Html.Parser
open AngleSharp.Xhtml
open AngleSharp.Html
open AngleSharp.Html.Dom.Events

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
    let (front_matter, html) = util.fm.get_front_matter src
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
    let (front_matter, html) = util.fm.get_front_matter src
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
    
let do_file_url f =
    let src = File.ReadAllText(f)
    let (front_matter, html) = util.fm.get_front_matter src
    if front_matter <> null then
        let new_html = html.Replace("software.ericsink.com", "ericsink.com").Replace("www.ericsink.com", "ericsink.com").Replace("http://ericsink.com", "https://ericsink.com").Replace("href=\"https://ericsink.com/", "href=\"/")
        if new_html <> html then
            util.fm.write_with_front_matter f front_matter new_html
    
let rec do_dir (from :string) =
    for f in (Directory.GetFiles(from)) do
        let name = Path.GetFileName(f)
        if (name.StartsWith("200")) then
            do_file_dedup f

    for from_sub in (Directory.GetDirectories(from)) do
        let name = Path.GetFileName(from_sub)
        // TODO skip _layouts at every depth, or just at the top?
        if name <> "_layouts" then
            do_dir from_sub

[<EntryPoint>]
let main argv =
    let dir_src = argv.[0]
    do_dir dir_src
    0 // return an integer exit code

