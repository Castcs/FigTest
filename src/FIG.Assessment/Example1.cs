using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Logging;

namespace FIG.Assessment;

/// <summary>
/// In this example, the goal of this GetPeopleInfo method is to fetch a list of Person IDs from our database (not implemented / not part of this example),
/// and for each Person ID we need to hit some external API for information about the person. Then we would like to return a dictionary of the results to the caller.
/// We want to perform this work in parallel to speed it up, but we don't want to hit the API too frequently, so we are trying to limit to 5 requests at a time at most
/// by using 5 background worker threads.
/// Feel free to suggest changes to any part of this example.
/// In addition to finding issues and/or ways of improving this method,
/// 
/// what is a name for this sort of queueing pattern?
/// The overall pattern is almost exactly how Microsoft describes a Producer/Consumer Pattern
/// https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/how-to-implement-a-producer-consumer-dataflow-pattern
/// We have a producer grabbing person IDs for our consumer to process
/// However, lets store them in a channel instead of a queue
/// https://learn.microsoft.com/en-us/dotnet/core/extensions/channels
/// Channels manage synchronization and is built for concurrency, which is good since we're using it asynchronously
/// It can also bound itself if we wanted to set a number for that
/// https://learn.microsoft.com/en-us/dotnet/core/extensions/queue-service
/// </summary>
public class Example1
{
    // One permanent http client instead of making new clients over and over
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    public Example1(ILogger logger, HttpClient httpClient)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    // Making this async to align with everything else, even though technically whoever calls this now needs to know it's async
    // This is 
    public async Task<ConcurrentDictionary<int, int>> GetPeopleInfoAsync()
    {
        // replace queue with channel, unbounded for now, probably bounded in real scenario but that depends on implementation of GetIds
        var personIdChannel = Channel.CreateUnbounded<int>();
        var results = new ConcurrentDictionary<int, int>();
        try
        {
            _logger.LogInformation("Initiating data retrieval...");
            // use async Tasks to get Ids
            var collectTask = GetIdsAsync(personIdChannel.Writer);

            // prepare 5 workers to go through the channel and fetch info on each item, adding to the result set
            // since we're not manually managing the threads anymore, the new version of the for loop only really has to do one thing, Task.Run our Gathering
            var workerTasks = Enumerable
                .Range(0, 5)
                .Select(i => Task.Run(() => GatherInfoAsync(personIdChannel, results)));

            // wait for collecting
            await collectTask;
            personIdChannel.Writer.Complete();

            await Task.WhenAll(workerTasks);
            _logger.LogInformation("Data retrieval complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetPeopleInfoAsync");
        }

        return results;
    }

    // Renamed methods to include Async in name, same convention in Example 3
    private async Task GetIdsAsync(ChannelWriter<int> personIdChannel)
    {
        try
        {
            // dummy implementation, would be pulling from a database
            for (var i = 1; i < 100; i++)
            {
                await personIdChannel.WriteAsync(i);
                if (i % 10 == 0) await Task.Delay(TimeSpan.FromMilliseconds(50)); // artificial delay every now and then
            }
            personIdChannel.Complete();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Ids in GetIdsAsync");
        }

    }
    // Renamed methods to include Async in name, same convention in Example 3
    private async Task GatherInfoAsync(ChannelReader<int> personIdChannel, ConcurrentDictionary<int, int> results)
    {
        // pull IDs off the queue until it is empty
        await foreach (var id in personIdChannel.ReadAllAsync())
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://some.example.api/people/{id}/age");
                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Request for ID {id} info failed with code {response.StatusCode}");
                }

                var age = int.Parse(await response.Content.ReadAsStringAsync());
                results[id] = age;
                _logger.LogInformation($"Successfully gathered info for ID {id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving data for ID {id} in GatherInfoAsync");
            }
        }
    }
}
