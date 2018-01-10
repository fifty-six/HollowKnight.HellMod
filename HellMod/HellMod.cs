using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;

namespace HellMod

{
    public class HellMod : Modding.Mod
    {
        public override void Initialize()
        {
            Log("Initializing");
            Modding.ModHooks.Instance.TakeHealthHook += OnHealthTaken;
            Modding.ModHooks.Instance.SoulGainHook += OnSoulGain;
            Modding.ModHooks.Instance.GetPlayerIntHook += OnInt;
            Modding.ModHooks.Instance.ColliderCreateHook += OnColliderCreate;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoad;
        }

        private void OnColliderCreate(GameObject go)
        {
            if (FSMUtility.ContainsFSM(go, "health_manager_enemy"))
            {
                foreach (NamedVariable var in FSMUtility.LocateFSM(go, "health_manager_enemy").FsmVariables.GetNamedVariables(VariableType.Int))
                {
                    if (var.Name == "HP")
                    {
                        FsmInt val = var as FsmInt;
                        val.Value = (int) Math.Round(val.Value * 1.2);
                    }
                }
            }
            if (FSMUtility.ContainsFSM(go, "health_manager"))
            {
                foreach (NamedVariable var in FSMUtility.LocateFSM(go, "health_manager").FsmVariables.GetNamedVariables(VariableType.Int))
                {
                    if (var.Name == "HP")
                    {
                        FsmInt val = var as FsmInt;
                        val.Value = (int) Math.Round(val.Value * 1.2);
                    }
                }
            }
        }

        private void OnSceneLoad(Scene dst, LoadSceneMode lsm)
        {
            Log("On Scene Load");
            foreach ( FsmState state in HeroController.instance.spellControl.FsmStates)
            {
                if (state.Name == "Deep Focus Speed")
                {
                    ((FloatMultiply)state.Actions[1]).multiplyBy.Value = (float)2.7225;
            }
            }
            HeroController.instance.spellControl.Fsm.GetFsmFloat("Time Per MP Drain UnCH").Value = (float)0.04455;
            HeroController.instance.spellControl.Fsm.GetFsmFloat("Time Per MP Drain CH").Value = (float)0.0297;

        }

        private bool nailDamage;
        private int OnInt(string intName)
        {
            switch(intName)
            {
                case "maxMP":
                    return PlayerData.instance.maxMP / 3;
                case "MPReserveMax":
                    return PlayerData.instance.MPReserveMax / 3;
                case "nailDamage":
                    nailDamage = !nailDamage;
                    return nailDamage ? (int) Math.Floor((5 + PlayerData.instance.nailSmithUpgrades * 4) / 2.4): (int)Math.Ceiling((float) (5 + PlayerData.instance.nailSmithUpgrades * 4f) / 2.4);
                case "charmCost_38":
                    return 1;
                default:
                    return PlayerData.instance.GetIntInternal(intName);
            }
        }

        public int OnHealthTaken(int damage)
        {
            return (damage * 2);
        }

        public bool soulGain; // used for OnSoulGain to have 1/2 soul, but still keep it going nicely into 33
        public int OnSoulGain(int amount)
        {
            soulGain = !soulGain;
            amount = PlayerData.instance.soulLimited ? 0: (soulGain ? amount / 2 : (int)Math.Ceiling((float) amount / 2)); // first hit is rounded down, second is rounded up
		    if (PlayerData.instance.GetInt("MPCharge") + amount > PlayerData.instance.GetInt("maxMP"))
            {
                if (PlayerData.instance.GetInt("MPReserve") < PlayerData.instance.GetInt("MPReserveMax")) 
                {
                    PlayerData.instance.MPReserve += amount - (PlayerData.instance.GetInt("maxMP") - PlayerData.instance.GetInt("MPCharge"));
                    if (PlayerData.instance.GetInt("MPReserve") > PlayerData.instance.GetInt("MPReserveMax"))
                    {
                        PlayerData.instance.MPReserve = PlayerData.instance.GetInt("MPReserveMax");
                    }
                }
                PlayerData.instance.MPCharge = PlayerData.instance.GetInt("maxMP");
            }
                else
                {
                    PlayerData.instance.MPCharge += amount;
                }

            return 0;
        }
    }
}
