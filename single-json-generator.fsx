#r "packages/FSharp.Data/lib/net45/FSharp.Data.dll"
open System
open System.IO
open FSharp.Data

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

let downloadPost = composePostUrl >> executeRequest

let toJsonString (post: JsonValue) =
    post.ToString()

let joinAll (posts: JsonValue[]) =
    posts
    |> Array.map toJsonString
    |> String.concat ", "
    |> sprintf "[%s]"

let writeToFile (postsJson: string) =
    File.WriteAllText ("posts.json", postsJson)

loadPosts 
|> extractPostUrls
|> Array.map downloadPost
|> joinAll
|> writeToFile

#quit
