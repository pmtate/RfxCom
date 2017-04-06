﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RfxCom.Messages;

namespace RfxCom
{
    public interface ITransceiver : IDisposable
    {
        Task SendAsync(IMessage message, CancellationToken cancellationToken);

        Task<IEnumerable<IMessage>> ReceiveAsync(CancellationToken cancellationToken);
    }
}