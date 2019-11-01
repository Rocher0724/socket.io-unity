using System;
using System.Collections.Generic;
using Socket.WebSocket4Net.Default;
using Socket.WebSocket4Net.System.Linq;

namespace Socket.WebSocket4Net.Protocol {
  internal class ProtocolProcessorFactory {
    private IProtocolProcessor[] m_OrderedProcessors;

    public ProtocolProcessorFactory (params IProtocolProcessor[] processors) {
      this.m_OrderedProcessors = ((IEnumerable<IProtocolProcessor>) processors)
        .OrderByDescending<IProtocolProcessor, int> ((Func<IProtocolProcessor, int>) (p => (int) p.Version))
        .ToArray<IProtocolProcessor> ();
    }

    public IProtocolProcessor GetProcessorByVersion (WebSocketVersion version) {
      return ((IEnumerable<IProtocolProcessor>) this.m_OrderedProcessors).FirstOrDefault<IProtocolProcessor> (
        (Predicate<IProtocolProcessor>) (p => p.Version == version));
    }

    public IProtocolProcessor GetPreferedProcessorFromAvialable (int[] versions) {
      foreach (int num in ((IEnumerable<int>) versions).OrderByDescending<int, int> ((Func<int, int>) (i => i))) {
        foreach (IProtocolProcessor orderedProcessor in this.m_OrderedProcessors) {
          int version = (int) orderedProcessor.Version;
          if (version >= num) {
            if (version <= num)
              return orderedProcessor;
          } else
            break;
        }
      }

      return (IProtocolProcessor) null;
    }
  }
}