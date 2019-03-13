
open System
open System.IO
open System.Linq
open System.Collections.Generic
open Newtonsoft.Json

let crunch (template :string) (content :string) (my_path :string) (pairs: Dictionary<string,string>) =
    let mutable t = template

    t <- t.Replace("{{{page.content}}}", content)

    if pairs.ContainsKey("title") then
        let title = pairs.["title"]
        t <- t.Replace("{{{page.title}}}", title)
        let datefiled = pairs.["datefiled"]
        let s = "<p class=\"ArticleDate\" align=right>" + datefiled + "</p><h1>" + title + "</h1>";
        t <- t.Replace("{{{article.title}}}", s)
    else
        t <- t.Replace("{{{page.title}}}", "Eric Sink")
        t <- t.Replace("{{{article.title}}}", "")

    t <- t.Replace("{{{site.copyright}}}", "Copyright 2001-2017 Eric Sink. All Rights Reserved")

    // TODO remove all these
    t <- (t.Replace("{{{link:id='1205'}}}", (blog.fsfun.make_link my_path "/laws/Immutable_Laws_Marketing.html")))
    t <- (t.Replace("{{{link:id='1150'}}}", (blog.fsfun.make_link my_path "/tocs/Software_Development.html")))
    t <- (t.Replace("{{{link:id='1616'}}}", (blog.fsfun.make_link my_path "/tocs/Laughs.html")))
    t <- (t.Replace("{{{link:id='1055'}}}", (blog.fsfun.make_link my_path "/rss.xml")))
    t <- (t.Replace("{{{link:id='1207'}}}", (blog.fsfun.make_link my_path "/Marketing_for_Geeks.html")))
    t <- (t.Replace("{{{link:id='1159'}}}", (blog.fsfun.make_link my_path "/bos/Business_of_Software.html")))
    t <- (t.Replace("{{{link:id='1182'}}}", (blog.fsfun.make_link my_path "/index.html")))
    t <- (t.Replace("{{{link:id='3052'}}}", (blog.fsfun.make_link my_path "/mssql_mobile/index.html")))
    t <- (t.Replace("{{{link:id='3022'}}}", (blog.fsfun.make_link my_path "/images/princeton.jpg")))
    t <- (t.Replace("{{{link:id='1152'}}}", (blog.fsfun.make_link my_path "/about_author.html")))
    t <- (t.Replace("{{{link:id='1802'}}}", (blog.fsfun.make_link my_path "/vcbe/index.html")))

    t

let get_front_matter (s :string) =
    let d = Dictionary<string,string>()
    let marker_lf = "---\n"
    let marker_crlf = "---\r\n"
    if (s.StartsWith(marker_lf)) || (s.StartsWith(marker_crlf)) then
        // first remove that first line
        let s2 = 
            // whether it was lf or crlf, we can just find the lf and chop there
            let n = s.IndexOf("\n") // TODO could assert, must be > 0
            s.Substring(n + 1)

        // now find the other marker

        // TODO should we be looking for the second marker only at
        // the beginning of a line?  in other words, matching with the
        // lf just before the ---

        let n = 
            let n_lf = s2.IndexOf(marker_lf)
            let n_crlf = s2.IndexOf(marker_crlf)
            if n_lf > 0 then
                if n_crlf > 0 then
                    // found 2nd marker in BOTH lf and crlf forms?
                    // take the first one
                    if n_lf < n_crlf then
                        n_lf
                    else
                        n_crlf
                else
                    n_lf
            elif n_crlf > 0 then
                n_crlf
            else
                raise (Exception("second front matter marker not found"))

        let s3 = s2.Substring(0, n)
        let remain = s2.Substring(n + marker_lf.Length)

        let a = s3.Split("\n")
        for pair in a do
            // the final pair line is empty
            if pair.Length > 0 then
                let n_colon = pair.IndexOf(":")
                let k = pair.Substring(0, n_colon).Trim()
                let v = pair.Substring(n_colon + 1).Trim()
                if (k.Length > 0) && (v.Length > 0) then
                    d.Add(k, v)
        (d, remain)
    else
        (d, s)

let do_file (url_dir :string) (from :string) (dest_dir :string) (template :string) =
    if (from.EndsWith(".ehtml")) then
        let ehtml = File.ReadAllText(from)
        let (front_matter, content) = get_front_matter ehtml
        let basename = Path.GetFileNameWithoutExtension(from)
        let filename_html = basename + ".html"
        let url_path = blog.fsfun.path_combine url_dir filename_html
        let all = crunch template content url_path front_matter
        let dest = Path.Combine(dest_dir, filename_html)
        File.WriteAllText(dest, all)
    else
        let name = Path.GetFileName(from)
        let dest_path = Path.Combine(dest_dir, name)
        File.Copy(from, dest_path)

let rec do_dir (url_dir :string) (from :string) (dest_dir :string) template =
    Directory.CreateDirectory(dest_dir)
    for f in (Directory.GetFiles(from)) do
        do_file url_dir f dest_dir template

    for from_sub in (Directory.GetDirectories(from)) do
        let name = Path.GetFileName(from_sub)
        let dest_sub = Path.Combine(dest_dir, name)
        let url_subdir = blog.fsfun.path_combine url_dir name
        do_dir url_subdir from_sub dest_sub template

[<EntryPoint>]
let main argv =
    let dir_content = argv.[0]
    let dir_dest = argv.[1]
    if (Directory.Exists(dir_dest)) then
        raise (Exception("dest directory must not already exist"))
    let path_template = Path.Combine(dir_content, "template.html")
    let template = File.ReadAllText(path_template)
    do_dir "/" dir_content dir_dest template
    0 // return an integer exit code

