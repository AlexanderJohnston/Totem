using System;
using System.Collections.Generic;
using Totem;
using WaxInterfaces;

namespace Decrypto.Records
{
    public class BlockRecord
    {
        public Id Id { get; set; } = null;
        public Block BlockAfterParsing { get; set; } = null;
        public string BlockBeforeParsing{ get; set; } = string.Empty;
        public List<Trx> Transactions { get; set; } = new List<Trx>();

    }
}
