using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;

namespace queues;

public class Queue<T> : IQueue<T> where T : IQueueItem
{
    private readonly ConcurrentQueue<T> _queue;
    private readonly IIpAddressesProvider _ipAddressesProvider;
    private readonly string _name;
    private readonly ILogger<Queue<T>> _logger;
    private HttpClient _httpClient;
    private readonly IPEndPoint _ipAddress;
    private string _httpAddress;
    private string _httpAddressPullCompleted;
    private static Object _locker = new Object();
    private bool _searchOthers;

    public Queue(IIpAddressesProvider ipAddressesProvider, string name)
    {
        _ipAddressesProvider = ipAddressesProvider;
        _name = name;
        _queue = new ConcurrentQueue<T>();
        var clientHandler = new HttpClientHandler();
        clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

        _httpClient = new HttpClient(clientHandler);
        _ipAddress = _ipAddressesProvider?.IPAddresses.FirstOrDefault();
        _searchOthers = _ipAddress != null;
        _httpAddress = _ipAddress == null ? string.Empty : $"https://{_ipAddress}/dequeue?search=stop";
        _httpAddressPullCompleted = _ipAddress == null ? string.Empty : $"https://{_ipAddress}/pullCompleted?search=stop";
    }

    public void Enqueue(T item)
    {
        Console.WriteLine($"Enqueue: [{item}]");
        _queue.Enqueue(item);
    }

    public T FirstMessage
    {
        get
        {
            var firstItem = _queue.Take(1).FirstOrDefault();
            return firstItem;
        }
    }

    public T Dequeue(int timeout = 0, string search = "")
    {

        T item = default(T);
        while (item == null && timeout >= 0)
        {
            lock (_locker)
            {
                if (_queue.TryDequeue(out T result))
                {
                    item = result;
                    return item;
                }
            }
            if (item == null && timeout >= 0)
            {
                if (item == null && timeout >= 0 && !string.IsNullOrEmpty(_httpAddress) && string.IsNullOrEmpty(search))
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, _httpAddress);

                    var sw = new Stopwatch();
                    sw.Start();

                    var httpResponseMessage = _httpClient.SendAsync(request).Result;
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        var message = httpResponseMessage.Content.ReadFromJsonAsync<T>().Result;
                        {
                            if (message != null)
                            {
                                item = message;
                                return item;
                            }
                        }

                    }
                    sw.Stop();
                    timeout -= (int)sw.ElapsedMilliseconds;
                }
            }
            else
            {
                break;
            }
            Thread.Sleep(250);
            timeout -= 250;
        }

        return item;
    }

    public IEnumerable<T> RemoveTop(int n, string search = "")
    {
        lock (_locker)
        {
            List<T> results = new List<T>();
            for (var i = 0; i < n; i++)
            {
                var workItem = Dequeue(0, "stop");
                if (workItem is null)
                {
                    break;
                }
                results.Add(workItem);
            }
            var searchMore = n - results.Count;
            if (_searchOthers && searchMore > 0)
            {
                _httpAddressPullCompleted = $"{_httpAddressPullCompleted}&top={searchMore}";
                if (_httpAddressPullCompleted != null && string.IsNullOrEmpty(search))
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, _httpAddressPullCompleted);
                    var httpResponseMessage = _httpClient.SendAsync(request).Result;
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        var message = httpResponseMessage.Content.ReadFromJsonAsync<IEnumerable<T>>().Result;
                        {
                            if (message != null)
                            {
                                results.AddRange(message);
                            }
                        }
                    }
                }
            }

            return results;
        }
    }

    public int Count
    {
        get
        {
            // try not to enumrate sequence
            if (_queue.TryGetNonEnumeratedCount(out int count))
            {
                return count;
            }
            return _queue.Count();
        }
    }
}
