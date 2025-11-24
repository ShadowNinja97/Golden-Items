using UnityEngine;
using Photon.Pun;

namespace GoldItems
{
    public class GoldHealthPack : MonoBehaviour
    {
        public int healAmount;

        private ItemToggle itemToggle;
        private ItemEquippable itemEquippable;
        private ItemAttributes itemAttributes;
        private PhotonView photonView;
        private PhysGrabObject physGrabObject;

        [Space]
        public ParticleSystem[] particles;
        public ParticleSystem[] rejectParticles;

        [Space]
        public PropLight propLight;
        public AnimationCurve lightIntensityCurve;
        private float lightIntensityLerp;

        public MeshRenderer mesh;
        private Material material;
        private Color materialEmissionOriginal;
        private readonly int materialPropertyEmission = Shader.PropertyToID("_EmissionColor");

        [Space]
        public Sound soundUse;
        public Sound soundReject;

        // "used" now means "used this *level*"
        private bool used;
        private int lastUsedLevelStep = int.MinValue;

        private void Start()
        {
            itemToggle = GetComponent<ItemToggle>();
            itemEquippable = GetComponent<ItemEquippable>();
            itemAttributes = GetComponent<ItemAttributes>();
            photonView = GetComponent<PhotonView>();
            physGrabObject = GetComponent<PhysGrabObject>();

            material = mesh != null ? mesh.material : null;
            if (material != null)
            {
                materialEmissionOriginal = material.GetColor(materialPropertyEmission);
            }
        }

        private void Update()
        {
            // No healing in shop (same as vanilla)
            if (SemiFunc.RunIsShop() || RunManager.instance.levelIsShop)
                return;

            // "Level step" = how many levels have been completed so far this run
            int currentStep = RunManager.instance != null
                ? RunManager.instance.levelsCompleted
                : 0;

            // Refresh per *level*: when we detect a level change, reset "used"
            if (used && currentStep != lastUsedLevelStep)
            {
                used = false;
                lightIntensityLerp = 0f;

                // Restore light & emission to original brightness
                if (propLight != null && propLight.lightComponent != null)
                {
                    propLight.lightComponent.intensity = propLight.originalIntensity;
                }

                if (material != null)
                {
                    material.SetColor(materialPropertyEmission, materialEmissionOriginal);
                }

                // Re-enable UI when a new level starts
                if (itemAttributes != null)
                {
                    itemAttributes.DisableUI(_disable: false);
                }
            }

            LightLogic();

            // Only master or singleplayer drives healing logic
            if (!SemiFunc.IsMasterClientOrSingleplayer() || itemToggle == null || !itemToggle.toggleState || used)
                return;

            PlayerAvatar playerAvatar = SemiFunc.PlayerAvatarGetFromPhotonID(itemToggle.playerTogglePhotonID);
            if (!playerAvatar)
                return;

            // If already at full health, reject
            if (playerAvatar.playerHealth.health >= playerAvatar.playerHealth.maxHealth)
            {
                if (SemiFunc.IsMultiplayer())
                {
                    photonView.RPC("RejectRPC", RpcTarget.All);
                }
                else
                {
                    RejectRPC();
                }

                itemToggle.ToggleItem(toggle: false);
                if (physGrabObject != null && physGrabObject.rb != null)
                {
                    physGrabObject.rb.AddForce(Vector3.up * 2f, ForceMode.Impulse);
                    physGrabObject.rb.AddTorque(-physGrabObject.transform.right * 0.05f, ForceMode.Impulse);
                }
            }
            else
            {
                // Heal as usual
                playerAvatar.playerHealth.HealOther(healAmount, effect: true);

                if (physGrabObject != null && physGrabObject.impactDetector != null)
                {
                    physGrabObject.impactDetector.indestructibleBreakEffects = true;
                }

                if (SemiFunc.IsMultiplayer())
                {
                    photonView.RPC("UsedRPC", RpcTarget.All);
                }
                else
                {
                    UsedRPC();
                }
            }
        }

        private void LightLogic()
        {
            // After use in this level, fade out light/emission like the original
            if (!used || lightIntensityLerp >= 1f)
                return;

            lightIntensityLerp += Time.deltaTime;

            float eval = lightIntensityCurve != null
                ? lightIntensityCurve.Evaluate(lightIntensityLerp)
                : 1f;

            if (propLight != null && propLight.lightComponent != null)
            {
                propLight.lightComponent.intensity = eval;
                propLight.originalIntensity = propLight.lightComponent.intensity;
            }

            if (material != null)
            {
                material.SetColor(
                    materialPropertyEmission,
                    Color.Lerp(Color.black, materialEmissionOriginal, eval)
                );
            }
        }

        [PunRPC]
        private void UsedRPC(PhotonMessageInfo _info = default)
        {
            if (SemiFunc.MasterOnlyRPC(_info))
            {
                GameDirector.instance.CameraImpact.ShakeDistance(
                    5f, 1f, 6f, transform.position, 0.2f
                );

                if (itemToggle != null)
                    itemToggle.ToggleItem(toggle: false);

                if (itemAttributes != null)
                    itemAttributes.DisableUI(_disable: true);

                if (particles != null)
                {
                    foreach (var p in particles)
                    {
                        if (p != null) p.Play();
                    }
                }

                if (soundUse != null)
                    soundUse.Play(transform.position);

                used = true;
                if (RunManager.instance != null)
                    lastUsedLevelStep = RunManager.instance.levelsCompleted;
            }
        }

        [PunRPC]
        private void RejectRPC(PhotonMessageInfo _info = default)
        {
            if (SemiFunc.MasterOnlyRPC(_info))
            {
                PlayerAvatar playerAvatar = SemiFunc.PlayerAvatarGetFromPhotonID(itemToggle.playerTogglePhotonID);
                if (playerAvatar != null && playerAvatar.isLocal)
                {
                    playerAvatar.physGrabber.ReleaseObjectRPC(
                        physGrabEnded: false,
                        1f,
                        photonView.ViewID
                    );
                }

                if (rejectParticles != null)
                {
                    foreach (var p in rejectParticles)
                    {
                        if (p != null) p.Play();
                    }
                }

                GameDirector.instance.CameraImpact.ShakeDistance(
                    5f, 1f, 6f, transform.position, 0.2f
                );

                if (soundReject != null)
                    soundReject.Play(transform.position);
            }
        }

        public void OnDestroy()
        {
            if (particles != null)
            {
                foreach (ParticleSystem ps in particles)
                {
                    if (ps && ps.isPlaying)
                    {
                        ps.transform.SetParent(null);
                        var main = ps.main;
                        main.stopAction = ParticleSystemStopAction.Destroy;
                    }
                }
            }

            if (rejectParticles != null)
            {
                foreach (ParticleSystem ps in rejectParticles)
                {
                    if (ps && ps.isPlaying)
                    {
                        ps.transform.SetParent(null);
                        var main = ps.main;
                        main.stopAction = ParticleSystemStopAction.Destroy;
                    }
                }
            }
        }
    }
}
