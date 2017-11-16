using System;
using System.Collections.Generic;

public class ServerEventsFactory
{
    protected static Dictionary<EnumServerEventType, Queue<BaseServerEvent>> _eventsPool = new Dictionary<EnumServerEventType, Queue<BaseServerEvent>>();

    public static BaseServerEvent GetEvent(byte[] payload)
    {
        // extracting the event's type from its payload data
        int typeStartIndex = BitConverter.ToInt16(payload, 0) + 2;
        EnumServerEventType type = (EnumServerEventType)BitConverter.ToInt16(payload, typeStartIndex);

        BaseServerEvent evt = null;
        Queue<BaseServerEvent> eventsQueue;

        // checking if any events of given type are available in the pool
        // and if yes - reuse an event from the pool
        _eventsPool.TryGetValue(type, out eventsQueue);
        if (eventsQueue != null && eventsQueue.Count > 0)
        {            
            evt = eventsQueue.Dequeue();
        }

        // if not - create a new event
        if (evt == null)
        {
            evt = CreateEventByType(type);
        }

        // set new data to the event & init it
        evt.Payload = payload;
        evt.Init();

        return evt;
    }

    protected static BaseServerEvent CreateEventByType(EnumServerEventType type)
    {
        BaseServerEvent result = null;
        switch (type)
        {
            case EnumServerEventType.RaceNewEntrant:
                {
                    result = new RaceNewEntrantServerEvent(OnEventDispose);
                    break;
                }

            case EnumServerEventType.RaceFinished:
                {
                    result = new RaceFinishedServerEvent(OnEventDispose);
                    break;
                }

            default:
                break;
        }

        return result;
    }

    protected static void OnEventDispose(BaseServerEvent evt)
    {       
        Queue<BaseServerEvent> eventsQueue;
        _eventsPool.TryGetValue(evt.Type, out eventsQueue);
        if (eventsQueue == null)
        {
            eventsQueue = new Queue<BaseServerEvent>();
            _eventsPool.Add(evt.Type, eventsQueue);
        }

        eventsQueue.Enqueue(evt);
    }
}
