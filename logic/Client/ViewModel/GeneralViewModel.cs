﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Client.Model;
using Protobuf;
using Grpc.Core;
using System.Diagnostics.Metrics;
using Client.Util;

namespace Client.ViewModel
{
    public partial class GeneralViewModel : BindableObject
    {
        private IDispatcherTimer timerViewModel;

        int counterViewModelTest = 0;

        private string title;
        public string Title
        {
            get => title;
            set
            {
                title = value;
                OnPropertyChanged();
            }
        }


        public MapPatch testPatch;
        public MapPatch TestPatch
        {
            get
            {
                return testPatch;
            }
            set
            {
                testPatch = value;
                OnPropertyChanged();
            }
        }


        private List<Link> links;
        public List<Link> Links
        {
            get
            {
                return links ?? (links = new List<Link>());
            }
            set
            {
                links = value;
                OnPropertyChanged();
            }
        }

        private long playerID;
        private SweeperType SweeperType;
        private long teamID;
        AvailableService.AvailableServiceClient? client;
        AsyncServerStreamingCall<MessageToClient>? responseStream;
        bool isSpectatorMode = false;
        // 连接Server,comInfo[]的格式：0-ip 1- port 2-playerID 3-teamID 4-SweeperType
        public void ConnectToServer(string[] comInfo)
        {
            if (!isSpectatorMode && comInfo.Length != 5 || isSpectatorMode && comInfo.Length != 4)
            {
                throw new Exception("注册信息有误！");
            }
            playerID = Convert.ToInt64(comInfo[2]);
            teamID = Convert.ToInt64(comInfo[3]);
            string connect = new string(comInfo[0]);
            connect += ':';
            connect += comInfo[1];
            Channel channel = new Channel(connect, ChannelCredentials.Insecure);
            client = new AvailableService.AvailableServiceClient(channel);
            PlayerMsg playerMsg = new PlayerMsg();
            playerMsg.PlayerId = playerID;
            playerMsg.TeamId = teamID;
            //playerMsg.X = 0;
            //playerMsg.Y = 0;
            if (!isSpectatorMode)
            {
                SweeperType = Convert.ToInt64(comInfo[4]) switch
                {
                    0 => SweeperType.NullSweeperType,
                    1 => SweeperType.CivilianSweeper,
                    2 => SweeperType.MilitarySweeper,
                    3 => SweeperType.FlagSweeper,
                    _ => SweeperType.NullSweeperType
                };
                playerMsg.SweeperType = SweeperType;
            }
            responseStream = client.AddPlayer(playerMsg);
            isClientStocked = false;
        }

        int testcounter = 0;

