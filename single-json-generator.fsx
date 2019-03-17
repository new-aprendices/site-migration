#r "packages/FSharp.Data/lib/net45/FSharp.Data.dll"
open System
open System.IO
open FSharp.Data

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

let toJsonString (post: JsonValue) =
    post.ToString()

let downloadPost = composePostUrl >> executeRequest >> toJsonString

let joinAll posts =
    posts
    |> String.concat ", "
    |> sprintf "[%s]"

let writeToFile name posts =
    File.WriteAllText (name, posts)

let generatePageName i =
    let index = match i with
                | 0 -> String.Empty
                | _ -> i.ToString()
    sprintf "posts%s.json" index

let createChunk generateName fileWriter index posts =
    let (pageName:string) = generateName index
                    
    fileWriter pageName posts

let createChunk' = createChunk generatePageName writeToFile

let posts = loadPosts 
            |> extractPostUrls
            |> Array.map downloadPost

let chunkSize = chunker posts.Length

posts 
|> Array.chunkBySize chunkSize 
|> Array.mapi (fun i p -> createChunk' i (p |> joinAll))

#quit
