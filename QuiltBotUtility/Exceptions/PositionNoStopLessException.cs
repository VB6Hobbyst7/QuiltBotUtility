using System;
using cAlgo.API;

namespace QuiltBotUtility.Exceptions
{
    public class PositionNoStopLessException : Exception
    {
        public Position Position { get; set; }

        public PositionNoStopLessException(Position position)
        {
            Position = position;
        }
    }
}