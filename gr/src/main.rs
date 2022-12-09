
use std::collections::HashMap;
use std::collections::HashSet;
use std::path::Path;
use std::io::Read;
use regex::Regex;
use time::Date;
use time::Month;
use time::Weekday;

// an implementation of Path.Combine which always uses fwd slash
fn path_combine(a : &str, b : &str) -> String {
	if a.len() == 0 {
		String::from(b)
	} else if b.starts_with("/") {
		String::from(b)
	} else if a.ends_with("/") {
		String::from(a) + b
	} else {
		String::from(a) + "/" + b
	}
}

fn get_front_matter(s : &str) -> (Option<HashMap<String,String>>, String) {
	let marker_front_lf = "---\n";
	let marker_front_crlf = "---\r\n";
	if (s.starts_with(marker_front_lf)) || (s.starts_with(marker_front_crlf)) {
		let s2 =
		{
			// whether it was lf or crlf, we can just find the lf and chop there
			let n = s.find("\n").unwrap(); // TODO could assert, must be > 0
			String::from(&s[n..])
		};
		// now find the other marker

		let (n, len) =
		{
			// the second marker should match \n--- for either EOL
			let marker_back_lf = "\n---\n";
			let marker_back_crlf = "\n---\r\n";
			let n_lf = s2.find(marker_back_lf);
			let n_crlf = s2.find(marker_back_crlf);
			if n_lf.is_some() {
				let n_lf = n_lf.unwrap();
				if n_crlf.is_some() {
					let n_crlf = n_crlf.unwrap();
					// found 2nd marker in BOTH lf and crlf forms?
					// take the first one
					if n_lf < n_crlf {
						(n_lf, marker_back_lf.len())
					} else {
						(n_crlf, marker_back_crlf.len())
					}
				} else {
					(n_lf, marker_back_lf.len())
				}
			} else if n_crlf.is_some() {
				(n_crlf.unwrap(), marker_back_crlf.len())
			} else {
				panic!("second front matter marker not found");
			}
		};

		let s3 = &s2[0.. n];
		let remain = String::from(&s2[n + len..]);

		// split on lf should work for either eol here.
		// the cr will remain, but it gets trimmed out.
		let a = s3.split('\n');
		let mut d = HashMap::new();
		for pair in a {
			if pair.len() > 0 {
				let n_colon = pair.find(":").unwrap();
				let k = pair[0.. n_colon].trim();
				let v = pair[n_colon + 1..].trim();
				// TODO we may want to allow v to be empty string or null
				if (k.len() > 0) && (v.len() > 0) {
					d.insert(String::from(k), String::from(v));
				}
			}
		}
		(Some(d), remain)
	} else {
		(None, String::from(s))
	}
}

fn month_enum(i : u8) -> Month {
    match i {
        1 => Month::January,
        2 => Month::February,
        3 => Month::March,
        4 => Month::April,
        5 => Month::May,
        6 => Month::June,
        7 => Month::July,
        8 => Month::August,
        9 => Month::September,
        10 => Month::October,
        11 => Month::November,
        12 => Month::December,
        _ => panic!(),
    }
}

fn format_date_rss(s: &str) -> String {
	let twoparts : Vec<&str> = s.split(' ').collect();
    let dateparts : Vec<&str>  = twoparts[0].split('-').collect();
    let timeparts : Vec<&str>  = twoparts[1].split(':').collect();
    let year = dateparts[0].parse::<i32>().unwrap();
    let month = dateparts[1].parse::<u8>().unwrap();
    let day = dateparts[2].parse::<u8>().unwrap();
    let hour = timeparts[0].parse::<i32>().unwrap();
    let min = timeparts[1].parse::<i32>().unwrap();
    let sec = timeparts[2].parse::<i32>().unwrap();

	fn short_month(m : Month) -> &'static str {
		match m {
			Month::January => "Jan",
			Month::February => "Feb",
			Month::March => "Mar",
			Month::April => "Apr",
			Month::May => "May",
			Month::June => "Jun",
			Month::July => "Jul",
			Month::August => "Aug",
			Month::September => "Sep",
			Month::October => "Oct",
			Month::November => "Nov",
			Month::December => "Dec",
		}
	}

	fn short_day(d : Weekday) -> &'static str {
		match d {
			Weekday::Monday => "Mon",
			Weekday::Tuesday => "Tue",
			Weekday::Wednesday => "Wed",
			Weekday::Thursday => "Thu",
			Weekday::Friday => "Fri",
			Weekday::Saturday => "Sat",
			Weekday::Sunday => "Sun",
		}
	}

	let month = month_enum(month);
	let d = Date::from_calendar_date(year, month, day).unwrap();

	format!("{ddd}, {dd:0>2} {MMM} {yyyy} {HH:0>2}:{mm:0>2}:{ss:0>2} CST", 
		yyyy=year, 
		dd=day, 
		MMM=short_month(month), 
		ddd=short_day(d.weekday()),
		HH=hour, 
		mm=min, 
		ss=sec)
}

