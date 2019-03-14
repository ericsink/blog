
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
        (d, remain)
    else
        (null, s)

let crunch (html :string) (pairs: Dictionary<string,string>) =
    let mutable t = html

    let expr = """{{(?<v>[^{}]+)}}"""
    let regx = Regex(expr)
    let a = regx.Matches(t);
    if a <> null then
        for m in a do
            let v = m.Groups.["v"].Value.Trim().ToLower()
            let replacement = pairs.[v]
            t <- t.Replace(m.Value, replacement)

    t

let add_defaults (d: Dictionary<string,string>) =
    d.Add("site.title", "Eric Sink")
    d.Add("site.tagline", "SourceGear Founder")
    d.Add("site.copyright", "Copyright 2001-2019 Eric Sink. All Rights Reserved")

let make_rss dir_content (items: Dictionary<string,Dictionary<string,string>>) = 
    let add (sb :StringBuilder) (s :string) =
        sb.Append(s) |> ignore

    // TODO maybe we should use XmlWriter for this

    let content = StringBuilder()

    add content "<?xml version=\"1.0\" encoding=\"iso-8859-1\" ?>"
    add content "<rss version=\"2.0\">"
    add content "<channel>"
    add content "<title>{{site.title}}</title>"
    // TODO https, no www
    add content "<link>http://www.ericsink.com/</link>"
    add content "<description>{{site.tagline}}</description>"
    add content "<copyright>{{site.copyright}}</copyright>"
    add content "<generator>mine</generator>"

    let a = items.OrderByDescending(fun kv -> kv.Value.["datefiled"]).Take(10).ToList()

    for kv in a do
        let path = kv.Key
        let title = if kv.Value.ContainsKey("title") then kv.Value.["title"] else null
        let datefiled = kv.Value.["datefiled"]

        //printfn "path: %s" path
        // TODO windows-specific code below
        let path_fixed = path.Substring(1).Replace("/", "\\")
        //printfn "path_fixed: %s" path_fixed
        let path_content = Path.Combine(dir_content, path_fixed)
        //printfn "path_content: %s" path_content
        let html = File.ReadAllText(path_content)
        let (front_matter, my_content) = get_front_matter html
        // TODO https, no www
        let local_link = "http://www.ericsink.com" + path

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
        add content (blog.fsfun.format_date_rss(datefiled))
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
    let pairs = Dictionary<string,string>()
    add_defaults pairs
    let result = crunch s pairs
    result

let do_file (url_dir :string) (from :string) (dest_dir :string) (template :string) (items: Dictionary<string,Dictionary<string,string>>) =
    let name = Path.GetFileName(from)
    let dest_path = Path.Combine(dest_dir, name)
    if (from.EndsWith(".html")) then
        let html = File.ReadAllText(from)
        let (front_matter, content) = get_front_matter html
        if front_matter <> null then
            add_defaults front_matter
            let url_path = blog.fsfun.path_combine url_dir name
            front_matter.Add("content", content)

            // TODO instead of passing in the template here, this should
            // check pairs for the template, find it, and use that.  so
            // pass in a dictionary of templates or an interface to get them.

            if front_matter.ContainsKey("title") then
                let title = front_matter.["title"]
                front_matter.Add("page.title", title)
                let datefiled = front_matter.["datefiled"]
                // TODO this is dorky.  the markup should go in the template, just replace the date
                // current implementation means that if there is no title, the markup around it is omitted.
                let s = "<p class=\"ArticleDate\" align=right>" + datefiled + "</p><h1>" + title + "</h1>";
                front_matter.Add("article.title", s)
            else
                front_matter.Add("page.title", "Eric Sink")
                front_matter.Add("article.title", "")

            let all = crunch template front_matter
            File.WriteAllText(dest_path, all)
            items.Add(url_path, front_matter)
        else
            File.Copy(from, dest_path)
    else
        File.Copy(from, dest_path)

let rec do_dir (url_dir :string) (from :string) (dest_dir :string) template (items: Dictionary<string,Dictionary<string,string>>) =
    Directory.CreateDirectory(dest_dir) |> ignore
    for f in (Directory.GetFiles(from)) do
        do_file url_dir f dest_dir template items

    for from_sub in (Directory.GetDirectories(from)) do
        let name = Path.GetFileName(from_sub)
        // TODO skip _layouts at every depth, or just at the top?
        if name <> "_layouts" then
            let dest_sub = Path.Combine(dest_dir, name)
            let url_subdir = blog.fsfun.path_combine url_dir name
            do_dir url_subdir from_sub dest_sub template items

[<EntryPoint>]
let main argv =
    let dir_content = argv.[0]
    let dir_dest = argv.[1]
    if (Directory.Exists(dir_dest)) then
        raise (Exception("dest directory must not already exist"))
    let path_template = 
        let dir_layouts = Path.Combine(dir_content, "_layouts")
        Path.Combine(dir_layouts, "default.html")
    let default_template = File.ReadAllText(path_template)
    let items = Dictionary<string,Dictionary<string,string>>()
    do_dir "/" dir_content dir_dest default_template items

    let full_path_content = Path.GetFullPath(dir_content)
    //printfn "full_path: %s" full_path_content
    let rss = make_rss full_path_content items
    let path_rss = Path.Combine(dir_dest, "rss.xml")
    File.WriteAllText(path_rss, rss)
    //printfn "%s" rss
    0 // return an integer exit code

