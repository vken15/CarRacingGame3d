using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CarRacingGame3d
{
    public class CarSelectUIHandler : MonoBehaviour
    {
        private GameObject car;
        private Animator animator = null;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
        }

        public void SetupCar(CarData carData, Transform transform)
        {
            car = Instantiate(carData.CarSelectPrefab, transform);
            car.transform.SetParent(gameObject.transform);
        }

        public void StartCarEntranceAnimation(bool isAppearingOnRightSide)
        {
            if (isAppearingOnRightSide)
                animator.Play("UI_Appear");
            else
                animator.Play("UI_Appear_Left");
        }

        public void StartCarExitAnimation(bool isExitingOnRightSide)
        {
            if (isExitingOnRightSide)
                animator.Play("UI_Disappear");
            else
                animator.Play("UI_Disappear_Left");
        }

        public void OnExitAnimationCompleted()
        {
            Destroy(gameObject);
        }
    }
}
