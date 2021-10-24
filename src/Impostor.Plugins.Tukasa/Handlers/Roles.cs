using System;
using Impostor.Api.Events;
using Impostor.Api.Events.Player;
using Impostor.Api.Net.Messages;
using Impostor.Api.Net.Messages.C2S;
using Impostor.Api.Net.Messages.S2C;
using Impostor.Api.Net.Messages.Rpcs;
using Impostor.Api.Net.Inner;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Impostor.Api.Innersloth;
using Impostor.Api.Innersloth.Customization;
using System.IO;
using System.Text;
using System.Collections.Immutable;
using ExtraPlayerFunctions.Desync;

namespace Impostor.Plugins.EBPlugin.Handlers
{
    public class GameEventListener : IEventListener
    {
        private const string iI = " is Impostor.";
        private const string iC = " is Crewmate.";
        private const string iT = " is Teruteru.";
        private const string iVM = " is VentMorpher";
        private const string iFT = " is FastTroll";
        private const string iST = " is Stopper";
        private const string iMT = " is MagnetTroll";
        private const string iDc = " is Doctor";
        private const string iSu = " is Suicider";
        private const string iBO = " is The Blackout";
        private const string iTR = " is Terrorist";
        private const string iL = " is Leader";
        public struct CRoleID
        {
            //役職ID付き変数
            public int TeruteruID; //0
            public int MadmateID; //1
            public int VentMorpherID; //3
            public int FastTrollID; //4
            public int StopperID; //6
            public int MagnetTrollID; //9
            public int TraitorID; //10
            public int DoctorID;
            public int SuiciderID;
            public int BlackoutID;
            public int TerroristID;
            public int LeaderID;
            //コメントアウト保管場所
            //public int DivinerID;
            //public int GhostFoxID; //7
            //public int DictatorID; //8
            //public int FlashID; //5
            //public int SheriffID; //2
            //役職以外の変数(多分本来ここに置くべきものじゃない)
            public float FlashSpeed;
            public float FlashMin;
            public int StopperStopTime;
            public int TargetID;
            public int SuiciderCount;
            public float SuiciderBeforeX;
            public float SuiciderBeforeY;
            public int CrewmatesTaskCount;
            public int TerroristTaskCount;
            public string SheriffVictim; //こいつに関しては数字ですらない
        }
        public struct CRoleEnable
        {
            public Boolean TeruteruEnabled;
            public Boolean MadmateEnabled;
            public Boolean VentMorpherEnabled;
            public Boolean VentMorpherVentable;
            public Boolean FastTrollEnabled;
            public Boolean FastMeetingStarted;
            public Boolean HideAndSeekEnabled;
            public Boolean NoScanTaskEnabled;
            public Boolean MadmateKnowsImpostor;
            public Boolean TeruteruWinDisabled;
            public Boolean MagnetTrollEnabled;
            public Boolean DoctorEnabled;
            public Boolean TargetModeEnabled;
            public Boolean SuiciderEnabled;
            public Boolean BlackoutEnabled;
            public Boolean TerroristEnabled;
            public Boolean TerroristCanWin;
            public Boolean StopperEnabled;
            public Boolean TeruOrMadEnabled;
            public Boolean LeaderEnabled;
        }
        public struct HideAndSeek
        {
            public float ImpostorSpawnX;
            public float ImpostorSpawnY;
        }
        public struct VMorpherInfo
        {
            public Boolean IsMorphing;
            public HatType Hat;
            public SkinType Skin;
            public ColorType Color;
            public PetType Pet;
            public string name;
            public float LastVentX;
            public float LastVentY;
        }
        public int DivineID;
        public int CompletedTaskPlayers;
        public int Winner;
        public List<Impostor.Api.Net.IClientPlayer> HavingTaskPlayers = new List<Impostor.Api.Net.IClientPlayer>();
        /*-1:未確定
          0:クルー
          1:インポスター
          2:てるてる
        */
        string[] RoleNameForDoctor;
        static float DefaultCrewLight;
        static float DefaultImpostorLight;
        public string GhostFoxName;
        public Boolean IsInFlashSpeedUpCooldown = false;
        string[] names = new string[15];
        CRoleID CRID = new CRoleID();
        CRoleEnable CRE = new CRoleEnable();
        VMorpherInfo VMInfo = new VMorpherInfo();
        HideAndSeek HaS = new HideAndSeek();
        static System.Random rand = new System.Random();
        private readonly ILogger<EmptyBottlePlugin> _logger;
        public GameEventListener(ILogger<EmptyBottlePlugin> logger)
        {
            _logger = logger;
        }

        //ターゲット設定
        private void SetTarget(Impostor.Api.Games.IGame game) {
            var TargetablePlayers = new List<Impostor.Api.Net.IClientPlayer>();
            foreach(var player in game.Players) {
                if(!player.Character.PlayerInfo.IsImpostor && !player.Character.PlayerInfo.IsDead) {
                    TargetablePlayers.Add(player);
                }
            }
            var TargetNum = rand.Next(TargetablePlayers.Count);
            CRID.TargetID = TargetablePlayers[TargetNum].Client.Id;
            var Target = TargetablePlayers[TargetNum];
            //ターゲット通知
            foreach(var imp in game.Players) {
                if(imp.Character.PlayerInfo.IsImpostor) {
                    var writer = game.StartRpc(imp.Character.NetId, RpcCalls.SetName, imp.Client.Id);
                    writer.Write(imp.Character.PlayerInfo.PlayerName + "\r\nTarget is " + Target.Character.PlayerInfo.PlayerName);
                    game.SendToAsync(writer, imp.Character.PlayerId);
                    game.FinishRpcAsync(writer);
                }
            }
        }

