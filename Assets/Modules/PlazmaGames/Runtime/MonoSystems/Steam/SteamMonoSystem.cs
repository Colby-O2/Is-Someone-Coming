//#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
//#define DISABLE_STEAMWORKS
//#endif

//using UnityEngine;
//using PlazmaGames.Core.Debugging;

//#if !DISABLE_STEAMWORKS
//using Steamworks;
//using System;
//#endif

//namespace PlazmaGames.Steam
//{
//    public sealed class SteamMonoSystem : MonoBehaviour, ISteamMonoSystem
//    {
//        [Header("Steam App Infomation")]
//        [SerializeField] private bool _enableSteam = true;
//        [SerializeField] private AppId_t _appID = AppId_t.Invalid;

//        public bool IsSteamAvivable { get { return IsInitalized && _enableSteam; } }
//        private bool IsInitalized { get; set; }

//#if !DISABLE_STEAMWORKS

//        [Header("Steam App Infomation")]
//        private SteamAPIWarningMessageHook_t _steamAPIWarningMessageHook;

//        private bool CheckAvailabilityOfSteamAchievementAndStat()
//        {
//            if (!SteamUserStats.RequestCurrentStats())
//            {
//                return false;
//            }

//            return true;
//        }

//        private bool IsAchievementVaild(string achievementID, bool ignoreCompleteFlag = false)
//        {
//            if (!CheckAvailabilityOfSteamAchievementAndStat())
//            {
//                PlazmaDebug.LogWarning($"Failed to fetch steam achevement {achievementID}. No user is logged in.", "Steamworks", 1);
//                return false;
//            }

//            bool doesAchievementExist = SteamUserStats.GetAchievement(achievementID, out bool hasCompleted);

//            if (!doesAchievementExist)
//            {
//                PlazmaDebug.LogWarning($"Failed to fetch steam achevement {achievementID}.", "Steamworks", 1);
//                return false;
//            }

//            return !hasCompleted || ignoreCompleteFlag;
//        }

//        private bool IsStatVaild(string statID, object next, SteamStatType type)
//        {
//            if (!CheckAvailabilityOfSteamAchievementAndStat())
//            {
//                PlazmaDebug.LogWarning($"Failed to fetch steam stat {statID}. No user is logged in.", "Steamworks", 1);
//                return false;
//            }

//            int currentI = -1;
//            float currentF = -1;
//            bool doesStatExist = (type == SteamStatType.INTEGER) ? SteamUserStats.GetStat(statID, out currentI) : SteamUserStats.GetStat(statID, out currentF);

//            if (!doesStatExist)
//            {
//                PlazmaDebug.LogWarning($"Failed to fetch steam stat {statID}.", "Steamworks", 1);
//                return false;
//            }

//            if (currentI > Convert.ToInt32(next) || currentF > Convert.ToSingle(next))
//            {
//                PlazmaDebug.LogWarning($"Trying to update a stat to less than it's current value. Ingoring request.", "Steamworks", 2);
//                return false;
//            }

//            return true;
//        }

//        public void SetStatProgress(string statID, object progress, SteamStatType type)
//        {
//            PlazmaDebug.Log($"Attemping to set {statID}'s progress to {progress}.", "Steamworks", Color.green, 2);
//            if (!IsSteamAvivable || !IsStatVaild(statID, progress, type)) return;

//            bool successful = (type == SteamStatType.INTEGER) ? SteamUserStats.SetStat(statID,Convert.ToInt32(progress)) : SteamUserStats.SetStat(statID, Convert.ToSingle(progress));
//            successful &= SteamUserStats.StoreStats();

//            if (successful) PlazmaDebug.Log($"Suscessfully updated {statID} stats.", "Steamworks", Color.green, 1);
//            else PlazmaDebug.LogWarning($"Failed to update {statID} stats of type {type} to {progress}.", "Steamworks", 1);
//        }

//        public void SetAchievementAsCompleted(string achievementID)
//        {
//            PlazmaDebug.Log($"Attemping to mark {achievementID} as complete.", "Steamworks", Color.green, 2);
//            if (!IsSteamAvivable || !IsAchievementVaild(achievementID)) return;

