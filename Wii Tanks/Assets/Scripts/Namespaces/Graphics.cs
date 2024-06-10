using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;


// This namespace contains classes, that set up graphics fo various objects
// It exists to eliminate some of the code repetition

namespace Graphics
{
    // There are 3 different abstract classes for tanks (Tank, Tank_SP, EnemyAI)
    // Since all tanks use the same graphics, this class stores methods for all 3 of them, rather to use the same code in 3 different classes
    // Although those are 3 completely different classes, they use similar variables.

    public sealed class TankGraphics
    {
        private const float LIGHT_INTENSITY = 0.15f;
        private const float LIGHT_APPEARING_SPEED = 0.0025f;
        private const float LIGHT_DISAPPEARING_SPEED = 0.0035f;

        private const float MATERIAL_MIN_VALUE = -0.50f;
        private const float MATERIAL_MAX_VALUE = 0.4f;
        private const float MATERIAL_APPEARING_SPEED = 0.01f;
        private const float MATERIAL_DISAPPEARING_SPEED = 0.01f;
        private readonly Material leftTrackMaterial;

        private readonly Light light;
        private readonly Material rightTrackMaterial;
        private readonly Material tankMaterial;
        private readonly Material turretMaterial;


        public TankGraphics(string color, Light lightData, MeshRenderer tankBody, MeshRenderer tankTurret,
            MeshRenderer leftTrack, MeshRenderer rightTrack)
        {
            light = lightData;

            tankMaterial = ApplyMaterial(tankBody, color);
            turretMaterial = ApplyMaterial(tankTurret, color);
            leftTrackMaterial = ApplyMaterial(leftTrack, "Track");
            rightTrackMaterial = ApplyMaterial(rightTrack, "Track");
            tankMaterial.SetFloat("_CurrentAppearence", MATERIAL_MAX_VALUE);
            turretMaterial.SetFloat("_CurrentAppearence", MATERIAL_MAX_VALUE);
            leftTrackMaterial.SetFloat("_CurrentAppearence", MATERIAL_MAX_VALUE);
            rightTrackMaterial.SetFloat("_CurrentAppearence", MATERIAL_MAX_VALUE);
            tankBody.enabled = true;
            tankTurret.enabled = true;
            lightData.color = tankMaterial.GetColor("_Color01");
            lightData.intensity = 0f;
        }


        private Material ApplyMaterial(MeshRenderer tankPart, string color)
        {
            tankPart.material = Addressables.LoadAssetAsync<Material>("Animated" + color).WaitForCompletion();
            return tankPart.material;
        }


        /// <summary>
        ///     Changes colors of prefabs used by this tank
        /// </summary>
        /// <param name="gameType">Specifies if game is played in singleplayer or multiplayer</param>
        /// <param name="tankType">Type of a tank</param>
        /// <returns>Prefabs that were effected</returns>
        public static Dictionary<string, GameObject> ChangePrefabsColours(string color, string gameType,
            string tankType)
        {
            Dictionary<string, GameObject> prefabs = new();

            switch (gameType)
            {
                case "Multiplayer":
                    prefabs["Explosion"] =
                        Addressables.LoadAssetAsync<GameObject>(color + "Explosion").WaitForCompletion();
                    prefabs["MuzzleFlash"] = Addressables.LoadAssetAsync<GameObject>(color + "MuzzleFlash")
                        .WaitForCompletion();
                    prefabs["Bullet"] = Addressables.LoadAssetAsync<GameObject>(color + tankType + "Bullet")
                        .WaitForCompletion();
                    break;
                case "Singleplayer":
                    prefabs["Explosion"] = Addressables.LoadAssetAsync<GameObject>(color + "ExplosionSP")
                        .WaitForCompletion();
                    prefabs["MuzzleFlash"] = Addressables.LoadAssetAsync<GameObject>(color + "MuzzleFlashSP")
                        .WaitForCompletion();
                    prefabs["Bullet"] = Addressables.LoadAssetAsync<GameObject>(color + tankType + "BulletSP")
                        .WaitForCompletion();
                    break;
                default:
                    throw new NotImplementedException();
            }

            return prefabs;
        }

