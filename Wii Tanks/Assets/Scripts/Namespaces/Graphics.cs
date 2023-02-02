using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering.HighDefinition;


// This namespace contains static classes, that set up graphics fo various objects
// It exists to eliminate some of the code repetition

namespace Graphics
{
    #region Structs

    // Those structs are used to either send data to the methods in TankGraphics class, or to send data back to the Tank, Tank_SP, or EnemyAI class

    public struct TankGet
    {
        public MeshRenderer tankBody;
        public MeshRenderer turretBody;
        public HDAdditionalLightData light;
        public string color;
    }

    public struct TankSet
    {
        public Material tankMaterial;
        public Material turretMaterial;
        public GameObject explosion;
        public GameObject muzzleFlash;
    }

    public struct Materials
    {
        public Material tankMaterial;
        public Material turretMaterial;
        public HDAdditionalLightData light;
    }

    #endregion

    // There are 3 different abstract classes for tanks (Tank, Tank_SP, EnemyAI)
    // Since all tanks use the same graphics, this class stores methods for all 3 of them, rather to use the same code in 3 different classes
    // Although those are 3 completely different classes, they use similar variables. Because of that, structs above are used to both send dath both from and to methods in this class

    public static class TankGraphics
    {
        private const float LIGHT_INTENSITY = 0.15f;
        private const float LIGHT_APPEARING_SPEED = 0.0025f;
        private const float LIGHT_DISAPPEARING_SPEED = 0.0035f;

        private const float MATERIAL_MIN_VALUE = -0.3f;
        private const float MATERIAL_MAX_VALUE = 0.4f;
        private const float MATERIAL_APPEARING_SPEED = 0.01f;
        private const float MATERIAL_DISAPPEARING_SPEED = 0.01f;


        // This method receives parts of the tank that will be affected (body, turret, light), as well as target coluor
        // It returns appropriately coloured effects, and applied materials

        public static TankSet ChangeTankColours(TankGet tankGet, string gameType)
        {
            TankSet tankSet = new();

            tankGet.tankBody.material = Addressables.LoadAssetAsync<Material>("Animated" + tankGet.color).WaitForCompletion();
            tankSet.tankMaterial = tankGet.tankBody.material;
            tankGet.turretBody.material = Addressables.LoadAssetAsync<Material>("Animated" + tankGet.color).WaitForCompletion();
            tankSet.turretMaterial = tankGet.turretBody.material;
            tankSet.tankMaterial.SetFloat("_CurrentAppearence", MATERIAL_MAX_VALUE);
            tankSet.turretMaterial.SetFloat("_CurrentAppearence", MATERIAL_MAX_VALUE);
            tankGet.tankBody.enabled = true;
            tankGet.turretBody.enabled = true;
            tankGet.light.color = tankSet.tankMaterial.GetColor("_Color01");
            tankGet.light.intensity = 0f;

            switch (gameType)
            {
                case "Multiplayer":
                    tankSet.explosion = Addressables.LoadAssetAsync<GameObject>(tankGet.color + "Explosion").WaitForCompletion();
                    tankSet.muzzleFlash = Addressables.LoadAssetAsync<GameObject>(tankGet.color + "MuzzleFlash").WaitForCompletion();
                    break;
                case "Singleplayer":
                    tankSet.explosion = Addressables.LoadAssetAsync<GameObject>(tankGet.color + "ExplosionSP").WaitForCompletion();
                    tankSet.muzzleFlash = Addressables.LoadAssetAsync<GameObject>(tankGet.color + "MuzzleFlashSP").WaitForCompletion();
                    break;
            }

            return tankSet;
        }

        // Bullets need to change colour in different method, because in Muliplayer it needs to change on server only, while the rest need to change on client only

        public static GameObject ChangeBulletColour(string color, string tankType, string gameType)
        {
            return gameType switch
            {
                "Multiplayer" => Addressables.LoadAssetAsync<GameObject>(color + tankType + "Bullet").WaitForCompletion(),
                "Singleplayer" => Addressables.LoadAssetAsync<GameObject>(color + tankType + "BulletSP").WaitForCompletion(),
                _ => throw new System.NotImplementedException(),
            };
        }

        // Tanks instantiate with values that their material is completely dissolved, and light is at 0
        // This method slowly changes those values, to slowly make the tank appear
        // Tank's body and turret use different instances of the same material, so the values need to change seperately
        // It is important that body appears first, and the turret second

        public static void SpawnAnimation(Materials materials)
        {
            if (materials.turretMaterial.GetFloat("_CurrentAppearence") > MATERIAL_MIN_VALUE)
            {
                if (materials.tankMaterial.GetFloat("_CurrentAppearence") > 0f)
                {
                    materials.tankMaterial.SetFloat("_CurrentAppearence", materials.tankMaterial.GetFloat("_CurrentAppearence") - MATERIAL_APPEARING_SPEED);

                    if (materials.light.intensity < LIGHT_INTENSITY)
                    {
                        materials.light.intensity += LIGHT_APPEARING_SPEED;
                    }
                }
                else
                {
                    if (materials.tankMaterial.GetFloat("_CurrentAppearence") > MATERIAL_MIN_VALUE)
                    {
                        materials.turretMaterial.SetFloat("_CurrentAppearence", materials.turretMaterial.GetFloat("_CurrentAppearence") - MATERIAL_APPEARING_SPEED);
                        materials.tankMaterial.SetFloat("_CurrentAppearence", materials.tankMaterial.GetFloat("_CurrentAppearence") - MATERIAL_APPEARING_SPEED);
                    }
                    else
                    {
                        materials.turretMaterial.SetFloat("_CurrentAppearence", materials.turretMaterial.GetFloat("_CurrentAppearence") - MATERIAL_APPEARING_SPEED);
                    }
                }
            }
        }

        // Opposite of SpawnAnimation method

        public static bool DespawnAnimation(Materials materials)
        {
            if (materials.tankMaterial.GetFloat("_CurrentAppearence") < MATERIAL_MAX_VALUE)
            {
                if (materials.turretMaterial.GetFloat("_CurrentAppearence") < 0f)
                {
                    materials.turretMaterial.SetFloat("_CurrentAppearence", materials.turretMaterial.GetFloat("_CurrentAppearence") + MATERIAL_DISAPPEARING_SPEED);
                }
                else
                {
                    if (materials.turretMaterial.GetFloat("_CurrentAppearence") < MATERIAL_MAX_VALUE)
                    {
                        materials.turretMaterial.SetFloat("_CurrentAppearence", materials.turretMaterial.GetFloat("_CurrentAppearence") + MATERIAL_DISAPPEARING_SPEED);
                        materials.tankMaterial.SetFloat("_CurrentAppearence", materials.tankMaterial.GetFloat("_CurrentAppearence") + MATERIAL_DISAPPEARING_SPEED);
                    }
                    else
                    {
                        materials.tankMaterial.SetFloat("_CurrentAppearence", materials.tankMaterial.GetFloat("_CurrentAppearence") + MATERIAL_DISAPPEARING_SPEED);


                        if (materials.light.intensity > 0)
                        {
                            materials.light.intensity -= LIGHT_DISAPPEARING_SPEED;
                        }
                    }
                }

                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
