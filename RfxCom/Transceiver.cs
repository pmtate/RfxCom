using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using RfxCom.Messages;
using RfxCom.Messages.InterfaceControl;

namespace RfxCom
{
    public class Transceiver : ITransceiver
    {
        public const int MaximumBufferSize = 1024 * 1024;

        private readonly IMessageCodec _codec;
        private readonly ICommunicationDevice _device;
        private readonly BlockingCollection<IMessage> _receivedMessages;
        private readonly BlockingCollection<IMessage> _sentMessages;
        private IDisposable _receiveSubscription;

        public Transceiver(ICommunicationDevice device, IMessageCodec codec) : this(device, codec,
            TaskPoolScheduler.Default)
        {
        }

        public Transceiver(ICommunicationDevice device, IMessageCodec codec, IScheduler scheduler)
        {
            _codec = codec;
            _device = device;

            _receivedMessages = new BlockingCollection<IMessage>();
            _sentMessages = new BlockingCollection<IMessage>();

            Received = _receivedMessages.GetConsumingEnumerable()
                .ToObservable(scheduler);

            Sent = _sentMessages.GetConsumingEnumerable()
                .ToObservable(scheduler);
        }

        public IObservable<IMessage> Received { get; }
        public IObservable<IMessage> Sent { get; }

        public async Task SendAsync(IMessage message, CancellationToken cancellationToken)
        {
            var result = _codec.Encode(message);

            if (result.Any())
            {
                await _device.SendAsync(result.First(), cancellationToken);
                _sentMessages.Add(message, cancellationToken);
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _device.OpenAsync(cancellationToken);
            await SendAsync(new ResetMessage(), cancellationToken);
            await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);
            await _device.FlushAsync(cancellationToken);
            await SendAsync(new GetStatusMessage(1), cancellationToken);

            _receiveSubscription = Observable
                .FromAsync(ReceiveAsync)
                .Repeat()
                .TakeWhile(x => x.Any())
                .SelectMany(x => x)
                .SubscribeOn(TaskPoolScheduler.Default)
                .Subscribe(message => { _receivedMessages.Add(message, cancellationToken); });
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _device.CloseAsync(cancellationToken);
            _receiveSubscription.Dispose();
        }

        private async Task<IEnumerable<IMessage>> ReceiveAsync(CancellationToken cancellationToken)
        {
            return (await _device.ReceiveAsync(cancellationToken)).Select(Decode);
        }

        private IMessage Decode(Packet packet)
        {
            return _codec
                .Decode(packet)
                .Match(message => message, () => new RawMessage(packet));
        }
    }
}