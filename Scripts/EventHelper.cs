public class EventHelper
{
    public delegate void Event();
    private event Event m_event;
    public void Add(Event method) { m_event += method; }
    public void Remove(Event method) { m_event -= method; }
    public void Invoke() { m_event?.Invoke(); }
}

public class SealedEventHelper
{
    public delegate void SealedEvent();
    private event SealedEvent m_event;
    public SealedEventHelper(EventHelper eventHelper) { eventHelper.Add(Invoke); }
    public void Add(SealedEvent method) { m_event += method; }
    public void Remove(SealedEvent method) { m_event -= method; }
    private void Invoke() { m_event?.Invoke(); }
}