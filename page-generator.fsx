#r "packages/FSharp.Data/lib/net45/FSharp.Data.dll"
open System
open System.IO
open FSharp.Data
open FSharp.Data.JsonExtensions

let toDateTime (timestamp:int64) =
    let start = DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    start.AddMilliseconds(float timestamp).ToLocalTime()
    
let baseUrl = "http://goyim.dnsdojo.org/aprendices"

let loadPosts = HtmlDocument.Load(baseUrl+"/list.html")

let extractPostUrls (page:HtmlDocument) =
    page.Descendants ["a"]
    |> Seq.map (fun x -> x.TryGetAttribute("href") 
                        |> Option.map (fun a -> a.Value())
                        |> Option.get) 
    |> Seq.rev
    |> Seq.toArray

let composePostUrl (url:string) =
    baseUrl + url.[1..]

let executeRequest (url:string) =
    // printfn "- Downloading %s with thread: %i" url Threading.Thread.CurrentThread.ManagedThreadId 
    JsonValue.Load url        

let replaceUrlWithHtmlLink (content:string) url =
    let newUrl = String.Format (@"<a target=""_blank"" href=""{0}"">{1}</a>", url, url)
    content.Replace(url, newUrl)

let normaliseUrl (content:string) =
    content.Split([|" "|], System.StringSplitOptions.RemoveEmptyEntries)
    |> Array.filter (fun x -> x.StartsWith("http"))
    |> Array.fold replaceUrlWithHtmlLink content

let redablePost (post: JsonValue) =
    let author, date, content, category = (post?author.AsString(), toDateTime(post?date.AsInteger64()).ToString(), post?content.AsString(), post?category.AsString())
    let content = normaliseUrl content

    String.Format ("**{0}** - *{1}* - {2}\n\n{3}\n\n", author, category, date, content)

let downloadPost = composePostUrl >> executeRequest >> redablePost   

let createPage index posts lastPage =
    let pageName i =
        match i with
        | 0 -> @"index.md"
        | _ -> @"index"+i.ToString()+".md"

    let nextPageLink = String.Format ("[Next page]({0})", (pageName (index+1)))

    let posts' = match lastPage with
                    | true -> posts
                    | false -> Array.append posts [|nextPageLink|]

    File.WriteAllLines (pageName index, posts')
    
let createSite (list: string[][]) =
    let isLastPage i = (i + 1) = list.Length
    list |> Array.mapi (fun index posts -> createPage index posts (isLastPage index))

loadPosts 
|> extractPostUrls
|> Array.map downloadPost 
|> Array.chunkBySize 50
|> createSite

#quit