// Learn more about F# at http://fsharp.org

open System
open System.IO
open System.Collections.Generic
open System.Text
open System.Text.RegularExpressions;

open AngleSharp.Html.Parser
open AngleSharp.Xhtml
open AngleSharp.Html
open AngleSharp.Html.Dom.Events

let do_file_dedup (f :string) =
    let dir = Path.GetDirectoryName(f)
    let name = Path.GetFileName(f)
    let src = File.ReadAllText(f)
    let (front_matter, html) = util.fm.get_front_matter src
    let expr = """item_[0-9]+.html"""
    let regx = Regex(expr)
    let a = regx.Matches(html);
    if a <> null then
        for m in a do
            let other_name = m.Value
            let other_path = Path.Combine(dir, other_name)
            if (File.Exists(other_path)) then
                printfn "%s is %s" name other_name
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

