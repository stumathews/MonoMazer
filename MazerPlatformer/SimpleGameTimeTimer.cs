using LanguageExt;
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

        public Either<IFailure, Unit> Update(GameTime dt) => Statics.Ensure(() =>
        {
            if (!_ready)
                return;
            _milli += dt.ElapsedGameTime.Milliseconds;
        });

        // These dont need to be eithers, they will not fail
        public void Start() => _ready = true;
        public void Stop() => _ready = false;
        public void Reset() => _milli = 0;
        public bool IsTimedOut() => _milli >= TimeoutMs;
    }
}