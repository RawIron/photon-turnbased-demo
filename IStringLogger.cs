using System;

namespace TurnbasedConsole
{
    interface IStringLogger
    {
        void Append(string entry);
        void Flush();
    }
}
