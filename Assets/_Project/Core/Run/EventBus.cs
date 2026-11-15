using System;
using System.Collections.Generic;

namespace BTH.Core.Run
{
    /// <summary>Handler nhận event struct qua ref — không boxing khi publish.</summary>
    public delegate void RefEventHandler<T>(in T evt) where T : struct;

    /// <summary>
    /// Bus event một chiều Core → UI. Subscribe bằng generic để lưu theo Type;
    /// event là struct để không cấp phát mỗi lần publish.
    /// </summary>
    public interface IEventBus
    {
        IDisposable Subscribe<T>(RefEventHandler<T> handler) where T : struct;

        void Unsubscribe<T>(RefEventHandler<T> handler) where T : struct;

        void Publish<T>(in T evt) where T : struct;

        void Clear();
    }

    public sealed class EventBus : IEventBus
    {
        private readonly Dictionary<Type, object> _handlers = new Dictionary<Type, object>();

        public IDisposable Subscribe<T>(RefEventHandler<T> handler) where T : struct
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var listObject))
            {
                listObject = new List<RefEventHandler<T>>();
                _handlers[type] = listObject;
            }

            var list = (List<RefEventHandler<T>>)listObject;
            list.Add(handler);
            return new Subscription<T>(this, handler);
        }

        public void Unsubscribe<T>(RefEventHandler<T> handler) where T : struct
        {
            if (handler == null)
            {
                return;
            }

            if (_handlers.TryGetValue(typeof(T), out var listObject))
            {
                var list = (List<RefEventHandler<T>>)listObject;
                list.Remove(handler);
            }
        }

        public void Publish<T>(in T evt) where T : struct
        {
            if (!_handlers.TryGetValue(typeof(T), out var listObject))
            {
                return;
            }

            var handlers = ((List<RefEventHandler<T>>)listObject).ToArray();
            for (int i = 0; i < handlers.Length; i++)
            {
                handlers[i](in evt);
            }
        }

        public void Clear()
        {
            _handlers.Clear();
        }

        private sealed class Subscription<T> : IDisposable where T : struct
        {
            private EventBus _bus;
            private RefEventHandler<T> _handler;

            public Subscription(EventBus bus, RefEventHandler<T> handler)
            {
                _bus = bus;
                _handler = handler;
            }

            public void Dispose()
            {
                if (_bus == null)
                {
                    return;
                }

                _bus.Unsubscribe(_handler);
                _bus = null;
                _handler = null;
            }
        }
    }
}
