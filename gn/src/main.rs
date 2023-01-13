
use mybatch::System;
use mybatch::GetIter;
use mybatch::AsBase;
use mybatch::AsInterface;
use mybatch::CloneHandle;
use System::Collections::Generic::Dictionary_2 as Dictionary;
use System::Collections::Generic::List_1 as List;
use System::Collections::Generic::KeyValuePair_2 as KeyValuePair;
use System::Collections::Generic::IEnumerable_1 as IEnumerable;
use System::String as NString;
use System::IO::Directory as Directory;
use System::IO::File as File;
use System::IO::FileInfo as FileInfo;
use System::IO::Path as Path;
use System::Text::StringBuilder;

trait StringExtensions {
	fn starts_with(&self, s : &str) -> Result<bool, System::Exception>;
	fn ends_with(&self, s : &str) -> Result<bool, System::Exception>;
	fn index_of(&self, s : &str) -> Result<i32, System::Exception>;
	fn split(&self, c : char) -> Result<mybatch::Arr<NString>, System::Exception>;
}

impl StringExtensions for NString {
	fn starts_with(&self, s : &str) -> Result<bool, System::Exception> {
        return self.StartsWith_String(NString::from(s));
	}
	fn ends_with(&self, s : &str) -> Result<bool, System::Exception> {
        return self.EndsWith_String(NString::from(s));
	}
	fn index_of(&self, s : &str) -> Result<i32, System::Exception> {
        return self.IndexOf_String(NString::from(s));
	}
	fn split(&self, c : char) -> Result<mybatch::Arr<NString>, System::Exception> {
		let mut b = [0; 1];
		// this will panic if it doesn't fit
		let b2 = c.encode_utf16(&mut b);
		let c2 = b2[0] as i16;
		return self.Split_Char_StringSplitOptions(System::Char::from_value(c2), System::StringSplitOptions::None());
	}
}

// an implementation of Path.Combine which always uses fwd slash
fn path_combine(a : &NString, b : &NString) -> Result<NString,System::Exception> {
	if a.get_Length()? == 0 {
		Ok(b.clone_handle())
	} else if b.starts_with("/")? {
		Ok(b.clone_handle())
	} else if a.ends_with("/")? {
		let s = System::String::Concat_String_String(Some(a), Some(b))?;
		Ok(s)
	} else {
		let s = System::String::Concat_String_String_String(Some(a), Some(NString::from("/")), Some(b))?;
		Ok(s)
	}
}

