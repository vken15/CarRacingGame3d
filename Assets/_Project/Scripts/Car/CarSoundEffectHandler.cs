using UnityEngine;

namespace CarRacingGame3d
{
    public class CarSoundEffectHandler : MonoBehaviour
    {
        [Header("Audio sources")]
        [SerializeField] private AudioSource tiresScreechingAudioSource;
        [SerializeField] private AudioSource engineAudioSource;
        [SerializeField] private AudioSource carHitAudioSource;
        [SerializeField] private AudioSource carLandingAudioSource;
        private float desiredEnginePitch = 0.5f;
        private float tireScreechPitch = 0.5f;
        private CarController carController;

        private void Awake()
        {
            carController = GetComponentInParent<CarController>();
        }

        // Update is called once per frame
        private void Update()
        {
            UpdateEngineSoundEffect();
            UpdateTiresScreechingSoundEffect();
        }

        private void UpdateEngineSoundEffect()
        {
            float velocityMagnitude = carController.GetVelocityMagnitude();
            float desiredEngineVolume = velocityMagnitude * 0.05f;

            desiredEngineVolume = Mathf.Clamp(desiredEngineVolume, 0.2f, 1.0f);

            engineAudioSource.volume = Mathf.Lerp(engineAudioSource.volume, desiredEngineVolume, Time.deltaTime * 10);

            desiredEnginePitch = velocityMagnitude * 0.2f;
            desiredEnginePitch = Mathf.Clamp(desiredEnginePitch, 0.5f, 2f);
            engineAudioSource.pitch = Mathf.Lerp(engineAudioSource.pitch, desiredEnginePitch, Time.deltaTime * 1.5f);
        }

        private void UpdateTiresScreechingSoundEffect()
        {
            if (carController.IsTireScreeching(out float lateralVelocity, out bool isBraking))
            {
                if (isBraking)
                {
                    tiresScreechingAudioSource.volume = Mathf.Lerp(tiresScreechingAudioSource.volume, 1.0f, Time.deltaTime * 10);
                    tireScreechPitch = Mathf.Lerp(tireScreechPitch, 0.5f, Time.deltaTime * 10);
                }
                else
                {
                    tiresScreechingAudioSource.volume = Mathf.Abs(lateralVelocity) * 0.05f;
                    tireScreechPitch = Mathf.Abs(lateralVelocity) * 0.1f;
                }
            }
            else tiresScreechingAudioSource.volume = Mathf.Lerp(tiresScreechingAudioSource.volume, 0, Time.deltaTime * 10);
        }

        public void PlayLandingSoundEffect()
        {
            carLandingAudioSource.Play();
        }

        private void OnCollisionEnter(Collision collision)
        {
            float relativeVelocity = collision.relativeVelocity.magnitude;

            float volume = relativeVelocity * 0.03f;

            carHitAudioSource.pitch = Random.Range(0.95f, 1.05f);
            carHitAudioSource.volume = volume;

            if (!carHitAudioSource.isPlaying)
                carHitAudioSource.Play();
        }
    }
}
