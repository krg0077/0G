using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if NS_DG_TWEENING
using DG.Tweening;
#endif

namespace _0G.Legacy
{
    public static class ExtensionMethods
    {
        // AXIS

        public static bool HasFlag(this Axis axs, Axis flag)
        {
            return (axs & flag) == flag;
        }

        public static Vector3 GetVector3(this Axis axs, float magnitude, Vector3 baseV3 = new Vector3())
        {
            if (magnitude < 0)
            {
                throw new System.Exception(string.Format("Magnitude is {0} but must be zero or positive.", magnitude));
            }
            //x
            if (axs.HasFlag(Axis.Xneg))
            {
                baseV3.x = -magnitude;
            }
            if (axs.HasFlag(Axis.Xpos))
            {
                baseV3.x = magnitude;
                if (axs.HasFlag(Axis.Xneg))
                {
                    throw new System.Exception("Both Xneg & Xpos are set, but only one (or neither) must be set.");
                }
            }
            //y
            if (axs.HasFlag(Axis.Yneg))
            {
                baseV3.y = -magnitude;
            }
            if (axs.HasFlag(Axis.Ypos))
            {
                baseV3.y = magnitude;
                if (axs.HasFlag(Axis.Yneg))
                {
                    throw new System.Exception("Both Yneg & Ypos are set, but only one (or neither) must be set.");
                }
            }
            //z
            if (axs.HasFlag(Axis.Zneg))
            {
                baseV3.z = -magnitude;
            }
            if (axs.HasFlag(Axis.Zpos))
            {
                baseV3.z = magnitude;
                if (axs.HasFlag(Axis.Zneg))
                {
                    throw new System.Exception("Both Zneg & Zpos are set, but only one (or neither) must be set.");
                }
            }
            //v3
            return baseV3;
        }

        // CAMERA

        /// <summary>
        /// Get the orthographic bounds of the camera.
        /// </summary>
        /// <returns>The orthographic bounds.</returns>
        /// <param name="camera">Camera.</param>
        public static Bounds GetOrthographicBounds(this Camera camera)
        {
            if (!camera.orthographic)
            {
                G.U.Err("The {0} Camera does not use orthographic projection.", camera.name);
                return new Bounds();
            }
            float screenAspect = (float)Screen.width / (float)Screen.height;
            float cameraHeight = camera.orthographicSize * 2;
            return new Bounds(
                camera.transform.position,
                new Vector3(cameraHeight * screenAspect, cameraHeight, 0)
            );
        }

        /// <summary>
        /// Get the perspective bounds of the camera.
        /// </summary>
        /// <returns>The perspective bounds.</returns>
        /// <param name="camera">Camera.</param>
        /// <param name="z">The distance to the specified plane from the camera along the z-axis.</param>
        public static Bounds GetPerspectiveBounds(this Camera camera, float z)
        {
            if (camera.orthographic)
            {
                G.U.Err("The {0} Camera does not use perspective projection.", camera.name);
                return new Bounds();
            }
            Vector3 bottomLeft = camera.ViewportToWorldPoint(new Vector3(0, 0, z));
            Vector3 topRight = camera.ViewportToWorldPoint(new Vector3(1, 1, z));
            Bounds bounds = new Bounds();
            bounds.SetMinMax(bottomLeft, topRight);
            return bounds;
        }

        // COLLIDER

        public static GameObjectBody GetBody(this Collider other) => other.GetComponent<GameObjectBody>();

        public static bool IsFromAttack(this Collider other)
        {
            GameObjectBody body = other.GetBody();
            return body != null && body.IsAttack;
        }

        public static bool IsFromCharacter(this Collider other)
        {
            GameObjectBody body = other.GetBody();
            return body != null && body.IsCharacter;
        }

        // COLLISION

        public static GameObjectBody GetBody(this Collision collision) => collision.collider.GetComponent<GameObjectBody>();

        public static bool IsFromAttack(this Collision collision)
        {
            GameObjectBody body = collision.GetBody();
            return body != null && body.IsAttack;
        }

        public static bool IsFromCharacter(this Collision collision)
        {
            GameObjectBody body = collision.GetBody();
            return body != null && body.IsCharacter;
        }

        // COLOR

        public static Color SetAlpha(this Color c, float a)
        {
            c.a = a;
            return c;
        }

        // COMPONENT

