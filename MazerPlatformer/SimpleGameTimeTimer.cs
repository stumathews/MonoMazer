using Microsoft.Xna.Framework;

namespace MazerPlatformer
{
    public class SimpleGameTimeTimer
    {
        public int TimeoutMs { get; }
        private int _milli = 0;
        private bool _ready = false;
        public SimpleGameTimeTimer(int timeoutMs)
        {
            TimeoutMs = timeoutMs;
        }

        public void Update(GameTime dt)
        {
            if (!_ready)
                return;
            _milli += dt.ElapsedGameTime.Milliseconds;
        }

        public void Start() => _ready = true;
        public void Stop() => _ready = false;

        public void Reset() => _milli = 0;

        public bool IsTimedOut() => _milli >= TimeoutMs;
    }
}