fn format_date(s: &str) -> String {
	let twoparts : Vec<&str> = s.split(' ').collect();
    let dateparts : Vec<&str>  = twoparts[0].split('-').collect();
    let timeparts : Vec<&str>  = twoparts[1].split(':').collect();
    let year = dateparts[0].parse::<i32>().unwrap();
    let month = dateparts[1].parse::<u8>().unwrap();
    let day = dateparts[2].parse::<u8>().unwrap();
    let hour = timeparts[0].parse::<i32>().unwrap();
    let min = timeparts[1].parse::<i32>().unwrap();
    let sec = timeparts[2].parse::<i32>().unwrap();

	let month = month_enum(month);
	let d = Date::from_calendar_date(year, month, day).unwrap();

	format!("{dddd}, {d} {MMMM} {yyyy}", yyyy=year, d=day, MMMM=month.to_string(), dddd=d.weekday().to_string())
}

fn crunch(html: &str, content: &str, pairs: &HashMap<String,HashMap<String,String>>) -> String {
    let mut t = String::from(html);
    let tcopy = String::from(html);

    let expr = r"\{\{(?P<v>[^{}]+)}}";
    let regx = Regex::new(expr).unwrap();
	for m in regx.captures_iter(&tcopy) {
		let v = m["v"].trim().to_lowercase();
		let replacement =
			if v == "content" {
				// special case
				content
			}
			else {
				let parts : Vec<&str> = v.split('.').collect();
				let section = parts[0];
				let k = parts[1];
				&pairs[section][k]
			};
		//dbg!(&m);
		let repl = &m[0];
		let tnew = t.replace(repl, replacement);
		t = tnew;
	}
    t
}

fn add_pair(d: &mut HashMap<String,HashMap<String,String>>, section: &str, k: &str, v: &str) {
	match d.get_mut(section) {
		Some(d3) => {
			d3.insert(String::from(k), String::from(v));
		},
		None =>
		{
			let mut d3 = HashMap::new();
			d3.insert(String::from(k), String::from(v));
			d.insert(String::from(section), d3);
		},
	};
}

fn add_site_defaults(d: &mut HashMap<String,HashMap<String,String>>) {
    add_pair(d, "site", "title", "Eric Sink");
    add_pair(d, "site", "tagline", "SourceGear Founder");
    add_pair(d, "site", "copyright", "Copyright 2001-2021 Eric Sink. All Rights Reserved");
}

// dir is a platform path
// uri_path is forward-slashes
// result is a platform path
fn weird_path_combine(dir: &Path, uri_path: &str) -> std::path::PathBuf {
    // TODO windows-specific code below
    let path_fixed =
        String::from(&uri_path[1..]).replace("/", "\\");
    let path = dir.join(path_fixed);
    path
}

fn read_from_src(dir_src: &Path, uri_path: &str) -> Result<String,std::io::Error> {
    let path_content = weird_path_combine(dir_src, uri_path);
	let html = std::fs::read_to_string(&path_content)?;
    Ok(html)
}

