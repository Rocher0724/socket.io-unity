using System;
using Socket.Quobject.Collections.Immutable;
using Socket.Quobject.EngineIoClientDotNet.Modules;

namespace Socket.Quobject.EngineIoClientDotNet.ComponentEmitter {
  public class Emitter {
    private ImmutableDictionary<string, ImmutableList<IListener>> callbacks;
    private ImmutableDictionary<IListener, IListener> _onceCallbacks;

    public Emitter() {
      this.Off();
    }

    public virtual Emitter Emit(string eventString, params object[] args) {
      if (this.callbacks.ContainsKey(eventString)) {
        foreach (IListener listener in this.callbacks[eventString])
          listener.Call(args);
      }

      return this;
    }

    public Emitter On(string eventString, IListener fn) {
      if (!this.callbacks.ContainsKey(eventString))
        this.callbacks = this.callbacks.Add(eventString, ImmutableList<IListener>.Empty);
      ImmutableList<IListener> immutableList = this.callbacks[eventString].Add(fn);
      this.callbacks = this.callbacks.Remove(eventString).Add(eventString, immutableList);
      return this;
    }

    public Emitter On(string eventString, ActionTrigger fn) {
      ListenerImpl listenerImpl = new ListenerImpl(fn);
      return this.On(eventString, (IListener) listenerImpl);
    }

    public Emitter On(string eventString, Action<object> fn) {
      ListenerImpl listenerImpl = new ListenerImpl(fn);
      return this.On(eventString, (IListener) listenerImpl);
    }

    public Emitter Once(string eventString, IListener fn) {
      OnceListener onceListener = new OnceListener(eventString, fn, this);
      this._onceCallbacks = this._onceCallbacks.Add(fn, (IListener) onceListener);
      this.On(eventString, (IListener) onceListener);
      return this;
    }

    public Emitter Once(string eventString, ActionTrigger fn) {
      ListenerImpl listenerImpl = new ListenerImpl(fn);
      return this.Once(eventString, (IListener) listenerImpl);
    }

    public Emitter Off() {
      this.callbacks = ImmutableDictionary.Create<string, ImmutableList<IListener>>();
      this._onceCallbacks = ImmutableDictionary.Create<IListener, IListener>();
      return this;
    }

    public Emitter Off(string eventString) {
      try {
        ImmutableList<IListener> immutableList;
        if (!this.callbacks.TryGetValue(eventString, out immutableList))
          LogManager.GetLogger(Global.CallerName("", 0, ""))
            .Info(string.Format("Emitter.Off Could not remove {0}", (object) eventString));
        if (immutableList != null) {
          this.callbacks = this.callbacks.Remove(eventString);
          foreach (IListener key in immutableList)
            this._onceCallbacks.Remove(key);
        }
      } catch (Exception ex) {
        this.Off();
      }

      return this;
    }

    public Emitter Off(string eventString, IListener fn) {
      try {
        if (this.callbacks.ContainsKey(eventString)) {
          ImmutableList<IListener> callback = this.callbacks[eventString];
          IListener listener;
          this._onceCallbacks.TryGetValue(fn, out listener);
          this._onceCallbacks = this._onceCallbacks.Remove(fn);
          if (callback.Count > 0 && callback.Contains(listener ?? fn)) {
            ImmutableList<IListener> immutableList = callback.Remove(listener ?? fn);
            this.callbacks = this.callbacks.Remove(eventString);
            this.callbacks = this.callbacks.Add(eventString, immutableList);
          }
        }
      } catch (Exception ex) {
        this.Off();
      }

      return this;
    }

    public ImmutableList<IListener> Listeners(string eventString) {
      if (this.callbacks.ContainsKey(eventString))
        return this.callbacks[eventString] ?? ImmutableList<IListener>.Empty;
      return ImmutableList<IListener>.Empty;
    }

    public bool HasListeners(string eventString) {
      return this.Listeners(eventString).Count > 0;
    }
  }
}