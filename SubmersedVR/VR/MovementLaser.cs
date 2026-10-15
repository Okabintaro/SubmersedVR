using UnityEngine;
namespace SubmersedVR
{
    // Visual for the hand-based movement axis: points exactly where "swim forward" goes.
    // The parent controller's rotation + this object's local pitch offset equal the
    // controllerTransform used by MoveDirectionOverride, independently of the per-tool aim offset.
    // Visible when Settings.ShowMovementLaser is on (hand based movement modes only).
    public class MovementLaser : MonoBehaviour
    {
        static readonly Color LaserColor = new Color(0f, 1f, 0f, 0.9f);
        const float Length = 2.0f;

        public bool isLeftHand;
        LineRenderer lineRenderer;
        GameObject tip;
        float lastPitch = -1f;

        void Start()
        {
            Material newMaterial = new Material(ShaderManager.preloadedShaders.DebugDisplaySolid);
            newMaterial.SetColor(ShaderPropertyID._Color, LaserColor);

            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.material = newMaterial;
            lineRenderer.startColor = LaserColor;
            lineRenderer.endColor = new Color(0f, 1f, 0f, 0.3f);
            lineRenderer.startWidth = 0.004f;
            lineRenderer.endWidth = 0.002f;
            lineRenderer.useWorldSpace = false;
            lineRenderer.SetPosition(0, Vector3.zero);
            lineRenderer.SetPosition(1, new Vector3(0f, 0f, Length));

            tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tip.name = "MovementLaserTip";
            tip.transform.parent = transform;
            tip.transform.localPosition = new Vector3(0f, 0f, Length);
            tip.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
            Destroy(tip.GetComponent<SphereCollider>());
            tip.GetComponent<Renderer>().material = newMaterial;
        }

        void Update()
        {
            if (Settings.HandMovementPitchOffset != lastPitch)
            {
                lastPitch = Settings.HandMovementPitchOffset;
                transform.localEulerAngles = new Vector3(lastPitch, 0f, 0f);
            }

            bool onMovementHand = Settings.HandBasedTurning && (isLeftHand == Settings.LeftHandBasedTurning);
            bool visible = onMovementHand && Settings.ShowMovementLaser;
            if (lineRenderer.enabled != visible)
            {
                lineRenderer.enabled = visible;
                tip.SetActive(visible);
            }
        }
    }
}