        private async void OnReceive()
        {
            try
            {
                while (responseStream != null && await responseStream.ResponseStream.MoveNext())
                {
                    lock (drawPicLock)
                    {
                        ballX += 20;
                        ballY += 20;

                        listOfAll.Clear();
                        listOfSweeper.Clear();
                        listOfBullet.Clear();
                        listOfBombedBullet.Clear();
                        listOfRecycleBank.Clear();
                        listOfChargeStation.Clear();
                        listOfSignalTower.Clear();
                        listOfGarbage.Clear();
                        listOfHome.Clear();
                        listOfBridge.Clear();
                        MessageToClient content = responseStream.ResponseStream.Current;
                        MessageOfMap mapMassage = new();
                        bool mapMessageExist = false;
                        switch (content.GameState)
                        {
                            case GameState.GameStart:
                                foreach (var obj in content.ObjMessage)
                                {
                                    switch (obj.MessageOfObjCase)
                                    {
                                        case MessageOfObj.MessageOfObjOneofCase.SweeperMessage:
                                            listOfSweeper.Add(obj.SweeperMessage);
                                            break;

                                        case MessageOfObj.MessageOfObjOneofCase.BulletMessage:
                                            listOfBullet.Add(obj.BulletMessage);
                                            break;

                                        case MessageOfObj.MessageOfObjOneofCase.BombedBulletMessage:
                                            listOfBombedBullet.Add(obj.BombedBulletMessage);
                                            break;

                                        case MessageOfObj.MessageOfObjOneofCase.RecyclebankMessage:
                                            listOfRecycleBank.Add(obj.RecyclebankMessage);
                                            break;

                                        case MessageOfObj.MessageOfObjOneofCase.ChargestationMessage:
                                            listOfChargeStation.Add(obj.ChargestationMessage);
                                            break;

                                        case MessageOfObj.MessageOfObjOneofCase.SignaltowerMessage:
                                            listOfSignalTower.Add(obj.SignaltowerMessage);
                                            break;

                                        case MessageOfObj.MessageOfObjOneofCase.GarbageMessage:
                                            listOfGarbage.Add(obj.GarbageMessage);
                                            break;

                                        case MessageOfObj.MessageOfObjOneofCase.HomeMessage:
                                            listOfHome.Add(obj.HomeMessage);
                                            break;

                                        case MessageOfObj.MessageOfObjOneofCase.MapMessage:
                                            mapMassage = obj.MapMessage;
                                            break;
                                    }
                                }
                                listOfAll.Add(content.AllMessage);
                                countMap.Clear();
                                countMap.Add((int)MapPatchType.Garbage, listOfGarbage.Count);
                                countMap.Add((int)MapPatchType.RecycleBank, listOfRecycleBank.Count);
                                countMap.Add((int)MapPatchType.ChargeStation, listOfChargeStation.Count);
                                countMap.Add((int)MapPatchType.SignalTower, listOfSignalTower.Count);
                                GetMap(mapMassage);
                                break;
                            case GameState.GameRunning:
                                foreach (var obj in content.ObjMessage)
                                {
                                    switch (obj.MessageOfObjCase)
                                    {
                                        case MessageOfObj.MessageOfObjOneofCase.SweeperMessage:
                                            listOfSweeper.Add(obj.SweeperMessage);
                                            break;

                                        case MessageOfObj.MessageOfObjOneofCase.RecyclebankMessage:
                                            listOfRecycleBank.Add(obj.RecyclebankMessage);
                                            break;

                                        case MessageOfObj.MessageOfObjOneofCase.ChargestationMessage:
                                            listOfChargeStation.Add(obj.ChargestationMessage);
                                            break;

                                        case MessageOfObj.MessageOfObjOneofCase.SignaltowerMessage:
                                            listOfSignalTower.Add(obj.SignaltowerMessage);
                                            break;

                                        case MessageOfObj.MessageOfObjOneofCase.BulletMessage:
                                            listOfBullet.Add(obj.BulletMessage);
                                            break;

                                        case MessageOfObj.MessageOfObjOneofCase.BombedBulletMessage:
                                            listOfBombedBullet.Add(obj.BombedBulletMessage);
                                            break;

                                        case MessageOfObj.MessageOfObjOneofCase.GarbageMessage:
                                            listOfGarbage.Add(obj.GarbageMessage);
                                            break;

                                        case MessageOfObj.MessageOfObjOneofCase.HomeMessage:
                                            listOfHome.Add(obj.HomeMessage);
                                            break;

                                        case MessageOfObj.MessageOfObjOneofCase.MapMessage:
                                            mapMassage = obj.MapMessage;
                                            mapMessageExist = true;
                                            break;
                                    }
                                }
                                listOfAll.Add(content.AllMessage);
                                if (mapMessageExist)
                                {
                                    countMap.Clear();
                                    countMap.Add((int)MapPatchType.Garbage, listOfGarbage.Count);
                                    countMap.Add((int)MapPatchType.RecycleBank, listOfRecycleBank.Count);
                                    countMap.Add((int)MapPatchType.ChargeStation, listOfChargeStation.Count);
                                    countMap.Add((int)MapPatchType.SignalTower, listOfSignalTower.Count);
                                    GetMap(mapMassage);
                                    mapMessageExist = false;
                                }
                                break;

                            case GameState.GameEnd:
                                //DisplayAlert("Info", "Game End", "OK");
                                foreach (var obj in content.ObjMessage)
                                {
                                    switch (obj.MessageOfObjCase)
                                    {
                                        case MessageOfObj.MessageOfObjOneofCase.SweeperMessage:
                                            listOfSweeper.Add(obj.SweeperMessage);
                                            break;

                                        //case MessageOfObj.MessageOfObjOneofCase.BuildingMessage:
                                        //    listOfBuilding.Add(obj.BuildingMessage);
                                        //    break;

                                        case MessageOfObj.MessageOfObjOneofCase.RecyclebankMessage:
                                            listOfRecycleBank.Add(obj.RecyclebankMessage);
                                            break;

                                        case MessageOfObj.MessageOfObjOneofCase.ChargestationMessage:
                                            listOfChargeStation.Add(obj.ChargestationMessage);
                                            break;

                                        case MessageOfObj.MessageOfObjOneofCase.SignaltowerMessage:
                                            listOfSignalTower.Add(obj.SignaltowerMessage);
                                            break;

                                        case MessageOfObj.MessageOfObjOneofCase.BulletMessage:
                                            listOfBullet.Add(obj.BulletMessage);
                                            break;

                                        case MessageOfObj.MessageOfObjOneofCase.BombedBulletMessage:
                                            listOfBombedBullet.Add(obj.BombedBulletMessage);
                                            break;

                                        case MessageOfObj.MessageOfObjOneofCase.GarbageMessage:
                                            listOfGarbage.Add(obj.GarbageMessage);
                                            break;

                                        case MessageOfObj.MessageOfObjOneofCase.HomeMessage:
                                            listOfHome.Add(obj.HomeMessage);
                                            break;

                                        case MessageOfObj.MessageOfObjOneofCase.MapMessage:
                                            mapMassage = obj.MapMessage;
                                            break;
                                    }
                                }
                                listOfAll.Add(content.AllMessage);
                                break;
                        }
                    }
                    if (responseStream == null)
                    {
                        throw new Exception("Unconnected");
                    }
                }
            }
            catch (Exception ex)
            {
                /* 
                    #TODO
                    Show the error message
                */
            }
        }

