using UnityEngine;

namespace CarRacingGame3d
{
    public class ItemOil : BaseItem
    {
        [SerializeField] private new BoxCollider collider;
        [SerializeField] private new ParticleSystem particleSystem;
        [SerializeField] private float slipMulti = 1.0f;

        public override void UseItem(CarController car)
        {
            gameObject.transform.parent = null;
            gameObject.transform.SetPositionAndRotation(new Vector3(car.transform.position.x,
                car.transform.position.y + 10,
                car.transform.position.z), car.transform.rotation);
            collider.enabled = true;
            var particles = GetComponentsInChildren<ParticleSystem>();
            foreach (var particle in particles)
            {
                particle.Play();
            }
        }

        private void Update()
        {
            if (particleSystem == null)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider collision)
        {
            if (collision.CompareTag("CarBody"))
            {
                if (collision.GetComponentInParent<CarController>().Immunity == false)
                    collision.GetComponentInParent<Rigidbody>().angularVelocity += new Vector3(0, slipMulti * 45, 0);
            }
        }

        private void OnParticleSystemStopped()
        {
            Destroy(gameObject);
        }
    }
}
