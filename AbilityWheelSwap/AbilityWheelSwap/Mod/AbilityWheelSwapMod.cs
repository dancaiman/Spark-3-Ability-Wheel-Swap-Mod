using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Reflection;
using MelonLoader;
using MelonLoader.Utils;
using Rewired;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;

[assembly: MelonInfo(typeof(AbilityWheelSwap.AbilityWheelSwapMod), "AbilityWheelSwap", "1.0.0", "Dancaiman/ChatGPT")]
[assembly: MelonGame("Feperd Games", "Spark the Electric Jester 3")]

namespace AbilityWheelSwap
{
    public class AbilityWheelSwapMod : MelonMod
    {
        private Player player;
        private int currentSet = 0;

        private string SavePath
        {
            get
            {
                try { return Path.Combine(MelonEnvironment.UserDataDirectory, "AbilityWheelSwapSets.xml"); }
                catch { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MelonLoader", "UserData", "AbilityWheelSwapSets.xml"); }
            }
        }

        private List<int[]> dpadSets = new List<int[]>();
        private ShopaloShop shopInstance;

       
        private Image upIcon, downIcon, leftIcon, rightIcon;

       
        private Dictionary<int, Sprite> abilitySprites = new Dictionary<int, Sprite>();

        
        private bool attemptedBundleLoad = false;
        private bool assetBundleLoaded = false;
        private string bundlePathCached = null;

        
        private bool attemptedFindHolder = false;
        private bool hasPowerupHolder = false;
        private float nextHolderCheckTime = 0f;
        private const float HOLDER_CHECK_INTERVAL = 0.5f;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("[AbilityWheelSwap] Iniciando mod...");
            EnsureFileExists();
            LoadSetsFromFile();
            LoadCurrentSet();
        }

       
        [Obsolete]
        public override void OnApplicationStart()
        {
            try
            {
                var harmony = new HarmonyLib.Harmony("AbilityWheelSwap.DisableHomingLock");
                harmony.PatchAll();
                MelonLogger.Msg("[AbilityWheelSwap] Harmony parche aplicado.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[AbilityWheelSwap] Error aplicando parches Harmony: " + ex);
            }
        }