//            bool successful = SteamUserStats.SetAchievement(achievementID);
//            successful &= SteamUserStats.StoreStats();

//            if (successful) PlazmaDebug.Log($"Suscessfully marked {achievementID} as compelete.", "Steamworks", Color.green, 1);
//            else PlazmaDebug.LogWarning($"Failed to marked {achievementID} as compelete.", "Steamworks", 1);
//        }

//        public void SetAchievementAsIncompleted(string achievementID)
//        {
//            PlazmaDebug.Log($"Attemping to mark {achievementID} as incomplete.", "Steamworks", Color.green, 2);
//            if (!IsSteamAvivable || !IsAchievementVaild(achievementID, true)) return;

//            bool successful = SteamUserStats.ClearAchievement(achievementID);
//            successful &= SteamUserStats.StoreStats();

//            if (successful) PlazmaDebug.Log($"Suscessfully marked {achievementID} as incompelete.", "Steamworks", Color.green, 1);
//            else PlazmaDebug.LogWarning($"Failed to marked {achievementID} as incompelete.", "Steamworks", 1);
//        }

//        public void ClearAllStats(bool removeAchieveemnts = true)
//        {
//            string achieveemntsMSG = (removeAchieveemnts) ? " and achievements" : string.Empty;
//            PlazmaDebug.Log($"Attemping to remove all stats{achieveemntsMSG}.", "Steamworks", Color.green, 2);
//            if (!IsSteamAvivable || !CheckAvailabilityOfSteamAchievementAndStat()) return;

//            bool successful = SteamUserStats.ResetAllStats(removeAchieveemnts);
//            successful &= SteamUserStats.StoreStats();

//            if (successful) PlazmaDebug.Log($"Suscessfully remove all stats{achieveemntsMSG}.", "Steamworks", Color.green, 1);
//            else PlazmaDebug.LogWarning($"Failed to remove all stats{achieveemntsMSG}.", "Steamworks", 1);
//        }

//        [AOT.MonoPInvokeCallback(typeof(SteamAPIWarningMessageHook_t))]
//        protected static void SteamAPIDebugTextHook(int nSeverity, System.Text.StringBuilder pchDebugText)
//        {
//            PlazmaDebug.LogWarning(pchDebugText.ToString(), "Steamworks", 1);
//        }

//        private void InitializeSteam()
//        {
//            PlazmaDebug.Log($"Initialize Steam.", "Steamworks", Color.green, 2);
//            IsInitalized = SteamAPI.Init();
//            if (!IsInitalized)
//            {
//                PlazmaDebug.LogError("SteamAPI_Init() failed. Refer to Valve's documentation or the comment above this line for more information.", "Steamworks");
//            }

//            if (_steamAPIWarningMessageHook == null)
//            {
//                _steamAPIWarningMessageHook = new SteamAPIWarningMessageHook_t(SteamAPIDebugTextHook);
//                SteamClient.SetWarningMessageHook(_steamAPIWarningMessageHook);
//            }
//        }

//        private void Awake()
//        {
//            IsInitalized = false;

//            if (!_enableSteam) return;

//            if (!Packsize.Test())
//            {
//                PlazmaDebug.LogError("Packsize Test returned false, the wrong version of Steamworks.NET is being run in this platform.", "Steamworks");
//            }

//            if (!DllCheck.Test())
//            {
//                PlazmaDebug.LogError("DllCheck Test returned false, One or more of the Steamworks binaries seems to be the wrong version.", "Steamworks");
//            }

//            try
//            {
//                if (SteamAPI.RestartAppIfNecessary(_appID)) return; 
//            }
//            catch (System.DllNotFoundException e)
//            {
//                PlazmaDebug.LogError("Could not load [lib]steam_api.dll/so/dylib. It's likely not in the correct location.\n" + e, "Steamworks");
//                return;
//            }

//            InitializeSteam();
//        }

//        private void Update()
//        {
//            if (!IsSteamAvivable) return;
//            SteamAPI.RunCallbacks();
//        }

//        private void OnDestroy()
//        {
//            if (!IsInitalized) return;
//            SteamAPI.Shutdown();
//        }
//#endif
//    }
//}
