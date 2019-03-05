namespace blog

type Item() =
    member val ``type`` = null : string with get,set
    member val pubname = null : string with get,set
    member val parentid = null : string with get,set
    member val active = false  with get,set
    member val usetemplate = false  with get,set
    member val title = null : string with get,set
    member val teaser = null : string with get,set
    member val datefiled = null : string with get,set
    member val keywords = null : string with get,set

type Site() =
    member val ``type`` = null : string with get,set
    member val title = null : string with get,set
    member val tagline = null : string with get,set
    member val author = null : string with get,set
    member val copyright = null : string with get,set
    member val publishhost = null : string with get,set
    member val publishpath = null : string with get,set
    member val publishaccount = null : string with get,set
    member val items = new System.Collections.Generic.Dictionary<string, Item>() with get,set

module Say =
    let hello name =
        printfn "Hello %s" name