        public override void OnLateUpdate()
        {
            try
            {
                
                if (player == null && ReInput.players != null)
                    player = ReInput.players.GetPlayer(0);

                
                if (shopInstance == null)
                    shopInstance = UnityEngine.Object.FindObjectOfType<ShopaloShop>();

                
                if (dpadSets == null || dpadSets.Count == 0) return;

                
                if (Time.unscaledTime >= nextHolderCheckTime)
                {
                    nextHolderCheckTime = Time.unscaledTime + HOLDER_CHECK_INTERVAL;
                    TryFindPowerupHolderOnce();
                }

               
                if ((upIcon == null || downIcon == null || leftIcon == null || rightIcon == null) && hasPowerupHolder)
                {
                    InitializeHUDReferences();
                }

                
                if (shopInstance != null && (abilitySprites == null || abilitySprites.Count < 14))
                {
                    TryLoadSpritesFromShopOnce();
                }

               
                if (hasPowerupHolder && !attemptedBundleLoad)
                {
                    attemptedBundleLoad = true;
                    TryLoadSpritesFromAssetBundle();
                }

               
                if ((abilitySprites == null || abilitySprites.Count < 14) && hasPowerupHolder)
                {
                    FillSpritesFromHUD();
                }

                
                if (player != null && player.GetButtonDown("R3"))
                {
                    currentSet++;
                    if (currentSet >= dpadSets.Count) currentSet = 0;
                    ApplySet(currentSet);
                }

               
                if (shopInstance != null)
                {
                    UpdateCurrentSetFromSave();
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[AbilityWheelSwap] Excepción en OnLateUpdate: " + ex);
            }
        }

        private void TryFindPowerupHolderOnce()
        {
            if (hasPowerupHolder) return;

            
            var go = GameObject.Find("PlayerObjects/Camera_Objects/Main Camera/Ui_Camera/UI/PowerupHolder");
            if (go != null)
            {
                hasPowerupHolder = true;
                MelonLogger.Msg("[AbilityWheelSwap] PowerupHolder encontrado en escena.");
            }
            else
            {
                attemptedFindHolder = true;
            }
        }

        
        private void InitializeHUDReferences()
        {
            try
            {
                var powerupHolder = GameObject.Find("PlayerObjects/Camera_Objects/Main Camera/Ui_Camera/UI/PowerupHolder");
                if (powerupHolder == null) return;

                upIcon = powerupHolder.transform.Find("DpadUP/Power_Up")?.GetComponent<Image>();
                downIcon = powerupHolder.transform.Find("DpadDown/Power_DI")?.GetComponent<Image>();
                
                leftIcon = powerupHolder.transform.Find("DpadRight/Power_LI")?.GetComponent<Image>();
                rightIcon = powerupHolder.transform.Find("DpadLeft/Power_RI")?.GetComponent<Image>();
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[AbilityWheelSwap] Error inicializando referencias HUD: " + ex);
            }
        }

        
        private void FillSpritesFromHUD()
        {
            try
            {
                if (abilitySprites == null) abilitySprites = new Dictionary<int, Sprite>();

                if (upIcon != null && upIcon.sprite != null) abilitySprites[1] = upIcon.sprite;
                if (downIcon != null && downIcon.sprite != null) abilitySprites[2] = downIcon.sprite;
                if (leftIcon != null && leftIcon.sprite != null) abilitySprites[3] = leftIcon.sprite;
                if (rightIcon != null && rightIcon.sprite != null) abilitySprites[4] = rightIcon.sprite;
                
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[AbilityWheelSwap] FillSpritesFromHUD fallo: " + ex);
            }
        }

        
        private void TryLoadSpritesFromShopOnce()
        {
            try
            {
                if (shopInstance == null) return;
                if (shopInstance.DpadIcons == null || shopInstance.DpadIcons.Length == 0) return;

                
                if (abilitySprites == null) abilitySprites = new Dictionary<int, Sprite>();

                
                for (int i = 0; i < shopInstance.DpadIcons.Length; i++)
                {
                    var s = shopInstance.DpadIcons[i];
                    if (s != null)
                    {
                        abilitySprites[i] = s;
                    }
                }

                MelonLogger.Msg("[AbilityWheelSwap] abilitySprites cargados desde ShopaloShop.DpadIcons.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[AbilityWheelSwap] TryLoadSpritesFromShop fallo: " + ex);
            }
        }

        
        private void TryLoadSpritesFromAssetBundle()
        {
            try
            {
                MelonLogger.Msg("[AbilityWheelSwap] Intentando cargar AssetBundle incrustado...");

               
                var asm = typeof(AbilityWheelSwapMod).Assembly;

                
                const string resourceName = "AbilityWheelSwap.abilitysprites";

                
                var names = asm.GetManifestResourceNames();
                bool found = false;
                foreach (var n in names)
                {
                    if (n.EndsWith("abilitysprites"))
                    {
                        found = true;
                        MelonLogger.Msg("[AbilityWheelSwap] Recurso encontrado: " + n);
                    }
                }

                if (!found)
                {
                    MelonLogger.Error("[AbilityWheelSwap] ERROR: no se encontró abilitysprites como recurso incrustado.");
                    return;
                }

                
                using (Stream stream = asm.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        MelonLogger.Error("[AbilityWheelSwap] ERROR: stream nulo, nombre de recurso incorrecto.");
                        MelonLogger.Error("[AbilityWheelSwap] Recursos disponibles:");
                        foreach (var r in names) MelonLogger.Error(" - " + r);
                        return;
                    }

                
                    byte[] data = new byte[stream.Length];
                    stream.Read(data, 0, data.Length);

                    
                    AssetBundle bundle = AssetBundle.LoadFromMemory(data);
                    if (bundle == null)
                    {
                        MelonLogger.Error("[AbilityWheelSwap] No se pudo cargar AssetBundle desde memoria.");
                        return;
                    }

                    MelonLogger.Msg("[AbilityWheelSwap] AssetBundle incrustado cargado correctamente.");


                    Dictionary<int, string> nameById = new Dictionary<int, string>()
            {
                {1,"SpeedBlastIcon"},
                {2,"BurstIcon"},
                {3,"SpeedBlastBoostIcon"},
                {4,"PowerIcon"},
                {5,"TeleportSpecial"},
                {6,"Scouter"},
                {7,"MachineGunSpecial"},
                {8,"ReaperPowerColored"},
                {9,"FloatIcon"},
                {10,"Icon_fark"},
                {11,"Icon_sfarx"},
                {12,"Heal"},
                {13,"Flutter"},
                {14,"Shield"}
            };

                    if (abilitySprites == null) abilitySprites = new Dictionary<int, Sprite>();
                    abilitySprites.Clear();

                    foreach (var kv in nameById)
                    {
                        string assetName = kv.Value;

                        Sprite sp = bundle.LoadAsset<Sprite>(assetName);
                        if (sp != null)
                        {
                            abilitySprites[kv.Key] = sp;
                            continue;
                        }

                        Texture2D tex = bundle.LoadAsset<Texture2D>(assetName);
                        if (tex != null)
                        {
                            try
                            {
                                Sprite created = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                                abilitySprites[kv.Key] = created;
                                continue;
                            }
                            catch { }
                        }

                        
                        for (int v = 1; v <= 4; v++)
                        {
                            string tryName = assetName + "_" + v;
                            Sprite spv = bundle.LoadAsset<Sprite>(tryName);
                            if (spv != null)
                            {
                                abilitySprites[kv.Key] = spv;
                                break;
                            }
                            Texture2D txv = bundle.LoadAsset<Texture2D>(tryName);
                            if (txv != null)
                            {
                                try
                                {
                                    Sprite createdv = Sprite.Create(txv, new Rect(0, 0, txv.width, txv.height), new Vector2(0.5f, 0.5f));
                                    abilitySprites[kv.Key] = createdv;
                                    break;
                                }
                                catch { }
                            }
                        }

                        if (!abilitySprites.ContainsKey(kv.Key))
                            MelonLogger.Warning("[AbilityWheelSwap] Sprite no encontrado en AssetBundle incrustado: " + assetName);
                    }

                    assetBundleLoaded = true;
                    MelonLogger.Msg("[AbilityWheelSwap] Sprites cargados exitosamente desde bundle incrustado.");

                    bundle.Unload(false);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[AbilityWheelSwap] Error cargando AssetBundle incrustado: " + ex);
            }
        }

        private void ApplySet(int index)
        {
            try
            {
                if (index < 0 || index >= dpadSets.Count)
                {
                    MelonLogger.Warning("[AbilityWheelSwap] Índice de set fuera de rango: " + index);
                    return;
                }

                var set = dpadSets[index];
                Save.SaveFile save = Save.GetCurrentSave();

                save.DpadUP = set[0];
                save.DpadDown = set[1];
                save.DpadLeft = set[2];
                save.DpadRight = set[3];

                
                Action_13_NewSuperMoves.UpPower = set[0];
                Action_13_NewSuperMoves.DownPower = set[1];
                Action_13_NewSuperMoves.LeftPower = set[2];
                Action_13_NewSuperMoves.RightPower = set[3];

               
                if (upIcon != null && downIcon != null && leftIcon != null && rightIcon != null)
                {
                    UpdateHUDIcons(set);
                }

               
                shopInstance?.SetDpadIcons();

                SaveSetsToFile();
                SaveCurrentSet();

                MelonLogger.Msg("[AbilityWheelSwap] SET " + index + " aplicado.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[AbilityWheelSwap] Error aplicando set: " + ex);
            }
        }

        
        private void UpdateHUDIcons(int[] set)
        {
            try
            {
                if (abilitySprites == null) abilitySprites = new Dictionary<int, Sprite>();

                // UP
                if (upIcon != null)
                {
                    Sprite sp;
                    if (abilitySprites.TryGetValue(set[0], out sp) && sp != null)
                        upIcon.sprite = sp;
                }

                // DOWN
                if (downIcon != null)
                {
                    Sprite sp;
                    if (abilitySprites.TryGetValue(set[1], out sp) && sp != null)
                        downIcon.sprite = sp;
                }

                // LEFT visual
                if (leftIcon != null)
                {
                    Sprite sp;
                    if (abilitySprites.TryGetValue(set[2], out sp) && sp != null)
                        leftIcon.sprite = sp;
                }

                // RIGHT visual
                if (rightIcon != null)
                {
                    Sprite sp;
                    if (abilitySprites.TryGetValue(set[3], out sp) && sp != null)
                        rightIcon.sprite = sp;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[AbilityWheelSwap] Error actualizando HUD icons: " + ex);
            }
        }

        
        private void UpdateCurrentSetFromSave()
        {
            try
            {
                Save.SaveFile save = Save.GetCurrentSave();
                while (currentSet >= dpadSets.Count)
                    dpadSets.Add(new int[] { save.DpadUP, save.DpadDown, save.DpadLeft, save.DpadRight });

                dpadSets[currentSet][0] = save.DpadUP;
                dpadSets[currentSet][1] = save.DpadDown;
                dpadSets[currentSet][2] = save.DpadLeft;
                dpadSets[currentSet][3] = save.DpadRight;

                
                if (upIcon != null && downIcon != null && leftIcon != null && rightIcon != null)
                    UpdateHUDIcons(dpadSets[currentSet]);

               
                SaveSetsToFile();
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[AbilityWheelSwap] Error en UpdateCurrentSetFromSave: " + ex);
            }
        }

        #region IO: XML y preferencias

        private void EnsureFileExists()
        {
            try
            {
                string dir = Path.GetDirectoryName(SavePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (!File.Exists(SavePath))
                {
                    dpadSets = new List<int[]>()
                    {
                        new int[]{3,8,9,2},  // Set 0
                        new int[]{11,7,10,5} // Set 1
                    };
                    SaveSetsToFile();
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[AbilityWheelSwap] Error asegurando archivo: " + ex);
            }
        }

        private void LoadSetsFromFile()
        {
            try
            {
                if (!File.Exists(SavePath))
                {
                    dpadSets = new List<int[]>() { new int[]{3,8,9,2}, new int[]{11,7,10,5} };
                    SaveSetsToFile();
                    return;
                }

                XmlSerializer serializer = new XmlSerializer(typeof(List<int[]>));
                using (FileStream fs = new FileStream(SavePath, FileMode.Open))
                {
                    object obj = serializer.Deserialize(fs);
                    dpadSets = obj as List<int[]>;
                    if (dpadSets == null)
                        dpadSets = new List<int[]>() { new int[]{3,8,9,2}, new int[]{11,7,10,5} };
                }

                MelonLogger.Msg("[AbilityWheelSwap] Sets cargados correctamente.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[AbilityWheelSwap] Error leyendo XML: " + ex);
                dpadSets = new List<int[]>() { new int[]{3,8,9,2}, new int[]{11,7,10,5} };
            }
        }

        private void SaveSetsToFile()
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<int[]>));
                using (FileStream fs = new FileStream(SavePath, FileMode.Create))
                {
                    serializer.Serialize(fs, dpadSets);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[AbilityWheelSwap] Error guardando XML: " + ex);
            }
        }

        private void SaveCurrentSet()
        {
            try
            {
                MelonPreferences.CreateEntry("AbilityWheelSwap", "currentSet", 0);
            }
            catch
            {
              
            }
            MelonPreferences.SetEntryValue("AbilityWheelSwap", "currentSet", currentSet);
            MelonPreferences.Save();
        }

        private void LoadCurrentSet()
        {
            try
            {
                MelonPreferences.CreateEntry("AbilityWheelSwap", "currentSet", 0);
            }
            catch { }
            currentSet = MelonPreferences.GetEntryValue<int>("AbilityWheelSwap", "currentSet");
        }

        #endregion
    }

