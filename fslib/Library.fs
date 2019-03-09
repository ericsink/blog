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

    let make_link (myPath :string) (otherPath: string) =
        let myParts = myPath.Split('/')
        let otherParts = otherPath.Split('/')

        // TODO this feels like a hack
        if (myPath = otherPath) then
            myParts.[myParts.Length - 1]
        else
            let mutable ndx = 0;
            while ( (ndx < myParts.Length) && (ndx < otherParts.Length) && (myParts.[ndx] = otherParts.[ndx])) do
                ndx <- ndx + 1

            let mutable result = ""

            let mutable i = ndx
            while (i < (myParts.Length - 1)) do
                result <- path_combine result ".."
                i <- i + 1

            while (ndx < (otherParts.Length)) do
                result <- path_combine result otherParts.[ndx]
                ndx <- ndx + 1

            result

    let rec get_path (site: Site) (it: Item) =
        if (it.parentid <> null) then
            let pit = site.items.[it.parentid];
            (get_path site pit) + "/" + it.pubname
        else
            it.pubname

