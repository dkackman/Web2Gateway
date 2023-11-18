using Web2Gateway;

// doing all of this in the mini-api expressjs-like approach
// instead of the IActionResult approach

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationInsightsTelemetry();

// when run as a service, we need an explicit path to a chia config
// file, which can be set in a appsettings.json file and passed on the command line
if (args.Any())
{
    var configurationBinder = new ConfigurationBuilder()
        .AddJsonFile(args.First());

    var config = configurationBinder.Build();
    builder.Configuration.AddConfiguration(config);
}

// Add services to the container.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ChiaService>();
builder.Services.AddSingleton<G2To3Service>();

var logger = LoggerFactory.Create(config =>
{
    config.AddConsole();
}).CreateLogger("Program");
var configuration = builder.Configuration;

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// the service end points are defined in here
app.ConfigureApi(logger)
    .UseCors()
    //.UseHttpsRedirection()
    .UseAuthorization();
app.MapControllers();
app.Run();
