
open System
open System.IO
open System.Linq
open System.Collections.Generic
open System.Text
open System.Text.RegularExpressions;

// an implementation of Path.Combine which always uses fwd slash
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

// TODO this is probably too strict
// just use DateTime.Parse?
let parse_date (s :string) =
    let twoparts = s.Split(' ')
    let dateparts = twoparts.[0].Split('-')
    let timeparts = twoparts.[1].Split(':')
    let year = System.Int32.Parse(dateparts.[0])
    let month = System.Int32.Parse(dateparts.[1])
    let day = System.Int32.Parse(dateparts.[2])
    let hour = System.Int32.Parse(timeparts.[0])
    let min = System.Int32.Parse(timeparts.[1])
    let sec = System.Int32.Parse(timeparts.[2])

    let d = new System.DateTime(year, month, day, hour, min, sec, 0)
    d

let format_date_rss s =
    //<pubDate>{{{loop.datefiled:format='ddd, dd MMM yyyy HH:mm:ss CST'}}}</pubDate>
    let d = parse_date(s)
    d.ToString("ddd, dd MMM yyyy HH:mm:ss CST")

let format_date s =
    let d = parse_date(s)
    d.ToString("dddd, d MMMM yyyy")

let crunch (html :string) (content :string) (pairs :Dictionary<string,Dictionary<string,string>>) =
    let mutable t = html

    let expr = """{{(?<v>[^{}]+)}}"""
    let regx = Regex(expr)
    let a = regx.Matches(t);
    if a <> null then
        for m in a do
            let v = m.Groups.["v"].Value.Trim().ToLower()
            let replacement =
                if v = "content" then
                    // special case
                    content
                else
                    let parts = v.Split(".")
                    let section = parts.[0]
                    let k = parts.[1]
                    pairs.[section].[k]
            t <- t.Replace(m.Value, replacement)

    t

let add_pair (d: Dictionary<string,Dictionary<string,string>>) section k v =
    if not (d.ContainsKey(section)) then
        d.Add(section, Dictionary<string,string>())
    d.[section].Add(k, v)

let add_site_defaults (d: Dictionary<string,Dictionary<string,string>>) =
    add_pair d "site" "title" "Eric Sink"
    add_pair d "site" "tagline" "SourceGear Founder"
    add_pair d "site" "copyright" "Copyright 2001-2019 Eric Sink. All Rights Reserved"

let read_from_src (dir_src :string) (uri_path :string) =
    // TODO windows-specific code below
    let path_fixed = uri_path.Substring(1).Replace("/", "\\")
    let path_content = Path.Combine(dir_src, path_fixed)
    let html = File.ReadAllText(path_content)
    html

let make_front_page template dir_src (items: Dictionary<string,Dictionary<string,string>>) = 
    let add (sb :StringBuilder) (s :string) =
        sb.Append(s) |> ignore

    let content = StringBuilder()

    let a = items.OrderByDescending(fun kv -> kv.Value.["date"]).Take(10).ToList()

    for kv in a do
        let path = kv.Key
        let title = if kv.Value.ContainsKey("title") then kv.Value.["title"] else null
        let date = kv.Value.["date"]

        let html = read_from_src dir_src path
        let (front_matter, my_content) = util.fm.get_front_matter html

        let line1 = sprintf """<p class="ArticleDate" align=right>%s</p><h1><a href="%s">%s</a></h1>""" (format_date date) path title

        add content line1
        add content "\n"
        add content my_content
        add content "\n"
        add content "<hr/>\n"

    let s = content.ToString()
    let pairs = Dictionary<string,Dictionary<string,string>>()
    add_site_defaults pairs
    add_pair pairs "page" "title" "Eric Sink"
    add_pair pairs "page" "title_markup" ""
    let result = crunch template s pairs
    result

let make_rss dir_src (items: Dictionary<string,Dictionary<string,string>>) = 
    let add (sb :StringBuilder) (s :string) =
        sb.Append(s) |> ignore

    // TODO maybe we should use XmlWriter for this

    let content = StringBuilder()

    add content "<?xml version=\"1.0\" encoding=\"iso-8859-1\" ?>"
    add content "<rss version=\"2.0\">"
    add content "<channel>"
    add content "<title>{{site.title}}</title>"
    add content "<link>https://ericsink.com/</link>"
    add content "<description>{{site.tagline}}</description>"
    add content "<copyright>{{site.copyright}}</copyright>"
    add content "<generator>mine</generator>"

    let a = items.OrderByDescending(fun kv -> kv.Value.["date"]).Take(10).ToList()

    for kv in a do
        let path = kv.Key
        let title = if kv.Value.ContainsKey("title") then kv.Value.["title"] else null
        let date = kv.Value.["date"]

        let html = read_from_src dir_src path
        let (front_matter, my_content) = util.fm.get_front_matter html
        let local_link = "https://ericsink.com" + path

        add content "<item>"
        add content "<title>"
        add content title
        add content "</title>"
        add content "<guid>"
        add content local_link
        add content "</guid>"
        add content "<link>"
        add content local_link
        add content "</link>"
        add content "<pubDate>"
        add content (format_date_rss(date))
        add content "</pubDate>"
        add content "<description>"
        add content "<![CDATA["
        add content my_content
        add content "]]>"
        add content "</description>"
        add content "</item>"

    add content "</channel>"
    add content "</rss>"

    let s = content.ToString()
    let pairs = Dictionary<string,Dictionary<string,string>>()
    add_site_defaults pairs
    let result = crunch s null pairs
    result

