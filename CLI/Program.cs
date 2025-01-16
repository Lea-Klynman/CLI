using System.CommandLine;

static string[] EndFiles(string[] langarr)
{
    string st = "";
    foreach (string lang in langarr)
    {
        switch (lang)
        {
            case "c#":
                st += "cs ";
                break;
            case "c++":
                st += "cpp";
                break;
            case "c":
                st += "c";
                break;
            case "phyton":
                st += "py";
                break;
            case "all":
                st = "cs cpp c py";
                break;
            default:
                break;
        }
    }
    return st.Split(" ");
}
static bool isExsitFile(string[] arr, string file)
{
    for (int i = 0; i < arr.Length; i++)
    {
        if (arr[i] == file) { return true; }
    }
    return false;
}
var languageOption = new Option<string>(new[] { "--language","--l" }, "language files to bundle.") { IsRequired = true };
var bundleOption = new Option<FileInfo>(new[] { "--output" ,"--o"}, "File path and name.");
var noteOption = new Option<bool>(new[] { "--note" ,"--n","--i"}, "Include source file paths as comments in the bundle.");
var sortOption = new Option<string>(new[] { "--sort","--s" }, "Sort files by 'name' or 'type'. Default is 'name'.");//i need to add default
var removeEmptyLinesOption = new Option<bool>(new[] { "remove-empty-lines" ,"--r"}, "Remove empty lines from the source files.");

var bundelCommand = new Command("bundle", "bundle code files to single file");
var authorOption = new Option<string>("--author", "Specify the author name to include in the bundle header.");
bundelCommand.AddOption(languageOption);
bundelCommand.AddOption(bundleOption);
bundelCommand.AddOption(noteOption);
bundelCommand.AddOption(sortOption);
bundelCommand.AddOption(removeEmptyLinesOption);
bundelCommand.AddOption(authorOption);
bundelCommand.SetHandler((language, note, sort, rel, author, output) =>
{
    try
    {
        string[] arr = EndFiles(language.Split(" "));
        var dir = Directory.GetCurrentDirectory();
        List<string> files = Directory.GetFiles(dir).ToList();
        foreach (string file in files)
        {
            if (!isExsitFile(arr, Path.GetExtension(file).TrimStart('.'))||
            file.Contains("bin")||file.Contains("debug"))
            {
                files.Remove(file);
            }
        }
        if (sort == "type")
        {
            files = files.OrderBy(file => Path.GetExtension(file)).ToList();
        }
        else
        {
            files = files.OrderBy(file => Path.GetFileName(file)).ToList();
        }
        using (FileStream resFile = new FileStream(output.FullName, FileMode.CreateNew, FileAccess.Write))
        using (StreamWriter writer = new StreamWriter(resFile))
        {
            if (author != null && author.Length > 0)
            {
                writer.WriteLine($"Author: {author} ");
            }
            foreach (string file in files)
            {
                using (FileStream copyfile = new FileStream(file, FileMode.Open, FileAccess.Read))
                using (StreamReader reader = new StreamReader(copyfile))
                {
                    if (note)
                    {
                        writer.WriteLine($"// Source: {Path.GetRelativePath(dir, file)}");
                    }
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!(rel && string.IsNullOrWhiteSpace(line)))
                        {
                            writer.WriteLine(line);
                        }
                    }
                }

            }


        }
    }
    catch (DirectoryNotFoundException ex)
    {
        Console.WriteLine("path file is invalid");
    }
}, languageOption, noteOption, sortOption, removeEmptyLinesOption, authorOption, bundleOption);
var createRspCommand = new Command("create-rsp","create a response file for the bundle command");
createRspCommand.SetHandler(() =>
{
    var quesAns = new Dictionary<string, string> {
        {"language (comma-separated, or 'all'","" } ,    
        { "include source file paths as comments in the bundle ? (true / false)","false"},
        { "sort files by ('name' or 'type'","name"},
        { "remove empty lines from the source files ? (true / false)","false"},
        {"add Author to the file?(auther name)","" },
        { "output -File path and name",""} 
    };
    foreach (var key in quesAns.Keys)
    {
        Console.Write($"Enter {key}: ");
        quesAns[key]=Console.ReadLine() ?? quesAns[key];
    }
    if (string.IsNullOrWhiteSpace(quesAns["language (comma-separated, or 'all'"])||
    string.IsNullOrWhiteSpace(quesAns["output -File path and name"]))
    {
        Console.WriteLine("Error: 'language' and 'output' are required fields.");
        return;
    }
    var rspName = "bundle.rsp";
    using (FileStream resFile = new FileStream(rspName, FileMode.CreateNew, FileAccess.Write))
    using (StreamWriter writer = new StreamWriter(resFile))
    {
        foreach (var (key,value) in quesAns)
        {
            var option = key[0];
            writer.WriteLine($"--{option} {value}");
        }
    }
    Console.WriteLine($"Response File created successfully. Run it with fib @{rspName}");


});
var rootCommand = new RootCommand("Root command for bundler CLI");
rootCommand.AddCommand(bundelCommand);
rootCommand.AddCommand(createRspCommand);
rootCommand.InvokeAsync(args);

