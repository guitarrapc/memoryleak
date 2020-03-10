using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DiagnosticCore.EventListeners;
using DiagnosticCore.Statistics;

namespace DiagnosticCore.TimerListeners
{
    public abstract class TimerListenerBase
    {
        protected bool Enabled = false;
        protected Action _eventWritten;

        public void RunWithCallback(Action handler, Action body)
        {
            Enabled = true;
            _eventWritten = handler;
            OnEventWritten();
            body();
        }
        public async Task RunWithCallbackAsync(Action handler, Func<Task> body)
        {
            Enabled = true;
            _eventWritten = handler;
            OnEventWritten();
            await body().ConfigureAwait(false);
        }

        public virtual void Stop()
        {
            Enabled = false;
        }

        public virtual void Restart()
        {
            Enabled = true;
        }

        protected abstract void OnEventWritten();
        public abstract void EventCreatedHandler();
    }
}
