
open System
open System.IO
open System.Linq
open System.Collections.Generic
open System.Text
open System.Text.RegularExpressions;

let get_front_matter (s :string) =
    let marker_front_lf = "---\n"
    let marker_front_crlf = "---\r\n"
    if (s.StartsWith(marker_front_lf)) || (s.StartsWith(marker_front_crlf)) then
        // first remove that first line
        let s2 = 
            // whether it was lf or crlf, we can just find the lf and chop there
            let n = s.IndexOf("\n") // TODO could assert, must be > 0
            s.Substring(n + 1)

        // now find the other marker

        let (n, len) = 
            // the second marker should match \n--- for either EOL
            let marker_back_lf = "\n---\n"
            let marker_back_crlf = "\n---\r\n"
            let n_lf = s2.IndexOf(marker_back_lf)
            let n_crlf = s2.IndexOf(marker_back_crlf)
            if n_lf > 0 then
                if n_crlf > 0 then
                    // found 2nd marker in BOTH lf and crlf forms?
                    // take the first one
                    if n_lf < n_crlf then
                        (n_lf, marker_back_lf.Length)
                    else
                        (n_crlf, marker_back_crlf.Length)
                else
                    (n_lf, marker_back_lf.Length)
            elif n_crlf > 0 then
                (n_crlf, marker_back_crlf.Length)
            else
                raise (Exception("second front matter marker not found"))

        let s3 = s2.Substring(0, n)
        let remain = s2.Substring(n + len)

        // split on lf should work for either eol here.
        // the cr will remain, but it gets trimmed out.
        let a = s3.Split("\n")
        let d = Dictionary<string,string>()
        for pair in a do
            if pair.Length > 0 then
                let n_colon = pair.IndexOf(":")
                let k = pair.Substring(0, n_colon).Trim()
                let v = pair.Substring(n_colon + 1).Trim()
                // TODO we may want to allow v to be empty string or null
                if (k.Length > 0) && (v.Length > 0) then
                    d.Add(k, v)
        (*
        if d.ContainsKey("date") then
            let s = d.["date"]
            let dt = DateTime.Parse(s)
            let normalized = dt.ToString("yyyy-MMM-dd HH:mm:ss")
            d.["date"] <- normalized
        *)
        (d, remain)
    else
        (null, s)

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

let make_front_page template dir_src (items: Dictionary<string,Dictionary<string,string>>) = 
    let add (sb :StringBuilder) (s :string) =
        sb.Append(s) |> ignore

    let content = StringBuilder()

    add content "</td></tr>"

    let a = items.OrderByDescending(fun kv -> kv.Value.["date"]).Take(10).ToList()

    for kv in a do
        let path = kv.Key
        let title = if kv.Value.ContainsKey("title") then kv.Value.["title"] else null
        let date = kv.Value.["date"]

        //printfn "path: %s" path
        // TODO windows-specific code below
        let path_fixed = path.Substring(1).Replace("/", "\\")
        //printfn "path_fixed: %s" path_fixed
        let path_content = Path.Combine(dir_src, path_fixed)
        //printfn "path_content: %s" path_content
        let html = File.ReadAllText(path_content)
        let (front_matter, my_content) = get_front_matter html

        add content "<tr><td><span align=\"right\" class=ArticleDate>"
        add content (blog.fsfun.format_date(date))
        add content "</span><br><a class=\"ArticleTitleGreen\" href=\""
        add content path
        add content "\">"
        add content title
        add content "</a><br><br></td></tr><tr><td>"
        add content my_content
        add content "<P> </td></tr> <tr><td>&nbsp;</td></tr>"

    add content "<tr><td bgcolor=\"white\">"

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

        //printfn "path: %s" path
        // TODO windows-specific code below
        let path_fixed = path.Substring(1).Replace("/", "\\")
        //printfn "path_fixed: %s" path_fixed
        let path_content = Path.Combine(dir_src, path_fixed)
        //printfn "path_content: %s" path_content
        let html = File.ReadAllText(path_content)
        let (front_matter, my_content) = get_front_matter html
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
        add content (blog.fsfun.format_date_rss(date))
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
        printfn "different %s -- %s" src dest
        printfn "copy %s" dest
        File.Copy(src, dest)

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
            let (template_front, template_html) = get_front_matter layout
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
        let (page_front_matter, src_content) = get_front_matter html
        if page_front_matter <> null then

            if not (page_front_matter.ContainsKey("title")) then
                page_front_matter.Add("title", "Eric Sink")

            let layout_name = get_layout_name page_front_matter
            let after_crunch = wrap layout_name page_front_matter src_content layouts
            write_if_changed after_crunch dest_path

            let url_path = blog.fsfun.path_combine url_dir name
            items.Add(url_path, page_front_matter)
        else
            copy_if_changed from dest_path
    else
        copy_if_changed from dest_path

let rec do_clean (url_dir :string) (skip :HashSet<string>) (src :string) (dest :string) =
    for f in (Directory.GetFiles(dest)) do
        let name = Path.GetFileName(f)
        let src_path = Path.Combine(src, name)
        let path = blog.fsfun.path_combine url_dir name
        if not (skip.Contains(path)) then
            if not (File.Exists(src_path)) then
                printfn "delete %s" f
                File.Delete(f)

    for sub in (Directory.GetDirectories(dest)) do
        let name = Path.GetFileName(sub)
        let src_sub = Path.Combine(src, name)
        let url_subdir = blog.fsfun.path_combine url_dir name
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
            let url_subdir = blog.fsfun.path_combine url_dir name
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

