using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;

namespace CarRacingGame3d
{
    public class ItemShield : BaseItem
    {
        new Renderer renderer;
        [SerializeField] AnimationCurve displacementCurve;
        [SerializeField] float displacementMagnitude;
        [SerializeField] float lerpSpeed;
        [SerializeField] float disolveSpeed;
        bool shieldOn = false;
        float shieldDuration = 5;
        Coroutine disolveCoroutine;
        
        private CarController carController;

        // Start is called before the first frame update
        void Start()
        {
            renderer = GetComponentInChildren<Renderer>();
        }

        public override void UseItem(CarController car)
        {
            car.Immunity = true;
            carController = car;
            OpenCloseShield();
        }

        private void OpenCloseShield()
        {
            shieldOn = !shieldOn;
            float target = shieldOn ? 0 : 1;
            if (disolveCoroutine != null)
            {
                StopCoroutine(disolveCoroutine);
            }
            disolveCoroutine = StartCoroutine(CoroutineDisolveShield(target));
        }

        private IEnumerator CountDown(float duration)
        {
            yield return new WaitForSeconds(duration);
            OpenCloseShield();
        }

        private IEnumerator CoroutineDisolveShield(float target)
        {
            float start = renderer.material.GetFloat("_Disolve");
            float lerp = 0;
            while (lerp < 1)
            {
                renderer.material.SetFloat("_Disolve", Mathf.Lerp(start, target, lerp));
                lerp += Time.deltaTime * disolveSpeed;
                yield return null;
            }

            if (target == 1)
            {
                carController.Immunity = false;
                Destroy(gameObject);
            } else
            {
                StartCoroutine(CountDown(shieldDuration));
            }
        }
    }
}
