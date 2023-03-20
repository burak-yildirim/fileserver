module FileServer.Program

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.Extensions.FileProviders
open Microsoft.Extensions.Hosting

let notFoundHandler: HttpHandler =
    Response.withStatusCode 404
    >> Response.ofPlainText "Not Found"

let staticFilesMiddleware path (app: IApplicationBuilder) =
    let fileProvider = new PhysicalFileProvider(path)
    let requestPath = ""

    app.UseStaticFiles() |> ignore

    app.UseStaticFiles(
        new StaticFileOptions(
            FileProvider = fileProvider,
            RequestPath = requestPath,
            ServeUnknownFileTypes = true,
            DefaultContentType = "application/octet-stream"
        )
    )
    |> ignore

    app.UseDirectoryBrowser(new DirectoryBrowserOptions(FileProvider = fileProvider, RequestPath = requestPath))

[<EntryPoint>]
let main args =
    let dirPath =
        match Array.tryItem 0 args with
        | Some path -> path
        | None -> Environment.CurrentDirectory

    printfn "Path to serve: %s" dirPath

    let port =
        match Array.tryItem 1 args with
        | Some port -> port
        | None -> "4445"

    printfn "Serving on port %s" port

    let url = sprintf "http://0.0.0.0:%s" port

    try
        webHost [| "--urls"; url |] {
            not_found notFoundHandler

            use_middleware (staticFilesMiddleware dirPath)
        }
    with
    | :? ArgumentException ->
        printfn "The path must be absolute. E.g. \"/home/user\" for linux and \"C:\\MyDir for windows"
    | :? DirectoryNotFoundException -> printfn "Directory %s not found" dirPath
    | _ -> printfn "Unknown error"

    0 // Exit code
