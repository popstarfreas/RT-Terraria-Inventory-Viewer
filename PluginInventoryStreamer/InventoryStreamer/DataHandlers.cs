using System;
using System.IO;
using System.IO.Streams;
using System.Collections.Generic;
using TShockAPI;
using Newtonsoft.Json.Linq;

namespace InventoryStreamer
{
    internal delegate bool GetDataHandlerDelegate(GetDataHandlerArgs args);

    internal class GetDataHandlerArgs : EventArgs
    {
        public TSPlayer Player { get; private set; }
        public MemoryStream Data { get; private set; }

        public GetDataHandlerArgs(TSPlayer player, MemoryStream data)
        {
            Player = player;
            Data = data;
        }
    }

    internal static class GetDataHandlers
    {
        static Random rnd = new Random();
        private static Dictionary<PacketTypes, GetDataHandlerDelegate> _getDataHandlerDelegates;

        public static void InitGetDataHandler()
        {
            _getDataHandlerDelegates = new Dictionary<PacketTypes, GetDataHandlerDelegate>
            {
                {PacketTypes.PlayerSlot, HandlePlayerSlot},
            };
        }

        public static bool HandlerGetData(PacketTypes type, TSPlayer player, MemoryStream data)
        {
            GetDataHandlerDelegate handler;
            if (_getDataHandlerDelegates.TryGetValue(type, out handler))
            {
                try
                {
                    return handler(new GetDataHandlerArgs(player, data));
                }
                catch (Exception ex)
                {
                    TShock.Log.Error(ex.ToString());
                }
            }
            return false;
        }

        private static bool HandlePlayerSlot(GetDataHandlerArgs args)
        {
            args.Data.ReadByte();
            int slot = args.Data.ReadByte();
            int stack = args.Data.ReadInt16();
            args.Data.ReadByte();
            int netID = args.Data.ReadInt16();
            
            JObject json = new JObject();
            json.Add("index", args.Player.Index);
            json.Add("netID", netID);
            json.Add("stack", stack);
            json.Add("slot", slot);
            InventoryStreamer.socket.Emit("slot", json.ToString());
            return false;
        }
    }
}