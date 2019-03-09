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

module fsfun =
    // an implementation of Path.Combine which always uses fwd slash
    let path_combine (a :string) (b :string) =
        if (a.Length = 0) then
            b
        elif (b.Substring(0, 1) = "/") then
            b
        elif (a.Substring(a.Length - 1, 1) = "/") then
            a + b
        else
            a + "/" + b;

