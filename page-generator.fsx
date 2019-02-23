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

let parsePosts (page:HtmlDocument) =
    page.Descendants ["a"]
    |> Seq.map (fun x -> x.TryGetAttribute("href") 
                        |> Option.map (fun a -> a.Value())
                        |> Option.get) 
    |> Seq.toArray

let composePostUrl (url:string) =
    baseUrl + url.[1..]

let executeRequest (url:string) =
    JsonValue.Load url
     
let redablePost (post: JsonValue) =
    "#### " + post?author.AsString() + "\n" +
    "" + toDateTime(post?date.AsInteger64()).ToString() + "\n\n" +  
    "" + post?content.AsString() + "\n"           

let downloadPost = composePostUrl >> executeRequest >> redablePost   

let createPage (index:int) posts =        
    File.WriteAllLines (@"posts"+index.ToString()+".md", posts)

loadPosts 
|> parsePosts
|> Array.map downloadPost 
|> Array.chunkBySize 10
|> Array.mapi createPage

#quit