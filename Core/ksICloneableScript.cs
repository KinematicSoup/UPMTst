using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KS.Reactor.Client.Unity
{
    /// <summary>Interface for scripts that can copy their values onto another script.</summary>
    public interface ksICloneableScript
    {
        /// <summary>Copies the values of this script onto <paramref name="script"/>.</summary>
        /// <param name="script">Script to copy to.</param>
        void CopyTo(Component script);
    }
}
