using System.Collections;
using UnityEngine;

namespace CarRacingGame3d
{
    public class ItemOil : BaseItem
    {
        [SerializeField] private new BoxCollider collider;
        [SerializeField] private new ParticleSystem particleSystem;
        [SerializeField] private float slipMulti = 1.0f;
        [SerializeField] private ParticleSystem oilBallParticleSystem;
        [SerializeField] private AnimationCurve speedCurve;
        [SerializeField] private float speed;
        [SerializeField] private ParticleSystem spillPrefab;

        public override void UseItem(CarController car)
        {
            Vector3 potition = transform.position.Add(y: 15);
            Vector3 forward = transform.forward * -1;
            gameObject.transform.parent = null;
            transform.position = potition;
            Ray ray = new(transform.position, forward.With(y: -1));
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                StopAllCoroutines();
                StartCoroutine(CoroutineThrow(hit.point));
            }
            oilBallParticleSystem.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (particleSystem == null)
            {
                Destroy(gameObject);
            }
        }

        IEnumerator CoroutineThrow(Vector3 target)
        {
            float lerp = 0;
            Vector3 startPos = transform.position;
            while (lerp < 1)
            {
                transform.position = Vector3.Lerp(startPos, target, speedCurve.Evaluate(lerp));
                float magnitude = (transform.position - target).magnitude;
                if (magnitude < 0.4f)
                {
                    break;
                }
                lerp += Time.deltaTime * speed;
                yield return null;
            }
            oilBallParticleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            Vector3 forward = target - startPos;
            forward.y = 0;
            transform.forward = forward;
            collider.enabled = true;
            var particles = GetComponentsInChildren<ParticleSystem>();
            foreach (var particle in particles)
            {
                particle.Play();
            }
            if (Vector3.Angle(startPos - target, Vector3.up) > 30)
            {
                ParticleSystem spill = Instantiate(spillPrefab, target, Quaternion.identity);
                spill.transform.forward = forward;
            }
            Destroy(oilBallParticleSystem, 0.5f);
        }

        private void OnTriggerEnter(Collider collision)
        {
            if (collision.CompareTag("CarBody"))
            {
                if (collision.GetComponentInParent<CarController>().immunity == false)
                    collision.GetComponentInParent<Rigidbody>().angularVelocity += new Vector3(0, slipMulti * 45, 0);
            }
        }

        private void OnParticleSystemStopped()
        {
            Destroy(gameObject);
        }
    }
}