fn make_rss(dir_src: &Path, items: &HashMap<String,HashMap<String,String>>) -> Result<String,std::io::Error> {
    fn add(sb: &mut String, s: &str) {
        sb.push_str(s);
	}

    let mut content = String::new();

    add(&mut content, "<?xml version=\"1.0\" encoding=\"iso-8859-1\" ?>");
    add(&mut content, "<rss version=\"2.0\">");
    add(&mut content, "<channel>");
    add(&mut content, "<title>{{site.title}}</title>");
    add(&mut content, "<link>https://ericsink.com/</link>");
    add(&mut content, "<description>{{site.tagline}}</description>");
    add(&mut content, "<copyright>{{site.copyright}}</copyright>");
    add(&mut content, "<generator>mine</generator>");

	let mut a = Vec::new();
	for kv in items {
		let (path, v) = kv;
		if v.contains_key("date") {
			a.push(kv);
		}
	}

	a.sort_by(
		|a,b|
		{
			let (_,va) = a;
			let (_,vb) = b;
			let sa = va.get("date");
			let sb = vb.get("date");
			let c =
				match sa {
					Some(sa) =>
						match sb {
							Some(sb) => sa.cmp(sb).reverse(),
							None => std::cmp::Ordering::Less
						},
					None => 
						match sb {
							Some(_) => std::cmp::Ordering::Greater,
							None => std::cmp::Ordering::Equal
						},
				};
			//dbg!(sa);
			//dbg!(sb);
			//dbg!(c);
			c
		}
		);

	let a = &a[..10];

	/*
	let a : Vec<(&String,&HashMap<String,HashMap<String,String>>)> = 
		items.iter()
		//.filter(|(k,v)| v.contains_key("date"))
		//.OrderByDescending(fun kv -> kv.Value.["date"])
		//.Take(10)
		.collect()
		;
		*/

    for kv in a {
		let (path, v) = kv;
        let title = match v.get("title") {
			Some(t) => t,
			None => "",
		};
        let date = &v["date"];

        let html = read_from_src(dir_src, path)?;
        let (_, my_content) = get_front_matter(&html);
        let local_link = format!("https://ericsink.com{}", path);

        add(&mut content, "<item>");
        add(&mut content, "<title>");
        add(&mut content, title);
        add(&mut content, "</title>");
        add(&mut content, "<guid>");
        add(&mut content, &local_link);
        add(&mut content, "</guid>");
        add(&mut content, "<link>");
        add(&mut content, &local_link);
        add(&mut content, "</link>");
        add(&mut content, "<pubDate>");
        add(&mut content, &format_date_rss(&date));
        add(&mut content, "</pubDate>");
        add(&mut content, "<description>");
        add(&mut content, "<![CDATA[");
        add(&mut content, &my_content);
        add(&mut content, "]]>");
        add(&mut content, "</description>");
        add(&mut content, "</item>");
	}

	add(&mut content, "</channel>");
    add(&mut content, "</rss>");

    let mut pairs = HashMap::<String,HashMap<String,String>>::new();
    add_site_defaults(&mut pairs);
    let result = crunch(&content, "", &pairs);
    Ok(result)
}

fn write_if_changed(content : &str, dest : &Path) -> Result<(),std::io::Error> {
    let different =
        if dest.exists() {
            let cur = std::fs::read_to_string(dest)?;
            cur != content
		} else {
            true
		};
    if different {
        println!("write {:?}", dest);
		std::fs::write(&dest, content)?;
	}
	Ok(())
}

fn copy_if_changed(src : &Path, dest : &Path) -> Result<(),std::io::Error> {
    let different =
        if dest.exists() {
            let fi_src = std::fs::metadata(src)?;
            let fi_dest = std::fs::metadata(dest)?;
            if fi_src.len() != fi_dest.len() {
                true
            } else {
                let ba_src = 
				{
					let mut f = std::fs::File::open(src)?;
					let mut buf = Vec::new();
					f.read_to_end(&mut buf)?;
					buf
				};
                let ba_dest = 
				{
					let mut f = std::fs::File::open(dest)?;
					let mut buf = Vec::new();
					f.read_to_end(&mut buf)?;
					buf
				};
                ba_src != ba_dest // TODO is this doing a full compare?
			}
        } else {
            true
		};
    if different {
        println!("copy {:?}", dest);
        std::fs::copy(src, dest)?;
	}
	Ok(())
}

