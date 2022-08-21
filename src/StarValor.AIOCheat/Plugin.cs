using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ElementalCyclone.UnityMod.StarValor.AIOCheat
{
    /// <summary>
    /// Entry and main class for Star Valor AIO Cheat.
    /// </summary>
    [BepInPlugin(Plugin.Info_GUID, Plugin.Info_Name, Plugin.Info_Version)]
    [BepInDependency("com.bepis.bepinex.configurationmanager", BepInDependency.DependencyFlags.HardDependency)]
    [BepInProcess("Star Valor.exe")]
    public class Plugin : BaseUnityPlugin
    {
        /// <summary>
        /// Constant for Plugin's GUID
        /// </summary>
        public const string Info_GUID = "ElementalCyclone.UnityMod.StarValor.AIOCheat";
        /// <summary>
        /// Contant for Plugin's human readable name/short name
        /// </summary>
        public const string Info_Name = "Star Valor AIO Cheat";
        /// <summary>
        /// Constant for current Plugin's version
        /// </summary>
        public const string Info_Version = "0.8.0";

        /// <summary>
        /// reference to <see cref="Harmony"/> instance
        /// </summary>
        protected static Harmony instance_Harmony = null;

        /// <summary>
        /// Approximate in-game UI character length for a 'Tab' character
        /// </summary>
        protected const int tabCharCount = 8;
        /// <summary>
        /// Dictionary of cheats that is enabled and need to be 'enforced' in every frame.
        /// </summary>
        /// <remarks>
        /// This dictionary key is the cheat name (<see cref="ConfigDefinition.Key"/>), where the value is the enforcing '<see cref="Action"/>' that will be invoked every frame.
        /// </remarks>
        protected ConcurrentDictionary<string, Action> enforcedCheats = new ConcurrentDictionary<string, Action>();
        /// <summary>
        /// Name of the gameplay scene.
        /// </summary>
        protected string targetedScene_Name = "Scene_1";
        /// <summary>
        /// A flag indicating whether the cheat menu is initialized.
        /// </summary>
        protected bool isListInitialized = false;
        /// <summary>
        /// A flag indicating whether the internal dictionaries is initialized.
        /// </summary>
        protected bool isDictInitialized = false;
        /// <summary>
        /// Shortcut variable to integer of 1 million
        /// </summary>
        protected readonly int aMillion = (int)Math.Pow(10, 6);
        /// <summary>
        /// Shortcut variable to integer of 1 billion
        /// </summary>
        protected readonly int aBillion = (int)Math.Pow(10, 9);
        /// <summary>
        /// Label and ConfigurationManager's section name for Ship cheats
        /// </summary>
        protected string sectionName_First = "Ship (Full reset on disable/reset + game reload/ship change)";
        /// <summary>
        /// Label and ConfigurationManager's section name for spawning items
        /// </summary>
        protected string sectionName_Scond = "Spawn Floating Item";
        /// <summary>
        /// Internal dictionary of weapons name and its ID.
        /// </summary>
        protected Dictionary<string, int> gameDict_WepnID = new Dictionary<string, int>()
        {
            {"", -1}
        };
        /// <summary>
        /// Internal dictionary of ships name and its ID.
        /// </summary>
        protected Dictionary<string, int> gameDict_ShipID = new Dictionary<string, int>()
        {
            {"", -1}
        };
        /// <summary>
        /// Internal dictionary of items/materials/ores name and its ID.
        /// </summary>
        protected Dictionary<string, int> gameDict_ItemID = new Dictionary<string, int>()
        {
            {"", -1}
        };
        /// <summary>
        /// Internal dictionary of ship's equipables name and its ID.
        /// </summary>
        protected Dictionary<string, int> gameDict_EqupID = new Dictionary<string, int>()
        {
            {"", -1}
        };
        /// <summary>
        /// Internal list for item spawning options
        /// </summary>
        protected Dictionary<string, int> gameDict_SpawnType = new Dictionary<string, int>
        {
            { " ", -1},
            { "Ship (w/ rarity & scale)\t\t", 4},
            { "Equipment\t\t\t\t", 2},
            { "Weapons\t\t\t\t", 1},
            { "Item/Material/Ore\t\t\t", 3},
            { "Item, Blueprint (Weapon)\t\t", 3},
            { "Item, Blueprint (Equipment)\t\t", 3}
        };
        protected Dictionary<string, int> formattingDictinry = new Dictionary<string, int>()
        {
            {"(Booster) Inertial Nullifier", 1},
            {"(Utility) Heatsink", 1},
            {"(Utility) Spotlight", 1}
        };
        /// <summary>
        /// If <see langword="true"/> will reset <see cref="CurrRefComp_ConfigurationManager"/>.DisplayingWindow on the next frame
        /// </summary>
        protected bool isConfManQueued = false;
        protected ConfigDefinition spawnDefn_ObjectType = null;
        protected ConfigDefinition spawnDefn_RarityLvel = null;
        protected ConfigDefinition spawnDefn_StackCount = null;
        protected ConfigDefinition spawnDefn_ObjectName = null;
        protected ConfigDescription spawnDesc_ObjectType = null;
        protected ConfigDescription spawnDesc_RarityLvel = null;
        protected ConfigDescription spawnDesc_StackCount = null;

        /// <summary>
        /// Current active reference to "Player" <see cref="GameObject"/>
        /// </summary>
        protected virtual GameObject CurrRef_Player
        {
            get;
            set;
        }
        /// <summary>
        /// Current active reference to Player's <see cref="SpaceShip"/>
        /// </summary>
        protected virtual SpaceShip CurrRef_PlayerShip
        {
            get;
            set;
        }
        /// <summary>
        /// Current active reference to Player's <see cref="CargoSystem"/>
        /// </summary>
        protected virtual CargoSystem CurrRef_PlayerCargo
        {
            get;
            set;
        }
        /// <summary>
        /// Reference to target <see cref="Scene"/> if active.
        /// </summary>
        /// <remarks>
        /// Returns <see langword="null"/> if current active scene is not targeted scene.
        /// </remarks>
        protected virtual Scene? CurrRef_TargetedScene
        {
            get;
            set;
        }
        /// <summary>
        /// BepInEx's <see cref="ConfigurationManager.ConfigurationManager"/> instance reference
        /// </summary>
        protected virtual ConfigurationManager.ConfigurationManager CurrRefComp_ConfigurationManager
        {
            get;
            set;
        }
        /// <summary>
        /// <see cref="Delegate"/> for accessing <see cref="LootSystem"/> field from an <see cref="CargoSystem"/> instance
        /// </summary>
        /// <remarks>
        /// A required component for spawning cheat
        /// </remarks>
        protected virtual AccessTools.FieldRef<CargoSystem, LootSystem> Accessor_CargoLootSystem
        {
            get;
            set;
        }

        public Plugin()
        {
            spawnDefn_ObjectType = new ConfigDefinition(sectionName_Scond, "Spawn Type");
            spawnDefn_RarityLvel = new ConfigDefinition(sectionName_Scond, "Rarity Level");
            spawnDefn_StackCount = new ConfigDefinition(sectionName_Scond, "Stack Count");
            spawnDefn_ObjectName = new ConfigDefinition(sectionName_Scond, "Object Name");

            spawnDesc_ObjectType = new ConfigDescription("Select spawn type", new AcceptableValueList<string>(gameDict_SpawnType.Keys.ToArray()), new Libs.ConfigurationManagerAttributes() { Order = 100 - 1 });
            spawnDesc_StackCount = new ConfigDescription("Select stack amount. 0 - 500", new AcceptableValueRange<int>(0, 500), new Libs.ConfigurationManagerAttributes() { Order = 100 - 3 });
            spawnDesc_RarityLvel = new ConfigDescription("Select rarity level, 1 - 5", new AcceptableValueList<string>("", "1", "2", "3", "4", "5"), new Libs.ConfigurationManagerAttributes() { Order = 100 - 4 });
        }

        /// <summary>
        /// Wrapper for populating internal dictionaries. Will only function if <see cref="CurrRef_TargetedScene"/> is not <see langword="null"/>.
        /// </summary>
        protected virtual void InitializeDictionaries()
        {
            void Initializer(ref Dictionary<string, int> targetDict, List<KeyValuePair<string, int>> srcList, int descEd)
            {
                var mostLgth = 0;
                var mostDivs = 0;
                targetDict = new Dictionary<string, int>()
                {
                    {"", -1}
                };
                if (descEd > 0)
                {
                    foreach (var item in srcList)
                    {
                        var currDiv = Math.DivRem(item.Key.Length, tabCharCount, out var _);
                        if (mostLgth < item.Key.Length)
                        {
                            mostLgth = item.Key.Length;
                        }
                        if (mostDivs < currDiv)
                        {
                            mostDivs = currDiv;
                        }
                    }
                }
                for (var idx = 0; idx < srcList.Count(); idx++)
                {
                    var currKy = srcList[idx];
                    var addKey = currKy.Key;
                    
                    if (descEd > 0)
                    {
                        var adderN = mostLgth - addKey.Length;
                        var difDiv = Math.DivRem(adderN, tabCharCount, out var difRem) + ((difRem > 0) ? 1 : 0);
                        var addTab = (difDiv > descEd) ? (difDiv - descEd) : 0;
                        addKey += new string(' ', adderN) + new string('\t', addTab + 1);
                        if (formattingDictinry.TryGetValue(currKy.Key, out var tAdd))
                        {
                            if (tAdd >= 0)
                            {
                                addKey += new string('\t', tAdd);
                            }
                            else
                            {
                                // TODO : Support -tAdd
                            }
                        }
                    }

                    targetDict.Add(addKey, currKy.Value);
                }
            }

            if (!isDictInitialized && (CurrRef_TargetedScene != null))
            {
                try
                {
                    var eqpList = new List<Equipment>();
                    for (int idx = 0; idx < EquipmentDB.count; idx++)
                    {
                        eqpList.Add(EquipmentDB.GetEquipment(idx));
                    }
                    var eqps = eqpList.
                        Where(x => !string.IsNullOrWhiteSpace(x.equipName)).
                        GroupBy(x => x.equipName).
                        Select(x => x.First()).
                        OrderBy(x => x.type.ToString()).
                        ThenBy(x => x.equipName.Contains("Warp") && (x.type == EquipmentType.Engine)).
                        ThenBy(x => x.equipName).
                        Select(x => new KeyValuePair<string, int>($"({x.type}) {x.equipName}", x.id)).
                        ToList();
                    Initializer(ref gameDict_EqupID, eqps, 3); // cntDivver = 3

                    var itmList = new List<Item>();
                    for (int idx = 0; idx < ItemDB.count; idx++)
                    {
                        itmList.Add(ItemDB.GetItem(idx));
                    }
                    var itms = itmList.
                        Where(x => !string.IsNullOrWhiteSpace(x.itemName) &&
                                   !string.IsNullOrWhiteSpace(x.refName) &&
                                   (x.itemName.IndexOf("Blueprint", StringComparison.InvariantCultureIgnoreCase) < 0)).
                        GroupBy(x => x.itemName).
                        Select(x => x.First()).
                        OrderBy(x => x.type.ToString()).
                        ThenBy(x => x.itemName).
                        Select(x => new KeyValuePair<string, int>($"({x.type}) {x.itemName}", x.id)).
                        ToList();
                    Initializer(ref gameDict_ItemID, itms, 2);

                    var shps = ShipDB.GetEntireList()?.
                        Where(x => !string.IsNullOrWhiteSpace(x.shipModelName)).
                        GroupBy(x => x.modelName).
                        Select(x => x.First()).
                        OrderBy(x => x.manufacturer.ToString()).
                        ThenBy(x => x.sizeScale).
                        ThenByDescending(x => x.rarity).
                        ThenBy(x => x.modelName).
                        Select(x => new KeyValuePair<string, int>($"({x.manufacturer}) {x.modelName} ({x.rarity}, {x.sizeScale})", x.id)).
                        ToList();
                    Initializer(ref gameDict_ShipID, shps, 2);

                    var wpns = GameManager.predefinitions?.weapons?.
                        Where(x => !string.IsNullOrWhiteSpace(x.name)).
                        GroupBy(x => x.name).
                        Select(x => x.First()).
                        OrderBy(x => x.type.ToString()).
                        ThenBy(x => x.name).
                        Select(x => new KeyValuePair<string, int>($"({x.type.ToString().Replace("Object", "")}) {x.name}", x.index)).
                        ToList();
                    Initializer(ref gameDict_WepnID, wpns, 2);

                    isDictInitialized = true;
                }
                catch (Exception ex)
                {
                    isDictInitialized = false;
                    Logger.LogError($"{nameof(InitializeDictionaries)}() | {ex.GetType().Name}, {ex.Message}\nSource : {ex.Source}\nStacktrace :\n{ex.StackTrace}");
                } 
            }
        }

        /// <summary>
        /// Wrapper for populating cheat list. Will only function if <see cref="CurrRef_TargetedScene"/> is not <see langword="null"/> and internal dictionatries populated.
        /// </summary>
        protected virtual void InitializeCheatList()
        {
            if (!isListInitialized && isDictInitialized && (CurrRef_TargetedScene != null))
            {
                enforcedCheats.Clear();
                Config.SettingChanged -= OnConfigSettingsChanged;
                Config.Clear();

                Config.Bind(sectionName_First, "Unlimited Armor", false, new ConfigDescription("Set then freeze Armor points to 1 million.", null, new Libs.ConfigurationManagerAttributes() { Order = 100 - 0, HideDefaultButton = true }));
                Config.Bind(sectionName_First, "Unlimited Energy", false, new ConfigDescription("Set then freeze Energy points to 1 million.", null, new Libs.ConfigurationManagerAttributes() { Order = 100 - 1, HideDefaultButton = true }));
                Config.Bind(sectionName_First, "Unlimited Shield", false, new ConfigDescription("Set then freeze Shield points to 1 million. Only works if you have shield.", null, new Libs.ConfigurationManagerAttributes() { Order = 100 - 2, HideDefaultButton = true }));
                Config.Bind(sectionName_First, "Unlimited Credit", false, new ConfigDescription("Set then freeze Credits to 1 billion.", null, new Libs.ConfigurationManagerAttributes() { Order = 100 - 3, HideDefaultButton = true }));
                Config.Bind(sectionName_First, "Unlimited Skill Points & Reset", false, new ConfigDescription("Set then freeze skill points and resets to 99.", null, new Libs.ConfigurationManagerAttributes() { Order = 100 - 4, HideDefaultButton = true }));
                Config.Bind(sectionName_First, "Super Weapon", false, new ConfigDescription("Set all weapon to 0 heat, 500 range, 1K proj. speed & 10K base damage.", null, new Libs.ConfigurationManagerAttributes() { Order = 100 - 5, HideDefaultButton = true }));
                Config.Bind(sectionName_First, "Super Warp", false, new ConfigDescription("0s cooldown, most minumum, 5K warp distance and towage cap.", null, new Libs.ConfigurationManagerAttributes() { Order = 100 - 6, HideDefaultButton = true }));
                Config.Bind(sectionName_First, "Super Cargo", false, new ConfigDescription("Set cargo space and passenger space to 10K", null, new Libs.ConfigurationManagerAttributes() { Order = 100 - 6, HideDefaultButton = true }));
                Config.Bind(sectionName_First, "Set : Max Speed", CurrRef_PlayerShip?.stats?.maxSpeed ?? 0, new ConfigDescription("Set ship's maximum speed. 0 - 5000.", new AcceptableValueRange<float>(0, 5000), new Libs.ConfigurationManagerAttributes() { Order = 100 - 7 }));
                Config.Bind(sectionName_First, "Set : Acceleration", CurrRef_PlayerShip?.stats?.acceleration ?? 0, new ConfigDescription("Set ship's acceleration. 0 - 5000.", new AcceptableValueRange<float>(0, 5000), new Libs.ConfigurationManagerAttributes() { Order = 100 - 8 }));
                Config.Bind(sectionName_First, "Set : Turn Speed", CurrRef_PlayerShip?.stats?.turnSpeed ?? 0, new ConfigDescription("Set ship's turning speed. 0 - 500.", new AcceptableValueRange<float>(0, 500), new Libs.ConfigurationManagerAttributes() { Order = 100 - 9 }));
                Config.Bind(sectionName_First, "Set : Strafe Speed", CurrRef_PlayerShip?.stats?.strafeSpeed ?? 0, new ConfigDescription("Set ship's strafe speed. 0 - 5000.", new AcceptableValueRange<float>(0, 1000000), new Libs.ConfigurationManagerAttributes() { Order = 100 - 10 }));
                Config.Bind(sectionName_First, "Set : Energy Gen.", CurrRef_PlayerShip?.stats?.energyGenerated ?? 0, new ConfigDescription("Set ship's energy generation. 0 - 1000000.", new AcceptableValueRange<float>(0, 1000000), new Libs.ConfigurationManagerAttributes() { Order = 100 - 11 }));
                Config.Bind(sectionName_First, "Set : Weapon Space", CurrRef_PlayerShip?.shipData?.weaponSpace ?? 0, new ConfigDescription($"Set ship's total weapon space. {CurrRef_PlayerShip?.shipData?.weaponSpace ?? 0} - 1000.", new AcceptableValueRange<float>(0, 1000), new Libs.ConfigurationManagerAttributes() { Order = 100 - 12 }));
                Config.Bind(sectionName_First, "Set : Equip Space", CurrRef_PlayerShip?.shipData?.equipmentSpace ?? 0, new ConfigDescription($"Set ship's total equipment space. {CurrRef_PlayerShip?.shipData?.weaponSpace ?? 0} - 1000.", new AcceptableValueRange<int>(0, 1000), new Libs.ConfigurationManagerAttributes() { Order = 100 - 13 }));

                Config.Bind(spawnDefn_ObjectType, "", spawnDesc_ObjectType);

                Config.SettingChanged += OnConfigSettingsChanged;
                isListInitialized = true;
            }
        }

        /// <summary>
        /// Event handler for whenever Plugin's <see cref="ConfigFile.SettingChanged"/> event raised = whenever a cheat's value is changed.
        /// </summary>
        /// <param name="sender">
        /// object that sends the event
        /// </param>
        /// <param name="e">
        /// event's parameters, including new value and config name.
        /// </param>
        protected virtual void OnConfigSettingsChanged(object sender, SettingChangedEventArgs e)
        {
            if ((CurrRef_TargetedScene != null) && (sender != null))
            {
                var isReset = e.ChangedSetting.BoxedValue == e.ChangedSetting.DefaultValue;

                if (e?.ChangedSetting?.Definition?.Section == sectionName_First)
                {
                    Action addedAct = null;

                    if (isReset)
                    {
                        if (!enforcedCheats.TryRemove(e.ChangedSetting.Definition.Key, out var _))
                        {
                            Logger.LogWarning($"{nameof(OnConfigSettingsChanged)}() | Cannot remove an item from \"{nameof(enforcedCheats)}\", this might results in unexpected behaviour.");
                        }
                    }

                    switch (e.ChangedSetting.Definition.Key)
                    {
                        case "Unlimited Armor":
                        {
                            if (!isReset)
                            {
                                addedAct = new Action(() =>
                                {
                                    CurrRef_PlayerShip.stats.baseHP = aMillion;
                                    CurrRef_PlayerShip.baseHP = aMillion;
                                    CurrRef_PlayerShip.currHP = aMillion;
                                });
                            }
                            else
                            {
                                // TODO : Make reset state
                            }
                            break;
                        }
                        case "Unlimited Energy":
                        {
                            if (!isReset)
                            {
                                addedAct = new Action(() =>
                                {
                                    CurrRef_PlayerShip.stats.baseEnergy = aMillion;
                                    CurrRef_PlayerShip.stats.currEnergy = aMillion;
                                });
                            }
                            else
                            {
                                // TODO : Make reset state
                            }
                            break;
                        }
                        case "Unlimited Shield":
                        {
                            if (!isReset)
                            {
                                addedAct = new Action(() =>
                                {
                                    if (CurrRef_PlayerShip.shipData.equipments.Any(x => EquipmentDB.GetEquipment(x.equipmentID).type == EquipmentType.Shield))
                                    {
                                        CurrRef_PlayerShip.stats.baseShield = aMillion;
                                        CurrRef_PlayerShip.stats.currShield = aMillion;
                                    }
                                });
                            }
                            else
                            {
                                // TODO : Make reset state
                            }
                            break;
                        }
                        case "Unlimited Credit":
                        {
                            if (!isReset)
                            {
                                addedAct = new Action(() =>
                                {
                                    CurrRef_PlayerCargo.credits = aBillion;
                                });
                            }
                            // Ponders : Should return to previous value on disable ?
                            break;
                        }
                        case "Unlimited Skill Points & Reset":
                        {
                            if (!isReset)
                            {
                                // TODO : Make reset state
                                addedAct = new Action(() =>
                                {
                                    PChar.Char.skillPoints = 99;
                                    PChar.Char.resetSkillsPoints = 99;
                                });
                            }
                            // Ponders : Should return to previous value on disable ?
                            break;
                        }
                        case "Super Weapon":
                        {
                            if (!isReset)
                            {
                                addedAct = new Action(() =>
                                {
                                    Parallel.For(0, CurrRef_PlayerShip.weapons.Count, (idx) =>
                                    {
                                        CurrRef_PlayerShip.weapons[idx].damage = 10000;
                                        CurrRef_PlayerShip.weapons[idx].range = 500;
                                        //CurrRef_PlayerShip.weapons[idx].wRef.range = 500;
                                        CurrRef_PlayerShip.weapons[idx].heatGen = 0;
                                        CurrRef_PlayerShip.weapons[idx].wRef.canHitProjectiles = true;
                                        if (CurrRef_PlayerShip.weapons[idx].chargeTime > 0)
                                        {
                                            CurrRef_PlayerShip.weapons[idx].wRef.chargeTime = 0;
                                            //CurrRef_PlayerShip.weapons[idx].chargeTime = 0;
                                        }
                                        if (CurrRef_PlayerShip.weapons[idx].wRef.turnSpeed > 0)
                                        {
                                            CurrRef_PlayerShip.weapons[idx].wRef.turnSpeed = 120;
                                        }
                                        else if ((CurrRef_PlayerShip.weapons[idx].wRef.compType != WeaponCompType.BeamWeaponObject) && (CurrRef_PlayerShip.weapons[idx].wRef.compType != WeaponCompType.MineObject))
                                        {
                                            CurrRef_PlayerShip.weapons[idx].projSpeed = CurrRef_PlayerShip.weapons[idx].wRef.speed * 25;
                                            //CurrRef_PlayerShip.weapons[idx].wRef.speed = 125;
                                        }
                                    });
                                });
                            }
                            else
                            {
                                // TODO : Make reset state
                            }
                            break;
                        }
                        case "Super Warp":
                        {
                            if (!isReset)
                            {
                                addedAct = new Action(() =>
                                {
                                    CurrRef_PlayerShip.warpCooldown = 0;
                                    CurrRef_PlayerShip.stats.warpCooldownTime = 0;
                                    CurrRef_PlayerShip.towWarpCooldown = 0;
                                    CurrRef_PlayerShip.stats.warpDistance = 5000;
                                    CurrRef_PlayerShip.stats.warpCostPerSector = 0;
                                    CurrRef_PlayerShip.stats.warpTowageCapacity = aMillion;
                                });
                            }
                            else
                            {
                                // TODO : Make reset state
                            }
                            break;
                        }
                        case "Super Cargo":
                        {
                            if (!isReset)
                            {
                                addedAct = new Action(() =>
                                {
                                    CurrRef_PlayerCargo.cargoSpace = 10000;
                                    CurrRef_PlayerCargo.passengerSpace = 10000;
                                });
                            }
                            else
                            {
                                // TODO : Make reset state
                            }
                            break;
                        }
                        case "Set : Max Speed":
                        {
                            if (!isReset)
                            {
                                addedAct = new Action(() =>
                                {
                                    CurrRef_PlayerShip.stats.maxSpeed = (float)e.ChangedSetting.BoxedValue;
                                });
                            }
                            else
                            {
                                CurrRef_PlayerShip.stats.maxSpeed = (float)e.ChangedSetting.DefaultValue;
                            }

                            break;
                        }
                        case "Set : Acceleration":
                        {
                            if (!isReset)
                            {
                                addedAct = new Action(() =>
                                {
                                    CurrRef_PlayerShip.stats.acceleration = (float)e.ChangedSetting.BoxedValue;
                                });
                            }
                            else
                            {
                                CurrRef_PlayerShip.stats.acceleration = (float)e.ChangedSetting.DefaultValue;
                            }
                            break;
                        }
                        case "Set : Turn Speed":
                        {
                            if (!isReset)
                            {
                                addedAct = new Action(() =>
                                {
                                    CurrRef_PlayerShip.stats.turnSpeed = (float)e.ChangedSetting.BoxedValue;
                                    CurrRef_PlayerShip.baseTurnSpeed = (float)e.ChangedSetting.BoxedValue;
                                });
                            }
                            else
                            {
                                CurrRef_PlayerShip.stats.turnSpeed = (float)e.ChangedSetting.DefaultValue;
                                CurrRef_PlayerShip.baseTurnSpeed = (float)e.ChangedSetting.DefaultValue;
                            }
                            break;
                        }
                        case "Set : Strafe Speed":
                        {
                            if (!isReset)
                            {
                                addedAct = new Action(() =>
                                {
                                    CurrRef_PlayerShip.stats.strafeSpeed = (float)e.ChangedSetting.BoxedValue;
                                });
                            }
                            else
                            {
                                CurrRef_PlayerShip.stats.strafeSpeed = (float)e.ChangedSetting.DefaultValue;
                            }
                            break;
                        }
                        case "Set : Energy Gen.":
                        {
                            if (!isReset)
                            {
                                addedAct = new Action(() =>
                                {
                                    CurrRef_PlayerShip.stats.energyGenerated = (float)e.ChangedSetting.BoxedValue;
                                });
                            }
                            else
                            {
                                CurrRef_PlayerShip.stats.energyGenerated = (float)e.ChangedSetting.DefaultValue;
                            }
                            break;
                        }
                        case "Set : Weapon Space":
                        {
                            if (!isReset)
                            {
                                addedAct = new Action(() =>
                                {
                                    CurrRef_PlayerShip.shipData.weaponSpace = (float)e.ChangedSetting.BoxedValue;
                                });
                            }
                            else
                            {
                                CurrRef_PlayerShip.shipData.weaponSpace = (float)e.ChangedSetting.DefaultValue;
                            }
                            break;
                        }
                        case "Set : Equip Space":
                        {
                            if (!isReset)
                            {
                                addedAct = new Action(() =>
                                {
                                    CurrRef_PlayerShip.shipData.equipmentSpace = (int)e.ChangedSetting.BoxedValue;
                                });
                            }
                            else
                            {
                                CurrRef_PlayerShip.shipData.equipmentSpace = (int)e.ChangedSetting.DefaultValue;
                            }
                            break;
                        }
                        default:
                        {
                            return;
                        }
                    }

                    if (addedAct != null)
                    {
                        // If TryAdd = false => Key exist, change instead
                        if (!enforcedCheats.TryAdd(e.ChangedSetting.Definition.Key, addedAct))
                        {
                            enforcedCheats[e.ChangedSetting.Definition.Key] = addedAct;
                        }
                    }
                }
                else if (e?.ChangedSetting?.Definition?.Section == sectionName_Scond)
                {
                    var confAttr = new Libs.ConfigurationManagerAttributes() { Order = 100 - 5 };

                    switch (e?.ChangedSetting?.Definition?.Key)
                    {
                        case "Spawn Type":
                        {
                            Config.Remove(spawnDefn_StackCount);
                            Config.Remove(spawnDefn_RarityLvel);
                            Config.Remove(spawnDefn_ObjectName);

                            if (!isReset)
                            {
                                switch (gameDict_SpawnType[(string)e.ChangedSetting.BoxedValue])
                                {
                                    case 4:
                                    {
                                        Config.Bind(spawnDefn_ObjectName, "", new ConfigDescription("Ship name", new AcceptableValueList<string>(gameDict_ShipID.Keys.ToArray()), confAttr));
                                        break;
                                    }
                                    case 2:
                                    {
                                        Config.Bind(spawnDefn_StackCount, 0, spawnDesc_StackCount);
                                        Config.Bind(spawnDefn_RarityLvel, "", spawnDesc_RarityLvel);
                                        Config.Bind(spawnDefn_ObjectName, "", new ConfigDescription("Equipment name", new AcceptableValueList<string>(gameDict_EqupID.Keys.ToArray()), confAttr));
                                        break;
                                    }
                                    case 1:
                                    {
                                        Config.Bind(spawnDefn_StackCount, 0, spawnDesc_StackCount);
                                        Config.Bind(spawnDefn_RarityLvel, "", spawnDesc_RarityLvel);
                                        Config.Bind(spawnDefn_ObjectName, "", new ConfigDescription("Weapon name", new AcceptableValueList<string>(gameDict_WepnID.Keys.ToArray()), confAttr));
                                        break;
                                    }
                                    case 3:
                                    {
                                        var currKey = (string)e.ChangedSetting.BoxedValue;
                                        if (currKey.IndexOf("/") > 0)
                                        {
                                            Config.Bind(spawnDefn_StackCount, 0, spawnDesc_StackCount);
                                            Config.Bind(spawnDefn_ObjectName, "", new ConfigDescription("Item name", new AcceptableValueList<string>(gameDict_ItemID.Keys.ToArray()), confAttr));
                                        }
                                        else if (currKey.IndexOf("(Weapon)") > 0)
                                        {
                                            Config.Bind(spawnDefn_StackCount, 0, spawnDesc_StackCount);
                                            Config.Bind(spawnDefn_ObjectName, "", new ConfigDescription("name of the weapon blueprint", new AcceptableValueList<string>(gameDict_WepnID.Keys.ToArray()), confAttr));
                                        }
                                        else if (currKey.IndexOf("(Equipment)") > 0)
                                        {
                                            Config.Bind(spawnDefn_StackCount, 0, spawnDesc_StackCount);
                                            Config.Bind(spawnDefn_ObjectName, "", new ConfigDescription("name of the equipment blueprint", new AcceptableValueList<string>(gameDict_EqupID.Keys.ToArray()), confAttr));
                                        }   
                                        break;
                                    }
                                    default:
                                    {
                                        return;
                                    }
                                }
                            }
                            break;
                        }
                        case "Rarity Level":
                        case "Stack Count":
                        case "Object Name":
                        {
                            if (!isReset)
                            {
                                var spwnKey = Config.FirstOrDefault(x => x.Key.Key == "Spawn Type").Value;
                                var confObj = Config.FirstOrDefault(x => x.Key.Key == "Object Name").Value;
                                var confStk = Config.FirstOrDefault(x => x.Key.Key == "Stack Count").Value;
                                var confRrt = Config.FirstOrDefault(x => x.Key.Key == "Rarity Level").Value;

                                switch (gameDict_SpawnType[(string)spwnKey.BoxedValue])
                                {
                                    case 4:
                                    {
                                        try
                                        {
                                            var dmsn = CurrRef_Player.GetComponent<Collider>();
                                            var ship = ShipDB.GetModel(gameDict_ShipID[(string)e.ChangedSetting.BoxedValue]);
                                            if ((dmsn != null) && (ship != null) && (CurrRef_Player?.transform != null))
                                            {
                                                var nPos = CurrRef_Player.transform.position;
                                                nPos.x += ((dmsn.bounds.size.x > dmsn.bounds.size.z) ? dmsn.bounds.size.x : dmsn.bounds.size.z) + UnityEngine.Random.Range(25, 100);
                                                nPos.z += UnityEngine.Random.Range(-25, 25);

                                                Accessor_CargoLootSystem(CurrRef_PlayerCargo).InstantiateDrop(4, ship.id, ship.rarity, nPos, 1, 0, -1, 0f, -1);

                                                Config.SettingChanged -= OnConfigSettingsChanged;
                                                confObj.BoxedValue = confObj.DefaultValue;
                                                Config.SettingChanged += OnConfigSettingsChanged;
                                            }
                                            else
                                            {
                                                Logger.LogError($"{nameof(OnConfigSettingsChanged)}() | Cannot get \"Player\" Collider, Transform and/or ShipData");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.LogError($"{nameof(OnConfigSettingsChanged)}() | {ex.GetType().Name}, {ex.Message}\nSource : {ex.Source}\n{ex.StackTrace}");
                                        }

                                        break;
                                    }
                                    case 2:
                                    case 1:
                                    case 3:
                                    {
                                        if ((confObj.BoxedValue != confObj.DefaultValue) && (confStk.BoxedValue != confStk.DefaultValue))
                                        {
                                            var dmsn = CurrRef_Player.GetComponent<Collider>();
                                            if (dmsn != null)
                                            {
                                                var nPos = CurrRef_Player.transform.position;
                                                nPos.x += ((dmsn.bounds.size.x > dmsn.bounds.size.z) ? dmsn.bounds.size.x : dmsn.bounds.size.z) + UnityEngine.Random.Range(25, 100);
                                                nPos.z += UnityEngine.Random.Range(-25, 25);

                                                if (!((string)spwnKey.BoxedValue).StartsWith("Item") && (confRrt.BoxedValue != confRrt.DefaultValue))
                                                {
                                                    var tID = gameDict_SpawnType[(string)spwnKey.BoxedValue];
                                                    var iID = (tID == 2) ? gameDict_EqupID[(string)confObj.BoxedValue] : gameDict_WepnID[(string)confObj.BoxedValue];
                                                    var rrt = int.Parse((string)confRrt.BoxedValue);
                                                    var cnt = (int)confStk.BoxedValue;

                                                    Accessor_CargoLootSystem(CurrRef_PlayerCargo).InstantiateDrop(tID, iID, rrt, nPos, cnt, 0, -1, 0f, -1, null);

                                                    Config.SettingChanged -= OnConfigSettingsChanged;
                                                    confObj.BoxedValue = confObj.DefaultValue;
                                                    Config.SettingChanged += OnConfigSettingsChanged;
                                                }
                                                else
                                                {
                                                    if (((string)spwnKey.BoxedValue).IndexOf("/") > 0)
                                                    {
                                                        var item = ItemDB.GetItem(gameDict_ItemID[(string)confObj.BoxedValue]);
                                                        var coun = (int)confStk.BoxedValue;

                                                        Accessor_CargoLootSystem(CurrRef_PlayerCargo).InstantiateDrop(3, item.id, item.rarity, nPos, coun, 0, -1, 0f, -1, null);
                                                    }
                                                    else if (((string)spwnKey.BoxedValue).Contains("(Weapon)"))
                                                    {
                                                        // TODO
                                                    }
                                                    else if (((string)spwnKey.BoxedValue).Contains("(Equipment)"))
                                                    {
                                                        // TODO
                                                    }
                                                    else
                                                    {
                                                        return;
                                                    }

                                                    Config.SettingChanged -= OnConfigSettingsChanged;
                                                    confObj.BoxedValue = confObj.DefaultValue;
                                                    Config.SettingChanged += OnConfigSettingsChanged;
                                                }
                                            }
                                            else
                                            {
                                                Logger.LogError($"{nameof(OnConfigSettingsChanged)}() | Cannot get Collider component from \"Player\"");
                                            }
                                        }
                                        else 
                                        {
                                            return;
                                        }
                                        break;
                                    }
                                    default:
                                    {
                                        return;
                                    }
                                } 
                            }
                            break;
                        }
                    }

                    if (CurrRefComp_ConfigurationManager?.DisplayingWindow == true)
                    {
                        CurrRefComp_ConfigurationManager.DisplayingWindow = false;
                        isConfManQueued = true;
                    }
                }
            }
        }

        /// <summary>
        /// Event handler for whenever <see cref="SceneManager.sceneUnloaded"/> event is raised.
        /// </summary>
        /// <param name="arg0">
        /// The unloaded scene
        /// </param>
        /// <remarks>
        /// Used to clear all running reference whenever target scene is reloaded. Also invoked on each warp, guaranteed to be called beforce <see cref="OnActiveSceneChanged(Scene, Scene)"/>.
        /// </remarks>
        protected virtual void OnActiveSceneUnloaded(Scene arg0)
        {
            if (arg0.name == targetedScene_Name)
            {
                // Clear references whenever scene is changed or just reloaded

                Accessor_CargoLootSystem = null;
                CurrRef_PlayerCargo = null;
                CurrRef_PlayerShip = null;
                CurrRef_Player = null;
                CurrRef_TargetedScene = null;
            }
        }

        /// <summary>
        /// Event handler for whenever <see cref="SceneManager.activeSceneChanged"/> event is raised.
        /// </summary>
        /// <param name="arg0">
        /// Previous scene
        /// </param>
        /// <param name="arg1">
        /// New active scene
        /// </param>
        /// <remarks>
        /// Used for populate and reset plugin's cheat list and reset its state. This also invoked on each warp.
        /// </remarks>
        protected virtual void OnActiveSceneChanged(Scene arg0, Scene arg1)
        {
            /// Considered on target scene if this instance is Active, Enabled 
            /// and current active scene name is same with targetedScene_Name's value.
            
            if (isActiveAndEnabled && (arg1.name == targetedScene_Name))
            {
                CurrRef_TargetedScene = arg1;
                CurrRef_Player = arg1.GetRootGameObjects()?.FirstOrDefault(x => x.tag == "Player");
                CurrRef_PlayerShip = CurrRef_Player?.GetComponent<SpaceShip>();
                CurrRef_PlayerCargo = CurrRef_Player?.GetComponent<CargoSystem>();
                Accessor_CargoLootSystem = AccessTools.FieldRefAccess<CargoSystem, LootSystem>("lootSystem");

                if ((CurrRef_Player != null) && (CurrRef_PlayerShip != null) && (CurrRef_PlayerCargo != null) && (Accessor_CargoLootSystem != null))
                {
                    InitializeDictionaries();
                    InitializeCheatList();
                }
                else
                {
                    Config.Bind("Error", "Cannot found \"Player\" reference. Reload game", "Error, please reload game", new ConfigDescription("", null, new Libs.ConfigurationManagerAttributes() { ReadOnly = true, HideDefaultButton = true }));
                    Logger.LogError($"{nameof(OnActiveSceneChanged)}() | reference to Player, SpaceShip component and/or CargoSystem componenet cannot be found");
                }
            }
            else
            {
                enforcedCheats.Clear();
                Config.SettingChanged -= OnConfigSettingsChanged;
                Config.Clear();

                CurrRef_TargetedScene = null;
                CurrRef_Player = null;
                CurrRef_PlayerShip = null;
                CurrRef_PlayerCargo = null;

                isListInitialized = false;
            }
        }

        /// <summary>
        /// Unity Message. Called first on plugin initialization.
        /// </summary>
        /// <remarks>
        /// Used for intial hooking the Plugin with <see cref="Harmony"/> and depedency check-enforce.
        /// </remarks>
        protected virtual void Awake()
        {
            try
            {
                instance_Harmony = Harmony.CreateAndPatchAll(typeof(Plugin));

                Config.Clear();
                Config.SaveOnConfigSet = false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Awake() | {ex.GetType().Name}, {ex.Message}\nSource : {ex.Source}\nStacktrace :\n{ex.StackTrace}");
                Destroy(this);
            }
        }

        /// <summary>
        /// Unity Message. Called on plugin initialization, guaranteed to be called after <see cref="Awake()"/>, but don't know when.
        /// </summary>
        /// <remarks>
        /// Used for finding reference or initialize required global non-game Unity's <see cref="UnityEngine.Component"/>
        /// </remarks>
        protected virtual void Start()
        {
            try
            {
                CurrRefComp_ConfigurationManager = gameObject.GetComponent<ConfigurationManager.ConfigurationManager>();

                SceneManager.activeSceneChanged += OnActiveSceneChanged;
                SceneManager.sceneUnloaded += OnActiveSceneUnloaded;

                Config.Bind("Difference", "check", "", new ConfigDescription("no desc", new AcceptableValueList<string>("", "tab\"", "tab\t\"")));
            }
            catch (Exception ex)
            {
                Logger.LogError($"Start() | {ex.GetType().Name}, {ex.Message}\nSource : {ex.Source}\nStacktrace :\n{ex.StackTrace}");
                Destroy(this);
            }
        }

        /// <summary>
        /// Unity Message. Called (later) in every frame.
        /// </summary>
        /// <remarks>
        /// Used to 'enforce' enabled ship cheat, if any.
        /// </remarks>
        protected virtual void LateUpdate()
        {
            if (isActiveAndEnabled && (CurrRef_TargetedScene != null))
            {
                if (isConfManQueued && (CurrRefComp_ConfigurationManager?.DisplayingWindow == false))
                {
                    isConfManQueued = false;
                    CurrRefComp_ConfigurationManager.DisplayingWindow = true;
                }

                if (CurrRef_TargetedScene?.GetRootGameObjects()?.FirstOrDefault(x => x.tag == "Player").GetComponent<SpaceShip>() != CurrRef_PlayerShip)
                {
                    // If current active SpaceShip is not same with current SpaceShip reference
                    // reset cheat state
                    isListInitialized = false;
                    OnActiveSceneChanged(CurrRef_TargetedScene.Value, CurrRef_TargetedScene.Value);
                }
                else if (enforcedCheats.Any() && isListInitialized && (CurrRef_Player != null))
                {
                    Parallel.ForEach(enforcedCheats.Values, act =>
                    {
                        act.Invoke();
                    });
                }
            }
        }

        /// <summary>
        /// Unity Message. Called whenever this plugin instance or the <see cref="GameObject"/> it is attached to is being destroyed.
        /// </summary>
        protected virtual void OnDestroy()
        {
            Config.SettingChanged -= OnConfigSettingsChanged;
            Config.Clear();

            Accessor_CargoLootSystem = null;
            CurrRef_PlayerCargo = null;
            CurrRef_PlayerShip = null;
            CurrRef_Player = null;
            CurrRef_TargetedScene = null;

            SceneManager.sceneUnloaded -= OnActiveSceneUnloaded;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;

            CurrRefComp_ConfigurationManager = null;
            instance_Harmony?.UnpatchSelf();
            instance_Harmony = null;
        }
    }
}
