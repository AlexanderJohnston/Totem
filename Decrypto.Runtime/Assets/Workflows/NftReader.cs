using System;
using System.Collections.Generic;
using Decrypto.Blocks;
using Totem;
using WaxInterfaces;

namespace Decrypto.Assets.Workflows
{
    public class NftReader : Workflow
    {
        static Id _head = (Id) "38e4a351-ccdc-4fa9-8eb6-af1a95135dfa";
        //public static Id Route(BlockCreated e) => e.Id;

        public static Id DeriveId(string transactionId) =>
            _head?.DeriveId(transactionId) ?? throw new ArgumentNullException(nameof(transactionId));

        public NftReader()
        {
            //Given<BlockCreated>(e => _blockId = e.Id);
        }

        //public void When(BlockCreated e)
        //{
        //    if(e.Id != null && !string.IsNullOrWhiteSpace(e.Block))
        //    {
        //        ThenEnqueue(new UnpackBlock(e.Id, e.Block));
        //    }
        //}
    }
}