fn get_layout_name(front_matter : &HashMap<String,String>) -> Option<String> {
	match front_matter.get("layout") {
	Some(layout_name) => {
		if layout_name == "null" {
			None
		} else {
			Some(String::from(layout_name))
		}
	},
	None => None
	}
}

fn wrap(layout_name : Option<String>, page_front_matter : &HashMap<String,String>, src_content : &str, layouts: &HashMap<String,String>) -> String {
    let mut pairs = HashMap::new();
    add_site_defaults(&mut pairs);
    pairs.insert(String::from("page"), page_front_matter.clone());

    let (next_layout, before_crunch, content) =
        match layout_name {
			Some(layout_name) =>
			{
				let layout = &layouts[&layout_name];
				let (template_front, template_html) = get_front_matter(&layout);
				let next_layout =
					match template_front {
					Some(fm) =>
					{
						pairs.insert(String::from("layout"), fm.clone());
						get_layout_name(&fm)
					},
					None =>
						None
					};
				(next_layout, template_html, src_content)
			},
			None =>
			{
				(None, String::from(src_content), "")
			}
		};

    let after_crunch = crunch(&before_crunch, content, &pairs);
    match next_layout {
		Some(next_layout_name) =>
			wrap(Some(next_layout_name), page_front_matter, &after_crunch, layouts),
		None =>
			after_crunch
	}
}

fn make_toc (magic: &HashMap<String,String>, items: &HashMap<String,HashMap<String,String>>) -> String {
    fn add(sb: &mut String, s: &str) {
        sb.push_str(s);
	}

    let showdate =
        match magic.get("showdate") {
			Some(v) => v == "true", // TODO parse bool
			None => true
		};
    let showteaser =
        match magic.get("showteaser") {
			Some(v) => v == "true", // TODO parse bool
			None => true
		};
    let sortby =
        match magic.get("sortby") {
			Some(s) => s,
			None => "date"
		};

    // TODO allow multiple included keywords here?
    // TODO allow keyword exclusion here?

    fn has_kw (fm: &HashMap<String,String>, kw :&str) -> bool {
        match fm.get("keywords") {
			Some(keywords) =>
			{
				//let keywords = &fm["keywords"];
				let a : Vec<&str> = keywords.split(' ').map(|x| x.trim()).collect();
				let mut h = HashSet::<String>::new();
				for k in a {
					h.insert(String::from(k));
				}
				let b = h.contains(kw);
				b
			},
			None =>
				false
		}
	}

	let mut filtered : Vec<(&String,&HashMap<String,String>)> =
        match magic.get("keyword") {
			Some(kw_include) => items.iter().filter(|(_,v)| has_kw(v, kw_include)).collect(),
			None => items.iter().collect()
		};

	filtered.sort_by(
		|a,b|
		{
			let (_,va) = a;
			let (_,vb) = b;
			let sa = va.get(sortby);
			let sb = vb.get(sortby);
			let c =
				match sa {
					Some(sa) =>
						match sb {
							Some(sb) => sa.cmp(sb).reverse(),
							None => std::cmp::Ordering::Less
						},
					None => 
						match sb {
							Some(_) => std::cmp::Ordering::Greater,
							None => std::cmp::Ordering::Equal
						},
				};
			//dbg!(sa);
			//dbg!(sb);
			//dbg!(c);
			c
		}
		);

    let mut content = String::new();

    for kv in filtered {
		let (path, v) = kv;
        let title = match v.get("title") {
			Some(t) => t,
			None => "",
		};

        add(&mut content, "<div class=\"toc_item\">\n");

        if showdate {
            let date =
                match v.get("date") {
					Some(date) => format_date(date),
					None => String::from("")
				};
            let s = format!("  <p class=\"toc_item_date\">{}</p>\n", date);
			add(&mut content, &s);
		}

		let s = format!("  <p class=\"toc_item_title\"><a href=\"{}\">{}</a></p>", path, title);
		add(&mut content, &s);

        if showteaser {
            match v.get("teaser") {
				Some(teaser) =>
				{
					let s = format!("  <p class=\"toc_item_teaser\">{}</p>\n", teaser);
					add(&mut content, &s);
				},
				None =>
				{
					let s = format!("  <p class=\"toc_item_teaser\"></p>\n");
					add(&mut content, &s);
				}
			}
		}

        add(&mut content, "</div>\n");
	}

    content
}

