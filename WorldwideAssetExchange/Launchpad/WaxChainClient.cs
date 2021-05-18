using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using tiesky.com;
using WaxInterfaces;

namespace Launchpad
{
	public class WaxChainClient
	{
		public int currentBlockNumber;
		public string currentBlockId;
		private SharmIpc sm = null;

		public Queue<int> BlockQueue = new Queue<int>();

		public WaxChainClient()
		{
			Init();
		}

		void Init()
		{
			if (sm == null)
				sm = new tiesky.com.SharmIpc("SammySnakeHissHiss", this.RemoteCall);
		}

		Tuple<bool, byte[]> RemoteCall(byte[] data)
		{
			//This will be called async when remote partner makes any request

			//This is a response to remote partner
			return new Tuple<bool, byte[]>(true, new byte[] { 1, 2, 3, 4 });
		}

		void Dispose()
		{
			//!!!For the graceful termination, in the end of your program, 
			//SharmIpc instance must be disposed
			if (sm != null)
			{
				sm.Dispose();
				sm = null;
			}
		}

		public void SendWallets(List<WaxAccount> wallets)
        {
			var json = JsonSerializer.Serialize(wallets);
			var bytes = Encoding.UTF8.GetBytes(json);
			bool res = sm.RemoteRequestWithoutResponse(bytes);
        }

		public void SendRawBlock(string json, string id)
        {
            var idBytes = Encoding.UTF8.GetBytes(id);
            var idArray = new byte[256];
            Array.Copy(idBytes, idArray, idBytes.Length);
			var bytes = Encoding.UTF8.GetBytes(json);
            var combinedByteArray = new byte[256 + bytes.Length];
            Buffer.BlockCopy(idArray, 0, combinedByteArray, 0, 256);
            Buffer.BlockCopy(bytes, 0, combinedByteArray, 256, bytes.Length);
			bool res = sm.RemoteRequestWithoutResponse(combinedByteArray);
        }

        public void MakeRemoteRequestWithoutResponse()
        {
            //Making remote request (a la send and forget)
            bool res = sm.RemoteRequestWithoutResponse(new byte[512]);
        }
	}
}
