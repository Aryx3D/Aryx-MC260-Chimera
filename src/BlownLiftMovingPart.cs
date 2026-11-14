using UnityEngine;

namespace Aryx_MC260_Chimera
{
    public class BlownLiftMovingPart : MonoBehaviour
    {
        [SerializeField]
        private bool move;

        [SerializeField]
        private bool rotate;

        [SerializeField]
        private Transform animatedTransform;

        [SerializeField]
        private Vector3 positionRetracted;

        [SerializeField]
        private Vector3 positionDeployed;

        [SerializeField]
        private Vector3 anglesRetracted;

        [SerializeField]
        private Vector3 anglesDeployed;

        public Transform AnimatedTransform => animatedTransform;

        public void Animate(float deployedAmount)
        {
            if (animatedTransform == null)
                return;
            deployedAmount = Mathf.Clamp01(deployedAmount);
            if (move)
                animatedTransform.localPosition = Vector3.Lerp(positionRetracted, positionDeployed,deployedAmount);
            if (rotate)
                animatedTransform.localEulerAngles = Vector3.Lerp(anglesRetracted, anglesDeployed, deployedAmount);       
        }
    }
}