using Unity.Netcode.Components;
using UnityEngine;

namespace SEE.Net
{
    /// <summary>
    /// Used for syncing a transform with client side changes. This includes host.
    /// Pure server as owner isn't supported by this. Please use NetworkTransform
    /// for transforms that'll always be owned by the server.
    /// </summary>
    /// <remarks>This stems from https://docs-multiplayer.unity3d.com/netcode/current/components/networktransform/index.html</remarks>
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        /// <summary>
        /// Used to determine who can write to this transform. Owner client only.
        /// This imposes state to the server. This is putting trust on your clients.
        /// Make sure no security-sensitive features use this transform.
        /// </summary>
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}