        /// <summary>
        ///     Plays animation for spawning. Main body appears before turret
        /// </summary>
        public void SpawnAnimation()
        {
            if (turretMaterial.GetFloat("_CurrentAppearence") > MATERIAL_MIN_VALUE)
            {
                if (tankMaterial.GetFloat("_CurrentAppearence") > 0f)
                {
                    tankMaterial.SetFloat("_CurrentAppearence",
                        tankMaterial.GetFloat("_CurrentAppearence") - MATERIAL_APPEARING_SPEED);
                    leftTrackMaterial.SetFloat("_CurrentAppearence",
                        tankMaterial.GetFloat("_CurrentAppearence") - MATERIAL_APPEARING_SPEED);
                    rightTrackMaterial.SetFloat("_CurrentAppearence",
                        tankMaterial.GetFloat("_CurrentAppearence") - MATERIAL_APPEARING_SPEED);

                    if (light.intensity < LIGHT_INTENSITY) light.intensity += LIGHT_APPEARING_SPEED;

                    return;
                }

                if (tankMaterial.GetFloat("_CurrentAppearence") > MATERIAL_MIN_VALUE)
                {
                    turretMaterial.SetFloat("_CurrentAppearence",
                        turretMaterial.GetFloat("_CurrentAppearence") - MATERIAL_APPEARING_SPEED);
                    tankMaterial.SetFloat("_CurrentAppearence",
                        tankMaterial.GetFloat("_CurrentAppearence") - MATERIAL_APPEARING_SPEED);
                    leftTrackMaterial.SetFloat("_CurrentAppearence",
                        tankMaterial.GetFloat("_CurrentAppearence") - MATERIAL_APPEARING_SPEED);
                    rightTrackMaterial.SetFloat("_CurrentAppearence",
                        tankMaterial.GetFloat("_CurrentAppearence") - MATERIAL_APPEARING_SPEED);
                    return;
                }

                turretMaterial.SetFloat("_CurrentAppearence",
                    turretMaterial.GetFloat("_CurrentAppearence") - MATERIAL_APPEARING_SPEED);
            }
        }

        /// <summary>
        ///     Plays animation for despawning. Turret despawns before main body
        /// </summary>
        /// <returns>A bool that informs whether the animation has finished or not</returns>
        public bool DespawnAnimation()
        {
            if (tankMaterial.GetFloat("_CurrentAppearence") < MATERIAL_MAX_VALUE)
            {
                if (turretMaterial.GetFloat("_CurrentAppearence") < 0f)
                {
                    turretMaterial.SetFloat("_CurrentAppearence",
                        turretMaterial.GetFloat("_CurrentAppearence") + MATERIAL_DISAPPEARING_SPEED);
                    return false;
                }

                if (turretMaterial.GetFloat("_CurrentAppearence") < MATERIAL_MAX_VALUE)
                {
                    turretMaterial.SetFloat("_CurrentAppearence",
                        turretMaterial.GetFloat("_CurrentAppearence") + MATERIAL_DISAPPEARING_SPEED);
                    tankMaterial.SetFloat("_CurrentAppearence",
                        tankMaterial.GetFloat("_CurrentAppearence") + MATERIAL_DISAPPEARING_SPEED);
                    leftTrackMaterial.SetFloat("_CurrentAppearence",
                        tankMaterial.GetFloat("_CurrentAppearence") + MATERIAL_DISAPPEARING_SPEED);
                    rightTrackMaterial.SetFloat("_CurrentAppearence",
                        tankMaterial.GetFloat("_CurrentAppearence") + MATERIAL_DISAPPEARING_SPEED);
                    return false;
                }

                tankMaterial.SetFloat("_CurrentAppearence",
                    tankMaterial.GetFloat("_CurrentAppearence") + MATERIAL_DISAPPEARING_SPEED);
                leftTrackMaterial.SetFloat("_CurrentAppearence",
                    tankMaterial.GetFloat("_CurrentAppearence") + MATERIAL_DISAPPEARING_SPEED);
                rightTrackMaterial.SetFloat("_CurrentAppearence",
                    tankMaterial.GetFloat("_CurrentAppearence") + MATERIAL_DISAPPEARING_SPEED);

                if (light.intensity > 0) light.intensity -= LIGHT_DISAPPEARING_SPEED;

                return false;
            }

            return true;
        }
    }


    public static class BulletGraphics
    {
        private const float LIGHT_INTENSITY = 0.5f;
        private const float LIGHT_DISAPPEARING_SPEED = 0.015f;


        public static void SetBulletLightIntensity(Light light)
        {
            light.intensity = LIGHT_INTENSITY;
        }

        public static void DecreaseBulletLightIntensity(Light light)
        {
            light.intensity -= LIGHT_DISAPPEARING_SPEED;
        }
    }
}