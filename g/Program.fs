// Learn more about F# at http://fsharp.org

open System
open System.IO
open System.Linq
open System.Collections.Generic
open Newtonsoft.Json

type Item() =
    member val title = null : string with get,set
    member val datefiled = null : string with get,set

let crunch (template :string) (content :string) (my_path :string) (index :Dictionary<string, Item>) =
    let mutable t = template

    t <- t.Replace("{{{page.content}}}", content)

    let title = index.[my_path].title

    if title <> null then
        t <- t.Replace("{{{page.title}}}", title)
        let datefiled = index.[my_path].datefiled
        let s = "<p class=\"ArticleDate\" align=right>" + datefiled + "</p><h1>" + title + "</h1>";
        t <- t.Replace("{{{article.title}}}", s)
    else
        t <- t.Replace("{{{page.title}}}", "Eric Sink")
        t <- t.Replace("{{{article.title}}}", "")

    t <- t.Replace("{{{site.copyright}}}", "Copyright 2001-2017 Eric Sink. All Rights Reserved")

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

let do_file (url_dir :string) (from :string) (dest_dir :string) (template :string) (index :Dictionary<string, Item>) =
    if (from.EndsWith(".ehtml")) then
        printfn "ehtml: %s" from
        let content = File.ReadAllText(from)
        let basename = Path.GetFileNameWithoutExtension(from)
        let filename_html = basename + ".html"
        let url_path = blog.fsfun.path_combine url_dir filename_html
        let all = crunch template content url_path index
        let dest = Path.Combine(dest_dir, filename_html)
        File.WriteAllText(dest, all)
        ()
    else
        printfn "copy: %s" from
        let name = Path.GetFileName(from)
        let dest_path = Path.Combine(dest_dir, name)
        File.Copy(from, dest_path)

let rec do_dir (url_dir :string) (from :string) (dest_dir :string) template index =
    Directory.CreateDirectory(dest_dir)
    for f in (Directory.GetFiles(from)) do
        printfn "from: %s" f
        do_file url_dir f dest_dir template index

    for from_sub in (Directory.GetDirectories(from)) do
        printfn "from_sub: %s" from_sub
        let name = Path.GetFileName(from_sub)
        let dest_sub = Path.Combine(dest_dir, name)
        let url_subdir = blog.fsfun.path_combine url_dir name
        do_dir url_subdir from_sub dest_sub template index

[<EntryPoint>]
let main argv =
    let dir_content = argv.[0]
    let dir_dest = argv.[1]
    if (Directory.Exists(dir_dest)) then
        raise (Exception("dest directory must not already exist"))
    let path_template = Path.Combine(dir_content, "template.html")
    let template = File.ReadAllText(path_template)
    let path_index = Path.Combine(dir_content, "index.json")
    let index = JsonConvert.DeserializeObject<Dictionary<string,Item>>(File.ReadAllText(path_index))
    do_dir "/" dir_content dir_dest template index
    0 // return an integer exit code

