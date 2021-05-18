using PostSharp.Patterns.Diagnostics;
using PostSharp.Patterns.Model;
using PostSharp.Patterns.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using tiesky.com;
using WaxInterfaces;
using static PostSharp.Patterns.Diagnostics.FormattedMessageBuilder;
using static SharpCrafting.Win32NT.EventSattelite;

namespace SharpCrafting.Helper
{
	[AssumeImmutable]
    class LaunchpadReceiver
    {
		[Reference]
		private SharmIpc sm = null;
		[Reference]
		private readonly LogSource _log = LogSource.Get()
			.WithLevels(PostSharp.Patterns.Diagnostics.LogLevel.Trace,
			PostSharp.Patterns.Diagnostics.LogLevel.Warning);
		private Del _del;

		public Queue<int> BlockQueue = new Queue<int>();

		public LaunchpadReceiver(Del handler)
		{
			_del = handler;
			Init();
		}

		void Init()
		{
			if (sm == null)
				sm = new tiesky.com.SharmIpc("SammySnakeHissHiss", this.AsyncRemoteCallHandler);
		}

		public void Dispose()
		{
			//!!!For the graceful termination, in the end of your program, 
			//SharmIpc instance must be disposed
			if (sm != null)
			{
				sm.Dispose();
				sm = null;
			}
		}

		Tuple<bool, byte[]> RemoteCall(byte[] data)
		{
			//This will be called async when remote partner makes any request
			//This is a response to remote partner
			return new Tuple<bool, byte[]>(true, new byte[] { 1, 2, 3, 4 });
		}

		void AsyncRemoteCallHandler(ulong msgId, byte[] data)
		{
			//msgId must be returned back
			//data is received from remote partner
			_log.Info.Write(Formatted("[Shared Memory Receiver]: The event sattelite received a new pack of wallets from launchpad."));
            var idArray = new byte[256];
            var dataBlock = data.Length - 256;
            var dataArray = new byte[dataBlock];
            Buffer.BlockCopy(data, 0, idArray, 0, 256);
            Buffer.BlockCopy(data, 256, dataArray, 0, dataBlock);
			var json = Encoding.UTF8.GetString(dataArray);
            var id = Encoding.UTF8.GetString(idArray);
            var cleaned = id.Replace("\0", string.Empty);
			_del(json, cleaned);
			sm.AsyncAnswerOnRemoteCall(msgId, new Tuple<bool, byte[]>(true, new byte[] { 5 }));
		}

    }
}
