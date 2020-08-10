using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Modding;
using UnityEngine;

namespace HellMod
{
    public class HellMod : Mod, ITogglableMod
    {
        public HellMod() : base("Hell Mod") { }

        public override ModSettings GlobalSettings
        {
            get => _settings;
            set => _settings = (GlobalModSettings) value;
        }

        private GlobalModSettings _settings = new GlobalModSettings();

        public override string GetVersion() => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public override void Initialize()
        {
            Log("Initializing");

            ModHooks.Instance.TakeHealthHook += OnHealthTaken;
            ModHooks.Instance.SoulGainHook += OnSoulGain;
            ModHooks.Instance.GetPlayerIntHook += OnInt;
            ModHooks.Instance.NewGameHook += OnNewGame;
            ModHooks.Instance.SavegameLoadHook += OnSaveLoaded;
            ModHooks.Instance.HitInstanceHook += OnHit;
        }

        private HitInstance OnHit(Fsm owner, HitInstance hit)
        {
            switch (hit.AttackType)
            {
                case AttackTypes.Nail when _settings.LimitNail:
                    hit.DamageDealt /= 2;
                    break;
                
                case AttackTypes.Spell when _settings.LimitSpells:
                    hit.DamageDealt = 5 * hit.DamageDealt / 6;
                    break;
            }

            return hit;
        }

        private void OnNewGame() => OnSaveLoaded();

        private void OnSaveLoaded(int id = -1) => GameManager.instance.StartCoroutine(LimitFocus());

        private IEnumerator LimitFocus()
        {
            if (!_settings.LimitFocus)
                yield break;

            yield return new WaitWhile(() => HeroController.instance == null);

            PlayMakerFSM sc = HeroController.instance.spellControl;

            // This is the state which actually changes the speed of focusing
            FsmState state = sc.FsmStates.First(x => x.Name == "Deep Focus Speed");

            // And this is the factor which it multiplies by
            FsmFloat deepScalar = state.Actions.OfType<FloatMultiply>().First().multiplyBy;

            // Make all healing slower by the factor of deep focus
            // So that normal healing is deep focus speed.
            sc.Fsm.GetFsmFloat("Time Per MP Drain UnCH").Value *= deepScalar.Value;
            sc.Fsm.GetFsmFloat("Time Per MP Drain CH").Value *= deepScalar.Value;
            deepScalar.Value *= deepScalar.Value;
        }

        private int OnInt(string intName)
        {
            return intName switch
            {
                "maxMP" when _settings.LimitSoulCapacity => PlayerData.instance.maxMP / 3,
                "MPReserveMax" when _settings.LimitSoulCapacity => PlayerData.instance.MPReserveMax / 3,

                // Dreamshield
                "charmCost_38" => 1,

                _ => PlayerData.instance.GetIntInternal(intName)
            };
        }

        private int OnHealthTaken(int damage)
        {
            return _settings.DoubleDeamage
                ? damage * 2
                : damage;
        }

        private bool _roundedSoul;

        private int OnSoulGain(int amount)
        {
            if (!_settings.LimitSoulGain)
                return amount;

            _roundedSoul = !_roundedSoul;

            // First hit is rounded down, second is rounded up
            var pd = PlayerData.instance;

            // If we have a shade, no soul
            // Otherwise swap between ceiling and floor to allow it to still be in 6 hits.
            amount = pd.soulLimited
                    ? 0
                    : _roundedSoul
                        ? amount / 2
                        : (int) Math.Ceiling((float) amount / 2)
                ;

            return amount;
        }

        public void Unload()
        {
            ModHooks.Instance.TakeHealthHook -= OnHealthTaken;
            ModHooks.Instance.SoulGainHook -= OnSoulGain;
            ModHooks.Instance.GetPlayerIntHook -= OnInt;
            ModHooks.Instance.NewGameHook -= OnNewGame;
            ModHooks.Instance.SavegameLoadHook -= OnSaveLoaded;
            ModHooks.Instance.HitInstanceHook -= OnHit;
        }
    }
}