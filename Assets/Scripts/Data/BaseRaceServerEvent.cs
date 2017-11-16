using System;

public class BaseRaceServerEvent : BaseServerEvent
{
    protected int? _raceId = null;
    public int RaceId { get; set; }

    public BaseRaceServerEvent(Action<BaseServerEvent> onDisposeCallback) : base(onDisposeCallback) { }

    public override void Init()
    {
        base.Init();
        _raceId = BitConverter.ToInt32(Payload, GetRaceIdStartIndex());
        RaceId = (int)_raceId;
    }

    public override void Dispose()
    {
        _raceId = null;
        RaceId = -1;

        base.Dispose();     
    }

    protected int GetRaceIdStartIndex()
    {
        return GetTypeStartIndex() + 2;
    }
}
