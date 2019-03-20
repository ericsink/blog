
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

// dir is a platform path
// uri_path is forward-slashes
// result is a platform path
let weird_path_combine (dir :string) (uri_path :string) =
    // TODO windows-specific code below
    let path_fixed = uri_path.Substring(1).Replace("/", "\\")
    let path = Path.Combine(dir, path_fixed)
    path

let read_from_src (dir_src :string) (uri_path :string) =
    let path_content = weird_path_combine dir_src uri_path
    let html = File.ReadAllText(path_content)
    html

let make_front_page template dir_src (items: Dictionary<string,Dictionary<string,string>>) = 
    let add (sb :StringBuilder) (s :string) =
        sb.Append(s) |> ignore

    let content = StringBuilder()

    let a = items.Where(fun kv -> kv.Value.ContainsKey("date")).OrderByDescending(fun kv -> kv.Value.["date"]).Take(10).ToList()

    for kv in a do
        let path = kv.Key
        let title = if kv.Value.ContainsKey("title") then kv.Value.["title"] else null
        let date = kv.Value.["date"]

        let html = read_from_src dir_src path
        let (_, my_content) = util.fm.get_front_matter html

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

    let a = items.Where(fun kv -> kv.Value.ContainsKey("date")).OrderByDescending(fun kv -> kv.Value.["date"]).Take(10).ToList()

    for kv in a do
        let path = kv.Key
        let title = if kv.Value.ContainsKey("title") then kv.Value.["title"] else null
        let date = kv.Value.["date"]

        let html = read_from_src dir_src path
        let (_, my_content) = util.fm.get_front_matter html
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
        None
    else
        if (front_matter.ContainsKey("layout")) then
            let layout_name = front_matter.["layout"]
            if layout_name = "null" then
                None
            else
                Some layout_name
        else
            None

let get_layout_name_opt (front_matter :Dictionary<string,string> option) =
    match front_matter with
    | Some front_matter -> get_layout_name front_matter
    | None -> None

let rec wrap (layout_name :string option) (page_front_matter :Dictionary<string,string>) (src_content :string) (layouts: Dictionary<string,string>) =
    let (layout_front_matter, before_crunch, content) =
        match layout_name with
        | Some layout_name ->
            let layout = layouts.[layout_name]
            let (template_front, template_html) = util.fm.get_front_matter layout
            (template_front, template_html, src_content)
        | None ->
            (None, src_content, null)

    let pairs = Dictionary<string,Dictionary<string,string>>()
    add_site_defaults pairs
    pairs.Add("page", page_front_matter)
    match layout_front_matter with
    | Some layout_front_matter ->
        pairs.Add("layout", layout_front_matter)
    | None ->
        ()

    let after_crunch = crunch before_crunch content pairs
    match get_layout_name_opt layout_front_matter with
    | Some next_layout_name ->
        wrap (Some next_layout_name) page_front_matter after_crunch layouts
    | None ->
        after_crunch

let make_toc (magic: Dictionary<string,string>) dir_src (items: Dictionary<string,Dictionary<string,string>>) = 
    let add (sb :StringBuilder) (s :string) =
        sb.Append(s) |> ignore

    let kw_include = magic.["keyword"]
    let showdate =
        if (magic.ContainsKey("showdate")) then
            magic.["showdate"] = "true"
        else
            true
    let showteaser =
        if (magic.ContainsKey("showteaser")) then
            magic.["showteaser"] = "true"
        else
            true
    let sortby =
        if (magic.ContainsKey("sortby")) then
            magic.["sortby"]
        else
            "date"

    // TODO allow multiple included keywords here?
    // TODO allow keyword exclusion here?

    let has_kw (fm :Dictionary<string,string>) (kw :string) =
        if (fm.ContainsKey("keywords")) then
            let keywords = fm.["keywords"]
            let a = keywords.Split(' ').Select(fun s -> s.Trim())
            let h = HashSet<string>()
            for k in a do
                h.Add(k) |> ignore
            let b = h.Contains(kw)
            b
        else
            false
        
    let filtered = items.Where(fun kv -> has_kw (kv.Value) kw_include)

    let a = filtered.OrderByDescending( fun kv -> if (kv.Value.ContainsKey(sortby)) then kv.Value.[sortby] else null )

    let content = StringBuilder()

    for kv in a do
        let path = kv.Key
        let title = kv.Value.["title"]

        add content "<div class=\"toc_item\">\n"

        if showdate then
            let date = kv.Value.["date"]
            sprintf "  <p class=\"toc_item_date\">%s</p>\n" (format_date date) |> add content

        sprintf "  <p class=\"toc_item_title\"><a href=\"%s\">%s</a></p>" path title |> add content

        if showteaser then
            if (kv.Value.ContainsKey("teaser")) then
                let teaser = kv.Value.["teaser"]
                sprintf "  <p class=\"toc_item_teaser\">%s</p>\n" teaser |> add content
            else
                sprintf "  <p class=\"toc_item_teaser\"></p>\n" |> add content
                () // TODO do we want an empty para for the teaser?

        add content "</div>\n"

    let s = content.ToString()
    s

