using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Switchboard.Server.Connection
{
    public abstract class SwitchboardConnection
    {
        public abstract bool IsSecure { get; }
    }
}
