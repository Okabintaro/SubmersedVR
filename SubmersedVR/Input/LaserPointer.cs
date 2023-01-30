using UnityEngine;
namespace SubmersedVR
{
    // Creates a laser pointer 
    // Attach it to a new gameobject to make a laserpointer
    public class LaserPointer : MonoBehaviour
    {
        // Properties
        public Color color;
        public static float thickness = 0.005f;
        public float defaultLength = 5.0f;

        // "Prefab Objects/Components"
        public GameObject pointerDot;

        public LineRenderer lineRenderer;
        public Camera eventCamera;
        public FPSInputModule inputModule;

        public bool doWorldRaycasts = true;
        public bool useUILayer = false;
        private float worldTargetTime;
        private GameObject worldTarget;
        private float worldTargetDistance;

        public bool disableAfterCreation = false;

        void Start()
        {
            Material newMaterial = new Material(ShaderManager.preloadedShaders.DebugDisplaySolid);
            newMaterial.SetColor(ShaderPropertyID._Color, Color.cyan);

            // Setup camera used for raycasting on canvases
            eventCamera = gameObject.AddComponent<Camera>();
            eventCamera.stereoTargetEye = StereoTargetEyeMask.None;
            eventCamera.nearClipPlane = 0.01f;
            eventCamera.farClipPlane = 10.0f;
            eventCamera.fieldOfView = 1.0f;
            eventCamera.enabled = false;

            // Setup PointerDot at the end
            pointerDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pointerDot.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
            pointerDot.transform.parent = transform;
            Destroy(pointerDot.GetComponent<SphereCollider>());
            pointerDot.GetComponent<Renderer>().material = newMaterial;

            // Setup Line Renderer
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.material = newMaterial;
            lineRenderer.startColor = new Color(0f, 1f, 1f, 1f);
            lineRenderer.endColor = new Color(1f, 0f, 0f, 1f);
            lineRenderer.startWidth = 0.004f;
            lineRenderer.endWidth = 0.005f;

            if (useUILayer)
            {
                pointerDot.layer = LayerMask.NameToLayer("UI");
                lineRenderer.gameObject.layer = LayerMask.NameToLayer("UI");
            }
            if (disableAfterCreation)
            {
                Show(false, false);
            }
        }

        public void SetEnd(Vector3 end)
        {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, end);
            pointerDot.transform.position = end;
        }

        private void Show(bool on, bool showDot)
        {
            this.lineRenderer.enabled = on;
            this.pointerDot.SetActive(showDot);
        }

        // TODO: This is out of scope, refactor/cleanup
        public void SetWorldTarget(GameObject worldTarget, float worldTargetDistance)
        {
            this.worldTargetTime = Time.unscaledTime;
            this.worldTarget = worldTarget;
            this.worldTargetDistance = worldTargetDistance;
        }

        protected readonly float hideTimeout = 0.5f;

        void Update()
        {
            // TODO: I know this is shitty, gotta refactor this
            if (disableAfterCreation)
            {
                return;
            }

            if (inputModule == null)
            {
                return;
            }
            var uiHitRelativeTime = Time.unscaledTime - inputModule.lastValidRaycastTime;
            var uiHitDistance = inputModule.lastRaycastResult.distance;
            var uiWasHit = uiHitDistance != 0 && uiHitRelativeTime < hideTimeout;

            var worldHitRelativeTime = Time.unscaledTime - worldTargetTime;
            var worldWasHit = doWorldRaycasts && worldTarget != null && worldHitRelativeTime < hideTimeout;

            float length = defaultLength;
            if (uiWasHit)
            {
                // We hit UI
                pointerDot.SetActive(false);
                length = uiHitDistance;
                // TODO: Hide World Laserpointer similar to UI one after a timeout
            }
            else if (worldWasHit)
            {
                pointerDot.SetActive(true);
                length = this.worldTargetDistance;
            }
            Show(Settings.AlwaysShowLaserPointer | uiWasHit | worldWasHit, worldWasHit);

            Vector3 endPos = transform.position + (transform.forward * length);

            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, endPos);
            pointerDot.transform.position = endPos;
        }
    }
}




