using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KS.Reactor.Client.Unity
{
    /// <summary>Tag proxy script classes to display a warning message in the inspector for the class.</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ksWarningAttribute : Attribute
    {
        /// <summary>Warning message</summary>
        public string Message
        {
            get { return m_message; }
        }
        private string m_message;

        /// <summary>Constructor</summary>
        /// <param name="message">Warning message</param>
        public ksWarningAttribute(string message)
        {
            m_message = message;
        }
    }
}
