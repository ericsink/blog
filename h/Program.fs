
open System
open System.IO;
open System.Linq;
open System.Collections.Generic

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

let do_file (url_dir :string) (from :string) dest_dir (new_index :Dictionary<string,Dictionary<string,string>>) (old_index :Dictionary<string,blog.Item>) (id_by_path :Dictionary<string,string>) (dir_data :string) =
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

        let write_ehtml_text (s :string) =
            let basename = Path.GetFileNameWithoutExtension(name)
            let filename_ehtml = basename + ".ehtml"
            let dest = Path.Combine(dest_dir, filename_ehtml)
            File.WriteAllText(dest, s)

        if (id_by_path.ContainsKey(url_path)) then
            let id = id_by_path.[url_path]
            let it = old_index.[id]
            if it.usetemplate then
                let data_name = sprintf "%s.html" id
                let data_path = Path.Combine(dir_data, data_name)
                if (File.Exists(data_path)) then
                    let data = File.ReadAllText(data_path)
                    let d2 = blog.pre.crunch(old_index, id, data)
                    let basename = Path.GetFileNameWithoutExtension(name)
                    let filename_ehtml = basename + ".ehtml"
                    let dest = Path.Combine(dest_dir, filename_ehtml)
                    File.WriteAllText(dest, d2)
                else
                    // probably js

                    let text = File.ReadAllText(from)
                    let a = remove_template_text text
                    write_ehtml_text a
            else
                let dest_path = Path.Combine(dest_dir, name)
                File.Copy(from, dest_path)
        else
            let text = File.ReadAllLines(from)
            if has_template text then
                let dnew = Dictionary<string, string>()

                let title = text.[2].Replace("<title>", "").Replace("</title>", "")
                dnew.Add("title", title)

                let date_begin = "<p class=\"ArticleDate\" align=right>"
                let matchFunc (s : string) = (s.StartsWith(date_begin))
                match Array.tryFind matchFunc text with
                | Some v -> 
                    let s = v.Replace(date_begin, "")
                    let i = s.IndexOf("</p>")
                    let q = if i > 0 then (s.Substring(0, i)) else s
                    dnew.Add("datefiled", q)
                | None -> ()

                // TODO teaser ?
                // TODO keywords ?

                new_index.Add(url_path, dnew)

                let text = File.ReadAllText(from)
                let a = remove_template_text text
                write_ehtml_text a
            else
                let dest_path = Path.Combine(dest_dir, name)
                File.Copy(from, dest_path)
    else
        let dest_path = Path.Combine(dest_dir, name)
        File.Copy(from, dest_path)

let rec do_dir (url_dir :string) from dest_dir new_index (old_index :Dictionary<string,blog.Item>) id_by_path dir_data =
    Directory.CreateDirectory(dest_dir)
    for f in (Directory.GetFiles(from)) do
        do_file url_dir f dest_dir new_index old_index id_by_path dir_data

    for from_sub in (Directory.GetDirectories(from)) do
        let name = Path.GetFileName(from_sub)
        let dest_sub = Path.Combine(dest_dir, name)
        let url_subdir = blog.fsfun.path_combine url_dir name
        do_dir url_subdir from_sub dest_sub new_index old_index id_by_path dir_data

[<EntryPoint>]
let main argv =
    let dir_data = Path.Combine("..", "data")
    let site = JsonConvert.DeserializeObject<blog.Site>(File.ReadAllText(Path.Combine(dir_data, "esbma.json")))
    let item_by_path = Dictionary<string, blog.Item>()
    let id_by_path = Dictionary<string, string>()
    let new_index = Dictionary<string, Dictionary<string, string>>()
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
            item_by_path.Add(it_path, it)
            id_by_path.Add(it_path, id)
            let dnew = Dictionary<string, string>()
            if it.title <> null then
                dnew.Add("title", it.title)
            if it.datefiled <> null then
                dnew.Add("datefiled", it.datefiled)
            // TODO teaser ?
            // TODO keywords ?
            new_index.Add(it_path, dnew)

    let dir_live = argv.[0]
    let dir_dest = argv.[1]
    if (Directory.Exists(dir_dest)) then
        raise (Exception("dest directory must not already exist"))
    do_dir "/" dir_live dir_dest new_index old_index id_by_path dir_data
    let json = JsonConvert.SerializeObject(new_index, Formatting.Indented)
    let path_json = Path.Combine(dir_dest, "index.json")
    File.WriteAllText(path_json, json)
    File.Copy(Path.Combine(dir_data, "template.html"), Path.Combine(dir_dest, "template.html"))
    0 // return an integer exit code

