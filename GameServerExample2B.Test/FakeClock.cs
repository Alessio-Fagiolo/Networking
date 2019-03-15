using System;
namespace GameServerExample2B.Test
{
    public class FakeClock : IMonotonicClock
    {
        private float timeStamp;

        public float setClock { set { timeStamp = value; } }

        public FakeClock(float timeStamp = 0)
        {
            this.timeStamp = timeStamp;
        }

        public float GetNow()
        {
            return timeStamp;
        }

        public void IncreaseTimeStamp(float delta)
        {
            if (delta <= 0)
            {
                throw new Exception("invalid delta value");
            }
            timeStamp += delta;
        }
    }
}
