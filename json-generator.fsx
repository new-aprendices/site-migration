#r "packages/FSharp.Data/lib/net45/FSharp.Data.dll"
open System
open System.IO
open FSharp.Data
open FSharp.Data.JsonExtensions

type Post = { Year : int; Content : string }

let toDateTime (timestamp:int64) =
    let start = DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    start.AddMilliseconds(float timestamp).ToLocalTime()    

let parameters = Environment.GetCommandLineArgs()
let chunkPostsBy = Array.tryFind (fun (x:string) -> x.StartsWith("chunkBy=")) parameters

let chunker = match chunkPostsBy with
              | Some x -> fun _ ->  x.Split [|'='|] |> Array.last |> int
              | None -> fun y -> y

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
    JsonValue.Load url        

let extractYear (post: JsonValue) =   
    let dt = post?date.AsInteger64() |> toDateTime 
    dt.Year 

let toPost (post: JsonValue) =
    let year = post |> extractYear  
    {Year = year; Content= post.ToString()}

let downloadPost = composePostUrl >> executeRequest >> toPost

let joinAll posts =
    posts
    |> Array.map (fun post -> post.Content)
    |> String.concat ", "
    |> sprintf "[%s]"

let writeToFile name posts =
    File.WriteAllText (name, posts)

let generateFileName year =
    match year with
    | 2019 -> "posts.json"
    | _ -> "posts_old.json"

let createFile generateFileName fileWriter index posts =
    let (pageName:string) = generateFileName index
                    
    fileWriter pageName posts

let createFile' = createFile generateFileName writeToFile

let createFiles (currentYearPosts,oldPosts) = 
    currentYearPosts |> joinAll |> createFile' 2019
    oldPosts |> joinAll |> createFile' 0


let posts = loadPosts 
            |> extractPostUrls
            |> Array.map downloadPost            

posts
|> Array.partition (fun post -> post.Year = 2019)
|> createFiles

#quit
