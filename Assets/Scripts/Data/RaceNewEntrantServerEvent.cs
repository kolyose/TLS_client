using System;

public class RaceNewEntrantServerEvent : BaseRaceServerEvent
{
    protected int? _entrantId;
    public int EntrantId { get; set; }

    public RaceNewEntrantServerEvent(Action<BaseServerEvent> onDisposeCallback) : base(onDisposeCallback) { }

    public override void Init()
    {
        base.Init();
        _entrantId = BitConverter.ToInt32(Payload, GetEntrantIdStartIndex());
        EntrantId = (int)_entrantId;
    }

    public override void Dispose()
    {
        _entrantId = null;
        EntrantId = -1;

        base.Dispose();      
    }

    protected int GetEntrantIdStartIndex()
    {
        return GetRaceIdStartIndex() + 4;
    }
}
