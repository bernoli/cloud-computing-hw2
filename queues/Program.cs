using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace queues;

internal class Program
{
    private static void Main(string[] args)
    {

        var builder = WebApplication.CreateBuilder(args);

        var name = args.Any(a => a.Contains("--urls")) ? "A" : "B";
        var iPAddresses = new List<IPEndPoint>();
        if (args.Length > 0)
        {
            foreach (var arg in args)
            {
                Console.WriteLine(arg);

                if (IPEndPoint.TryParse(arg, out IPEndPoint ip))
                {
                    System.Console.WriteLine($"parsed Ip endpoint Address: [{ip}]");
                    iPAddresses.Add(ip);
                }
            }
        }

        var IpAddressesProvider = new IpAddressesProvider(iPAddresses);
        var _workersQueue = new Queue<WorkerItem>(IpAddressesProvider, name);
        var _completedQueue = new Queue<CompletedWork>(IpAddressesProvider, name);
        // Add services to the container.
        builder.Services.AddSingleton<IQueue<WorkerItem>>(_workersQueue);
        builder.Services.AddSingleton<IQueue<CompletedWork>>(_completedQueue);

        builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.PropertyNameCaseInsensitive = true);

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.WebHost.SuppressStatusMessages(true);
        var app = builder.Build();

        app.UseCors(builder =>
        {
            builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
        });
        // Configure the HTTP request pipeline.
        // if (app.Environment.IsDevelopment())
        // {
        app.UseSwagger();
        app.UseSwaggerUI();
        // Console.WriteLine("Dev");
        // }

        app.UseHttpsRedirection();

        // app.UseAuthorization();

        app.MapControllers();

        app.MapPost("pullcompleted", (HttpRequest request) =>
        {
            var topParameter = request.Query["top"];
            int top = 0;
            if (!string.IsNullOrEmpty(topParameter))
            {
                int.TryParse(topParameter, out top);
            }
            var search = string.Empty;
            if (request.Query.ContainsKey("search"))
            {
                search = request.Query["search"];
            }
            var completed = _completedQueue.RemoveTop(top, search);
            Console.WriteLine($"Top [{top}], Found [{completed.Count()}]");
            return completed;
        });

        app.MapGet("/size", () =>
        {
            return _workersQueue.Count;

        });
        app.MapPut("/enqueue", ([FromBody] string buffer, int iterations) =>
        {
            var workItem = new WorkerItem
            {
                Iterations = iterations,
                Buffer = Encoding.UTF8.GetBytes(buffer),
                Id = Guid.NewGuid().ToString(),
                RequestedtAt = DateTime.UtcNow,
            };

            _workersQueue.Enqueue(workItem);
            Console.WriteLine($"Enqueue [{workItem}]");
            return workItem.Id;
        });

        app.MapGet("/sizecompleted", () =>
        {
            Console.WriteLine($"Size of the queue is: {_completedQueue.Count}");
            return _completedQueue.Count;
        });

        app.MapGet("/dequeue", (HttpRequest request) =>
        {
            // if search paramter is stop, perform only local queue dequeue, otherwise search other nodes queue parts.
            var timeoutParameter = request.Query["timeout"];
            int timeout = 0;
            if (!string.IsNullOrEmpty(timeoutParameter))
            {
                int.TryParse(timeoutParameter, out timeout);
            }
            var search = string.Empty;
            if (request.Query.ContainsKey("search"))
            {
                search = request.Query["search"];
            }
            // var search = Request.Query["search"];
            Console.WriteLine($"Dequeue with parameters: timeout [{timeout}], search [{search}]");
            var work = _workersQueue.Dequeue(timeout, search);

            return work;
        });

        app.MapPut("/result", ([FromBody] string buffer, string id) =>
        {
            Console.WriteLine($"Completed work posted for work Id: [{id}], buffer = {buffer}");
            var completedWork = new CompletedWork()
            {
                Id = id,
                Result = Encoding.UTF8.GetBytes(buffer),
            };

            _completedQueue.Enqueue(completedWork);
            return HttpStatusCode.OK;
        });

        app.MapGet("/head", () =>
        {
            var firstMessage = _workersQueue.FirstMessage;
            Console.WriteLine($"First (oldest) item in the queue: [{firstMessage}]");
            return firstMessage;
        });

        app.Run();
    }
}