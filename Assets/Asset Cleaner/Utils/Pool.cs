using System;
using System.Collections.Generic;

namespace Asset_Cleaner {
    class Pool<T> : IDisposable where T : class {
        Func<T> _ctor;
        readonly Stack<T> _stack;

        // todo place asserts on app quit
        Action<T> _reset;
        Action<T> _destroy;

        static Action<T> Empty = _ => { };

        public Pool(Func<T> ctor, Action<T> reset, Action<T> destroy = null) {
            _ctor = ctor;
#if !M_DISABLE_POOLING
            _destroy = destroy ?? Empty;
            _reset = reset;
            _stack = new Stack<T>();
#endif
        }

        public T Get() {
#if M_DISABLE_POOLING
            return _ctor.Invoke();
#else
            T element;
            if (_stack.Count == 0) {
                element = _ctor();
            }
            else {
                element = _stack.Pop();
            }

            return element;
#endif
        }

        public void Release(ref T element) {
#if !M_DISABLE_POOLING
            Asr.IsFalse(_stack.Count > 0 && ReferenceEquals(_stack.Peek(), element),
                "Internal error. Trying to release object that is already released to pool. ");

            _reset.Invoke(element);
            _stack.Push(element);
#endif

            element = null;
        }


        public void Dispose() {
#if !M_DISABLE_POOLING
            while (_stack.Count > 0) {
                var t = _stack.Pop();
                _destroy.Invoke(t);
            }
#endif
        }

        public _Scope GetScoped(out T tmp) {
            tmp = Get();
            return new _Scope(this, ref tmp);
        }

        public struct _Scope : IDisposable {
            Pool<T> _pool;
            T _val;

            internal _Scope(Pool<T> pool, ref T val) {
                _pool = pool;
                _val = val;
            }

            public void Dispose() {
                _pool.Release(ref _val);
            }
        }
    }
}