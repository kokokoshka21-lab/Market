using UnityEngine;

namespace MarketShopandRetailSystem
{
    public class TargetPointer : MonoBehaviour
    {
        public Texture arrow;
        public float size = 30;
        public GameObject PointedTarget;
        public bool hoverOnScreen = true;
        public float distanceAbove = 20;
        public float blindSpot = 0.5f;
        public float hoverAngle = 270;

        private float xCenter;
        private float yCenter;
        private float halfSize;
        private float screenSlope;
        private bool errorless = false;
        private Camera cam;
        public static TargetPointer Instance;

        private void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            if (PointedTarget != null)
            {
                var theCam = this.gameObject.GetComponent<Camera>();

                if (this.gameObject.GetComponent<Camera>())
                {
                    cam = theCam;
                    if (arrow != null)
                    {
                        errorless = true;
                    }
                    else
                    {
                        errorless = false;
                    }
                }
                else
                {
                    errorless = false;
                }
            }
        }

        void OnGUI()
        {
            if (PointedTarget == null) return;


            if (PointedTarget != null && Time.timeScale != 0)
            {
                var theCam = this.gameObject.GetComponent<Camera>();

                if (this.gameObject.GetComponent<Camera>())
                {
                    cam = theCam;
                    if (arrow != null)
                    {
                        errorless = true;
                    }
                    else
                    {
                        errorless = false;
                    }
                }
                else
                {
                    errorless = false;
                }
            }
            if (Event.current.type.Equals(EventType.Repaint) && errorless)
            {
                xCenter = cam.pixelWidth / 2;
                yCenter = cam.pixelHeight / 2;
                screenSlope = cam.pixelHeight / cam.pixelWidth;
                halfSize = size / 2;

                float angle = hoverAngle - 180;
                float rad = angle * (Mathf.PI / 180);
                Vector3 arrowPos = cam.transform.right * Mathf.Cos(rad) + cam.transform.up * Mathf.Sin(rad);
                Vector3 worldPos = PointedTarget.transform.position + (arrowPos * distanceAbove);
                Vector3 pos = cam.WorldToViewportPoint(worldPos);

                if (pos.z < 0)
                {
                    pos.x *= -1;
                    pos.y *= -1;
                }

                if (pos.z > 0 || (pos.z < 0 && (pos.x > .5 + (blindSpot / 2) || pos.x < .5 - (blindSpot / 2))
                        && (pos.y < .5 - (blindSpot / 2) || pos.y > .5 + (blindSpot / 2))))
                {
                    var newX = pos.x * cam.pixelWidth;
                    var newY = cam.pixelHeight - pos.y * cam.pixelHeight;
                    if (pos.z < 0 || (newY < 0 || newY > cam.pixelHeight || newX < 0 || newX > cam.pixelWidth))
                    {
                        float a = CalculateAngle(cam.pixelWidth / 2, cam.pixelHeight / 2, newX, newY);
                        Vector2 coord = ProjectToEdge(newX, newY);
                        GUIUtility.RotateAroundPivot(a, coord);
                        Graphics.DrawTexture(new Rect(coord.x - halfSize, coord.y - halfSize, size, size), arrow);
                        GUIUtility.RotateAroundPivot(-a, coord);
                    }
                    else if (hoverOnScreen)
                    {
                        float nh = Mathf.Sin(rad) * size;
                        float nw = Mathf.Cos(rad) * size;
                        GUIUtility.RotateAroundPivot(-angle + 180, new Vector2(newX + nw, newY - nh));
                        Graphics.DrawTexture(new Rect(newX + nw, newY - nh - halfSize, size, size), arrow, null);
                        GUIUtility.RotateAroundPivot(angle - 180, new Vector2(newX + nw, newY - nh));
                    }
                }
            }
        }

        float CalculateAngle(float x1, float y1, float x2, float y2)
        {
            var xDiff = x2 - x1;
            var yDiff = y2 - y1;
            var rad = Mathf.Atan(yDiff / xDiff);
            var deg = rad * 180 / Mathf.PI;

            if (xDiff < 0)
            {
                deg += 180;
            }

            return deg;
        }

        Vector2 ProjectToEdge(float x2, float y2)
        {
            float xDiff = x2 - (cam.pixelWidth / 2);
            float yDiff = y2 - (cam.pixelHeight / 2);
            float slope = yDiff / xDiff;

            Vector2 coord = new Vector2(0, 0);

            float ratio;

            if (slope > screenSlope || slope < -screenSlope)
            {
                ratio = (yCenter - halfSize) / yDiff;
                if (yDiff < 0)
                {
                    coord.y = halfSize;
                    ratio *= -1;
                }
                else coord.y = cam.pixelHeight - halfSize;
                coord.x = xCenter + xDiff * ratio;
            }
            else
            {
                ratio = (xCenter - halfSize) / xDiff;
                if (xDiff < 0)
                {
                    coord.x = halfSize;
                    ratio *= -1;
                }
                else coord.x = cam.pixelWidth - halfSize;
                coord.y = yCenter + yDiff * ratio;
            }
            return coord;
        }
    }
}