//Lifted from https://github.com/MgAl2O4/PatMeDalamud
//and modified by me
using System;
using System.Linq;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Hooking;
using DoteTracker;
using EmoteLog.Utils;

namespace EmoteLog.Hooks
{
    public class EmoteReaderHooks : IDisposable
    {
        public delegate void EmoteDelegate(IPlayerCharacter playerCharacter, ushort emoteId, bool IsIncoming);

        public event EmoteDelegate? OnEmote;

        private delegate void OnEmoteFuncDelegate(ulong unk, ulong instigatorAddr, ushort emoteId, ulong targetId, ulong unk2);
        private readonly Hook<OnEmoteFuncDelegate> hookEmote;

        public EmoteReaderHooks()
        {
            hookEmote = PluginServices.GameInteropProvider.HookFromSignature<OnEmoteFuncDelegate>("E8 ?? ?? ?? ?? 48 8D 8B ?? ?? ?? ?? 4C 89 74 24", OnEmoteDetour);
            hookEmote.Enable();
            
        }

        public void Dispose()
        {
            hookEmote?.Dispose();
        }

        void OnEmoteDetour(ulong unk, ulong instigatorAddr, ushort emoteId, ulong targetId, ulong unk2)
        {
            // unk - some field of event framework singleton? doesn't matter here anyway
            //PluginServices.PluginLog.Info($"Emote >> unk:{unk:X}, instigatorAddr:{instigatorAddr:X}, emoteId:{emoteId}, targetId:{targetId:X}, unk2:{unk2:X}");

            if (PluginServices.ObjectTable.LocalPlayer != null)
            {
                var instigatorOb = PluginServices.ObjectTable.FirstOrDefault(x => (ulong)x.Address == instigatorAddr) as IPlayerCharacter;

                if (targetId == PluginServices.ObjectTable.LocalPlayer.GameObjectId)
                {
                    // incoming emote

                    if (instigatorOb != null)
                    {
                        if (instigatorOb.GameObjectId != targetId)
                        {
                            OnEmote?.Invoke(instigatorOb, emoteId, true);
                        }
                    }
                }
                else
                {
                    // was emote instigated by local player

                    if (instigatorOb != null)
                    {
                        if (instigatorOb.GameObjectId == PluginServices.ObjectTable.LocalPlayer.GameObjectId)
                        {
                            var EmoteTarget = PluginServices.ObjectTable.LocalPlayer.TargetObject as IPlayerCharacter;
                            if (EmoteTarget != null)
                            {
                                // was from local player, pass target rather than local player
                                OnEmote?.Invoke(EmoteTarget, emoteId, false);
                            }
                        }
                    }
                }
            }

            hookEmote.Original(unk, instigatorAddr, emoteId, targetId, unk2);
        }
    }
}
