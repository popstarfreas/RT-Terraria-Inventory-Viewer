using System;
using System.Timers;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quobject.SocketIoClientDotNet.Client;

namespace InventoryStreamer
{
    [ApiVersion(1, 21)]
    public class InventoryStreamer : TerrariaPlugin
    {
        public static Socket socket;
        public static Config Config = new Config();
        public static bool Connected = false;

        public override string Author
        {
            get
            {
                return "popstarfreas";
            }
        }

        public override string Description
        {
            get
            {
                return "Removes the need to play the game";
            }
        }

        public override string Name
        {
            get
            {
                return "InventoryStreamer";
            }
        }

        public override Version Version
        {
            get
            {
                return new Version(1, 0, 0);
            }
        }

        public InventoryStreamer(Main game) : base(game)
        {
            Order = 0;
        }

        public override void Initialize()
        {
            string path = Path.Combine(TShock.SavePath, "InventoryStreamer.json");
            if (!File.Exists(path))
                Config.WriteTemplates(path);
            Config = Config.Read(path);

            socket = IO.Socket("http://localhost:3002");

            socket.On(Socket.EVENT_CONNECT, () =>
            {
                Timer tm = new Timer(1);
                tm.Elapsed += (Object source, ElapsedEventArgs e) =>
                {
                    tm.Stop(); tm.Dispose();
                    socket.Emit("serverauth", Config.Token);
                };
                tm.Start();
                Connected = true;
                Console.WriteLine("InventoryStreamer connected to Socket Server.");
                Console.WriteLine("InventoryStreamer sent serverauth");
            });

            socket.On(Socket.EVENT_CONNECT_ERROR, (data) =>
            {
                Connected = false;
                //Console.WriteLine("FAILURE CONNECTED!!");
            });

            socket.On("getplayers", (data) =>
            {
                JObject json = new JObject();
                json.Add("socketid", data.ToString());
                json.Add("list", JsonConvert.SerializeObject(TShock.Players.Where(p => p != null).Select(p => p.Name), Formatting.Indented));
                socket.Emit("players", json);
            });

            socket.On("getinventory", (data) =>
            {
                dynamic json = JsonConvert.DeserializeObject(data.ToString());
                string socketID = json.id;
                string playerName = json.name;

                var player = Main.player.FirstOrDefault(p => p != null && p.name == playerName);
                var tPlayer = TShock.Players.FirstOrDefault(p => p != null && p.Name == playerName);
                if (player != null && tPlayer != null)
                {
                    List<SimpleItem> inventory = new List<SimpleItem>();
                    for (int i = 0; i < NetItem.MaxInventory; i++)
                    {
                        if (i < NetItem.InventorySlots)
                        {
                            //0-58
                            inventory.Add(new SimpleItem(player.inventory[i].netID, player.inventory[i].stack));
                        }
                        else if (i < NetItem.InventorySlots + NetItem.ArmorSlots)
                        {
                            //59-78
                            var index = i - NetItem.InventorySlots;
                            inventory.Add(new SimpleItem(player.armor[index].netID, player.armor[index].stack));
                        }
                        else if (i < NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots)
                        {
                            //79-88
                            var index = i - (NetItem.InventorySlots + NetItem.ArmorSlots);
                            inventory.Add(new SimpleItem(player.dye[index].netID, player.dye[index].stack));
                        }
                        else if (i <
                            NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots + NetItem.MiscEquipSlots)
                        {
                            //89-93
                            var index = i - (NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots);
                            inventory.Add(new SimpleItem(player.miscEquips[index].netID, player.miscEquips[index].stack));
                        }
                        else if (i <
                            NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots + NetItem.MiscEquipSlots
                            + NetItem.MiscDyeSlots)
                        {
                            //93-98
                            var index = i - (NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots
                                + NetItem.MiscEquipSlots);
                            inventory.Add(new SimpleItem(player.miscDyes[index].netID, player.miscDyes[index].stack));
                        }
                    }
                    JObject sendJson = new JObject();
                    sendJson.Add("socketid", socketID);
                    sendJson.Add("state", "success");
                    sendJson.Add("index", tPlayer.Index);
                    sendJson.Add("inventory", JsonConvert.SerializeObject(inventory));
                    socket.Emit("getinventory_response", sendJson);
                }
                else
                {
                    JObject sendJson = new JObject();
                    sendJson.Add("socketid", socketID);
                    sendJson.Add("state", "failure");
                    socket.Emit("getinventory_response", sendJson);
                }
            });

            ServerApi.Hooks.NetGetData.Register(this, GetData);
            ServerApi.Hooks.NetGreetPlayer.Register(this, GreetPlayer);
            ServerApi.Hooks.ServerLeave.Register(this, LeavePlayer);

            // Commands
            TShockAPI.Commands.ChatCommands.Add(new Command("inventorystreamer.checkstate", CheckStateCommand, "streamercheck"));
            TShockAPI.Commands.ChatCommands.Add(new Command("inventorystreamer.reload", Reload, "isreload"));

            GetDataHandlers.InitGetDataHandler();

        }

        void CheckStateCommand(CommandArgs args)
        {
            args.Player.SendInfoMessage(Connected ? "InventoryStreamer is connected to the Socket Server." : "InventoryStreamer is not connected to the Socket Server.");
        }

        void GreetPlayer(GreetPlayerEventArgs args)
        {
            if (TShock.Players[args.Who] != null) {
                JObject json = new JObject();
                json.Add("name", TShock.Players[args.Who].Name);
                json.Add("index", TShock.Players[args.Who].Index);
                socket.Emit("playerjoin", json.ToString());
            }
        }

        void LeavePlayer(LeaveEventArgs args)
        {
            if (TShock.Players[args.Who] != null)
            {
                JObject json = new JObject();
                json.Add("name", TShock.Players[args.Who].Name);
                json.Add("index", TShock.Players[args.Who].Index);
                socket.Emit("playerleave", json.ToString());
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.NetGetData.Deregister(this, GetData);
                base.Dispose(disposing);
            }
        }

        private void GetData(GetDataEventArgs args)
        {
            var type = args.MsgID;
            var player = TShock.Players[args.Msg.whoAmI];

            if (player == null)
            {
                args.Handled = true;
                return;
            }

            if (!player.ConnectionAlive)
            {
                args.Handled = true;
                return;
            }

            using (var data = new MemoryStream(args.Msg.readBuffer, args.Index, args.Length))
            {
                try
                {
                    if (GetDataHandlers.HandlerGetData(type, player, data))
                        args.Handled = true;
                }
                catch (Exception ex)
                {
                    TShock.Log.ConsoleError(ex.ToString());
                }
            }
        }
        void Reload(CommandArgs e)
        {
            string path = Path.Combine(TShock.SavePath, "InventoryStreamer.json");
            if (!File.Exists(path))
                Config.WriteTemplates(path);
            Config = Config.Read(path);
            e.Player.SendSuccessMessage("Reloaded InventoryStream Config.");
        }
    }

    public class SimpleItem
    {
        public int netID;
        public int stack;

        public SimpleItem (int netID, int stack)
        {
            this.netID = netID;
            this.stack = stack;
        }
    }
}
