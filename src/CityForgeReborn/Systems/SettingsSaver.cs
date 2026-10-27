using System;
using System.IO;
using System.Threading.Tasks;
using Colossal.IO.AssetDatabase;
using Game;
using UnityEngine;

namespace CityForgeReborn.Systems
{
    /// <summary>
    /// Writes the settings file, and makes sure it actually happened. The game runs every
    /// mod's settings save through one shared queue that holds at most two saves; any
    /// further request is dropped without a word. So saves are debounced (dragging a
    /// slider doesn't fill the queue), and after each attempt the file's timestamp is
    /// checked. If it didn't change, the save is retried with a growing delay.
    /// </summary>
    public partial class SettingsSaver : GameSystemBase
    {
        private const float kDebounceSeconds = 0.5f;
        private const float kVerifySeconds = 2f;
        private const int kMaxAttempts = 5;

        private static bool s_Dirty;
        private static float s_RequestedAt;

        private int m_Attempts;
        private float m_VerifyAt = -1f;
        private DateTime m_AttemptUtc;
        private string m_Path;

        /// <summary>Asks for the settings to be written soon. Safe to call often.</summary>
        public static void Request()
        {
            s_Dirty = true;
            s_RequestedAt = UnityEngine.Time.realtimeSinceStartup;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Path = Path.Combine(Application.persistentDataPath, nameof(CityForgeReborn) + ".coc");
        }

        protected override void OnUpdate()
        {
            float now = UnityEngine.Time.realtimeSinceStartup;

            if (s_Dirty && now - s_RequestedAt >= kDebounceSeconds)
            {
                s_Dirty = false;
                m_Attempts = 0;
                Attempt(now);
                return;
            }

            if (m_VerifyAt < 0f || now < m_VerifyAt)
                return;

            m_VerifyAt = -1f;
            if (Written())
            {
                if (m_Attempts > 1)
                    Mod.Log.Info($"Settings saved after {m_Attempts} attempts.");
                return;
            }

            if (m_Attempts >= kMaxAttempts)
            {
                // Also happens when nothing changed, as the game may skip writing an identical file.
                Mod.Log.Info($"Settings file not updated after {m_Attempts} attempts; giving up until the next change.");
                return;
            }

            Attempt(now);
        }

        private void Attempt(float now)
        {
            m_Attempts++;
            m_AttemptUtc = DateTime.UtcNow;
            m_VerifyAt = now + kVerifySeconds * m_Attempts;

            try
            {
                AssetDatabase.global.SaveSpecificSetting(nameof(CityForgeSetting)).ContinueWith(
                    t => Mod.Log.Warn($"Saving settings failed: {t.Exception?.GetBaseException().Message}"),
                    TaskContinuationOptions.OnlyOnFaulted);
            }
            catch (Exception e)
            {
                Mod.Log.Warn($"Saving settings failed: {e.Message}");
            }
        }

        // One second of slack for file systems with coarse timestamps.
        private bool Written() => File.Exists(m_Path) && File.GetLastWriteTimeUtc(m_Path) >= m_AttemptUtc.AddSeconds(-1);
    }
}
