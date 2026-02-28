using Microsoft.Xna.Framework;

namespace Merchant.Management;

public sealed class StateManager<T>(T defaultValue)
    where T : Enum
{
    public delegate void stateChanged();
    public T Current
    {
        get => field;
        set
        {
            field = value;
            Next = value;
            Timer = TimeSpan.Zero;
            changeCallback = null;
        }
    } = defaultValue;
    public T Next { get; private set; } = defaultValue;
    public TimeSpan Timer { get; private set; } = TimeSpan.Zero;
    private double timerTotalMS = -1;
    public float TimerProgress => (float)(Timer.TotalMilliseconds / timerTotalMS);
    private stateChanged? changeCallback = null;

    public void SetNext(T next, double transition, stateChanged? onChange = null, bool force = false)
    {
        if (force || Timer == TimeSpan.Zero)
        {
            Next = next;
            changeCallback = onChange;
            Timer = TimeSpan.FromMilliseconds(transition);
            timerTotalMS = transition;
        }
    }

    public void Update(GameTime time)
    {
        // state transition
        if (Timer > TimeSpan.Zero)
        {
            Timer -= time.ElapsedGameTime;
            if (Timer <= TimeSpan.Zero)
            {
                T old = Current;
                stateChanged? cb = changeCallback;
                Current = Next;
                cb?.Invoke();
            }
        }
    }

    public override string ToString()
    {
        return $"State.{typeof(T).Name}: {Current}";
    }
}