fn crunch_magic(html: &str, items: &HashMap<String,HashMap<String,String>>) -> String {
    let mut t = String::from(html);
    let tcopy = String::from(html);

    let expr = r"\{@(?P<magic>[^{}]+)@}";
    let regx = Regex::new(expr).unwrap();
	for m in regx.captures_iter(&tcopy) {
		let magic = m["magic"].trim().to_lowercase();
		let parts : Vec<&str> = magic.split(',').map(|x| x.trim()).collect();
		let mut d = HashMap::new();
		for p in parts {
			let subparts : Vec<&str> = p.split('=').collect();
			let k = subparts[0].trim();
			let v = subparts[1].trim();
			d.insert(String::from(k), String::from(v));
		}
		let gen_type = &d["type"];
		let replacement =
			if gen_type == "toc" {
				make_toc(&d, items)
			} else if gen_type == "all" {
				make_toc(&d, items)
			} else {
				panic!("magic type: {:?}", gen_type);
			};
		let repl = &m[0];
		//dbg!(repl, &replacement);
		let tnew = t.replace(repl, &replacement);
		t = tnew;
	}
    t
}

fn do_file_with_magic(from: &Path, dest_path: &Path, layouts: &HashMap<String,String>, items: &HashMap<String,HashMap<String,String>>) -> Result<(), std::io::Error> {
	let html = std::fs::read_to_string(from)?;
	let (page_front_matter, src_content) = get_front_matter(&html);
    match page_front_matter {
		Some(mut page_front_matter) => {
			let tocs_done = crunch_magic(&src_content, items);
			let layout_name = get_layout_name(&page_front_matter);
			let after_crunch = wrap(layout_name, &mut page_front_matter, &tocs_done, layouts);
			write_if_changed(&after_crunch, &dest_path)?;
		},
		None =>
		{
			// TODO should never happen here
		}
	}
	Ok(())
}

fn do_file(url_dir: &str, path_from: &Path, dir_dest: &Path, layouts: &HashMap<String,String>, items: &mut HashMap<String,HashMap<String,String>>) -> Result<(),Box<dyn std::error::Error>>
{
	let name = path_from.file_name().unwrap();
	let dest_path = dir_dest.join(name);
	let name = name.to_string_lossy();
	if name.ends_with(".html") {
		let html = std::fs::read_to_string(path_from)?;
		let (page_front_matter, src_content) = get_front_matter(&html);
		match page_front_matter {
			Some(mut page_front_matter) => {
				if ! (page_front_matter.contains_key("gen")) {
					if ! (page_front_matter.contains_key("title")) {
						page_front_matter.insert(String::from("title"), String::from("Eric Sink"));
					}

					let layout_name = get_layout_name(&page_front_matter);
					let after_crunch = wrap(layout_name, &mut page_front_matter, &src_content, layouts);
					write_if_changed(&after_crunch, &dest_path)?;
				}

				let url_path = path_combine(url_dir, &name);
				items.insert(url_path, page_front_matter);
			},
			None => {
				copy_if_changed(path_from, &dest_path)?;
			}
		}
	} else if name.ends_with(".xml") {
		let html = std::fs::read_to_string(path_from)?;
		let (page_front_matter, src_content) = get_front_matter(&html);
		match page_front_matter {
			Some(page_front_matter) => {
				if page_front_matter.contains_key("gen") {
				} else {
					panic!();
				}

				let url_path = path_combine(url_dir, &name);
				items.insert(url_path, page_front_matter);
			},
			None => {
				copy_if_changed(path_from, &dest_path)?;
			}
		}
	} else {
		copy_if_changed(path_from, &dest_path)?;
	}
	//dbg!(path_from);
	Ok(())
}