        public static void Dispose(this Component me)
        {
            if (me == null)
            {
                G.U.Warn("The Component you wish to dispose of is null.");
                return;
            }
            Object.Destroy(me);
        }

        public static void PersistNewScene(this Component me, PersistNewSceneType persistNewSceneType)
        {
            Transform t = me.transform;
            switch (persistNewSceneType)
            {
                case PersistNewSceneType.PersistAllParents:
                    while (t.parent != null)
                    {
                        t = t.parent;
                    }
                    break;
                case PersistNewSceneType.MoveToHierarchyRoot:
                    t.SetParent(null);
                    break;
                default:
                    G.U.Unsupported(me, persistNewSceneType);
                    break;
            }
            Object.DontDestroyOnLoad(t);
        }

        /// <summary>
        /// REQUIRE COMPONENT to be on SPECIFIED COMPONENT'S GAME OBJECT:
        /// Require the specified Component type to exist on the specified source Component's GameObject.
        /// </summary>
        /// <param name="me">Source Component.</param>
        /// <param name="throwException">If set to <c>true</c> throw exception.</param>
        /// <typeparam name="T">The required Component type.</typeparam>
        public static T Require<T>(this Component me, bool throwException = true) where T : Component
        {
            if (!G.U.SourceExists(me, typeof(T), throwException)) return null;
            T comp = me.GetComponent<T>();
            if (G.U.IsNull(comp))
            {
                string s = string.Format("A {0} Component must exist on the {1}'s {2} GameObject.",
                    typeof(T), me.GetType(), me.name);
                G.U.ErrorOrException(s, throwException);
                return null;
            }
            return comp;
        }

        // FLOAT

        /// <summary>
        /// Is approximately equal to...
        /// </summary>
        /// <param name="me">The first value to be compared.</param>
        /// <param name="f">The second value to be compared.</param>
        /// <param name="tolerance">If greater than 0, use this value. Else use Mathf.Approximately.</param>
        public static bool Ap(this float me, float f, float tolerance = 0)
        {
            if (tolerance > 0)
            {
                return Mathf.Abs(me - f) <= tolerance;
            }
            else
            {
                return Mathf.Approximately(me, f);
            }
        }

        public static Rotation Rotation(this float me)
        {
            return new Rotation(me);
        }

        public static Sign Sign(this float me)
        {
            return new Sign(me);
        }

        // GAME OBJECT

        public static void Dispose(this GameObject me)
        {
            if (me == null)
            {
                G.U.Warn("The GameObject you wish to dispose of is null.");
                return;
            }
            Object.Destroy(me);
        }

        public static void PersistNewScene(this GameObject me, PersistNewSceneType persistNewSceneType)
        {
            Transform t = me.transform;
            switch (persistNewSceneType)
            {
                case PersistNewSceneType.PersistAllParents:
                    while (t.parent != null)
                    {
                        t = t.parent;
                    }
                    break;
                case PersistNewSceneType.MoveToHierarchyRoot:
                    t.SetParent(null);
                    break;
                default:
                    G.U.Unsupported(me, persistNewSceneType);
                    break;
            }
            Object.DontDestroyOnLoad(t);
        }

        /// <summary>
        /// REQUIRE COMPONENT to be on SPECIFIED GAME OBJECT:
        /// Require the specified Component type to exist on the specified source GameObject.
        /// </summary>
        /// <param name="me">Source GameObject.</param>
        /// <param name="throwException">If set to <c>true</c> throw exception.</param>
        /// <typeparam name="T">The required Component type.</typeparam>
        public static T Require<T>(this GameObject me, bool throwException = true) where T : Component
        {
            if (!G.U.SourceExists(me, typeof(T), throwException)) return null;
            T comp = me.GetComponent<T>();
            if (G.U.IsNull(comp))
            {
                string s = string.Format("A {0} Component must exist on the {1} GameObject.",
                    typeof(T), me.name);
                G.U.ErrorOrException(s, throwException);
                return null;
            }
            return comp;
        }

        //  Interfaces (allows for similar functionality across vastly different objects)
        //  NOTE: The following methods may need to be revised.

        /// <summary>
        /// Calls all interfaces of the specified type that are on this GameObject,
        /// specifically by passing each as a parameter into the provided Action.
        /// </summary>
        public static void CallInterfaces<T>(this GameObject me, System.Action<T> action)
        {
            T[] interfaces = GetInterfaces<T>(me);
            for (int i = 0; i < interfaces.Length; i++)
            {
                action(interfaces[i]);
            }
        }

