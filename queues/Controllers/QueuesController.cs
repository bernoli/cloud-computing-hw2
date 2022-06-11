// using Microsoft.AspNetCore.Mvc;
// using System.Text;

// namespace queues.Controllers;

// // [ApiController]
// public class QueuesController : ControllerBase
// {
//     private readonly IQueue<WorkerItem> _workersQueue;
//     private readonly IQueue<CompletedWork> _completedQueue;
//     private readonly ILogger<QueuesController> _logger;


//     public QueuesController(ILogger<QueuesController> logger, IQueue<WorkerItem> workersQueue, IQueue<CompletedWork> completedQueue)
//     {
//         _logger = logger;
//         _workersQueue = workersQueue;
//         _completedQueue = completedQueue;
//     }

//     // [HttpPut("enqueue")]
//     // public IActionResult enqueue([FromBody] string buffer, int iterations)
//     // {
//     //     var workItem = new WorkerItem
//     //     {
//     //         Iterations = iterations,
//     //         Buffer = Encoding.UTF8.GetBytes(buffer),
//     //         Id = Guid.NewGuid().ToString(),
//     //         RequestedtAt = DateTime.UtcNow,
//     //     };

//     //     _workersQueue.Enqueue(workItem);
//     //     _logger.LogInformation($"Enqueue [{workItem}]");
//     //     return Ok(workItem.Id);
//     // }

//     // [HttpPost("pullCompleted")]
//     // [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
//     // public IEnumerable<CompletedWork> pullCompleted(int top)
//     // {
//     //     var search = Request.Query["search"];
//     //     var completed = _completedQueue.RemoveTop(top, search);
//     //     _logger.LogInformation($"Top [{top}], Found [{completed.Count()}]");
//     //     return completed;
//     // }

//     // Returns only the size of the local queue and not the aggregated size.
//     // [HttpGet("size")]
//     // public int size()
//     // {
//     //     _logger.LogInformation($"Size of the queue is: {_workersQueue.Count}");
//     //     return _workersQueue.Count;
//     // }

//     // Returns onlt the size of local queue and not the aggregated size. 
//     // [HttpGet("sizecompleted")]
//     // public int sizecompleted()
//     // {
//     //     _logger.LogInformation($"Size of the queue is: {_completedQueue.Count}");
//     //     return _completedQueue.Count;
//     // }

//     // [HttpGet("dequeue")]
//     // [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
//     // public ActionResult<WorkerItem> dequeue(int timeout)
//     // {
//     //     // if search paramter is stop, perform only local queue dequeue, otherwise search other nodes queue parts.
//     //     var search = Request.Query["search"];
//     //     var work = _workersQueue.Dequeue(timeout, search);

//     //     if (work == null)
//     //         return NotFound("No items in the queue");

//     //     return work;
//     // }

//     // [HttpPut("result")]
//     // public IActionResult result([FromBody] string buffer, string id)
//     // {
//     //     _logger.LogInformation($"Completed work posted for work Id: [{id}], buffer = {buffer}");
//     //     var completedWork = new CompletedWork()
//     //     {
//     //         Id = id,
//     //         Result = Encoding.UTF8.GetBytes(buffer),
//     //     };

//     //     _completedQueue.Enqueue(completedWork);
//     //     return Ok();
//     // }

//     // [HttpGet("head")]
//     // [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
//     // public ActionResult<WorkerItem> head()
//     // {
//     //     var firstMessage = _workersQueue.FirstMessage;
//     //     _logger.LogInformation($"First (oldest) item in the queue: [{firstMessage}]");

//     //     if (firstMessage == null)
//     //         return NotFound("No items in the queue");

//     //     return firstMessage;
//     // }



// }
