using System.Threading.Channels;
using CommonHall.Domain.Entities;

namespace CommonHall.Infrastructure.Services;

public sealed class TrackingEventChannel
{
    private readonly Channel<TrackingEvent> _channel;

    public TrackingEventChannel()
    {
        _channel = Channel.CreateBounded<TrackingEvent>(new BoundedChannelOptions(10000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public ChannelWriter<TrackingEvent> Writer => _channel.Writer;
    public ChannelReader<TrackingEvent> Reader => _channel.Reader;
}
