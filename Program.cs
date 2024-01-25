using itey.Services;


// list contents of /dev
// var d = new DirectoryInfo("/dev");
// var files = d.GetFiles();
// Console.WriteLine("Files in /dev:");
// foreach (var file in files)
// {
//     Console.WriteLine(file.Name);
// }

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682



// Add services to the container.
builder.Services.AddGrpc();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<CommanderService>();
app.MapGet("/", () => "itey is running.");

app.Run();