fn get_front_matter(s : &NString) -> Result<(Option<Dictionary<NString,NString>>, NString),System::Exception> {
	let marker_front_lf = "---\n";
	let marker_front_crlf = "---\r\n";
	if (s.starts_with(marker_front_lf)?) || (s.starts_with(marker_front_crlf)?) {
		let s2 =
		{
			// whether it was lf or crlf, we can just find the lf and chop there
			let n = s.index_of("\n")?; // TODO could assert, must be > 0
			s.Substring_i32(n)?
		};
		// now find the other marker

		let (n, len) =
		{
			// the second marker should match \n--- for either EOL
			let marker_back_lf = "\n---\n";
			let marker_back_crlf = "\n---\r\n";
			let n_lf = s2.index_of(marker_back_lf)?;
			let n_crlf = s2.index_of(marker_back_crlf)?;
			if n_lf >= 0 {
				if n_crlf >= 0 {
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
			} else if n_crlf >= 0 {
				(n_crlf, marker_back_crlf.len())
			} else {
				panic!("second front matter marker not found");
			}
		};

		let s3 = s2.Substring_i32_i32(0, n)?;
		let remain = s2.Substring_i32(n + (len as i32))?;

		// split on lf should work for either eol here.
		// the cr will remain, but it gets trimmed out.
		let a = s3.split('\n')?;
		let d = Dictionary::ctor()?;
		for pair in a.iter()? {
			if pair.get_Length()? > 0 {
				let n_colon = pair.index_of(":")?;
				let k = pair.Substring_i32_i32(0, n_colon)?.Trim()?;
				let v = pair.Substring_i32(n_colon + 1)?.Trim()?;
				// TODO we may want to allow v to be empty string or null
				if (k.get_Length()? > 0) && (v.get_Length()? > 0) {
					d.Add(k, v)?;
				}
			}
		}
		Ok((Some(d), remain))
	} else {
		let t = s.clone_handle();
		Ok((None, t))
	}
}

fn parse_date(s: &NString) -> Result<System::DateTime,System::Exception> {
	let twoparts = s.split(' ')?;
    let dateparts = twoparts.get_item(0)?.split('-')?;
    let timeparts = twoparts.get_item(1)?.split(':')?;
	let year = System::Int32::Parse_String(dateparts.get_item(0)?)?;
	let month = System::Int32::Parse_String(dateparts.get_item(1)?)?;
	let day = System::Int32::Parse_String(dateparts.get_item(2)?)?;
	let hour = System::Int32::Parse_String(timeparts.get_item(0)?)?;
	let min = System::Int32::Parse_String(timeparts.get_item(1)?)?;
	let sec = System::Int32::Parse_String(timeparts.get_item(2)?)?;

	let d = System::DateTime::ctor_i32_i32_i32_i32_i32_i32(year, month, day, hour, min, sec)?;
	Ok(d)
}

fn format_date_rss(s: &NString) -> Result<NString,System::Exception> {
    //<pubDate>{{{loop.datefiled:format='ddd, dd MMM yyyy HH:mm:ss CST'}}}</pubDate>
    let d = parse_date(s)?;
    d.ToString_String(Some(NString::from("ddd, dd MMM yyyy HH:mm:ss CST")))
}

fn format_date(s: &NString) -> Result<NString,System::Exception> {
    let d = parse_date(s)?;
    d.ToString_String(Some(NString::from("dddd, d MMMM yyyy")))
}

fn crunch(html: &NString, content: &NString, pairs: &Dictionary<NString,Dictionary<NString,NString>>) -> Result<NString,System::Exception> {
    let mut t = html.clone_handle();

    let expr = "{{(?<v>[^{}]+)}}";
    let regx = System::Text::RegularExpressions::Regex::ctor_String(NString::from(expr))?;
    let a = regx.Matches_String(&t)?;
	for m in a.get_iter() {
		let grp = m.get_Groups()?.get_Item_String(NString::from("v"))?;
		let cap : System::Text::RegularExpressions::Capture = grp.as_base();
		let v = cap.get_Value()?.Trim()?.ToLower()?;
		let replacement =
			if v == "content" {
				// special case
				content.clone_handle()
			}
			else {
				let parts = v.split('.')?;
				let section = parts.get_item(0)?;
				let k = parts.get_item(1)?;
				pairs.get_Item(section)?.get_Item(k)?
			};
		//dbg!(&m);
		let m_cap : System::Text::RegularExpressions::Capture = m.as_base();
		let repl = m_cap.get_Value()?;
		t = t.Replace_String_String(repl, Some(replacement))?;
	}
    Ok(t)
}

fn add_pair(d: &mut Dictionary<NString,Dictionary<NString,NString>>, section: &str, k: &str, v: &str) -> Result<(),System::Exception> {
	match d.MyGet(NString::from(section))? {
		Some(d3) => {
			d3.Add(NString::from(k), NString::from(v))?;
		},
		None =>
		{
			let d3 = Dictionary::ctor()?;
			d3.Add(NString::from(k), NString::from(v))?;
			d.Add(NString::from(section), d3)?;
		},
	};
	Ok(())
}

fn add_site_defaults(d: &mut Dictionary<NString,Dictionary<NString,NString>>) -> Result<(),System::Exception> {
    add_pair(d, "site", "title", "Eric Sink")?;
    add_pair(d, "site", "tagline", "SourceGear Founder")?;
    add_pair(d, "site", "copyright", "Copyright 2001-2023 Eric Sink. All Rights Reserved")?;
    Ok(())
}

// dir is a platform path
// uri_path is forward-slashes
// result is a platform path
fn weird_path_combine(dir: &NString, uri_path: &NString) -> Result<NString,System::Exception> {
    // TODO windows-specific code below
    let path_fixed = uri_path.Substring_i32(1)?.Replace_String_String(NString::from("/"), Some(NString::from("\\")))?;
    let path = Path::Combine_String_String(dir, path_fixed)?;
    Ok(path)
}

fn read_from_src(dir_src: &NString, uri_path: &NString) -> Result<NString,System::Exception> {
    let path_content = weird_path_combine(dir_src, uri_path)?;
	let html = File::ReadAllText_String(&path_content)?;
    Ok(html)
}

fn make_rss(dir_src: &NString, items: &Dictionary<NString,Dictionary<NString,NString>>) -> Result<NString,System::Exception> {
    fn add_string(sb: &System::Text::StringBuilder, s: &NString) -> Result<(),System::Exception> {
		sb.Append_String(Some(s))?;
		Ok(())
	}

    fn add(sb: &System::Text::StringBuilder, s: &str) -> Result<(),System::Exception> {
		add_string(sb, &NString::from(s))?;
		Ok(())
	}

    let mut content = System::Text::StringBuilder::ctor()?;

    add(&mut content, "<?xml version=\"1.0\" encoding=\"iso-8859-1\" ?>")?;
    add(&mut content, "<rss version=\"2.0\">")?;
    add(&mut content, "<channel>")?;
    add(&mut content, "<title>{{site.title}}</title>")?;
    add(&mut content, "<link>https://ericsink.com/</link>")?;
    add(&mut content, "<description>{{site.tagline}}</description>")?;
    add(&mut content, "<copyright>{{site.copyright}}</copyright>")?;
    add(&mut content, "<generator>mine</generator>")?;

	let a = List::ctor()?;
	for kv in items.get_iter() {
		let v = kv.get_Value()?;
		if v.ContainsKey(NString::from("date"))? {
			a.Add(kv)?;
		}
	}

	let z_enumerable : IEnumerable<KeyValuePair<NString,Dictionary<NString,NString>>> = a.as_interface();
	let f_orderby = System::Func_2::<KeyValuePair<NString,Dictionary<NString,NString>>, NString>::create(
        |kv| kv.get_Value()?.get_Item(NString::from("date"))
        )?;
	let z_ordered = System::Linq::Enumerable::OrderByDescending_System_Collections_Generic_IEnumerable_UTSource__System_Func_UTSource_UTKey_(z_enumerable, f_orderby)?;
	let z2_enumerable : IEnumerable<KeyValuePair<NString,Dictionary<NString,NString>>> = z_ordered.as_interface();
	let z_take : IEnumerable<KeyValuePair<NString,Dictionary<NString,NString>>> = System::Linq::Enumerable::Take_System_Collections_Generic_IEnumerable_UTSource__i32(z2_enumerable, 10)?;
	let a = z_take;

    for kv in a.get_iter() {
        let path = kv.get_Key()?;
        let v = kv.get_Value()?;
        let title = match v.MyGet(NString::from("title"))? {
			Some(t) => t,
			None => NString::from(""),
		};
        let date = v.get_Item(NString::from("date"))?;

        let html = read_from_src(dir_src, &path)?;
        let (_, my_content) = get_front_matter(&html)?;
		let local_link = System::String::Format_String_Object(NString::from("https://ericsink.com{0}"), Some(path.as_base()))?;

        add(&mut content, "<item>")?;
        add(&mut content, "<title>")?;
        add_string(&mut content, &title)?;
        add(&mut content, "</title>")?;
        add(&mut content, "<guid>")?;
        add_string(&mut content, &local_link)?;
        add(&mut content, "</guid>")?;
        add(&mut content, "<link>")?;
        add_string(&mut content, &local_link)?;
        add(&mut content, "</link>")?;
        add(&mut content, "<pubDate>")?;
        add_string(&mut content, &format_date_rss(&date)?)?;
        add(&mut content, "</pubDate>")?;
        add(&mut content, "<description>")?;
        add(&mut content, "<![CDATA[")?;
        add_string(&mut content, &my_content)?;
        add(&mut content, "]]>")?;
        add(&mut content, "</description>")?;
        add(&mut content, "</item>")?;
	}

	add(&mut content, "</channel>")?;
    add(&mut content, "</rss>")?;

    let mut pairs = Dictionary::<NString,Dictionary<NString,NString>>::ctor()?;
    add_site_defaults(&mut pairs)?;
	let s = content.ToString()?;
    let result = crunch(&s, &NString::from(""), &pairs)?;
    Ok(result)
}

fn write_if_changed(content : &NString, dest : &NString) -> Result<(),System::Exception> {
    let different =
        if File::Exists(Some(dest))? {
            let cur = File::ReadAllText_String(dest)?;
            cur != *content
		} else {
            true
		};
    if different {
        println!("write {:?}", dest);
		File::WriteAllText_String_String(dest, Some(content))?;
	}
	Ok(())
}

fn copy_if_changed(src : &NString, dest : &NString) -> Result<(),System::Exception> {
    let different =
        if File::Exists(Some(dest))? {
            let fi_src = FileInfo::ctor_String(src)?;
            let fi_dest = FileInfo::ctor_String(dest)?;
            if fi_src.get_Length()? != fi_dest.get_Length()? {
                true
            } else {
                let ba_src = 
				{
					let buf = File::ReadAllBytes(src)?;
					buf
				};
                let ba_dest = 
				{
					let buf = File::ReadAllBytes(dest)?;
					buf
				};
                ba_src.iter()?.ne(ba_dest.iter()?)
			}
        } else {
            true
		};
    if different {
        println!("copy {:?}", dest);
        File::Copy_String_String_bool(src, dest, true)?;
	}
	Ok(())
}

fn get_layout_name(front_matter : &Dictionary<NString,NString>) -> Result<Option<NString>,System::Exception> {
	match front_matter.MyGet(NString::from("layout"))? {
	Some(layout_name) => {
		if layout_name == "null" {
			Ok(None)
		} else {
			Ok(Some(NString::from(layout_name)))
		}
	},
	None => Ok(None)
	}
}

fn wrap(layout_name : Option<NString>, page_front_matter : &Dictionary<NString,NString>, src_content : &NString, layouts: &Dictionary<NString,NString>) -> Result<NString,System::Exception> {
    let mut pairs = Dictionary::ctor()?;
    add_site_defaults(&mut pairs)?;
    pairs.Add(NString::from("page"), page_front_matter.clone())?;

	let src_content = src_content.clone_handle();

    let (next_layout, before_crunch, content) =
        match layout_name {
			Some(layout_name) =>
			{
				let layout = layouts.get_Item(layout_name)?;
				let (template_front, template_html) = get_front_matter(&layout)?;
				let next_layout =
					match template_front {
					Some(fm) =>
					{
						let r = get_layout_name(&fm)?;
						pairs.Add(NString::from("layout"), fm)?;
						r
					},
					None =>
						None
					};
				(next_layout, template_html, src_content)
			},
			None =>
			{
				(None, src_content, NString::from(""))
			}
		};

    let after_crunch = crunch(&before_crunch, &content, &pairs)?;
    match next_layout {
		Some(next_layout_name) =>
			Ok(wrap(Some(next_layout_name), page_front_matter, &after_crunch, layouts)?),
		None =>
			Ok(after_crunch)
	}
}

fn make_toc (magic: &Dictionary<NString,NString>, items: &Dictionary<NString,Dictionary<NString,NString>>) -> Result<NString,System::Exception> {
    fn add(sb: &StringBuilder, s: &str) -> Result<(),System::Exception> {
		sb.Append_String(Some(NString::from(s)))?;
		Ok(())
	}

    let showdate =
        match magic.MyGet(NString::from("showdate"))? {
			Some(v) => v == "true", // TODO parse bool
			None => true
		};
    let showteaser =
        match magic.MyGet(NString::from("showteaser"))? {
			Some(v) => v == "true", // TODO parse bool
			None => true
		};
    let sortby =
        match magic.MyGet(NString::from("sortby"))? {
			Some(s) => s,
			None => NString::from("date")
		};

    // TODO allow multiple included keywords here?
    // TODO allow keyword exclusion here?

    fn has_kw (fm: &Dictionary<NString,NString>, kw :&NString) -> Result<bool,System::Exception> {
        match fm.MyGet(NString::from("keywords"))? {
			Some(keywords) =>
			{
				//let keywords = &fm["keywords"];
				let h = System::Collections::Generic::HashSet_1::<NString>::ctor()?;
				for k in 
				keywords.split(' ')?
					.iter()?
					.map(|x| x.Trim().unwrap())
				{
					h.Add(NString::from(k))?;
				}
				let b = h.Contains(kw)?;
				Ok(b)
			},
			None =>
				Ok(false)
		}
	}

	let filtered = List::ctor()?;
	match magic.MyGet(NString::from("keyword"))? {
		Some(kw_include) => 
		{
			for kv in items.get_iter() {
				let v = kv.get_Value()?;
				if has_kw(&v, &kw_include)? {
					filtered.Add(kv)?;
				}
			}
		},
		None => 
		{
			for kv in items.get_iter() {
				filtered.Add(kv)?;
			}
		}
	};

	let z_enumerable : IEnumerable<KeyValuePair<NString,Dictionary<NString,NString>>> = filtered.as_interface();

	let f_where = System::Func_2::<KeyValuePair<NString,Dictionary<NString,NString>>, bool>::create(
		|kv| kv.get_Value()?.ContainsKey(&sortby)
        )?;
	let z_where = System::Linq::Enumerable::Where_System_Collections_Generic_IEnumerable_UTSource__System_Func_UTSource_bool_(z_enumerable, f_where)?;

	let f_orderby = System::Func_2::<KeyValuePair<NString,Dictionary<NString,NString>>, NString>::create(
        |kv| kv.get_Value()?.get_Item(&sortby) 
        )?;
	let z_ordered = System::Linq::Enumerable::OrderByDescending_System_Collections_Generic_IEnumerable_UTSource__System_Func_UTSource_UTKey_(z_where, f_orderby)?;

	let filtered = z_ordered;

    let mut content = System::Text::StringBuilder::ctor()?;

    for kv in filtered.get_iter() {
		let path = kv.get_Key()?;
		let v = kv.get_Value()?;
        let title = match v.MyGet(NString::from("title"))? {
			Some(t) => t,
			None => NString::from(""),
		};

        add(&mut content, "<div class=\"toc_item\">\n")?;

        if showdate {
            let date =
                match v.MyGet(NString::from("date"))? {
					Some(date) => format_date(&date)?,
					None => NString::from("")
				};
            let s = format!("  <p class=\"toc_item_date\">{}</p>\n", date);
			add(&mut content, &s)?;
		}

		let s = format!("  <p class=\"toc_item_title\"><a href=\"{}\">{}</a></p>", path, title);
		add(&mut content, &s)?;

        if showteaser {
            match v.MyGet(NString::from("teaser"))? {
				Some(teaser) =>
				{
					let s = format!("  <p class=\"toc_item_teaser\">{}</p>\n", teaser);
					add(&mut content, &s)?;
				},
				None =>
				{
					let s = format!("  <p class=\"toc_item_teaser\"></p>\n");
					add(&mut content, &s)?;
				}
			}
		}

        add(&mut content, "</div>\n")?;
	}

    Ok(content.ToString()?)
}

fn crunch_magic(html: &NString, items: &Dictionary<NString,Dictionary<NString,NString>>) -> Result<NString,System::Exception> {
	let mut t = html.clone_handle();

    let expr = "{@(?<magic>[^{}]+)@}";
    let regx = System::Text::RegularExpressions::Regex::ctor_String(NString::from(expr))?;
    let a = regx.Matches_String(&t)?;
	for m in a.get_iter() {
		let grp = m.get_Groups()?.get_Item_String(NString::from("magic"))?;
		let cap : System::Text::RegularExpressions::Capture = grp.as_base();
		let magic = cap.get_Value()?.Trim()?.ToLower()?;
		let d = Dictionary::ctor()?;
		for p in 
		magic.split(',')?
			.iter()?
			.map(|x| x.Trim().unwrap())
		{
			let subparts = p.split('=')?;
			let k = subparts.get_item(0)?.Trim()?;
			let v = subparts.get_item(1)?.Trim()?;
			d.Add(k, v)?;
		}
		let gen_type = d.get_Item(NString::from("type"))?;
		let replacement =
			if gen_type == "toc" {
				make_toc(&d, items)?
			} else if gen_type == "all" {
				make_toc(&d, items)?
			} else {
				panic!("magic type: {:?}", gen_type);
			};
		let m_cap : System::Text::RegularExpressions::Capture = m.as_base();
		let repl = m_cap.get_Value()?;
		//let repl = m.get_Groups()?.get_Item_String(NString::from("magic"))?;
		//dbg!(repl, &replacement);
		let tnew = t.Replace_String_String(repl, Some(&replacement))?;
		t = tnew;
	}
    Ok(t)
}

fn do_file_with_magic(from: &NString, dest_path: &NString, layouts: &Dictionary<NString,NString>, items: &Dictionary<NString,Dictionary<NString,NString>>) -> Result<(), System::Exception> {
	let html = File::ReadAllText_String(from)?;
	let (page_front_matter, src_content) = get_front_matter(&html)?;
    match page_front_matter {
		Some(mut page_front_matter) => {
			let tocs_done = crunch_magic(&src_content, items)?;
			let layout_name = get_layout_name(&page_front_matter)?;
			let after_crunch = wrap(layout_name, &mut page_front_matter, &tocs_done, layouts)?;
			write_if_changed(&after_crunch, &dest_path)?;
		},
		None =>
		{
			// TODO should never happen here
		}
	}
	Ok(())
}

fn do_file(url_dir: &NString, path_from: &NString, dir_dest: &NString, layouts: &Dictionary<NString,NString>, items: &mut Dictionary<NString,Dictionary<NString,NString>>) -> Result<(),System::Exception>
{
	let name = Path::GetFileName_String(Some(path_from))?.unwrap();
	let dest_path = Path::Combine_String_String(dir_dest, &name)?;
	if name.ends_with(".html")? {
		let html = File::ReadAllText_String(path_from)?;
		let (page_front_matter, src_content) = get_front_matter(&html)?;
		match page_front_matter {
			Some(mut page_front_matter) => {
				if ! (page_front_matter.ContainsKey(NString::from("gen")))? {
					if ! (page_front_matter.ContainsKey(NString::from("title")))? {
						page_front_matter.Add(NString::from("title"), NString::from("Eric Sink"))?;
					}

					let layout_name = get_layout_name(&page_front_matter)?;
					let after_crunch = wrap(layout_name, &mut page_front_matter, &src_content, layouts)?;
					write_if_changed(&after_crunch, &dest_path)?;
				}

				let url_path = path_combine(url_dir, &name)?;
				items.Add(url_path, page_front_matter)?;
			},
			None => {
				copy_if_changed(path_from, &dest_path)?;
			}
		}
	} else if name.ends_with(".xml")? {
		let html = File::ReadAllText_String(path_from)?;
		let (page_front_matter, _) = get_front_matter(&html)?;
		match page_front_matter {
			Some(page_front_matter) => {
				if page_front_matter.ContainsKey(NString::from("gen"))? {
				} else {
					panic!();
				}

				let url_path = path_combine(url_dir, &name)?;
				items.Add(url_path, page_front_matter)?;
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

fn do_clean(url_dir: &NString, src: &NString, dest: &NString) -> Result<(),System::Exception> {
	for path in Directory::GetFiles_String(dest)?.iter()? {
		let name = Path::GetFileName_String(Some(&path))?.unwrap();
		let src_path = Path::Combine_String_String(src, name)?;
		//let path = path_combine(url_dir, &name.to_str().unwrap());
		if !File::Exists(Some(src_path))? {
			println!("delete {:?}", path);
			File::Delete(path)?;
		}
	}

	for path in Directory::GetDirectories_String(dest)?.iter()? {
		let name = Path::GetFileName_String(Some(&path))?.unwrap();
		let src_sub = Path::Combine_String_String(src, &name)?;
		let url_subdir = path_combine(url_dir, &name)?;
		do_clean(&url_subdir, &src_sub, &path)?;
	}

	Ok(())
}

fn do_dir(url_dir: &NString, dir_src: &NString, dir_dest: &NString, layouts: &Dictionary<NString,NString>, items: &mut Dictionary<NString,Dictionary<NString,NString>>) -> Result<(),System::Exception>
{
	Directory::CreateDirectory_String(dir_dest)?;

	for path in Directory::GetFiles_String(dir_src)?.iter()? {
		do_file(url_dir, &path, dir_dest, layouts, items)?;
	}

	for path in Directory::GetDirectories_String(dir_src)?.iter()? {
		let name = Path::GetFileName_String(Some(&path))?.unwrap();
		if name == "_layouts" {
		} else {
			let dest_sub = Path::Combine_String_String(dir_dest, &name)?;
			let url_sub = path_combine(url_dir, &name)?;
			do_dir(&url_sub, &path, &dest_sub, layouts, items)?;
		}
	}

	Ok(())
}

fn do_gen(dir_src : &NString, dir_dest : &NString, path : &NString, layouts: &Dictionary<NString,NString>, items: &Dictionary<NString,Dictionary<NString,NString>>) -> Result<(),System::Exception>  {
    let front_matter = items.get_Item(path)?;
    let gen_type = front_matter.get_Item(NString::from("gen"))?;
    let path_dest = weird_path_combine(dir_dest, path)?;
    if gen_type == "rss" {
        let rss = make_rss(dir_src, items)?;
        write_if_changed(&rss, &path_dest)?;
    } else if gen_type == "magic" {
        let path_src = weird_path_combine(dir_src, path)?;
        let path_dest = weird_path_combine(dir_dest, path)?;
        do_file_with_magic(&path_src, &path_dest, layouts, items)?;
    } else {
        panic!("unknown gen type");
	}
	Ok(())
}

fn main() -> Result<(),System::Exception> 
{
    let args: Vec<std::string::String> = std::env::args().collect();
    let dir_src = NString::from(&args[1] as &str);
    let dir_dest = NString::from(&args[2] as &str);

    let layouts = 
    {
		let layouts = Dictionary::<NString,NString>::ctor()?;

		for f in Directory::GetFiles_String(Path::Combine_String_String(&dir_src, NString::from("_layouts"))?)?.iter()? {
			//println!("{:?}", entry.path().file_name().ok_or("none")?);
			let html = File::ReadAllText_String(&f)?;
			let basename = Path::GetFileNameWithoutExtension_String(Some(f))?.unwrap();
			layouts.Add(basename, html)?;
		}
		layouts
    };
	//dbg!(&layouts);

	let items =
	{
		let mut items = Dictionary::<NString,Dictionary<NString,NString>>::ctor()?;

		do_dir(&NString::from("/"), &dir_src, &dir_dest, &layouts, &mut items)?;

		items
	};

	do_clean(&NString::from("/"), &dir_src, &dir_dest)?;

	let full_path_content = Path::GetFullPath_String(dir_src)?;

	for kv in items.get_iter() {
        let path = kv.get_Key()?;
        let front_matter = kv.get_Value()?;
		if front_matter.ContainsKey(NString::from("gen"))? {
			do_gen(&full_path_content, &dir_dest, &path, &layouts, &items)?;
		}
	}

    Ok(())
}