let write_if_changed content dest =
    let different =
        if (File.Exists(dest)) then
            let cur = File.ReadAllText(dest)
            cur <> content
        else
            true
    if different then
        printfn "write %s" dest
        File.WriteAllText(dest, content)

let copy_if_changed src dest =
    let different =
        if (File.Exists(dest)) then
            let fi_src = FileInfo(src)
            let fi_dest = FileInfo(dest)
            if fi_src.Length <> fi_dest.Length then
                true
            else
                let ba_src = File.ReadAllBytes src
                let ba_dest = File.ReadAllBytes dest
                ba_src <> ba_dest
        else
            true
    if different then
        printfn "copy %s" dest
        File.Copy(src, dest, true)

let get_layout_name (front_matter :Dictionary<string,string>) =
    if front_matter = null then
        null
    else
        if (front_matter.ContainsKey("layout")) then
            let layout_name = front_matter.["layout"]
            if layout_name = "null" then
                null
            else
                layout_name
        else
            null

let rec wrap (layout_name :string) (page_front_matter :Dictionary<string,string>) (src_content :string) (layouts: Dictionary<string,string>) =
    let (layout_front_matter, before_crunch, content) =
        if layout_name = null then
            (null, src_content, null)
        else
            let layout = layouts.[layout_name]
            let (template_front, template_html) = util.fm.get_front_matter layout
            (template_front, template_html, src_content)

    let pairs = Dictionary<string,Dictionary<string,string>>()
    add_site_defaults pairs
    pairs.Add("page", page_front_matter)
    pairs.Add("layout", layout_front_matter)

    let after_crunch = crunch before_crunch content pairs
    let next_layout_name = get_layout_name layout_front_matter
    if next_layout_name <> null then
        wrap next_layout_name page_front_matter after_crunch layouts
    else
        after_crunch

let do_file (url_dir :string) (from :string) (dest_dir :string) (layouts: Dictionary<string,string>) (items: Dictionary<string,Dictionary<string,string>>) =
    let name = Path.GetFileName(from)
    let dest_path = Path.Combine(dest_dir, name)
    if (from.EndsWith(".html")) then
        let html = File.ReadAllText(from)
        let (page_front_matter, src_content) = util.fm.get_front_matter html
        if page_front_matter <> null then

            if not (page_front_matter.ContainsKey("title")) then
                page_front_matter.Add("title", "Eric Sink")

            let layout_name = get_layout_name page_front_matter
            let after_crunch = wrap layout_name page_front_matter src_content layouts
            write_if_changed after_crunch dest_path

            let url_path = path_combine url_dir name
            items.Add(url_path, page_front_matter)
        else
            copy_if_changed from dest_path
    else
        copy_if_changed from dest_path

let rec do_clean (url_dir :string) (skip :HashSet<string>) (src :string) (dest :string) =
    for f in (Directory.GetFiles(dest)) do
        let name = Path.GetFileName(f)
        let src_path = Path.Combine(src, name)
        let path = path_combine url_dir name
        if not (skip.Contains(path)) then
            if not (File.Exists(src_path)) then
                printfn "delete %s" f
                File.Delete(f)

    for sub in (Directory.GetDirectories(dest)) do
        let name = Path.GetFileName(sub)
        let src_sub = Path.Combine(src, name)
        let url_subdir = path_combine url_dir name
        do_clean url_subdir skip src_sub sub

let rec do_dir (url_dir :string) (from :string) (dest_dir :string) (layouts: Dictionary<string,string>) (items: Dictionary<string,Dictionary<string,string>>) =
    Directory.CreateDirectory(dest_dir) |> ignore
    for f in (Directory.GetFiles(from)) do
        do_file url_dir f dest_dir layouts items

    for from_sub in (Directory.GetDirectories(from)) do
        let name = Path.GetFileName(from_sub)
        // TODO skip _layouts at every depth, or just at the top?
        if name <> "_layouts" then
            let dest_sub = Path.Combine(dest_dir, name)
            let url_subdir = path_combine url_dir name
            do_dir url_subdir from_sub dest_sub layouts items

[<EntryPoint>]
let main argv =
    let dir_src = argv.[0]
    let dir_dest = argv.[1]
    let layouts =
        let dir_layouts = Path.Combine(dir_src, "_layouts")
        let d = Dictionary<string,string>()
        for f in (Directory.GetFiles(dir_layouts)) do
            let basename = Path.GetFileNameWithoutExtension(f)
            let html = File.ReadAllText(f)
            // TODO get front matter from template
            d.Add(basename, html)
        d
    let items = Dictionary<string,Dictionary<string,string>>()
    do_dir "/" dir_src dir_dest layouts items

    let skip_on_clean = HashSet<string>()
    skip_on_clean.Add("/rss.xml")
    skip_on_clean.Add("/index.html")
    do_clean "/" skip_on_clean dir_src dir_dest

    let full_path_content = Path.GetFullPath(dir_src)

    let rss = make_rss full_path_content items
    let path_rss = Path.Combine(dir_dest, "rss.xml")
    write_if_changed rss path_rss

    let default_layout = layouts.["default"]
    let front_page = make_front_page default_layout full_path_content items
    let path_front_page = Path.Combine(dir_dest, "index.html")
    write_if_changed front_page path_front_page

    0 // return an integer exit code

