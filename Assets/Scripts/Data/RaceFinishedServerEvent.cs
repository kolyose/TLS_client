using System;

public class RaceFinishedServerEvent:BaseRaceServerEvent
{
    public RaceFinishedServerEvent(Action<BaseServerEvent> onDisposeCallback) : base(onDisposeCallback) { }
}
