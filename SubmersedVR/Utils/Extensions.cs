using UnityEngine;
using UnityEngine.Animations;

namespace SubmersedVR
{
    static class Extensions
    {
        public static GameObject WithParent(this GameObject obj, Transform target)
        {
            obj.transform.parent = target;
            return obj;
        }
        public static GameObject WithParent(this GameObject obj, GameObject target)
        {
            obj.transform.parent = target.transform;
            return obj;
        }
        public static GameObject ResetTransform(this GameObject obj)
        {
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            return obj;
        }

        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            if (component == null)
            {
                component = go.AddComponent<T>();
            }
            return component;
        }

        public static ParentConstraint ParentTo(this ParentConstraint parentConstraint, Transform target, Vector3 translationOffset)
        {
            // Remove old sources
            for (int i = 0; i < parentConstraint.sourceCount; i++)
            {
                parentConstraint.RemoveSource(0);
            }

            var cs = new ConstraintSource();
            cs.sourceTransform = target;
            cs.weight = 1.0f;
            parentConstraint.AddSource(cs);
            parentConstraint.SetTranslationOffset(0, translationOffset);
            parentConstraint.SetRotationOffset(0, Vector3.zero);
            parentConstraint.locked = true;
            parentConstraint.constraintActive = true;
            parentConstraint.weight = 1.0f;

            return parentConstraint;
        }
    }
}