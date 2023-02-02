using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering.HighDefinition;


namespace Graphics
{
    public struct TankGet
    {
        public GameObject tankBody;
        public GameObject turretBody;
        public GameObject mainBody;
        public string color;
        public string tankType;
    }

    public struct TankSet
    {
        public Material tankMaterial;
        public Material turretMaterial;
        public GameObject explosion;
        public GameObject muzzleFlash;
        public GameObject bullet;
    }

    public struct Materials
    {
        public Material tankMaterial;
        public Material turretMaterial;
        public GameObject mainBody;
    }


    public static class TankGraphics
    {
        private const float LIGHT_INTENSITY = 0.15f;
        private const float LIGHT_APPEARING_SPEED = 0.0025f;
        private const float LIGHT_DISAPPEARING_SPEED = 0.0035f;

        private const float MATERIAL_MIN_VALUE = -0.3f;
        private const float MATERIAL_MAX_VALUE = 0.4f;
        private const float MATERIAL_APPEARING_SPEED = 0.01f;
        private const float MATERIAL_DISAPPEARING_SPEED = 0.01f;



        public static TankSet ChangeTankColours(TankGet tankGet, string gameType)
        {
            TankSet tankSet = new();

            tankGet.tankBody.GetComponent<MeshRenderer>().material = Addressables.LoadAssetAsync<Material>("Animated" + tankGet.color).WaitForCompletion();
            tankSet.tankMaterial = tankGet.tankBody.GetComponent<MeshRenderer>().material;
            tankGet.turretBody.GetComponent<MeshRenderer>().material = Addressables.LoadAssetAsync<Material>("Animated" + tankGet.color).WaitForCompletion();
            tankSet.turretMaterial = tankGet.turretBody.GetComponent<MeshRenderer>().material;
            tankSet.tankMaterial.SetFloat("_CurrentAppearence", MATERIAL_MAX_VALUE);
            tankSet.turretMaterial.SetFloat("_CurrentAppearence", MATERIAL_MAX_VALUE);
            tankGet.tankBody.GetComponent<MeshRenderer>().enabled = true;
            tankGet.turretBody.GetComponent<MeshRenderer>().enabled = true;
            tankGet.mainBody.GetComponent<HDAdditionalLightData>().color = tankSet.tankMaterial.GetColor("_Color01");
            tankGet.mainBody.GetComponent<HDAdditionalLightData>().intensity = 0f;

            switch (gameType)
            {
                case "Multiplayer":
                    tankSet.explosion = Addressables.LoadAssetAsync<GameObject>(tankGet.color + "Explosion").WaitForCompletion();
                    tankSet.muzzleFlash = Addressables.LoadAssetAsync<GameObject>(tankGet.color + "MuzzleFlash").WaitForCompletion();
                    tankSet.bullet = Addressables.LoadAssetAsync<GameObject>(tankGet.color + tankGet.tankType + "Bullet").WaitForCompletion();
                    break;
                case "Singleplayer":
                    tankSet.explosion = Addressables.LoadAssetAsync<GameObject>(tankGet.color + "ExplosionSP").WaitForCompletion();
                    tankSet.muzzleFlash = Addressables.LoadAssetAsync<GameObject>(tankGet.color + "MuzzleFlashSP").WaitForCompletion();
                    tankSet.bullet = Addressables.LoadAssetAsync<GameObject>(tankGet.color + tankGet.tankType + "BulletSP").WaitForCompletion();
                    break;
            }

            return tankSet;
        }
        

        public static void SpawnAnimation(Materials materials)
        {
            if (materials.turretMaterial.GetFloat("_CurrentAppearence") > MATERIAL_MIN_VALUE)
            {
                if (materials.tankMaterial.GetFloat("_CurrentAppearence") > 0f)
                {
                    materials.tankMaterial.SetFloat("_CurrentAppearence", materials.tankMaterial.GetFloat("_CurrentAppearence") - MATERIAL_APPEARING_SPEED);

                    if (materials.mainBody.GetComponent<HDAdditionalLightData>().intensity < LIGHT_INTENSITY)
                    {
                        materials.mainBody.GetComponent<HDAdditionalLightData>().intensity += LIGHT_APPEARING_SPEED;
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


                        if (materials.mainBody.GetComponent<HDAdditionalLightData>().intensity > 0)
                        {
                            materials.mainBody.GetComponent<HDAdditionalLightData>().intensity -= LIGHT_DISAPPEARING_SPEED;
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
