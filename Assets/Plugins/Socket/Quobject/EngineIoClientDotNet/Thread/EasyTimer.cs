using System.Threading;

namespace Socket.Quobject.EngineIoClientDotNet.Thread {
  public class EasyTimer {
    private Timer m_timer = (Timer) null;
    private bool m_stop = false;

    public EasyTimer(ActionTrigger method, int delayInMilliseconds, bool once = true) {
      EasyTimer easyTimer = this;
      if (once)
        ThreadPool.QueueUserWorkItem((WaitCallback) (arg => {
          System.Threading.Thread.Sleep(delayInMilliseconds);
          if (easyTimer.m_stop)
            return;
          EasyTimer.DoWork((object) method);
        }));
      else
        this.m_timer = new Timer(new TimerCallback(EasyTimer.DoWork), (object) method, 0, delayInMilliseconds);
    }

    private static void DoWork(object obj) {
      ActionTrigger actionTrigger = (ActionTrigger) obj;
      if (actionTrigger == null)
        return;
      actionTrigger();
    }

    public static EasyTimer SetTimeout(ActionTrigger method, int delayInMilliseconds) {
      return new EasyTimer(method, delayInMilliseconds, true);
    }

    public static EasyTimer SetInterval(ActionTrigger method, int delayInMilliseconds) {
      return new EasyTimer(method, delayInMilliseconds, false);
    }

    public void Stop() {
      if (this.m_timer != null)
        this.m_timer.Dispose();
      this.m_stop = true;
    }

    public static void TaskRun(ActionTrigger action) {
      ManualResetEvent resetEvent = new ManualResetEvent(false);
      ThreadPool.QueueUserWorkItem((WaitCallback) (arg => {
        EasyTimer.DoWork((object) action);
        resetEvent.Set();
      }));
      resetEvent.WaitOne();
    }

    public static void TaskRunNoWait(ActionTrigger action) {
      ThreadPool.QueueUserWorkItem(new WaitCallback(EasyTimer.DoWork), (object) action);
    }
  }
}