        private void Refresh(object sender, EventArgs e)
        {
            try
            {
                lock (drawPicLock)
                {
                    //if (UIinitiated)
                    //{
                    //    redPlayer.SlideLengthSet();
                    //    bluePlayer.SlideLengthSet();
                    //    gameStatusBar.SlideLengthSet();
                    //}
                    if (!isClientStocked)
                    {
                        if (!hasDrawn && getMapFlag)
                        {
                            //PureDrawMap(defaultMap);
                            hasDrawn = true;
                        }

                        foreach (var data in listOfAll)
                        {
                            RedPlayer.Money = data.RedTeamEnergy;
                            RedPlayer.Hp = data.RedHomeHp;
                            RedPlayer.Score = data.RedTeamScore;
                            BluePlayer.Money = data.BlueTeamEnergy;
                            BluePlayer.Hp = data.BlueHomeHp;
                            BluePlayer.Score = data.BlueTeamScore;
                        }

                        foreach (var data in listOfHome)
                        {
                            DrawHome(data);
                            if (data.TeamId == (long)PlayerTeam.Red)
                            {
                                RedPlayer.Team = data.TeamId;
                            }
                            else if (data.TeamId == (long)PlayerTeam.Blue)
                            {
                                BluePlayer.Team = data.TeamId;
                            }
                        }

                        foreach (var data in listOfSweeper)
                        {
                            if (data.TeamId == (long)PlayerTeam.Red)
                            {
                                Sweeper ship = new Sweeper
                                {
                                    Type = data.SweeperType,
                                    State = data.SweeperState,
                                    ArmorModule = data.ArmorType,
                                    ShieldModule = data.ShieldType,
                                    WeaponModule = data.WeaponType,
                                    ProducerModule = data.ProducerType,
                                    ConstuctorModule = data.ConstructorType,
                                    Type_s = UtilInfo.SweeperTypeNameDict[data.SweeperType],
                                    State_s = UtilInfo.SweeperStateNameDict[data.SweeperState],
                                    ArmorModule_s = UtilInfo.SweeperArmorTypeNameDict[data.ArmorType],
                                    ShieldModule_s = UtilInfo.SweeperShieldTypeNameDict[data.ShieldType],
                                    WeaponModule_s = UtilInfo.SweeperWeaponTypeNameDict[data.WeaponType],
                                    ConstuctorModule_s = UtilInfo.SweeperConstructorNameDict[data.ConstructorType],
                                    ProducerModule_s = UtilInfo.SweeperProducerTypeNameDict[data.ProducerType]
                                };
                                RedPlayer.Sweepers.Add(ship);
                            }
                            else if (data.TeamId == (long)PlayerTeam.Blue)
                            {
                                Sweeper ship = new Sweeper
                                {
                                    Type = data.SweeperType,
                                    State = data.SweeperState,
                                    ArmorModule = data.ArmorType,
                                    ShieldModule = data.ShieldType,
                                    WeaponModule = data.WeaponType,
                                    ProducerModule = data.ProducerType,
                                    ConstuctorModule = data.ConstructorType,
                                    Type_s = UtilInfo.SweeperTypeNameDict[data.SweeperType],
                                    State_s = UtilInfo.SweeperStateNameDict[data.SweeperState],
                                    ArmorModule_s = UtilInfo.SweeperArmorTypeNameDict[data.ArmorType],
                                    ShieldModule_s = UtilInfo.SweeperShieldTypeNameDict[data.ShieldType],
                                    WeaponModule_s = UtilInfo.SweeperWeaponTypeNameDict[data.WeaponType],
                                    ConstuctorModule_s = UtilInfo.SweeperConstructorNameDict[data.ConstructorType],
                                    ProducerModule_s = UtilInfo.SweeperProducerTypeNameDict[data.ProducerType]
                                };
                                BluePlayer.Sweepers.Add(ship);
                            }
                        }

                        foreach (var data in listOfChargeStation)
                        {
                            DrawChargeStation(data);
                        }

                        foreach (var data in listOfRecycleBank)
                        {
                            DrawRecycleBank(data);
                        }

                        foreach (var data in listOfBridge)
                        {
                            DrawWormHole(data);
                        }

                        foreach (var data in listOfSignalTower)
                        {
                            DrawSignalTower(data);
                        }

                        foreach (var data in listOfGarbage)
                        {
                            DrawResource(data);
                        }
                    }
                }
            }
            finally
            {

            }
            //counter++;
        }
        private void UpdateTest(object sender, EventArgs e)
        {
            counterViewModelTest++;
            if (!hasDrawn && getMapFlag)
            {
                PureDrawMap(defaultMap);
                hasDrawn = true;
            }
            if (testcounter < 30)
            {
                testcounter++;
                if (testcounter % 3 == 0)
                {
                    Sweeper ship = new Sweeper
                    {
                        Type_s = "CivilSweeper",
                        State_s = "Idle",
                        ArmorModule_s = "LightArmor"
                    };
                    RedPlayer.Sweepers.Add(ship);
                    BluePlayer.Sweepers.Add(ship);
                }
            }
            DrawHome(new MessageOfHome
            {
                X = 10,
                Y = 10,
                Hp = 100,
                TeamId = 1
            });

            //DrawRecycleBank(new MessageOfRecycleBank
            //{
            //    X = 11,
            //    Y = 11,
            //    Hp = 100,
            //    TeamId = 2
            //});

            DrawWormHole(new MessageOfBridge
            {
                X = 12,
                Y = 12,
                Hp = 100
            });

            //ballX += 1;
            //ballY += 1;
        }

