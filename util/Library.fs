﻿namespace util

module fm =
    open System
    open System.IO
    open System.Collections.Generic
    open System.Text

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
            let a = s3.Split('\n')
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
            (Some d, remain)
        else
            (None, s)

    let create_front_matter (d: Dictionary<string,string>) =
        // front matter parsing is strict
        let sb = StringBuilder()
        sb.Append("---\n") |> ignore
        for kv in d do
            let line = sprintf "%s: %s\n" kv.Key kv.Value
            sb.Append(line) |> ignore
        sb.Append("---\n") |> ignore
        sb.ToString()

    let write_with_front_matter (dest : string) (d: Dictionary<string,string>) (s :string) =
        let front = create_front_matter d
        File.WriteAllText(dest, front + s)

