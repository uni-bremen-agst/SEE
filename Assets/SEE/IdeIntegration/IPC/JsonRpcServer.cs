using System;
using System.Threading.Tasks;
using StreamRpc;

namespace Assets.SEE.IdeIntegration.IPC
{
    public abstract class JsonRpcServer : IDisposable
    {
        protected JsonRpc Rpc;
        protected object Target;
        protected Task Server;

        public void Start(object target)
        {
            _ = target ?? throw new ArgumentNullException(nameof(target));
            Target = target;

            Dispose();

            Server ??= StartServerAsync();
        }

        public abstract Task CallRemoteProcessAsync(string targetName);

        public bool IsConnected()
        {
            return Rpc != null;
        }

        protected abstract Task StartServerAsync();

        public virtual void Dispose()
        {
            Rpc?.Dispose();
        }
    }
}
