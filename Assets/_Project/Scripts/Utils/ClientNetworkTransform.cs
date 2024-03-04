using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Components;
using UnityEngine;

namespace CarRacingGame3d
{
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        public AuthorityMode authorityMode = AuthorityMode.Client;

        protected override bool OnIsServerAuthoritative() => authorityMode == AuthorityMode.Server;
    }
}