        /// <summary>
        /// Gets all interfaces of the specified type that are on this GameObject.
        /// </summary>
        public static T[] GetInterfaces<T>(this GameObject me)
        {
            if (typeof(T).IsInterface)
            {
                return me.GetComponents<Component>().OfType<T>().ToArray<T>();
            }
            else
            {
                G.U.Err("Error while getting interfaces for {0}: {1} is not an interface.",
                    me.name, typeof(T));
                return new T[0];
            }
        }

        // IMAGE

        public static void SetAlpha(this Image i, float a)
        {
            i.color = i.color.SetAlpha(a);
        }

        // INT

        public static bool Between(this int val, int fromInclusive, int toExclusive)
        {
            return val >= fromInclusive && val < toExclusive;
        }

        public static int ClampRotationDegrees(this int deg)
        {
            while (deg < 0) deg += 360;
            return deg % 360;
        }

        public static bool HasFlag(this int flagsEnum, int flag)
        {
            return (flagsEnum & flag) == flag;
        }

        // RECT TRANSFORM

        /// <summary>
        /// Center the specified RectTransform on its parent using the specified size.
        /// This will modify the pivot, anchors, position, size, and scale.
        /// </summary>
        /// <param name="rt">RectTransform.</param>
        /// <param name="size">Size.</param>
        public static void Center(this RectTransform rt, Vector2 size)
        {
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchorMin = rt.pivot;
            rt.anchorMax = rt.pivot;

            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
        }

