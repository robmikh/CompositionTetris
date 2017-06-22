using System;
using System.Diagnostics;

namespace CompositionTetris
{
    class StepTimer
    {
        public StepTimer()
        {
            _targetElapsedTicks = TicksPerSecond / 60;

            _qpcFrequency = Stopwatch.Frequency;
            _qpcLastTime = Stopwatch.GetTimestamp();

            // Initialize max delta to 1/10 of a second.
            _qpcMaxDelta = _qpcFrequency / 10;
        }

        // Get elapsed time since the previous Update call.
        public long GetElapsedTicks() { return _elapsedTicks; }
        public double GetElapsedSeconds() { return TicksToSeconds(_elapsedTicks); }

        // Get total time since the start of the program.
        public long GetTotalTicks() { return _totalTicks; }
        public double GetTotalSeconds() { return TicksToSeconds(_totalTicks); }

        // Get total number of updates since start of the program.
        public long GetFrameCount() { return _frameCount; }

        // Get the current framerate.
        public long GetFramesPerSecond() { return _framesPerSecond; }

        // Set whether to use fixed or variable timestep mode.
        public void SetFixedTimeStep(bool isFixedTimestep) { _isFixedTimeStep = isFixedTimestep; }

        // Set how often to call Update when in fixed timestep mode.
        public void SetTargetElapsedTicks(long targetElapsed) { _targetElapsedTicks = targetElapsed; }
        public void SetTargetElapsedSeconds(double targetElapsed) { _targetElapsedTicks = SecondsToTicks(targetElapsed); }

        // Integer format represents time using 10,000,000 ticks per second.
        public static readonly uint TicksPerSecond = 10000000;

        public static double TicksToSeconds(long ticks) { return ((double)ticks) / TicksPerSecond; }
        public static long SecondsToTicks(double seconds) { return (long)(seconds * TicksPerSecond); }

        // After an intentional timing discontinuity (for instance a blocking IO operation)
        // call this to avoid having the fixed timestep logic attempt a set of catch-up 
        // Update calls.

        void ResetElapsedTime()
        {
            _qpcLastTime = Stopwatch.GetTimestamp();

            _leftOverTicks = 0;
            _framesPerSecond = 0;
            _framesThisSecond = 0;
            _qpcSecondCounter = 0;
        }

        // Update timer state, calling the specified Update function the appropriate number of times.
        public void Tick(Action update)
        {
            // Query the current time.
            long currentTime = Stopwatch.GetTimestamp();

            long timeDelta = currentTime - _qpcLastTime;

            _qpcLastTime = currentTime;
            _qpcSecondCounter += timeDelta;

            // Clamp excessively large time deltas (e.g. after paused in the debugger).
            if (timeDelta > _qpcMaxDelta)
            {
                timeDelta = _qpcMaxDelta;
            }

            // Convert QPC units into a canonical tick format. This cannot overflow due to the previous clamp.
            timeDelta *= TicksPerSecond;
            timeDelta /= _qpcFrequency;

            long lastFrameCount = _frameCount;

            if (_isFixedTimeStep)
            {
                // Fixed timestep update logic

                // If the app is running very close to the target elapsed time (within 1/4 of a millisecond) just clamp
                // the clock to exactly match the target value. This prevents tiny and irrelevant errors
                // from accumulating over time. Without this clamping, a game that requested a 60 fps
                // fixed update, running with vsync enabled on a 59.94 NTSC display, would eventually
                // accumulate enough tiny errors that it would drop a frame. It is better to just round 
                // small deviations down to zero to leave things running smoothly.

                if (Math.Abs((long)(timeDelta - _targetElapsedTicks)) < TicksPerSecond / 4000)
                {
                    timeDelta = _targetElapsedTicks;
                }

                _leftOverTicks += timeDelta;

                while (_leftOverTicks >= _targetElapsedTicks)
                {
                    _elapsedTicks = _targetElapsedTicks;
                    _totalTicks += _targetElapsedTicks;
                    _leftOverTicks -= _targetElapsedTicks;
                    _frameCount++;


                    update();
                }
            }
            else
            {
                // Variable timestep update logic.
                _elapsedTicks = timeDelta;
                _totalTicks += timeDelta;
                _leftOverTicks = 0;
                _frameCount++;


                update();
            }

            // Track the current framerate.
            if (_frameCount != lastFrameCount)
            {
                _framesThisSecond++;
            }

            if (_qpcSecondCounter >= _qpcFrequency)
            {
                _framesPerSecond = _framesThisSecond;
                _framesThisSecond = 0;
                _qpcSecondCounter %= _qpcFrequency;
            }
        }

        // Source timing data uses QPC units.
        private long _qpcFrequency;
        private long _qpcLastTime;
        private long _qpcMaxDelta;

        // Derived timing data uses a canonical tick format.
        private long _elapsedTicks;
        private long _totalTicks;
        private long _leftOverTicks;

        // Members for tracking the framerate.
        private long _frameCount;
        private long _framesPerSecond;
        private long _framesThisSecond;
        private long _qpcSecondCounter;

        // Members for configuring fixed timestep mode.
        private bool _isFixedTimeStep;
        private long _targetElapsedTicks;
    }

}