fn do_clean(url_dir: &str, src: &Path, dest: &Path) -> Result<(),std::io::Error> {
    for entry in std::fs::read_dir(dest)? {
		let entry = entry?;
        let path = entry.path();

        let metadata = std::fs::metadata(&path)?;
		if metadata.is_file() {
			let name = path.file_name().unwrap();
			let src_path = src.join(&name);
			//let path = path_combine(url_dir, &name.to_str().unwrap());
			if !src_path.exists() {
				println!("delete {:?}", path);
				std::fs::remove_file(path)?;
			}
		}
	}

    for entry in std::fs::read_dir(dest)? {
		let entry = entry?;
        let path = entry.path();

        let metadata = std::fs::metadata(&path)?;
		if metadata.is_dir() {
			let name = path.file_name().unwrap();
			let src_sub = src.join(&name);
			let url_subdir = path_combine(url_dir, &name.to_str().unwrap());
			do_clean(&url_subdir, &src_sub, &path)?;
		}
	}

	Ok(())
}

fn do_dir(url_dir: &str, dir_src: &Path, dir_dest: &Path, layouts: &HashMap<String,String>, items: &mut HashMap<String,HashMap<String,String>>) -> Result<(),Box<dyn std::error::Error>>
{
	std::fs::create_dir_all(dir_dest)?;

	for entry in std::fs::read_dir(Path::new(dir_src))? {
		let entry = entry?;
        let path = entry.path();

        let metadata = std::fs::metadata(&path)?;
		if metadata.is_file() {
			do_file(url_dir, &path, dir_dest, layouts, items)?;
		}
	}

	for entry in std::fs::read_dir(Path::new(dir_src))? {
		let entry = entry?;
        let path = entry.path();

        let metadata = std::fs::metadata(&path)?;
		if metadata.is_dir() {
			let name = entry.file_name().into_string().unwrap();
			if name == "_layouts" {
			} else {
				let dest_sub = dir_dest.join(&name);
				let url_sub = path_combine(url_dir, &name);
				do_dir(&url_sub, &path, &dest_sub, layouts, items)?;
			}
		}
	}

	Ok(())
}

fn do_gen(dir_src : &Path, dir_dest : &Path, path : &str, layouts: &HashMap<String,String>, items: &HashMap<String,HashMap<String,String>>) -> Result<(),std::io::Error>  {
    let front_matter = &items[path];
    let gen_type = &front_matter["gen"];
    let path_dest = weird_path_combine(dir_dest, path);
    if gen_type == "rss" {
        let rss = make_rss(dir_src, items)?;
        write_if_changed(&rss, &path_dest)?;
    } else if gen_type == "magic" {
        let path_src = weird_path_combine(dir_src, path);
        let path_dest = weird_path_combine(dir_dest, path);
        do_file_with_magic(&path_src, &path_dest, layouts, items)?;
    } else {
        panic!("unknown gen type");
	}
	Ok(())
}

fn main() -> Result<(),Box<dyn std::error::Error>> 
{
    let args: Vec<String> = std::env::args().collect();
    let dir_src = Path::new(&args[1]);
    let dir_dest = Path::new(&args[2]);

    let layouts = 
    {
		let mut layouts = HashMap::new();

		for entry in std::fs::read_dir(dir_src.join("_layouts"))? {
			let entry = entry?;
			//println!("{:?}", entry.path().file_name().ok_or("none")?);
			let html = std::fs::read_to_string(entry.path())?;
			let name = String::from(entry.path().file_stem().unwrap().to_string_lossy());
			layouts.insert(name, html);
		}
		layouts
    };
	//dbg!(&layouts);

	let items =
	{
		let mut items = HashMap::<String,HashMap<String,String>>::new();

		do_dir("/", dir_src, dir_dest, &layouts, &mut items)?;

		items
	};

	do_clean("/", dir_src, dir_dest)?;

	let full_path_content = dir_src.canonicalize()?;

	for kv in &items {
		let (path, front_matter) = kv;
		if front_matter.contains_key("gen") {
			do_gen(&full_path_content, dir_dest, &path, &layouts, &items)?;
		}
	}

    Ok(())
}