        /// <summary>
        /// Stretch the specified RectTransform to the full extent of its parent.
        /// This will modify the pivot, anchors, position, size, and scale.
        /// </summary>
        /// <param name="rt">RectTransform.</param>
        public static void Stretch(this RectTransform rt)
        {
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            //all three of the following are required, else irregularities arise under particular circumstances
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        // STRING ARRAY

        public static string[] plus(this string[] me, string to_add)
        {
            string[] ret = new string[me.Length + 1];
            me.CopyTo(ret, 0);
            ret[me.Length] = to_add;
            return ret;
        }

        public static string[] plus(this string[] me, string[] to_add)
        {
            string[] ret = new string[me.Length + to_add.Length];
            me.CopyTo(ret, 0);
            to_add.CopyTo(ret, me.Length);
            return ret;
        }

        public static string[] plus(this string[] me, List<string> to_add)
        {
            string[] ret = new string[me.Length + to_add.Count];
            me.CopyTo(ret, 0);
            to_add.CopyTo(ret, me.Length);
            return ret;
        }

        // TEXT MESH PRO UGUI

#if NS_DG_TWEENING
        public static Tweener DOColor(this TextMeshProUGUI text, Color endValue, float duration)
        {
            return DOTween.To(() => text.color, x => text.color = x, endValue, duration);
        }

        public static Tweener DOFade(this TextMeshProUGUI text, float endValue, float duration)
        {
            return DOTween.To(() => text.alpha, x => text.alpha = x, endValue, duration);
        }

        public static Tweener DOFontSize(this TextMeshProUGUI text, float endValue, float duration)
        {
            return DOTween.To(() => text.fontSize, x => text.fontSize = x, endValue, duration);
        }
#endif

        // TRANSFORM OPTIONS

        public static bool HasFlag(this TransformOptions opt, TransformOptions flag)
        {
            return (opt & flag) == flag;
        }

        // TWEEN

#if NS_DG_TWEENING
        public static T SetTimeThread<T>(this T t, TimeThread th) where T : Tween
        {
            th.AddTween(t);
            return t;
        }
#endif

        // ULONG

        public static bool HasFlag(this ulong flagsEnum, ulong flag)
        {
            return (flagsEnum & flag) == flag;
        }

        // VECTOR 2

        public delegate float V2Func(float value);

        public static Vector2 Abs(this Vector2 v2)
        {
            v2.x = Mathf.Abs(v2.x);
            v2.y = Mathf.Abs(v2.y);
            return v2;
        }

        public static Vector2 Add(this Vector2 v2, float x = 0, float y = 0)
        {
            v2.x += x;
            v2.y += y;
            return v2;
        }

        public static Vector2 Func(this Vector2 v2, V2Func fx = null, V2Func fy = null)
        {
            if (fx != null) v2.x = fx(v2.x);
            if (fy != null) v2.y = fy(v2.y);
            return v2;
        }

        public static Vector2 Multiply(this Vector2 v2, float x = 1, float y = 1)
        {
            v2.x *= x;
            v2.y *= y;
            return v2;
        }

        public static Vector2 Multiply(this Vector2 v2, Vector2 m)
        {
            v2.x *= m.x;
            v2.y *= m.y;
            return v2;
        }

        public static Vector2 SetX(this Vector2 v2, float x)
        {
            v2.x = x;
            return v2;
        }

        public static Vector2 SetY(this Vector2 v2, float y)
        {
            v2.y = y;
            return v2;
        }

        // VECTOR 3

        public delegate float V3Func(float value);

        public static Vector2 Abs(this Vector3 v3)
        {
            v3.x = Mathf.Abs(v3.x);
            v3.y = Mathf.Abs(v3.y);
            v3.z = Mathf.Abs(v3.z);
            return v3;
        }

        public static Vector3 Add(this Vector3 v3, float x = 0, float y = 0, float z = 0)
        {
            v3.x += x;
            v3.y += y;
            v3.z += z;
            return v3;
        }

        public static void AddRef(this ref Vector3 v3, float x = 0, float y = 0, float z = 0)
        {
            v3.x += x;
            v3.y += y;
            v3.z += z;
        }

        /// <summary>
        /// Is approximately equal to...
        /// </summary>
        /// <param name="me">The first value to be compared.</param>
        /// <param name="v3">The second value to be compared.</param>
        /// <param name="tolerance">If greater than 0, use this value. Else use Mathf.Approximately.</param>
        public static bool Ap(this Vector3 me, Vector3 v3, float tolerance = 0)
        {
            bool x, y, z;
            if (tolerance > 0)
            {
                x = Mathf.Abs(me.x - v3.x) <= tolerance;
                y = Mathf.Abs(me.y - v3.y) <= tolerance;
                z = Mathf.Abs(me.z - v3.z) <= tolerance;
            }
            else
            {
                x = Mathf.Approximately(me.x, v3.x);
                y = Mathf.Approximately(me.y, v3.y);
                z = Mathf.Approximately(me.z, v3.z);
            }
            return x && y && z;
        }

        public static Vector3 Func(this Vector3 v3, V3Func fx = null, V3Func fy = null, V3Func fz = null)
        {
            if (fx != null) v3.x = fx(v3.x);
            if (fy != null) v3.y = fy(v3.y);
            if (fz != null) v3.z = fz(v3.z);
            return v3;
        }

        public static Vector3 Multiply(this Vector3 v3, float x = 1, float y = 1, float z = 1)
        {
            v3.x *= x;
            v3.y *= y;
            v3.z *= z;
            return v3;
        }

        public static Vector3 Multiply(this Vector3 v3, Vector3 m)
        {
            v3.x *= m.x;
            v3.y *= m.y;
            v3.z *= m.z;
            return v3;
        }

        public static Vector3 Set2(this Vector3 v3, float? x = null, float? y = null, float? z = null)
        {
            if (x.HasValue) v3.x = x.Value;
            if (y.HasValue) v3.y = y.Value;
            if (z.HasValue) v3.z = z.Value;
            return v3;
        }

        public static Vector3 SetSign(this Vector3 v3, bool? x = null, bool? y = null, bool? z = null)
        {
            if (x.HasValue) v3.x = Mathf.Abs(v3.x) * (x.Value ? 1f : -1f);
            if (y.HasValue) v3.y = Mathf.Abs(v3.y) * (y.Value ? 1f : -1f);
            if (z.HasValue) v3.z = Mathf.Abs(v3.z) * (z.Value ? 1f : -1f);
            return v3;
        }

        public static Vector3 SetX(this Vector3 v3, float x)
        {
            v3.x = x;
            return v3;
        }

        public static Vector3 SetY(this Vector3 v3, float y)
        {
            v3.y = y;
            return v3;
        }

        public static Vector3 SetZ(this Vector3 v3, float z)
        {
            v3.z = z;
            return v3;
        }

        public static Vector2 ToVector2(this Vector3 v3)
        {
            return new Vector2(v3.x, v3.y);
        }
    }
}