let crunch_magic html dir_src (items: Dictionary<string,Dictionary<string,string>>) =
    let mutable t = html

    let expr = """{@(?<magic>[^{}]+)@}"""
    let regx = Regex(expr)
    let a = regx.Matches(t);
    if a <> null then
        for m in a do
            let magic = m.Groups.["magic"].Value.Trim().ToLower()
            let parts = magic.Split(',').Select(fun s -> s.Trim()).ToList()
            let d = Dictionary<string,string>()
            for p in parts do
                let subparts = p.Split('=')
                let k = subparts.[0].Trim()
                let v = subparts.[1].Trim()
                d.Add(k, v)
            let gen_type = d.["type"]
            let replacement =
                if gen_type = "toc" then
                    make_toc d dir_src items
                else
                    raise(NotImplementedException(sprintf "magic type %s" gen_type))
            t <- t.Replace(m.Value, replacement)

    t

let do_file_with_magic (from :string) (dir_src :string) (dest_path :string) (layouts: Dictionary<string,string>) (items: Dictionary<string,Dictionary<string,string>>) =
    let html = File.ReadAllText(from)
    let (page_front_matter, src_content) = util.fm.get_front_matter html
    match page_front_matter with
    | Some page_front_matter ->
        let tocs_done = crunch_magic src_content dir_src items
        let layout_name = get_layout_name page_front_matter
        let after_crunch = wrap layout_name page_front_matter tocs_done layouts
        write_if_changed after_crunch dest_path
    | None ->
        // TODO should never happen here
        ()

let do_file (url_dir :string) (from :string) (dest_dir :string) (layouts: Dictionary<string,string>) (items: Dictionary<string,Dictionary<string,string>>) =
    let name = Path.GetFileName(from)
    let dest_path = Path.Combine(dest_dir, name)
    if (from.EndsWith(".html")) then
        let html = File.ReadAllText(from)
        let (page_front_matter, src_content) = util.fm.get_front_matter html
        match page_front_matter with
        | Some page_front_matter ->
            if not (page_front_matter.ContainsKey("gen")) then
                if not (page_front_matter.ContainsKey("title")) then
                    page_front_matter.Add("title", "Eric Sink")

                let layout_name = get_layout_name page_front_matter
                let after_crunch = wrap layout_name page_front_matter src_content layouts
                write_if_changed after_crunch dest_path

            let url_path = path_combine url_dir name
            items.Add(url_path, page_front_matter)
        | None ->
            copy_if_changed from dest_path
    elif (from.EndsWith(".xml")) then
        let html = File.ReadAllText(from)
        let (page_front_matter, src_content) = util.fm.get_front_matter html
        match page_front_matter with
        | Some page_front_matter ->
            if (page_front_matter.ContainsKey("gen")) then
                ()
            else
                raise(NotImplementedException())
            let url_path = path_combine url_dir name
            items.Add(url_path, page_front_matter)
        | None ->
            copy_if_changed from dest_path
    else
        copy_if_changed from dest_path

let rec do_clean (url_dir :string) (src :string) (dest :string) =
    for f in (Directory.GetFiles(dest)) do
        let name = Path.GetFileName(f)
        let src_path = Path.Combine(src, name)
        let path = path_combine url_dir name
        if not (File.Exists(src_path)) then
            printfn "delete %s" f
            File.Delete(f)

    for sub in (Directory.GetDirectories(dest)) do
        let name = Path.GetFileName(sub)
        let src_sub = Path.Combine(src, name)
        let url_subdir = path_combine url_dir name
        do_clean url_subdir src_sub sub

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

let do_gen dir_src dir_dest path (layouts: Dictionary<string,string>) (items: Dictionary<string,Dictionary<string,string>>) = 
    let front_matter = items.[path]
    let gen_type = front_matter.["gen"]
    let path_dest = weird_path_combine dir_dest path
    if gen_type = "rss" then
        let rss = make_rss dir_src items
        write_if_changed rss path_dest
    elif gen_type = "front_index" then
        let layout_name = front_matter.["layout"]
        let layout = layouts.[layout_name]
        let front_page = make_front_page layout dir_src items
        write_if_changed front_page path_dest
    elif gen_type = "magic" then
        let path_src = weird_path_combine dir_src path
        let path_dest = weird_path_combine dir_dest path
        do_file_with_magic path_src dir_src path_dest layouts items
    else
        raise(NotImplementedException(sprintf "unknown gen type %s" gen_type))

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

    do_clean "/" dir_src dir_dest

    let full_path_content = Path.GetFullPath(dir_src)

    for kv in items do
        let path = kv.Key
        let front_matter = kv.Value
        if (front_matter.ContainsKey("gen")) then
            do_gen full_path_content dir_dest path layouts items

    0 // return an integer exit code

