using Microsoft.Xna.Framework;

namespace Merchant.Management;

public sealed class StateManager<T>(T defaultValue)
    where T : Enum
{
    public T Current
    {
        get => field;
        set
        {
            field = value;
            Next = value;
            Timer = TimeSpan.Zero;
            changeCallback = null;
            ModEntry.LogDebug(ToString());
        }
    } = defaultValue;
    public T Next { get; private set; } = defaultValue;
    public TimeSpan Timer { get; private set; } = TimeSpan.Zero;
    private Action<T, T>? changeCallback = null;

    public void SetNext(T next, double transition, Action<T, T>? onChange = null, bool force = false)
    {
        if (force || Timer == TimeSpan.Zero)
        {
            Next = next;
            changeCallback = onChange;
            Timer = TimeSpan.FromMilliseconds(transition);
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
                Action<T, T>? cb = changeCallback;
                Current = Next;
                cb?.Invoke(old, Next);
            }
        }
    }

    public override string ToString()
    {
        return $"State.{typeof(T).Name}: {Current}";
    }
}