        [EventListener]
        public void ChooseCustomRole(IGameStartingEvent e)
        {
            RoleNameForDoctor = new string[15];
            List<Impostor.Api.Net.IClientPlayer> AllPlayers = new List<Impostor.Api.Net.IClientPlayer>();
            //プレイヤーリスト作成
            foreach(var player in e.Game.Players)
            {
                if(!player.Character.PlayerInfo.IsImpostor) {
                    AllPlayers.Add(player);
                }
            }
            //DefaultLightを設定
            DefaultCrewLight = e.Game.Options.CrewLightMod;
            DefaultImpostorLight = e.Game.Options.ImpostorLightMod;
            //##ImpostorAddingTest##
            /*if(1 == 1) {
                var SSheriffNum = rand.Next(AllPlayers.Count);
                var SSheriff = AllPlayers[SSheriffNum];
                AllPlayers.Remove(AllPlayers[SSheriffNum]);
                SSheriff.Character.SendChatToPlayerAsync("You are NEW sheriff!");
                //RPC
                var SheriffPlayerID = SSheriff.Character.PlayerId;
                var InfectedIdsByte = new byte[SheriffPlayerID];
                var InfectedIds = new ReadOnlyMemory<byte>(InfectedIdsByte);
                    var writer = e.Game.StartRpc(SSheriff.Character.NetId, RpcCalls.SetInfected, SSheriff.Client.Id);
                    writer.Write(1);
                    writer.Write(InfectedIdsByte);
                    e.Game.SendToAsync(writer, SSheriff.Character.PlayerId);
                    //e.Game.SendToAllAsync(writer);
                    SSheriff.Character.PlayerInfo.IsImpostor = true;
                    e.Game.FinishRpcAsync(writer);
            }/**/
        }
        [EventListener]
        public void GameStarted(IGameStartedEvent e)
        {
            //役職指定
            _logger.LogInformation("Choosing Custom Roles...");
            List<Impostor.Api.Net.IClientPlayer> AllPlayers = new List<Impostor.Api.Net.IClientPlayer>();
            List<Impostor.Api.Net.IClientPlayer> Impostors = new List<Impostor.Api.Net.IClientPlayer>();
            List<Impostor.Api.Net.IClientPlayer> Everyone = new List<Impostor.Api.Net.IClientPlayer>();
            List<int> DontLeader = new List<int>();
            //クルー判定役職設定
            foreach(var player in e.Game.Players)
            {
                if(!player.Character.PlayerInfo.IsImpostor) {
                    AllPlayers.Add(player);
                }
            }
            //##TeruOrMadランダム化##
            if(CRE.TeruOrMadEnabled) {
                var ToMRand = rand.Next(2);
                if(ToMRand == 0) {
                    CRE.TeruteruEnabled = true;
                    CRE.MadmateEnabled = false;
                }
                if(ToMRand == 1) {
                    CRE.TeruteruEnabled = false;
                    CRE.MadmateEnabled = true;
                }
                _logger.LogInformation("変数'ToMRand'は" + ToMRand + "です");
            }
            //##Teruteru##
            if(CRE.TeruteruEnabled) {
                var TeruNum = rand.Next(AllPlayers.Count);
                CRID.TeruteruID = AllPlayers[TeruNum].Client.Id;
                var Teruteru = AllPlayers[TeruNum];
                AllPlayers.Remove(AllPlayers[TeruNum]);
                DontLeader.Add(Teruteru.Client.Id);
                //RoleNameForDoctor[Teruteru.Character.PlayerId] = "てるてる";
            }
            //##Madmate##
            if(CRE.MadmateEnabled) {
                var MMNum = rand.Next(AllPlayers.Count);
                CRID.MadmateID = AllPlayers[MMNum].Client.Id;
                var Madmate = AllPlayers[MMNum];
                AllPlayers.Remove(AllPlayers[MMNum]);
                DontLeader.Add(Madmate.Client.Id);
                //RoleNameForDoctor[Madmate.Character.PlayerId] = "狂人";
            }
            //##自殺狂人##
            if(CRE.SuiciderEnabled) {
                var SuiciderNum = rand.Next(AllPlayers.Count);
                CRID.SuiciderID = AllPlayers[SuiciderNum].Client.Id;
                var Suicider = AllPlayers[SuiciderNum];
                AllPlayers.Remove(AllPlayers[SuiciderNum]);
                DontLeader.Add(Suicider.Client.Id);
            }
            //##医者(Doctor)##
            if(CRE.DoctorEnabled) {
                var DoctorNum = rand.Next(AllPlayers.Count);
                CRID.DoctorID = AllPlayers[DoctorNum].Client.Id;
                var Doctor = AllPlayers[DoctorNum];
                AllPlayers.Remove(AllPlayers[DoctorNum]);
                DontLeader.Add(Doctor.Client.Id);
                //RoleNameForDoctor[Doctor.Character.PlayerId] = "医者";
            }
            //##テロリスト(Terrorist)##
            if(CRE.TerroristEnabled) {
                var TerroristNum = rand.Next(AllPlayers.Count);
                CRID.TerroristID = AllPlayers[TerroristNum].Client.Id;
                var Terrorist = AllPlayers[TerroristNum];
                AllPlayers.Remove(AllPlayers[TerroristNum]);
                DontLeader.Add(Terrorist.Client.Id);
            }
            //クルー
            foreach(var crew in AllPlayers) {
                //RoleNameForDoctor[crew.Character.PlayerId] = "クルー";
            }
            //インポスター判定役職設定
            foreach(var player in e.Game.Players)
            {
                if(player.Character.PlayerInfo.IsImpostor) {
                    Impostors.Add(player);
                }
            }
            //##VentMorpher##
            if(CRE.VentMorpherEnabled) {
                var VentMorpherNum = rand.Next(Impostors.Count);
                CRID.VentMorpherID = Impostors[VentMorpherNum].Client.Id;
                var VentMorpher = Impostors[VentMorpherNum];
                Impostors.Remove(Impostors[VentMorpherNum]);
                DontLeader.Add(VentMorpher.Client.Id);
            }
            //##TheBlackout##
            if(CRE.BlackoutEnabled) {
                var BlackoutNum = rand.Next(Impostors.Count);
                CRID.BlackoutID = Impostors[BlackoutNum].Client.Id;
                var Blackout = Impostors[BlackoutNum];
                Impostors.Remove(Impostors[BlackoutNum]);
                DontLeader.Add(Blackout.Client.Id);
            }
            //インポスター役職名設定
            foreach(var Imp in Impostors) {
                //RoleNameForDoctor[Imp.Character.PlayerId] = "インポスター";
            }
            //アビリティ設定(クルー・インポスター関係なく設定される)
            foreach(var player in e.Game.Players)
            {
                Everyone.Add(player);
            }
            //##FastTroll##
            if(CRE.FastTrollEnabled) {
                var FastTrollNum = rand.Next(Everyone.Count);
                CRID.FastTrollID = Everyone[FastTrollNum].Client.Id;
                var FastTroll = Everyone[FastTrollNum];
                Everyone.Remove(Everyone[FastTrollNum]);
            }
            //##MagnetTroll##
            if(CRE.MagnetTrollEnabled) {
                var MagnetTrollNum = rand.Next(Everyone.Count);
                CRID.MagnetTrollID = Everyone[MagnetTrollNum].Client.Id;
                var MagnetTroll = Everyone[MagnetTrollNum];
                Everyone.Remove(Everyone[MagnetTrollNum]);
            }
            //##リーダー##
            List<Impostor.Api.Net.IClientPlayer> CLeader = new List<Impostor.Api.Net.IClientPlayer>();
            foreach(var player in e.Game.Players) {
                if(!DontLeader.Contains(player.Client.Id)) {
                    CLeader.Add(player);
                }
            }
            if(CRE.LeaderEnabled) {
                var LeaderNum = rand.Next(CLeader.Count);
                CRID.LeaderID = CLeader[LeaderNum].Client.Id;
                var Leader = CLeader[LeaderNum];
                CLeader.Remove(CLeader[LeaderNum]);
            }
            //変数初期化処理
            CRE.TeruteruWinDisabled = false;
            CRID.TargetID = -1;
            CRID.TraitorID = -1;
            CRID.SuiciderCount = 0;
            CRE.TerroristCanWin = true;
            //タスク数変数初期化用意
            var TaskPerPlayer = 
                e.Game.Options.NumLongTasks +
                e.Game.Options.NumCommonTasks +
                e.Game.Options.NumShortTasks;
            HavingTaskPlayers = new List<Impostor.Api.Net.IClientPlayer>();
            //ターゲット指定
            if(CRE.TargetModeEnabled) SetTarget(e.Game);
            var PlayingOn = "Playing on localhost";
            var PlayingOnRandom = rand.NextDouble();
            if(PlayingOnRandom < 0.05) {
                PlayingOn = "Playing on remotehost";
            }
            foreach(var player in e.Game.Players)
            {
                var PlayerID = player.Character.PlayerId;
                //ID設定処理
                var RealPlayerName = player.Character.PlayerInfo.PlayerName;
                //player.Character.SetNameAsync(player.Character.PlayerInfo.PlayerName + "(" + player.Character.PlayerId + ")");
                //_logger.LogInformation(RealPlayerName + " name was changed to " + player.Character.PlayerInfo.PlayerName);
                //VentMorpherの情報を保存
                if(player.Client.Id == CRID.VentMorpherID) {
                        var MorpherInfo = player.Character.PlayerInfo;
                        VMInfo.Color = MorpherInfo.Color;
                        VMInfo.Hat = MorpherInfo.Hat;
                        VMInfo.Skin = MorpherInfo.Skin;
                        VMInfo.Pet = MorpherInfo.Pet;
                        VMInfo.name = MorpherInfo.PlayerName;
                        VMInfo.IsMorphing = false;
                }
                //元の名前を保存
                names[PlayerID] = player.Character.PlayerInfo.PlayerName;
                //名前を変更
                //string[] names = new string[e.Game.PlayerCount];
                player.Character.SetNameAsync(PlayingOn);
                //CompletedTaskPlayersを初期化
                CompletedTaskPlayers = 0;
                //Winnerを初期化
                Winner = -1;
                //HideAndSeek通知
                if(CRE.HideAndSeekEnabled) {
                    var writer = e.Game.StartRpc(player.Character.NetId, RpcCalls.SetName, player.Client.Id);
                    writer.Write(player.Character.PlayerInfo.PlayerName + "\r\nHideAndSeek");
                    e.Game.SendToAsync(writer, player.Character.PlayerId);
                    e.Game.FinishRpcAsync(writer);
                }
                //役職確認処理
                if(player.Character.PlayerInfo.IsImpostor)
                {
                    RoleNameForDoctor[player.Character.PlayerId] = "インポスター";
                    if(player.Client.Id == CRID.VentMorpherID && CRE.VentMorpherEnabled) {
                        _logger.LogInformation(player.Character.PlayerInfo.PlayerName + iVM);
                        player.Character.SendChatToPlayerAsync("You are VentMorpher.");
                        var writer = e.Game.StartRpc(player.Character.NetId, RpcCalls.SetName, player.Client.Id);
                        writer.Write(player.Character.PlayerInfo.PlayerName + "\r\nYou are VentMorpher");
                        e.Game.SendToAsync(writer, player.Character.PlayerId);
                        e.Game.FinishRpcAsync(writer);
                    } else if(player.Client.Id == CRID.BlackoutID && CRE.BlackoutEnabled) {
                        _logger.LogInformation(player.Character.PlayerInfo.PlayerName + iBO);
                        player.Character.SendChatToPlayerAsync("You are The Blackout.");
                        var writer = e.Game.StartRpc(player.Character.NetId, RpcCalls.SetName, player.Client.Id);
                        writer.Write(player.Character.PlayerInfo.PlayerName + "\r\nYou are The Blackout");
                        e.Game.SendToAsync(writer, player.Character.PlayerId);
                        e.Game.FinishRpcAsync(writer);
                    } else {
                        _logger.LogInformation(player.Character.PlayerInfo.PlayerName + iI);
                        player.Character.SendChatToPlayerAsync("You are Impostor.");
                    }
                    if(CRE.HideAndSeekEnabled) {
                        HaS.ImpostorSpawnX = player.Character.NetworkTransform.Position.X;
                        HaS.ImpostorSpawnY = player.Character.NetworkTransform.Position.Y;
                        player.Character.NetworkTransform.SnapToAsync(new System.Numerics.Vector2(64,64));
                    }
                } else if(player.Client.Id == CRID.TeruteruID && CRE.TeruteruEnabled) {
                    _logger.LogInformation(player.Character.PlayerInfo.PlayerName + iT);
                    player.Character.SendChatToPlayerAsync("You are Teruteru.");
                    player.Character.SetNameDesync(player.Character.PlayerInfo.PlayerName + "\r\nYou are Teruteru", e.Game);
                    RoleNameForDoctor[player.Character.PlayerId] = "てるてる";
                    //タスク関連処理
                    //タスク処理は名前を戻す処理と同タイミングでやるのでコメントアウト
                    /* var writer2 = e.Game.StartRpc(player.Character.NetId, RpcCalls.CompleteTask, player.Client.Id);
                    writer2.Write(1);
                    e.Game.SendToAsync(writer2, player.Character.PlayerId);
                    e.Game.FinishRpcAsync(writer2); */
                } else if(player.Client.Id == CRID.MadmateID && CRE.MadmateEnabled) {
                    _logger.LogInformation(player.Character.PlayerInfo.PlayerName + iT);
                    player.Character.SendChatToPlayerAsync("You are Madmate.");
                    player.Character.SetNameDesync(player.Character.PlayerInfo.PlayerName + "\r\n<color=red>You are Madmate", e.Game);
                    RoleNameForDoctor[player.Character.PlayerId] = "狂人";
                } else if(player.Client.Id == CRID.SuiciderID && CRE.SuiciderEnabled) {
                    _logger.LogInformation(player.Character.PlayerInfo.PlayerName + iSu);
                    player.Character.SendChatToPlayerAsync("You are Suicider.");
                    player.Character.SetNameDesync(player.Character.PlayerInfo.PlayerName + "\r\n<color=red>You are Suicider", e.Game);
                    RoleNameForDoctor[player.Character.PlayerId] = "狂人";
                } else if(player.Client.Id == CRID.DoctorID && CRE.DoctorEnabled) {
                    _logger.LogInformation(player.Character.PlayerInfo.PlayerName + iDc);
                    player.Character.SendChatToPlayerAsync("You are Doctor.");
                    player.Character.SetNameDesync(player.Character.PlayerInfo.PlayerName + "\r\nYou are Doctor", e.Game);
                    HavingTaskPlayers.Add(player);
                    RoleNameForDoctor[player.Character.PlayerId] = "医者";
                } else if(player.Client.Id == CRID.TerroristID && CRE.TerroristEnabled) {
                    _logger.LogInformation(player.Character.PlayerInfo.PlayerName + iTR);
                    player.Character.SendChatToPlayerAsync("You are Terrorist.");
                    player.Character.SetNameDesync(player.Character.PlayerInfo.PlayerName + "\r\nYou are Terrorist", e.Game);
                    RoleNameForDoctor[player.Character.PlayerId] = "テロリスト";
                } else {
                    _logger.LogInformation(player.Character.PlayerInfo.PlayerName + iC);
                    player.Character.SendChatToPlayerAsync("You are Crewmate.");
                    HavingTaskPlayers.Add(player);
                    RoleNameForDoctor[player.Character.PlayerId] = "クルー";
                }
                //アビリティ
                if(player.Client.Id == CRID.FastTrollID && CRE.FastTrollEnabled) {
                    _logger.LogInformation(player.Character.PlayerInfo.PlayerName + iFT);
                    player.Character.SendChatToPlayerAsync("You were FastTroll.");
                    player.Character.SetNameDesync(player.Character.PlayerInfo.PlayerName + "\r\nYou are FastTroll", e.Game);
                }
                if(player.Client.Id == CRID.MagnetTrollID && CRE.MagnetTrollEnabled) {
                    _logger.LogInformation(player.Character.PlayerInfo.PlayerName + iMT);
                    player.Character.SetNameDesync(player.Character.PlayerInfo.PlayerName + "\r\nYou are MagnetTroll", e.Game);
                }
                if(player.Client.Id == CRID.LeaderID && CRE.LeaderEnabled) {
                    _logger.LogInformation(player.Character.PlayerInfo.PlayerName + iL);
                    player.Character.SendChatToPlayerAsync("You are Leader.");
                    SetLederNameColor(e.Game);
                    if(!e.Game.Host.Character.PlayerInfo.IsImpostor) {
                        player.Character.SetNameDesync("<color=yellow>" + player.Character.PlayerInfo.PlayerName, e.Game, e.Game.Host.Character);
                    }
                    player.Character.SetNameDesync(player.Character.PlayerInfo.PlayerName + "\r\nYou are Leader", e.Game);
                    RoleNameForDoctor[player.Character.PlayerId] = "偽のリーダー";
                }
                //FastMeetgingStartedを初期化
                CRE.FastMeetingStarted = false;
                //スキャンタスク無効化処理
                if(CRE.NoScanTaskEnabled) {
                    foreach(var Tasks in player.Character.PlayerInfo.Tasks) {
                        if(Tasks.Task.Type == TaskTypes.SubmitScan) {
                            Tasks.CompleteAsync();
                        }
                    }
                }
                //名前を戻す処理
                Task task = Task.Run(() => {
                    _logger.LogInformation("非同期処理の始まり");
                    Thread.Sleep(20000);
                    player.Character.SetNameAsync(names[PlayerID]);
                    SetLederNameColor(e.Game);
                    //Madmate用
                    if(player.Client.Id == CRID.MadmateID && CRE.MadmateEnabled) {
                        //madmateにインポスターの名前を通知
                        if(CRE.MadmateKnowsImpostor) {
                            foreach(var ImpostorPlayer in e.Game.Players) {
                                if(ImpostorPlayer.Character.PlayerInfo.IsImpostor) {
                                    player.Character.SendChatToPlayerAsync(ImpostorPlayer.Character.PlayerInfo.PlayerName + " is an impostor.");
                                }
                            }
                        }
                    }
                    _logger.LogInformation("非同期処理の終わり");
                    //鬼ごっこ用インポスターを戻す
                    if(CRE.HideAndSeekEnabled && player.Character.PlayerInfo.IsImpostor) {
                        Thread.Sleep(10000);
                        player.Character.NetworkTransform.SnapToAsync(new System.Numerics.Vector2(HaS.ImpostorSpawnX,HaS.ImpostorSpawnY));
                        player.Character.SetNameAsync(player.Character.PlayerInfo.PlayerName + "\r\nImpostor");
                        if(e.Game.Options.Map == MapTypes.Airship) {
                            player.Character.NetworkTransform.SnapToAsync(new System.Numerics.Vector2(0,0));
                        }
                    }
                    //ターゲット再通知
                    if(CRE.TargetModeEnabled) {
                        Impostor.Api.Net.IClientPlayer Target = null;
                        foreach(var target in e.Game.Players) {
                            if(target.Client.Id == CRID.TargetID) {
                                Target = target;
                            }
                        }
                        foreach(var imp in e.Game.Players) {
                            if(imp.Character.PlayerInfo.IsImpostor && Target != null) {
                                var writer = e.Game.StartRpc(imp.Character.NetId, RpcCalls.SetName, imp.Client.Id);
                                writer.Write(imp.Character.PlayerInfo.PlayerName + "\r\nTarget is " + Target.Character.PlayerInfo.PlayerName);
                                e.Game.SendToAsync(writer, imp.Character.PlayerId);
                                e.Game.FinishRpcAsync(writer);
                            }
                        }
                    }
                    //Teruteruのタスクを終わらせる
                    /*if(player.Client.Id == CRID.TeruteruID && CRE.TeruteruEnabled) {
                        foreach(var task in  player.Character.PlayerInfo.Tasks) {
                            task.CompleteAsync();
                        }
                    }*/
                    //スキャンタスクを終わらせる
                    if(CRE.NoScanTaskEnabled) {
                        foreach(var task in player.Character.PlayerInfo.Tasks) {
                            if(task.Task.Type == TaskTypes.SubmitScan) {
                                task.CompleteAsync();
                            }
                        }
                    }
                    SetRoleColor(e.Game);
                });
                if(CRE.HideAndSeekEnabled  && player.Character.PlayerInfo.IsImpostor) {
                    Task HaStask = Task.Run(() => {
                        _logger.LogInformation("鬼ごっこ用非同期処理の始まり");
                        Thread.Sleep(5000);
                        player.Character.NetworkTransform.SnapToAsync(new System.Numerics.Vector2(64,64));
                        Thread.Sleep(5000);
                        player.Character.NetworkTransform.SnapToAsync(new System.Numerics.Vector2(64,64));
                        Thread.Sleep(5000);
                        player.Character.NetworkTransform.SnapToAsync(new System.Numerics.Vector2(64,64));
                        _logger.LogInformation("鬼ごっこ用非同期処理の終わり");
                    });
                }
                //TeruteruRPCの送信
            }//ここでやっとforeachが終了
            //タスク数初期化処理
            CRID.CrewmatesTaskCount = HavingTaskPlayers.Count * TaskPerPlayer;
            CRID.TerroristTaskCount = TaskPerPlayer;
            _logger.LogInformation("テロリストの残りタスク数：" + CRID.TerroristTaskCount);
            _logger.LogInformation("クルーの残りタスク数：" + CRID.CrewmatesTaskCount);
            //定期処理作成
            //先にTimerを定義しておく
            Timer checkPosTimer = null;
            //それから処理とTimerの中身の作成する
            TimerCallback checkPosTimerCallBack = state => {
                if(e.Game.GameState != GameStates.Started) {
                    _logger.LogWarning("タイマーを内部破壊しました");
                    checkPosTimer.Dispose();
                } else {
                    foreach(var player in e.Game.Players) {
                        var playerPos = player.Character.NetworkTransform.Position;
                        if(player.Client.Id == CRID.SuiciderID && CRE.SuiciderEnabled && !player.Character.PlayerInfo.IsDead) {
                            if(playerPos.X == CRID.SuiciderBeforeX && playerPos.Y == CRID.SuiciderBeforeY) {
                                CRID.SuiciderCount++;
                                _logger.LogInformation("Suiciderが" + CRID.SuiciderCount + "秒間立ち止まっています");
                                var writer = e.Game.StartRpc(player.Character.NetId, RpcCalls.SetName, player.Client.Id);
                                writer.Write("<color=red>" + player.Character.PlayerInfo.PlayerName + "\r\n</color><color=white>Count:" + CRID.SuiciderCount);
                                e.Game.SendToAsync(writer, player.Character.PlayerId);
                                e.Game.FinishRpcAsync(writer);
                            } else {
                                CRID.SuiciderCount = 0;
                                CRID.SuiciderBeforeX = playerPos.X;
                                CRID.SuiciderBeforeY = playerPos.Y;
                                var writer = e.Game.StartRpc(player.Character.NetId, RpcCalls.SetName, player.Client.Id);
                                writer.Write(player.Character.PlayerInfo.PlayerName);
                                e.Game.SendToAsync(writer, player.Character.PlayerId);
                                e.Game.FinishRpcAsync(writer);
                            }
                            if(CRID.SuiciderCount >= 30) {
                                _logger.LogInformation("Suiciderは自殺しました");
                                foreach(var imp in e.Game.Players) {
                                    if(imp.Character.PlayerInfo.IsImpostor && imp.Character.PlayerInfo.IsDead) {
                                        var impX = imp.Character.NetworkTransform.Position.X;
                                        var impY = imp.Character.NetworkTransform.Position.Y;
                                        imp.Character.MurderPlayerAsync(player.Character);
                                        imp.Character.NetworkTransform.SnapToAsync(new System.Numerics.Vector2(impX,impY));
                                    }
                                }
                            }
                        }
                    }
                }
            };
            checkPosTimer = new Timer(checkPosTimerCallBack, null, 0, 1000);
        }
        [EventListener]
        public void PlayerVotedOut(IPlayerExileEvent e)
        {
            CheckWinner(e.Game);
            //When Teruteru has voted out
            if(CRE.TeruteruWinDisabled) {_logger.LogInformation("teruteruの勝利は無効化されています");}
            if(e.ClientPlayer.Client.Id == CRID.TeruteruID && !e.PlayerControl.PlayerInfo.IsImpostor && CRE.TeruteruEnabled && CRE.TeruteruWinDisabled == false && e.ClientPlayer.Client.Id != CRID.TraitorID)
            {
                if(e.PlayerControl.PlayerInfo.IsDead) {
                    CheckWinner(e.Game, true);
                } else {
                    _logger.LogInformation("同数投票が発生");
                }
            }
            if(e.ClientPlayer.Client.Id == CRID.LeaderID && CRE.LeaderEnabled && e.PlayerControl.PlayerInfo.IsDead) {
                LeaderDead(e.Game, e.ClientPlayer, null);
                CheckWinner(e.Game);
            }
        }
        [EventListener]
        public void Commands(IPlayerChatEvent e)
        {
            //prefix
            if(e.Message.StartsWith("/")){
                string cmd1;
                string cmd2;
                var FirstSpace = e.Message.IndexOf(" ");
                if (FirstSpace == -1){
                    cmd1 = e.Message.Substring(1,e.Message.Length - 1);
                    cmd2 = null;
                } else {
                    cmd1 = e.Message.Substring(1, FirstSpace - 1);
                    cmd2 = e.Message.Substring(FirstSpace + 1);
                }
                var PIDFail = "エラー:IDを正常に変換できませんでした。\r\n()内の数字を正確に入力してください。";
                var PlayerCTRL = e.PlayerControl;
                //役職ON/OFF
                if(cmd1 == "teruteru") {
                    if(e.Game.GameState == 0) {
                        if(cmd2 == "on") {
                            PlayerCTRL.SendChatToPlayerAsync("Teruteruが有効化されました。");
                            CRE.TeruteruEnabled = true;
                        } else if(cmd2 == "off") {
                            PlayerCTRL.SendChatToPlayerAsync("Teruteruが無効化されました。");
                            CRE.TeruteruEnabled = false;
                        } else {
                            PlayerCTRL.SendChatToPlayerAsync("エラー:2つ目の引数が無効です。\r\non/offで指定してください。");
                        }
                    } else {
                        PlayerCTRL.SendChatToPlayerAsync("エラー:既にゲームは開始されています。\r\nゲームが開始していない状態で変更してください。");
                    }
                }
                if(cmd1 == "madmate") {
                    if(e.Game.GameState == 0) {
                        if(cmd2 == "on") {
                            PlayerCTRL.SendChatToPlayerAsync("Madmateが有効化されました。");
                            CRE.MadmateEnabled = true;
                        } else if(cmd2 == "off") {
                            PlayerCTRL.SendChatToPlayerAsync("Madmateが無効化されました。");
                            CRE.MadmateEnabled = false;
                        } else {
                            PlayerCTRL.SendChatToPlayerAsync("エラー:2つ目の引数が無効です。\r\non/offで指定してください。");
                        }
                    } else {
                        PlayerCTRL.SendChatToPlayerAsync("エラー:既にゲームは開始されています。\r\nゲームが開始していない状態で変更してください。");
                    }
                }
                if(cmd1 == "leader") {
                    if(e.Game.GameState == 0) {
                        if(cmd2 == "on") {
                            PlayerCTRL.SendChatToPlayerAsync("リーダーが有効化されました。");
                            CRE.LeaderEnabled = true;
                        } else if(cmd2 == "off") {
                            PlayerCTRL.SendChatToPlayerAsync("リーダーが無効化されました。");
                            CRE.LeaderEnabled = false;
                        } else {
                            PlayerCTRL.SendChatToPlayerAsync("エラー:2つ目の引数が無効です。\r\non/offで指定してください。");
                        }
                    } else {
                        PlayerCTRL.SendChatToPlayerAsync("エラー:既にゲームは開始されています。\r\nゲームが開始していない状態で変更してください。");
                    }
                }
                if(cmd1 == "teruormad") {
                    if(e.Game.GameState == 0) {
                        if(cmd2 == "on") {
                            PlayerCTRL.SendChatToPlayerAsync("TeruMadランダムモードが有効化されました。\r\n※このモードはteruteru,madmateの設定に関わらずランダム化されます。");
                            CRE.TeruOrMadEnabled = true;
                        } else if(cmd2 == "off") {
                            PlayerCTRL.SendChatToPlayerAsync("TeruMadランダムモードが無効化されました。");
                            CRE.TeruOrMadEnabled = false;
                        } else {
                            PlayerCTRL.SendChatToPlayerAsync("エラー:2つ目の引数が無効です。\r\non/offで指定してください。");
                        }
                    } else {
                        PlayerCTRL.SendChatToPlayerAsync("エラー:既にゲームは開始されています。\r\nゲームが開始していない状態で変更してください。");
                    }
                }
                if(cmd1 == "suicider") {
                    if(e.Game.GameState == 0) {
                        if(cmd2 == "on") {
                            PlayerCTRL.SendChatToPlayerAsync("自殺狂人が有効化されました。");
                            CRE.SuiciderEnabled = true;
                        } else if(cmd2 == "off") {
                            PlayerCTRL.SendChatToPlayerAsync("自殺狂人が無効化されました。");
                            CRE.SuiciderEnabled = false;
                        } else {
                            PlayerCTRL.SendChatToPlayerAsync("エラー:2つ目の引数が無効です。\r\non/offで指定してください。");
                        }
                    } else {
                        PlayerCTRL.SendChatToPlayerAsync("エラー:既にゲームは開始されています。\r\nゲームが開始していない状態で変更してください。");
                    }
                }
                if(cmd1 == "fasttroll") {
                    if(e.Game.GameState == 0) {
                        if(cmd2 == "on") {
                            PlayerCTRL.SendChatToPlayerAsync("FastTrollが有効化されました。");
                            CRE.FastTrollEnabled = true;
                        } else if(cmd2 == "off") {
                            PlayerCTRL.SendChatToPlayerAsync("FastTrollが無効化されました。");
                            CRE.FastTrollEnabled = false;
                        } else {
                            PlayerCTRL.SendChatToPlayerAsync("エラー:2つ目の引数が無効です。\r\non/offで指定してください。");
                        }
                    } else {
                        PlayerCTRL.SendChatToPlayerAsync("エラー:既にゲームは開始されています。\r\nゲームが開始していない状態で変更してください。");
                    }
                }
                if(cmd1 == "magnettroll") {
                    if(e.Game.GameState == 0) {
                        if(cmd2 == "on") {
                            PlayerCTRL.SendChatToPlayerAsync("MagnetTrollが有効化されました。");
                            CRE.MagnetTrollEnabled = true;
                        } else if(cmd2 == "off") {
                            PlayerCTRL.SendChatToPlayerAsync("MagnetTrollが無効化されました。");
                            CRE.MagnetTrollEnabled = false;
                        } else {
                            PlayerCTRL.SendChatToPlayerAsync("エラー:2つ目の引数が無効です。\r\non/offで指定してください。");
                        }
                    } else {
                        PlayerCTRL.SendChatToPlayerAsync("エラー:既にゲームは開始されています。\r\nゲームが開始していない状態で変更してください。");
                    }
                }
                if(cmd1 == "blackout") {
                    if(e.Game.GameState == 0) {
                        if(cmd2 == "on") {
                            PlayerCTRL.SendChatToPlayerAsync("The Blackoutが有効化されました。");
                            CRE.BlackoutEnabled = true;
                        } else if(cmd2 == "off") {
                            PlayerCTRL.SendChatToPlayerAsync("The Blackoutが無効化されました。");
                            CRE.BlackoutEnabled = false;
                        } else {
                            PlayerCTRL.SendChatToPlayerAsync("エラー:2つ目の引数が無効です。\r\non/offで指定してください。");
                        }
                    } else {
                        PlayerCTRL.SendChatToPlayerAsync("エラー:既にゲームは開始されています。\r\nゲームが開始していない状態で変更してください。");
                    }
                }
                if(cmd1 == "doctor") {
                    if(e.Game.GameState == 0) {
                        if(cmd2 == "on") {
                            PlayerCTRL.SendChatToPlayerAsync("医者が有効化されました。");
                            CRE.DoctorEnabled = true;
                        } else if(cmd2 == "off") {
                            PlayerCTRL.SendChatToPlayerAsync("医者が無効化されました。");
                            CRE.DoctorEnabled = false;
                        } else {
                            PlayerCTRL.SendChatToPlayerAsync("エラー:2つ目の引数が無効です。\r\non/offで指定してください。");
                        }
                    } else {
                        PlayerCTRL.SendChatToPlayerAsync("エラー:既にゲームは開始されています。\r\nゲームが開始していない状態で変更してください。");
                    }
                }
                if(cmd1 == "terrorist") {
                    if(e.Game.GameState == 0) {
                        if(cmd2 == "on") {
                            PlayerCTRL.SendChatToPlayerAsync("テロリストが有効化されました。");
                            CRE.TerroristEnabled = true;
                        } else if(cmd2 == "off") {
                            PlayerCTRL.SendChatToPlayerAsync("テロリストが無効化されました。");
                            CRE.TerroristEnabled = false;
                        } else {
                            PlayerCTRL.SendChatToPlayerAsync("エラー:2つ目の引数が無効です。\r\non/offで指定してください。");
                        }
                    } else {
                        PlayerCTRL.SendChatToPlayerAsync("エラー:既にゲームは開始されています。\r\nゲームが開始していない状態で変更してください。");
                    }
                }
                if(cmd1 == "ventmorpher") {
                    if(e.Game.GameState == 0) {
                        if(cmd2 == "on") {
                            PlayerCTRL.SendChatToPlayerAsync("VentMorpherが有効化されました。\r\n警告:この役職には致命的なバグがあります。");
                            CRE.VentMorpherEnabled = true;
                        } else if(cmd2 == "off") {
                            PlayerCTRL.SendChatToPlayerAsync("VentMorpherが無効化されました。");
                            CRE.VentMorpherEnabled = false;
                        } else {
                            PlayerCTRL.SendChatToPlayerAsync("エラー:2つ目の引数が無効です。\r\non/offで指定してください。");
                        }
                    } else {
                        PlayerCTRL.SendChatToPlayerAsync("エラー:既にゲームは開始されています。\r\nゲームが開始していない状態で変更してください。");
                    }
                }
                //役職の設定
                if(cmd1 == "hideandseek") {
                    if(cmd2 == "on") {
                        PlayerCTRL.SendChatToPlayerAsync("HideAndSeekが有効化されました。");
                        CRE.HideAndSeekEnabled = true;
                    } else if(cmd2 == "off") {
                        PlayerCTRL.SendChatToPlayerAsync("HideAndSeekが無効化されました。");
                        CRE.HideAndSeekEnabled = false;
                    } else {
                        PlayerCTRL.SendChatToPlayerAsync("エラー:2つ目の引数が無効です。\r\non/offで指定してください。");
                    }
                }
                //部屋の設定
                if(cmd1 == "killcool") {
                    float cmd2float;
                    if(float.TryParse(cmd2, out cmd2float)) {
                        e.Game.Options.KillCooldown = cmd2float;
                        PlayerCTRL.SendChatToPlayerAsync("キルクールダウンを" + cmd2 + "に変更しました。");
                        e.Game.SyncSettingsAsync();
                    } else {
                        PlayerCTRL.SendChatToPlayerAsync(PIDFail);
                    }
                }
                if(cmd1 == "flashspeed") {
                    float cmd2float;
                    if(float.TryParse(cmd2, out cmd2float)) {
                        CRID.FlashSpeed = cmd2float;
                        PlayerCTRL.SendChatToPlayerAsync("Flashのスピードを" + cmd2 + "に変更しました。");
                        e.Game.SyncSettingsAsync();
                    } else {
                        PlayerCTRL.SendChatToPlayerAsync(PIDFail + "\r\n現在のFlashのスピードは" + CRID.FlashSpeed + "です。\r\n警告:上げすぎると壁を抜けます。");
                    }
                }
                if(cmd1 == "flashmin") {
                    float cmd2float;
                    if(float.TryParse(cmd2, out cmd2float)) {
                        CRID.FlashMin = cmd2float;
                        PlayerCTRL.SendChatToPlayerAsync("Flashのしきい値を" + cmd2 + "に変更しました。");
                        e.Game.SyncSettingsAsync();
                    } else {
                        PlayerCTRL.SendChatToPlayerAsync(PIDFail + "\r\n現在のFlashのしきい値は" + CRID.FlashMin + "です。\r\n警告:上げすぎると操作不能になります。");
                    }
                }
                if(cmd1 == "stoppertime") {
                    int cmd2int;
                    if(int.TryParse(cmd2, out cmd2int)) {
                        CRID.StopperStopTime = cmd2int;
                        PlayerCTRL.SendChatToPlayerAsync("Stopperの足止め時間を" + cmd2 + "秒に変更しました。");
                    } else {
                        PlayerCTRL.SendChatToPlayerAsync(PIDFail);
                    }
                }
                if(cmd1 == "commontask") {
                    int cmd2int;
                    if(int.TryParse(cmd2, out cmd2int)) {
                        e.Game.Options.NumCommonTasks = cmd2int;
                        PlayerCTRL.SendChatToPlayerAsync("コモンタスクの量を" + cmd2 + "に変更しました。");
                        e.Game.SyncSettingsAsync();
                    } else {
                        PlayerCTRL.SendChatToPlayerAsync(PIDFail);
                    }
                }
                if(cmd1 == "longtask") {
                    int cmd2int;
                    if(int.TryParse(cmd2, out cmd2int)) {
                        e.Game.Options.NumLongTasks = cmd2int;
                        PlayerCTRL.SendChatToPlayerAsync("ロングタスクの量を" + cmd2 + "に変更しました。");
                        e.Game.SyncSettingsAsync();
                    } else {
                        PlayerCTRL.SendChatToPlayerAsync(PIDFail);
                    }
                }
                if(cmd1 == "shorttask") {
                    int cmd2int;
                    if(int.TryParse(cmd2, out cmd2int)) {
                        e.Game.Options.NumShortTasks = cmd2int;
                        PlayerCTRL.SendChatToPlayerAsync("ショートタスクの量を" + cmd2 + "に変更しました。");
                        e.Game.SyncSettingsAsync();
                    } else {
                        PlayerCTRL.SendChatToPlayerAsync(PIDFail);
                    }
                }
                if(cmd1 == "map") {
                    if(cmd2 == "skeld") {
                        e.Game.Options.Map = MapTypes.Skeld;
                        PlayerCTRL.SendChatToPlayerAsync("mapをSkeldに変更しました。");
                        e.Game.SyncSettingsAsync();
                    } else if(cmd2 == "mirahq") {
                        e.Game.Options.Map = MapTypes.MiraHQ;
                        PlayerCTRL.SendChatToPlayerAsync("mapをMiraHQに変更しました。");
                        e.Game.SyncSettingsAsync();
                    } else if(cmd2 == "polus") {
                        e.Game.Options.Map = MapTypes.Polus;
                        PlayerCTRL.SendChatToPlayerAsync("mapをPolusに変更しました。");
                        e.Game.SyncSettingsAsync();
                    } else if(cmd2 == "airship") {
                        e.Game.Options.Map = MapTypes.Airship;
                        PlayerCTRL.SendChatToPlayerAsync("mapをAirshipに変更しました。");
                        e.Game.SyncSettingsAsync();
                    } else {
                        PlayerCTRL.SendChatToPlayerAsync("エラー:mapの名前が無効です。以下の名前が使えます。\r\nskeld, mirahq, polus, airship");
                    }
                }
                if(cmd1 == "noscantask") {
                    if(e.Game.GameState == 0) {
                        if(cmd2 == "on") {
                            PlayerCTRL.SendChatToPlayerAsync("医務室のスキャンタスクが無効化されました。");
                            CRE.NoScanTaskEnabled = true;
                        } else if(cmd2 == "off") {
                            PlayerCTRL.SendChatToPlayerAsync("医務室のスキャンタスクが有効化されました。");
                            CRE.NoScanTaskEnabled = false;
                        } else {
                            PlayerCTRL.SendChatToPlayerAsync("エラー:2つ目の引数が無効です。\r\non/offで指定してください。");
                        }
                    } else {
                        PlayerCTRL.SendChatToPlayerAsync("エラー:既にゲームは開始されています。\r\nゲームが開始していない状態で変更してください。");
                    }
                }
                if(cmd1 == "madmateknowsimpostor") {
                    if(e.Game.GameState == 0) {
                        if(cmd2 == "on") {
                            PlayerCTRL.SendChatToPlayerAsync("MadmateへのImpostorの通知が有効化されました。");
                            CRE.MadmateKnowsImpostor = true;
                        } else if(cmd2 == "off") {
                            PlayerCTRL.SendChatToPlayerAsync("MadmateへのImpostorの通知が無効化されました。");
                            CRE.MadmateKnowsImpostor = false;
                        } else {
                            PlayerCTRL.SendChatToPlayerAsync("エラー:2つ目の引数が無効です。\r\non/offで指定してください。");
                        }
                    } else {
                        PlayerCTRL.SendChatToPlayerAsync("エラー:既にゲームは開始されています。\r\nゲームが開始していない状態で変更してください。");
                    }
                }
                if(cmd1 == "targetmode") {
                    if(e.Game.GameState == 0) {
                        if(cmd2 == "on") {
                            PlayerCTRL.SendChatToPlayerAsync("ターゲットモードが有効化されました。");
                            CRE.TargetModeEnabled = true;
                        } else if(cmd2 == "off") {
                            PlayerCTRL.SendChatToPlayerAsync("ターゲットモードが無効化されました。");
                            CRE.TargetModeEnabled = false;
                        } else {
                            PlayerCTRL.SendChatToPlayerAsync("エラー:2つ目の引数が無効です。\r\non/offで指定してください。");
                        }
                    } else {
                        PlayerCTRL.SendChatToPlayerAsync("エラー:既にゲームは開始されています。\r\nゲームが開始していない状態で変更してください。");
                    }
                }
                //試合中のコマンド
                if(cmd1 == "killgame") {
                    if(e.ClientPlayer.IsHost) {
                        foreach(var player in e.Game.Players) {
                            if(player.Character.PlayerInfo.IsImpostor) {
                                player.Character.SetNameAsync("ゲームは強制的に終了されました");
                                foreach(var player2 in e.Game.Players) {
                                    if(!player2.Character.PlayerInfo.IsImpostor) {
                                        player.Character.MurderPlayerAsync(player2.Character);
                                    }
                                }
                                player.Character.MurderPlayerAsync(player.Character);
                            }
                        }
                    } else {
                        PlayerCTRL.SendChatToPlayerAsync("エラー:あなたはホストではないため、このコマンドを実行できません。");
                    }
                }
                //テスト
                if(cmd1 == "namedm") {
                    if(cmd2 == null) {
                        e.ClientPlayer.Character.SendChatToPlayerAsync("エラー:名前が指定されていません。");
                    } else {
                        var player = e.ClientPlayer;
                        var writer = e.Game.StartRpc(player.Character.NetId, RpcCalls.SetName, player.Client.Id);
                        writer.Write(cmd2);
                        e.Game.SendToAsync(writer, player.Character.PlayerId);
                        e.Game.FinishRpcAsync(writer);
                    }
                }
                if(cmd1 == "rpc") {
                    if(byte.TryParse(cmd2, out var playerID)) {
                        if(e.ClientPlayer.IsHost) {
                            foreach(var player in e.Game.Players) {
                                var writer2 = e.Game.StartRpc(e.ClientPlayer.Character.NetId, RpcCalls.CustomRoles, player.Character.PlayerId);
                                writer2.Write(playerID);
                                e.Game.SendToAsync(writer2, player.Character.PlayerId);
                                e.Game.FinishRpcAsync(writer2);
                            }
                        } else {
                            e.ClientPlayer.Character.SendChatToPlayerAsync("エラー:この機能はホストのみ使用可能です");
                        }
                    } else {
                        e.ClientPlayer.Character.SendChatToPlayerAsync(PIDFail);
                    }
                }
                if(cmd1 == "beimpostor") {
                    if(int.TryParse(cmd2, out var InfectID)) {
                        if(e.ClientPlayer.IsHost) {
                            foreach(var player in e.Game.Players) {
                                if(player.Character.PlayerId == InfectID) {
                                    var writer2 = e.Game.StartRpc(e.Game.Host.Character.NetId, RpcCalls.SetInfected, player.Character.PlayerId);
                                    var InfectedIds = new byte[player.Character.PlayerId];
                                    writer2.Write(InfectID);
                                    e.Game.SendToAsync(writer2, player.Character.PlayerId);
                                    e.Game.FinishRpcAsync(writer2);
                                    player.Character.SendChatAsync("私はインポスターになる予定です。");
                                    e.ClientPlayer.Character.SendChatToPlayerAsync("警告:この機能はデバッグ用のチートです");
                                }
                            }
                        } else {
                            e.ClientPlayer.Character.SendChatToPlayerAsync("エラー:この機能はホストのみ使用可能です");
                        }
                    } else {
                        e.ClientPlayer.Character.SendChatToPlayerAsync(PIDFail);
                    }
                }
                if(cmd1 == "sometext") {
                    Task task = Task.Run(() => {
                        PlayerCTRL.SetNameAsync("I am <color=ff0000ff>Impostor.");
                        Thread.Sleep(10000);
                        PlayerCTRL.SetNameAsync("I am <color=ff0000>Impostor.");
                        Thread.Sleep(10000);
                        PlayerCTRL.SetNameAsync("I am <size=12>Impostor.");
                        Thread.Sleep(10000);
                        PlayerCTRL.SetNameAsync("[ff0000ff]I am Impostor.");
                    });
                }
                if(cmd1 == "task") {
                    if(int.TryParse(cmd2, out var TaskID)) {
                        if(e.ClientPlayer.IsHost) {
                            foreach(var player in e.Game.Players) {
                                var writer2 = e.Game.StartRpc(e.ClientPlayer.Character.NetId, RpcCalls.CompleteTask, player.Character.PlayerId);
                                writer2.Write(TaskID);
                                e.Game.SendToAsync(writer2, player.Character.PlayerId);
                                e.Game.FinishRpcAsync(writer2);
                                e.ClientPlayer.Character.SendChatToPlayerAsync("警告:この機能はデバッグ用のチートです");
                            }
                        } else {
                            e.ClientPlayer.Character.SendChatToPlayerAsync("エラー:この機能はホストのみ使用可能です");
                        }
                    } else {
                        e.ClientPlayer.Character.SendChatToPlayerAsync(PIDFail);
                    }
                }
                if(cmd1 == "optionview") {
                    PlayerCTRL.SendChatToPlayerAsync("GameDisplayName\r\n" + e.Game.DisplayName);
                    PlayerCTRL.SendChatToPlayerAsync("GameKeyword\r\n" + e.Game.Options.Keywords);
                }
                //プレイヤーのオプション
                if(cmd1 == "rename") {
                    if(cmd2 == null) {
                        e.ClientPlayer.Character.SendChatToPlayerAsync("エラー:名前が指定されていません。");
                    } else {
                        e.ClientPlayer.Character.SetNameAsync(cmd2);
                    }
                }
                if(cmd1 == "color") {
                    if(cmd2 == null) {
                        e.ClientPlayer.Character.SendChatToPlayerAsync("エラー:色が指定されていません");
                    } else {
                        e.ClientPlayer.Character.SetNameAsync("<color=" + cmd2 + ">" + e.ClientPlayer.Character.PlayerInfo.PlayerName);
                    }
                }
                if(cmd1 == "namecode") {
                    if(cmd2 == null) {
                        e.ClientPlayer.Character.SendChatToPlayerAsync("エラー:文字列が指定されていません");
                    } else {
                        e.ClientPlayer.Character.SetNameAsync("<" + cmd2 + ">" + e.ClientPlayer.Character.PlayerInfo.PlayerName);
                    }
                }
                if(cmd1 == "tp") {
                    if(int.TryParse(cmd2, out var TargetID)) {
                        foreach(var target in e.Game.Players) {
                            if(target.Character.PlayerId == TargetID) {
                                e.ClientPlayer.Character.NetworkTransform.SnapToAsync(target.Character.NetworkTransform.Position);
                            }
                        }
                    } else {
                        e.ClientPlayer.Character.SendChatToPlayerAsync(PIDFail);
                    }
                }
                if(cmd1 == "endmeeting") {
                    _logger.LogInformation("会議強制終了テスト");
                    var h = e.Game.Host.Character.PlayerId;
                    byte[] VoteStatus = {h,h,h,h,h,h,h,h,h,h,h,h,h,h,h};
                    var writer2 = e.Game.StartRpc(e.Game.Host.Character.NetId, RpcCalls.CastVote, e.Game.Host.Character.PlayerId);
                    Rpc24CastVote.Serialize(writer2, e.PlayerControl.PlayerId, 0);
                    e.Game.SendToAllAsync(writer2);
                    e.Game.FinishRpcAsync(writer2);
                }
                if(cmd1 == "speed") {
                    _logger.LogInformation("設定変更テスト");
                    var CustomOption = e.Game.Options;
                    CustomOption.PlayerSpeedMod = CustomOption.PlayerSpeedMod * 3f;
                    var writer2 = e.Game.StartRpc(e.ClientPlayer.Character.NetId, RpcCalls.CompleteTask, e.ClientPlayer.Character.PlayerId);
                    Rpc02SyncSettings.Serialize(writer2, CustomOption);
                    e.Game.SendToAsync(writer2, e.ClientPlayer.Character.PlayerId);
                    e.Game.FinishRpcAsync(writer2);
                }
                if(cmd1 == "idlist") {
                    foreach(var player in e.Game.Players) {
                        e.PlayerControl.SendChatToPlayerAsync(player.Character.PlayerInfo.PlayerName + ":" + player.Character.PlayerId);
                    }
                }
                if(cmd1 == "ghosttask") {
                    if(e.Game.GameState == 0) {
                        if(cmd2 == "on") {
                            PlayerCTRL.SendChatToPlayerAsync("幽霊のタスクが有効化されました。\r\n警告:このコマンドは機能していません");
                            e.Game.Options.GhostsDoTasks = true;
                        } else if(cmd2 == "off") {
                            PlayerCTRL.SendChatToPlayerAsync("幽霊のタスクが無効化されました。\r\n警告:このコマンドは機能していません");
                            e.Game.Options.GhostsDoTasks = false;
                        } else {
                            PlayerCTRL.SendChatToPlayerAsync("エラー:2つ目の引数が無効です。\r\non/offで指定してください。");
                        }
                    } else {
                        PlayerCTRL.SendChatToPlayerAsync("エラー:既にゲームは開始されています。\r\nゲームが開始していない状態で変更してください。");
                    }
                }
                if(cmd1 == "lobbyoutside") {
                    if(e.Game.GameState == 0) {
                        e.PlayerControl.NetworkTransform.SnapToAsync(new System.Numerics.Vector2(0,5));
                    } else {
                        PlayerCTRL.SendChatToPlayerAsync("エラー:既にゲームは開始されています。\r\nゲームが開始していない状態で変更してください。");
                    }
                }
                if(cmd1 == "lobbyinside") {
                    if(e.Game.GameState == 0) {
                        e.PlayerControl.NetworkTransform.SnapToAsync(new System.Numerics.Vector2(0,0));
                    } else {
                        PlayerCTRL.SendChatToPlayerAsync("エラー:既にゲームは開始されています。\r\nゲームが開始していない状態で変更してください。");
                    }
                }
                //help
                if(cmd1 == "help") {
                    if(cmd2 == null) {
                        e.ClientPlayer.Character.SendChatToPlayerAsync("以下のオプションが使用可能です。\r\nrole, option, user");
                    }
                    if(cmd2 == "role") {
                        e.ClientPlayer.Character.SendChatToPlayerAsync(
                            "/teruteru, /sheriff, /stopper\r\n" + 
                            "/fasttroll, /magnettroll, /madmate\r\n" + 
                            "/suicider"
                            );
                    }
                    if(cmd2 == "option") {
                        e.ClientPlayer.Character.SendChatToPlayerAsync(
                            "/stoppertime, /killcool /madmateknowsimpostor\r\n" + 
                            "/map, /noscantask /hideandseek\r\n" + 
                            "/commontask, /longtask, /shorttask\r\n" + 
                            "/targetmode"
                            );
                    }
                    if(cmd2 == "user") {
                        e.ClientPlayer.Character.SendChatToPlayerAsync(
                            "/tp, /rename, /idlist\r\n" + 
                            "/lobbyoutside, /lobbyinside"
                            );
                    }
                }
            //ログ
            _logger.LogInformation("// Command executed.\r\n" + cmd1 + "\r\n" + cmd2);
            e.IsCancelled = true;
            }
        }
        [EventListener]
        public void SheriffAndDictatorKill(IPlayerVotedEvent e) {
            /* Encoding sjisEnc = Encoding.GetEncoding("Unicode");
            StreamWriter Logger = new StreamWriter("tmp.log", true, sjisEnc);
            Logger.WriteLine(e.ClientPlayer.Character.PlayerInfo.PlayerName + " voted for " + e.VotedFor.PlayerInfo.PlayerName);
            Logger.Close(); */
            var VoteBy = e.ClientPlayer;
        }
        [EventListener]
        public void Vented(IPlayerEnterVentEvent e) {
            if(CRE.VentMorpherEnabled && e.ClientPlayer.Client.Id == CRID.VentMorpherID) {
                var Morpher = e.ClientPlayer.Character;
                VMInfo.LastVentX = e.Vent.Position.X;
                VMInfo.LastVentY = e.Vent.Position.Y;
                if(VMInfo.IsMorphing) {
                    //変身自主解除処理
                    VMInfo.IsMorphing = false;
                    Morpher.SetColorAsync(VMInfo.Color);
                    Morpher.SetHatAsync(VMInfo.Hat);
                    Morpher.SetPetAsync(VMInfo.Pet);
                    Morpher.SetSkinAsync(VMInfo.Skin);
                    Morpher.SetNameAsync(VMInfo.name);
                } else {
                    //変身処理
                    List<Impostor.Api.Net.IClientPlayer> MorpherTargets = new List<Impostor.Api.Net.IClientPlayer>();
                    foreach(var player in e.Game.Players) {
                        if(player.Client.Id != CRID.VentMorpherID) {
                            MorpherTargets.Add(player);
                        }
                    }
                    var MorpherTargetNum = rand.Next(MorpherTargets.Count);
                    var MorpherTarget = MorpherTargets[MorpherTargetNum];
                    var MorpherTargetInfo = MorpherTarget.Character.PlayerInfo;
                    VMInfo.IsMorphing = true;
                    Morpher.SetColorAsync(MorpherTargetInfo.Color);
                    Morpher.SetHatAsync(MorpherTargetInfo.Hat);
                    Morpher.SetPetAsync(MorpherTargetInfo.Pet);
                    Morpher.SetSkinAsync(MorpherTargetInfo.Skin);
                    Morpher.SetNameAsync(MorpherTargetInfo.PlayerName);
                }
            }
        }
        [EventListener]
        public void VentExitReturn(IPlayerExitVentEvent e) {
            //VentMorpherがベントを使えない設定
            if(CRE.VentMorpherEnabled && e.ClientPlayer.Client.Id == CRID.VentMorpherID && !CRE.VentMorpherVentable) {
                e.ClientPlayer.Character.NetworkTransform.SnapToAsync(new System.Numerics.Vector2(VMInfo.LastVentX, VMInfo.LastVentY + 0.35f));
            }
        }
        [EventListener]
        public void MeetingStarted(IPlayerStartMeetingEvent e) {
            CheckWinner(e.Game);
            //名前をリセット
            foreach(var player in e.Game.Players) {
                var PlayerID = player.Character.PlayerId;
                player.Character.SetNameAsync(names[PlayerID]);
            }
            SetRoleColor(e.Game);
            SetLederNameColor(e.Game);
            foreach(var player in e.Game.Players) {
                //VentMorpherを元に戻す
                if(player.Client.Id == CRID.VentMorpherID) {
                    var Morpher = player.Character;
                    VMInfo.IsMorphing = false;
                    Morpher.SetColorAsync(VMInfo.Color);
                    Morpher.SetHatAsync(VMInfo.Hat);
                    Morpher.SetPetAsync(VMInfo.Pet);
                    Morpher.SetSkinAsync(VMInfo.Skin);
                    Morpher.SetNameAsync(VMInfo.name);
                }
                //ターゲット再設定
                if(CRE.TargetModeEnabled) {
                    SetTarget(e.Game);
                }
            }
            if(!CRE.FastMeetingStarted) {
                _logger.LogInformation("初手ボタン時の処理");
            }
            CRE.FastMeetingStarted = true;
            //Doctor用処理
            if(CRE.DoctorEnabled) {
                _logger.LogInformation("医者への役職通知処理");
                var chat = 
                    e.Body.PlayerInfo.PlayerName + "は" + RoleNameForDoctor[e.Body.PlayerId] + "でした。";
                foreach(var Doctor in e.Game.Players) {
                    if(Doctor.Client.Id == CRID.DoctorID) {
                        Doctor.Character.SendChatToPlayerAsync(chat);
                    }
                }
            }
        }
        [EventListener]
        public void WhenKilled(IPlayerMurderEvent e) {
            CheckWinner(e.Game);
            if(!CRE.FastMeetingStarted) {
                foreach(var player in e.Game.Players) {
                    if(player.Character.PlayerId == e.Victim.PlayerId && player.Client.Id == CRID.FastTrollID && CRE.FastTrollEnabled) {
                        Task task = Task.Run(() => {
                            Thread.Sleep(100);
                            e.PlayerControl.MurderPlayerAsync(e.PlayerControl);
                            e.PlayerControl.SendChatAsync(e.PlayerControl.PlayerInfo.PlayerName + " killed FastTroll.");
                            CheckWinner(e.Game);
                        });
                    }
                    _logger.LogInformation("##FastTrollInfomation##\r\nVictim Player ID : " + e.Victim.PlayerId + "\r\nPlayer ID : " + player.Character.PlayerId);
                }
            }
            foreach(var player in e.Game.Players) {
                if(player.Character.PlayerId == e.Victim.PlayerId && player.Client.Id == CRID.StopperID && CRE.StopperEnabled) {
                    e.PlayerControl.SendChatToPlayerAsync("You killed Stopper.");
                    var VictimX = e.Victim.NetworkTransform.Position.X;
                    var VictimY = e.Victim.NetworkTransform.Position.Y;
                    Task task = Task.Run(() => {
                        _logger.LogInformation("インポスター足止めの非同期処理の始まり");
                        for(var i = 0; i <= CRID.StopperStopTime * 10; i++) {
                            e.ClientPlayer.Character.NetworkTransform.SnapToAsync(new System.Numerics.Vector2(VictimX,VictimY));
                            Thread.Sleep(100);
                            _logger.LogInformation("Snap!");
                        }
                        _logger.LogInformation("インポスター足止めの非同期処理の終わり");
                    });
                }
                if(player.Character.PlayerId == e.Victim.PlayerId && player.Client.Id == CRID.MagnetTrollID && CRE.MagnetTrollEnabled) {
                    e.PlayerControl.SendChatToPlayerAsync("You killed MagnetTroll.");
                    var VictimX = e.Victim.NetworkTransform.Position.X;
                    var VictimY = e.Victim.NetworkTransform.Position.Y;
                    Task task = Task.Run(() => {
                        _logger.LogInformation("全員TPの非同期処理の始まり");
                        foreach(var player in e.Game.Players) {
                            player.Character.NetworkTransform.SnapToAsync(new System.Numerics.Vector2(VictimX, VictimY));
                        }
                        _logger.LogInformation("全員TPの非同期処理の終わり");
                    });
                }
            }
            if(CRE.HideAndSeekEnabled) {
                float bodyTP = -64;
                e.Victim.NetworkTransform.SnapToAsync(new System.Numerics.Vector2(bodyTP,bodyTP));
                Task task = Task.Run(() => {
                    _logger.LogInformation("プレイヤーを戻す非同期処理の始まり");
                    Thread.Sleep(3000);
                    e.Victim.NetworkTransform.SnapToAsync(new System.Numerics.Vector2(0,0));
                    _logger.LogInformation("プレイヤーを戻す非同期処理の終わり");
                });
                //旧式タスク完了処理
                /* foreach(var player in e.Game.Players) {
                    for(int TaskIDCount = 0; TaskIDCount < 33; TaskIDCount++) {
                        var writer2 = e.Game.StartRpc(e.Victim.NetId, RpcCalls.CompleteTask, player.Character.PlayerId);
                        writer2.Write(TaskIDCount);
                        e.Game.SendToAsync(writer2, e.Victim.PlayerId);
                        e.Game.FinishRpcAsync(writer2);
                    }
                } */
                //新
                foreach(var VictimTask in e.Victim.PlayerInfo.Tasks) {
                    VictimTask.CompleteAsync();
                }
            }
            //TargetMode用の処理
            if(CRE.TargetModeEnabled) {
                Impostor.Api.Net.IClientPlayer Target = null;
                foreach(var tgt in e.Game.Players) {
                    if(tgt.Client.Id == CRID.TargetID) {
                        Target = tgt;
                    }
                }
                if(e.Victim != Target.Character) {
                    var name = e.PlayerControl.PlayerInfo.PlayerName;
                    e.PlayerControl.SetNameAsync("<color=red>" + name + "\r\nI am Impostor");
                }
                SetTarget(e.Game);
            }
            if(e.ClientPlayer.Client.Id == CRID.BlackoutID && CRE.BlackoutEnabled) {
                _logger.LogInformation("Blackout killed.");
                var CrewLight = e.Game.Options.CrewLightMod;
                var ImpostorLight = e.Game.Options.ImpostorLightMod;
                e.Game.Options.CrewLightMod = 0f;
                e.Game.Options.ImpostorLightMod = 0.25f;
                e.Game.SyncSettingsAsync();
                Task task = Task.Run(() => {
                    _logger.LogInformation("TheBlackout用の非同期処理開始");
                    Thread.Sleep(5000);
                    e.Game.Options.CrewLightMod = CrewLight;
                    if(ImpostorLight * 0.8f < CrewLight) {
                        e.Game.Options.ImpostorLightMod = CrewLight;
                    } else {
                        e.Game.Options.ImpostorLightMod = ImpostorLight * 0.8f;
                    }
                    e.Game.SyncSettingsAsync();
                });
            }
            if(e.Victim.PlayerId == e.Game.GetClientPlayer(CRID.LeaderID).Character.PlayerId && CRE.LeaderEnabled) {
                LeaderDead(e.Game, e.Game.GetClientPlayer(CRID.LeaderID), e.ClientPlayer);
                CheckWinner(e.Game);
            }
        }
        [EventListener]
        public void TaskComplete(IPlayerCompletedTaskEvent e) {
            if(e.ClientPlayer.Client.Id == CRID.TerroristID && CRE.TerroristEnabled && !e.PlayerControl.PlayerInfo.IsDead) {
                _logger.LogInformation("テロリストがタスクを行いました。");
                CRID.TerroristTaskCount--;
            } else if(!e.PlayerControl.PlayerInfo.IsImpostor && HavingTaskPlayers.Contains(e.ClientPlayer)) {
                _logger.LogInformation("クルーがタスクを行いました。");
                CRID.CrewmatesTaskCount--;
            }
            CheckWinner(e.Game);
        }
        [EventListener]
        public void PlayerLeftGame(IGamePlayerLeftEvent e){
            if(e.Game.GameState == GameStates.Started) {
                CheckWinner(e.Game);
            }
        }
        [EventListener]
        public void ToSabotageWin(IGameEndedEvent e) {
            e.Game.Options.CrewLightMod = DefaultCrewLight;
            e.Game.Options.ImpostorLightMod = DefaultImpostorLight;
            e.Game.SyncSettingsAsync();
            if(e.GameOverReason == GameOverReason.ImpostorBySabotage){
                _logger.LogInformation("サボタージュによる勝利");
            }
        }
        public void SetRoleColor(Impostor.Api.Games.IGame Game) {
            foreach(var player in Game.Players) {
                if(CRE.TeruteruEnabled)
                    SetPrivateNameColor(Game.GetClientPlayer(CRID.TeruteruID), Game, "orange");
                if(CRE.DoctorEnabled)
                    SetPrivateNameColor(Game.GetClientPlayer(CRID.DoctorID), Game, "green");
                if(CRE.MadmateEnabled)
                    SetPrivateNameColor(Game.GetClientPlayer(CRID.MadmateID), Game, "red");
                if(CRE.TerroristEnabled)
                    SetPrivateNameColor(Game.GetClientPlayer(CRID.TerroristID), Game, "green");
            }
        }
        public void SetPrivateNameColor(Api.Net.IClientPlayer player, Api.Games.IGame Game, string color) {
            if(!player.IsHost) {
                var writer = Game.StartRpc(player.Character.NetId, RpcCalls.SetName, player.Client.Id);
                writer.Write("<color=" + color + ">" + player.Character.PlayerInfo.PlayerName);
                Game.SendToAsync(writer, player.Character.PlayerId);
                Game.FinishRpcAsync(writer);
            }
        }
        public void SetLederNameColor(Api.Games.IGame Game) {
            foreach (var player in Game.Players) {
                var CID = player.Client.Id;
                var Leader = Game.GetClientPlayer(CRID.LeaderID);
                if(!player.Character.PlayerInfo.IsImpostor &&
                   CID != CRID.MadmateID && CID != CRID.TerroristID &&
                   CID != CRID.TeruteruID) {
                    if(!Leader.IsHost && !player.IsHost) {
                        Leader.Character.SetNameDesync("<color=yellow>" + Leader.Character.PlayerInfo.PlayerName, Game, player.Character);
                    }
                }
            }
        }
        public void CheckWinner(Impostor.Api.Games.IGame Game, Boolean HasTeruteruWon = false){
            _logger.LogInformation("勝利確認開始");
            var winner = -1;
            //winnerのID一覧
            //-1:未決定
            //0:クルー
            //1:インポスター
            //2:Teruteru
            //3:Terrorist(テロリスト)
            //#人数取得処理
            var AllPlayersCount = 0;
            var CrewmateCount = 0;
            var ImpostorCount = 0;
            foreach(var player in Game.Players) {
                if(!player.Character.PlayerInfo.IsDead) {
                    if(player.Character.PlayerInfo.IsImpostor) {
                        ImpostorCount++;
                    } else {
                        CrewmateCount++;
                    }
                    AllPlayersCount++;
                }
            }
            //#勝利陣営判定処理
            var AllTasksComplete = true;
            foreach(var player in Game.Players) {
                foreach(var task in player.Character.PlayerInfo.Tasks) {
                    if(!task.Complete) {
                        AllTasksComplete = false;
                    }
                }
            }
            if(AllTasksComplete == true) {
                //全タスクが完了
                winner = 0;
            }
            if(CRID.CrewmatesTaskCount <= 0) {
                //全タスクが完了
                foreach(var player in Game.Players) {
                    foreach(var task in player.Character.PlayerInfo.Tasks) {
                        task.CompleteAsync();
                    }
                }
                winner = 0;
            }
            if(ImpostorCount >= CrewmateCount) {
                //インポスターの人数がクルーの人数と同数またはそれ以上
                winner = 1;
            }
            if(ImpostorCount <= 0) {
                //インポスターが0人以下
                winner = 0;
            }
            if(CrewmateCount <= 0) {
                //クルー全滅
                winner = 1;
            }
            if(HasTeruteruWon == true) {
                //Teruteruが勝利しているか
                winner = 2;
            }
            var Terrorist = Game.GetClientPlayer(CRID.TerroristID);
            if(Terrorist != null && CRE.TerroristEnabled) {
                _logger.LogInformation("テロリストの残りタスク数：" + CRID.TerroristTaskCount);
                _logger.LogInformation("クルーの残りタスク数：" + CRID.CrewmatesTaskCount);
                if(Terrorist.Character.PlayerInfo.IsDead && Terrorist.Character.PlayerInfo.LastDeathReason != DeathReason.Disconnect) {
                    if(CRID.TerroristTaskCount <= 0) {
                        winner = 3;
                    } else {
                        _logger.LogInformation("テロリストの勝利を無効化");
                        CRE.TerroristCanWin = false;
                        foreach(var task in Terrorist.Character.PlayerInfo.Tasks) {
                            task.CompleteAsync();
                        }
                    }
                }
            }
            //各陣営勝利時の処理
            if(winner == 0) {
                //特殊役職敗北処理
                if(CRE.TeruteruEnabled) SetLose(Game,CRID.TeruteruID);
                if(CRE.TerroristEnabled) SetLose(Game,CRID.TerroristID);
                if(CRE.MadmateEnabled) SetLose(Game,CRID.MadmateID);
                foreach(var player in Game.Players) {
                    if(player.Client.Id == CRID.TeruteruID && CRE.TeruteruEnabled) {
                        //player.Character.SetHatAsync(HatType.DumSticker);
                    }
                }
            }
            if(winner == 1) {
                if(CRE.MadmateEnabled) SetWin(Game,CRID.MadmateID);
            }
            if(winner == 2) {
                //特殊役職敗北処理
                if(CRE.TerroristEnabled) SetLose(Game,CRID.TerroristID);
                foreach(var teruteru in Game.Players) {
                    _logger.LogInformation("teruteru特定開始");
                    if(teruteru.Client.Id == CRID.TeruteruID && CRE.TeruteruEnabled) {
                        string TeruteruName = teruteru.Character.PlayerInfo.PlayerName;
                        teruteru.Character.SetNameAsync(TeruteruName + "はてるてるだった<size=0>");
                        _logger.LogInformation(TeruteruName);
                        foreach(var player in Game.Players)
                        {
                            if(player.Character.PlayerInfo.IsImpostor)
                            {
                                player.Character.SetColorAsync(teruteru.Character.PlayerInfo.Color);
                                player.Character.SetHatAsync(teruteru.Character.PlayerInfo.Hat);
                                player.Character.SetPetAsync(teruteru.Character.PlayerInfo.Pet);
                                player.Character.SetSkinAsync(teruteru.Character.PlayerInfo.Skin);
                                player.Character.SetNameAsync(TeruteruName + "\r\nTeruteru wins.");
                            }
                        }
                        Task ExileTask = Task.Run(() => {
                            Thread.Sleep(11000);
                            teruteru.Character.SetNameAsync(TeruteruName + "<size=0>てるてる");
                            Thread.Sleep(1000);
                            foreach(var player in Game.Players) {
                                if(!player.Character.PlayerInfo.IsImpostor && player.Client.Id != CRID.TeruteruID && Game.GameState == GameStates.Started) {
                                    player.Character.ExileAsync();
                                }
                            }
                        });
                    }
                }
            }
            if(winner == 3) {
                //特殊役職敗北処理
                if(CRE.TeruteruEnabled) SetLose(Game,CRID.TeruteruID);
                var terrorist = Game.GetClientPlayer(CRID.TerroristID);
                _logger.LogInformation("テロリスト特定完了");
                string TerroristName = terrorist.Character.PlayerInfo.PlayerName;
                _logger.LogInformation(TerroristName);
                foreach(var player in Game.Players)
                {
                    if(player.Character.PlayerInfo.IsImpostor)
                    {
                        player.Character.SetColorAsync(terrorist.Character.PlayerInfo.Color);
                        player.Character.SetHatAsync(terrorist.Character.PlayerInfo.Hat);
                        player.Character.SetPetAsync(terrorist.Character.PlayerInfo.Pet);
                        player.Character.SetSkinAsync(terrorist.Character.PlayerInfo.Skin);
                        player.Character.SetNameAsync(TerroristName + "\r\nTerrorist wins.");
                    }
                }
                Task ExileTask = Task.Run(() => {
                    if(terrorist.Character.PlayerInfo.LastDeathReason == DeathReason.Exile) {
                        Thread.Sleep(12000);
                    } else {
                        Thread.Sleep(100);
                    }
                    terrorist.Character.SetNameAsync(TerroristName + "<size=0>テロリスト");
                    foreach(var player in Game.Players) {
                        if(!player.Character.PlayerInfo.IsImpostor && player.Client.Id != CRID.TeruteruID && Game.GameState == GameStates.Started) {
                            player.Character.ExileAsync();
                        }
                    }
                });
            }
            /* if(winner != -1) {
                Encoding sjisEnc = Encoding.GetEncoding("Unicode");
                StreamWriter Logger = new StreamWriter("tmp.log", true, sjisEnc);
                Logger.WriteLine("==Game is ended==");
                Logger.WriteLine("Winner ID:" + Winner);
                Logger.Close();
                var dt = DateTime.Now;
                var FileName = "log-" + dt.Year + "-" + dt.Month + "-" + dt.Date + "-" + dt.Hour + "-" + dt.Minute + "-" + dt.Second + ".log";
                //File.Move("tmp.log", FileName);
            } */
        }
        public void SetWin(Api.Games.IGame Game, int WinnerID) {
            var Winner = Game.GetClientPlayer(WinnerID);
            Winner.Character.SetNameAsync(Winner.Character.PlayerInfo.PlayerName + "\r\n<size=0>#WIN#");
        }
        public void SetLose(Api.Games.IGame Game, int LoserID) {
            var Loser = Game.GetClientPlayer(LoserID);
            Loser.Character.SetNameAsync(Loser.Character.PlayerInfo.PlayerName + "\r\n<size=0>#LOSE#");
        }
        public void LeaderDead(Api.Games.IGame Game, Api.Net.IClientPlayer Leader, Api.Net.IClientPlayer Murderer) {
            _logger.LogInformation("被害者：" + Leader.Character.PlayerInfo.PlayerName);
            if(Murderer != null) _logger.LogInformation("加害者：" + Murderer.Character.PlayerInfo.PlayerName);
            _logger.LogInformation("リーダーが死亡しました");
            if(!Leader.Character.PlayerInfo.IsImpostor) {
                _logger.LogInformation("死亡したリーダーは本物でした");
                //名前変更処理
                if(Leader.Character.PlayerInfo.LastDeathReason == DeathReason.Exile) {
                    Leader.Character.SetNameAsync(Leader.Character.PlayerInfo.PlayerName + "は本物のリーダーだった<size=0>");
                }
                List<Api.Net.IClientPlayer> PlayersToKill = new List<Api.Net.IClientPlayer>();
                //全員殺害処理
                foreach(var p in Game.Players) {
                    if(!p.Character.PlayerInfo.IsImpostor) {
                        PlayersToKill.Add(p);
                    }
                }
                if(Murderer != null) {
                    foreach(var p in PlayersToKill) Murderer.Character.MurderPlayerAsync(p.Character);
                }
                foreach(var p in PlayersToKill) p.Character.ExileAsync();
            }
        }
    }
}