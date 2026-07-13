using System;
using Microsoft.Win32;

namespace naiwa
{
    public class SystemEventService
    {
        public void RegisterSessionEndingHandler(SessionEndingEventHandler handler)
        {
            SystemEvents.SessionEnding += handler;
        }

        public void UnregisterSessionEndingHandler(SessionEndingEventHandler handler)
        {
            SystemEvents.SessionEnding -= handler;
        }
    }
}
