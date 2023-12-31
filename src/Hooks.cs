using System.Linq;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;

namespace Celeste.Mod.MaymayHelper
{
    public static class Hooks
    {
        public static void Load()
        {
            On.Celeste.Player.DashBegin += OnDashBegin;
            On.Celeste.Player.Die += OnDeath;
            On.Celeste.LevelLoader.LoadingThread += CustomDashInitialize;
            IL.Celeste.Level.Update += PatchLevelUpdate;
        }

        public static void Unload()
        {
            On.Celeste.Player.DashBegin -= OnDashBegin;
            On.Celeste.Player.Die -= OnDeath;
            On.Celeste.LevelLoader.LoadingThread -= CustomDashInitialize;
            IL.Celeste.Level.Update -= PatchLevelUpdate;
        }

        private static void OnDashBegin(On.Celeste.Player.orig_DashBegin orig, Player self)
        {
            if (MaymayHelperModuleSession.HasRecallDash && self.Dashes == 0)
            {
                PlayBackGhost playBackGhost = self.Scene.Tracker.GetEntity<PlayBackGhost>();
                if (playBackGhost != null)
                {
                    Vector2 teleportTarget = playBackGhost.GetTeleportPosition();

                    self.Position = teleportTarget;



                    if (self.Scene.CollideCheck<Solid>(self.Collider.Bounds) || self.Scene.CollideCheck<FloatySpaceBlock>(self.Collider.Bounds))
                    {
                        self.Die(Vector2.Zero);
                    }

                    CleanCustomState(self.Scene);
                }
            }


            orig(self);
        }

        private static PlayerDeadBody OnDeath(On.Celeste.Player.orig_Die orig, Player self, Vector2 direction, bool evenIfInvincible, bool registerDeathInStats)
        {
            CleanCustomState(self.Scene);
            return orig.Invoke(self, direction, evenIfInvincible, registerDeathInStats);
        }

        private static void CustomDashInitialize(On.Celeste.LevelLoader.orig_LoadingThread orig, LevelLoader self)
        {
            orig.Invoke(self);
            CleanCustomState(null);
        }

        private static void CleanCustomState(Scene scene)
        {
            if (scene != null)
            {
                foreach (var playbackGhost in scene.Tracker.GetEntities<PlayBackGhost>())
                {
                    playbackGhost.RemoveSelf();
                }

            }

            MaymayHelperModuleSession.HasRecallDash = false;
        }

        private static void PatchLevelUpdate(ILContext context)
        {
            ILCursor cursor = new(context);
            cursor.Emit(OpCodes.Ldarg_0).EmitDelegate((Level level) =>
            {
                float unpausedTimer = (float)new DynamicData(level).Get("unpauseTimer");
                if (unpausedTimer > 0f)
                {
                    foreach (PlayBackGhost playBackGhost in level.Tracker.GetEntities<PlayBackGhost>().Cast<PlayBackGhost>())
                    {
                        if (playBackGhost != null)
                        {
                            float offset = Engine.DeltaTime;

                            if (unpausedTimer - Engine.RawDeltaTime <= 0f)
                            {
                                offset *= 2;
                            }

                            playBackGhost.pauseOffset += offset;
                        }
                    }

                }
            });
        }
    }
}