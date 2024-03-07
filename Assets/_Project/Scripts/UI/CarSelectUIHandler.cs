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

        public void SetupCar(CarData carData)
        {
            car = carData.CarPrefab;
        }

        public void StartCarEntranceAnimation(bool isAppearingOnRightSide)
        {
            if (isAppearingOnRightSide)
            {
                animator.Play("UI_Appear");
            }
            else
            {
                animator.Play("UI_Disappear");
            }
        }

        public void StartCarExitAnimation(bool isExitingOnRightSide)
        {
            if (isExitingOnRightSide)
            {
                animator.Play("UI_Appear");
            }
            else
            {
                animator.Play("UI_Disappear");
            }
        }

        public void OnExitAnimationCompleted()
        {
            Destroy(gameObject);
        }
    }
}
