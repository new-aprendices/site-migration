**How to run the script**

`mono .paket/paket.exe install`

To generate a single json file:
`fsharpi --use:json-generator.fsx`

To generate multiple json specifying the number of posts per file:
`fsharpi --use:json-generator.fsx "chunkBy=150"`

More info at https://www.mono-project.com/ and https://fsharp.org/use/linux/
