﻿
open System
open System.IO;
open System.Linq;
open System.Collections.Generic
open System.Text
open System.Text.RegularExpressions;

open Newtonsoft.Json

let has_template (text :string[]) =
    if 
        (text.[0] = "<html>")
        && (text.[1] = "<head>")
        && (text.[2].StartsWith("<title>"))
        && (text.[20] = ".class_sidebar ul {")
        && (text.[163] = "<tr><td>")
        //&& (text.[164].StartsWith("<p class=\"ArticleDate\" align=right>"))
        && (text.[text.Length - 1] = "")
        && (text.[text.Length - 2] = "</body></html>")
        && (text.[text.Length - 8].Contains("00244d"))
        && (text.[text.Length - 14] = "</td></tr>")
    then
        true
    else
        false

let do_file (url_dir :string) (from :string) dest_dir (old_index :Dictionary<string,blog.Item>) (id_by_path :Dictionary<string,string>) (dir_data :string) =
    let name = Path.GetFileName(from)
    let url_path = blog.fsfun.path_combine url_dir name
    if (name.EndsWith(".html")) then

        let remove_template_text (text: string) =
            let t_front_find = "<table cellspacing=\"0\" cellpadding=\"0\" width=\"100%\" border=\"0\" bgcolor=\"white\">\r\n<tr><td>&nbsp;</td></tr>\r\n<tr><td>\r\n"
            let t_back_find = "</td></tr>\r\n</table>\r\n</div>\r\n</td>"

            let n1 = text.IndexOf(t_front_find)
            //printfn "n1: %d" n1
            let n2 = n1 + t_front_find.Length
            let t2 = text.Substring(n2)
            let n3 = t2.IndexOf("\n")
            let t3 = t2.Substring(n3 + 1)
            //printfn "t3: %s" t3
            let n4 = t3.IndexOf(t_back_find)
            let t4 = t3.Substring(0, n4 - 2)
            t4

        let create_front_matter (d: Dictionary<string,string>) =
            // front matter parsing is strict
            let sb = StringBuilder()
            sb.Append("---\n")
            for kv in d do
                let line = sprintf "%s: %s\n" kv.Key kv.Value
                sb.Append(line)
            sb.Append("---\n")
            sb.ToString()

        let write_with_front_matter (d: Dictionary<string,string>) (s :string) =
            let front = create_front_matter d
            let dest = Path.Combine(dest_dir, name)
            File.WriteAllText(dest, front + s)

        let clean_teaser (s :string) =
            s
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("&nbsp;", " ")
                .Replace("<i>", " ")
                .Replace("</i>", " ")
                .Replace("<b>", " ")
                .Replace("</b>", " ")
                .Replace("<em>", " ")
                .Replace("</em>", " ")
                .Replace("<strong>", " ")
                .Replace("</strong>", " ")
                .Replace("<br>", " ")
                .Replace("<br/>", " ")
                .Replace("<br />", " ")
                .Replace("<a href=\"http://dictionary.reference.com/search?q=dragnet\">", "")
                .Replace("</a>", " ")

        let consolidate_spaces (s :string) =
            let mutable t = s
            let regx = Regex("  +")
            let a = regx.Matches(t);
            if a <> null then
                for m in a do
                    t <- t.Replace(m.Value, " ")
            t

        if (id_by_path.ContainsKey(url_path)) then
            let id = id_by_path.[url_path]
            let it = old_index.[id]
            if it.usetemplate then
                let data_path = 
                    let data_name = sprintf "%s.html" id
                    Path.Combine(dir_data, data_name)
                let pairs = Dictionary<string,string>()
                pairs.Add("layout", "default")
                pairs.Add("esbma_id", id)
                if it.title <> null then
                    pairs.Add("title", it.title)
                if it.datefiled <> null then
                    pairs.Add("datefiled", it.datefiled)
                if it.keywords <> null then
                    pairs.Add("keywords", it.keywords)
                if it.teaser <> null then
                    pairs.Add("teaser", it.teaser |> clean_teaser |> consolidate_spaces)
                if (File.Exists(data_path)) then
                    let data = File.ReadAllText(data_path)
                    let d2 = blog.pre.crunch(old_index, data)
                    write_with_front_matter pairs d2
                else
                    let js_name = sprintf "%s.js" id
                    if (File.Exists(Path.Combine(dir_data, js_name))) then
                        pairs.Add("esbma_type", "js")

                    let text = File.ReadAllText(from)
                    let a = remove_template_text text
                    write_with_front_matter pairs a
            else
                // probably a print- item
                let dest_path = Path.Combine(dest_dir, name)
                File.Copy(from, dest_path)
        else
            let text = File.ReadAllLines(from)
            if has_template text then
                let pairs = Dictionary<string, string>()

                pairs.Add("esbma_id", "unknown")

                let title = text.[2].Replace("<title>", "").Replace("</title>", "")
                pairs.Add("title", title)

                let date_begin = "<p class=\"ArticleDate\" align=right>"
                let matchFunc (s : string) = (s.StartsWith(date_begin))
                match Array.tryFind matchFunc text with
                | Some v -> 
                    let s = v.Replace(date_begin, "")
                    let i = s.IndexOf("</p>")
                    let q = if i > 0 then (s.Substring(0, i)) else s
                    pairs.Add("datefiled", q)
                | None -> ()

                let text = File.ReadAllText(from)
                let a = remove_template_text text
                write_with_front_matter pairs a
            else
                let dest_path = Path.Combine(dest_dir, name)
                File.Copy(from, dest_path)
    else
        let dest_path = Path.Combine(dest_dir, name)
        File.Copy(from, dest_path)

let rec do_dir (url_dir :string) from dest_dir (old_index :Dictionary<string,blog.Item>) id_by_path dir_data =
    Directory.CreateDirectory(dest_dir)
    for f in (Directory.GetFiles(from)) do
        do_file url_dir f dest_dir old_index id_by_path dir_data

    for from_sub in (Directory.GetDirectories(from)) do
        let name = Path.GetFileName(from_sub)
        let dest_sub = Path.Combine(dest_dir, name)
        let url_subdir = blog.fsfun.path_combine url_dir name
        do_dir url_subdir from_sub dest_sub old_index id_by_path dir_data

[<EntryPoint>]
let main argv =
    let dir_data = Path.Combine("..", "data")
    let site = JsonConvert.DeserializeObject<blog.Site>(File.ReadAllText(Path.Combine(dir_data, "esbma.json")))
    let id_by_path = Dictionary<string, string>()
    let old_index = site.items
    let want typ =
        if typ = "html" then
            true
        elif typ = "js" then
            true
        else
            false
    for kv in old_index do
        let id = kv.Key
        let it = kv.Value
        if want it.``type`` then
            let it_path = "/" + blog.fsfun.get_path old_index it
            id_by_path.Add(it_path, id)

    let dir_live = argv.[0]
    let dir_dest = argv.[1]
    if (Directory.Exists(dir_dest)) then
        raise (Exception("dest directory must not already exist"))
    do_dir "/" dir_live dir_dest old_index id_by_path dir_data
    let dir_layouts = Path.Combine(dir_dest, "_layouts")
    Directory.CreateDirectory(dir_layouts)
    File.Copy(Path.Combine(dir_data, "template.html"), Path.Combine(dir_layouts, "default.html"))
    0 // return an integer exit code