    [HarmonyPatch]
    public static class Patch_HomingLockDisable
    {
       
        static MethodBase TargetMethod()
        {
            var t = AccessTools.TypeByName("HomingAttackControl");
            if (t == null)
            {
                
                try { t = typeof(HomingAttackControl); }
                catch { return null; }
            }
            if (t == null) return null;
            return AccessTools.Method(t, "Update");
        }

        static void Postfix(object __instance)
        {
            try
            {
                if (__instance == null) return;
                Type t = __instance.GetType();

                
                FieldInfo fi = t.GetField("Lock", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (fi != null)
                {
                    if (fi.IsStatic)
                        fi.SetValue(null, false);
                    else
                        fi.SetValue(__instance, false);
                    return;
                }

                
                PropertyInfo pi = t.GetProperty("Lock", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (pi != null && pi.CanWrite)
                {
                    MethodInfo setm = pi.GetSetMethod(true);
                    if (setm != null && setm.IsStatic)
                        pi.SetValue(null, false);
                    else
                        pi.SetValue(__instance, false);
                    return;
                }

                
                var staticFi = AccessTools.Field(t, "Lock");
                if (staticFi != null && staticFi.IsStatic)
                {
                    staticFi.SetValue(null, false);
                    return;
                }
                var staticPi = AccessTools.Property(t, "Lock");
                if (staticPi != null)
                {
                    var setMethod = staticPi.GetSetMethod(true);
                    if (setMethod != null && setMethod.IsStatic)
                        staticPi.SetValue(null, false);
                }
            }
            catch (Exception ex)
            {
    
                MelonLogger.Warning("[AbilityWheelSwap] Patch_HomingLockDisable fallo al intentar desactivar Lock: " + ex.Message);
            }
        }
    }
}