        public GeneralViewModel()
        {
            InitiateObjects();
            Title = "THUAI7";
            Links = [
                new Link { Name = "天梯信息", Url = "" },
                new Link { Name = "获取更新", Url = "" },
                new Link { Name = "我的AI", Url = "" },
                new Link { Name = "配置链接", Url = "" }
            ];
            RedPlayer.Hp = 100;
            RedPlayer.Money = 1000;

            Sweeper ship = new Sweeper
            {
                Type_s = "CivilSweeper",
                State_s = "Idle",
                ArmorModule_s = "LightArmor"
            };
            RedPlayer.Sweepers.Add(ship);

            WormHole1HP = 100;
            WormHole2HP = 100;
            WormHole3HP = 100;

            for (int i = 0; i < 50; i++)
            {
                for (int j = 0; j < 50; j++)
                {
                    MapPatchesList.Add(new MapPatch
                    {
                        PatchColor = Colors.AliceBlue,
                    });
                }
            }
            PureDrawMap(GameMap.GameMapArray);

            ConnectToServer(new string[]{
                "127.0.0.1",
                "8888",
                "1",
                "1",
                "1"
            });

            timerViewModel = Dispatcher.CreateTimer();
            timerViewModel.Interval = TimeSpan.FromMilliseconds(5);
            timerViewModel.Tick += new EventHandler(UpdateTest);
            timerViewModel.Start();

            OnReceive();
        }
    }
}
