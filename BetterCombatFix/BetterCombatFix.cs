using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace BetterCombatFix
{
	[BepInPlugin(ModGUID, ModName, ModVersion)]
	public class BetterCombatFix : BaseUnityPlugin
	{
		private const string ModName = "Better Combat Fix";
		private const string ModVersion = "1.0.0";
		private const string ModGUID = "org.bepinex.plugins.bettercombatfix";

		private void Awake()
		{
			Harmony harmony = new(ModGUID);
			harmony.PatchAll();
		}

		[HarmonyPatch(typeof(Attack), nameof(Attack.OnAttackTrigger))]
		private static class Patch_Attack_DoMeleeAttack
		{
			private static bool Prefix(Attack __instance)
			{
				if (__instance.m_character is Player player)
				{
					Vector3 vector = player.transform.forward;
					__instance.m_weapon.m_shared.m_hitEffect.Create(vector, Quaternion.identity);
					__instance.m_hitEffect.Create(vector, Quaternion.identity);
					foreach (Character character in FindObjectsOfType<Character>())
					{
						if (character is not Player)
						{
							Skills.SkillType skill = __instance.m_weapon.m_shared.m_skillType;
							if (__instance.m_specialHitSkill != Skills.SkillType.None)
							{
								skill = __instance.m_specialHitSkill;
							}
							float randomSkillFactor = player.GetRandomSkillFactor(skill);
							Collider collider = character.GetCollider();
							Vector3 position = player.transform.position;
							Vector3 hitPoint = collider.ClosestPoint(position);
							HitData hitData = new()
							{
								m_toolTier = __instance.m_weapon.m_shared.m_toolTier,
								m_statusEffect = (bool)(Object)__instance.m_weapon.m_shared.m_attackStatusEffect ? __instance.m_weapon.m_shared.m_attackStatusEffect.name : "",
								m_pushForce = __instance.m_weapon.m_shared.m_attackForce * randomSkillFactor * __instance.m_forceMultiplier,
								m_backstabBonus = __instance.m_weapon.m_shared.m_backstabBonus,
								m_staggerMultiplier = __instance.m_staggerMultiplier,
								m_dodgeable = __instance.m_weapon.m_shared.m_dodgeable,
								m_blockable = __instance.m_weapon.m_shared.m_blockable,
								m_skill = skill,
								m_damage = __instance.m_weapon.GetDamage(),
								m_point = hitPoint,
								m_dir = (hitPoint - position).normalized,
								m_hitCollider = collider
							};
							hitData.SetAttacker(player);
							hitData.m_damage.Modify(__instance.m_damageMultiplier);
							hitData.m_damage.Modify(randomSkillFactor);
							hitData.m_damage.Modify(__instance.GetLevelDamageFactor());
							if (__instance.m_attackChainLevels > 1 && __instance.m_currentAttackCainLevel == __instance.m_attackChainLevels - 1)
							{
								hitData.m_damage.Modify(2f);
								hitData.m_pushForce *= 1.2f;
							}
							player.GetSEMan().ModifyAttack(skill, ref hitData);
							character.Damage(hitData);
						}
					}

					return false;
				}
				return true;
			}
		}
	}
}
