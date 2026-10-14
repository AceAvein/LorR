using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HandPoseDriver : MonoBehaviour
{
    InputAction grip;
    InputAction trigger;
    Animator animator;
    Transform[] indexBones;
    Quaternion[] openIndex;

    public void Configure(bool leftHand)
    {
        animator = GetComponent<Animator>();

        grip?.Dispose();
        trigger?.Dispose();

        string hand = leftHand ? "{LeftHand}" : "{RightHand}";
        grip = new InputAction(type: InputActionType.Value, binding: $"<XRController>{hand}/grip");
        trigger = new InputAction(type: InputActionType.Value, binding: $"<XRController>{hand}/trigger");
        grip.Enable();
        trigger.Enable();
        CacheOpenIndex();
    }

    void OnDestroy()
    {
        grip?.Dispose();
        trigger?.Dispose();
    }

    void Update()
    {
        if (animator == null)
            return;

        animator.SetFloat("Grip", grip != null ? grip.ReadValue<float>() : 0f);
        animator.SetFloat("Trigger", trigger != null ? trigger.ReadValue<float>() : 0f);
    }

    void LateUpdate()
    {
        if (indexBones == null)
            return;

        float gripAmount = grip != null ? grip.ReadValue<float>() : 0f;
        float triggerAmount = trigger != null ? trigger.ReadValue<float>() : 0f;
        float point = Mathf.Clamp01(gripAmount) * (1f - Mathf.Clamp01(triggerAmount));
        if (point <= 0.001f)
            return;

        for (int i = 0; i < indexBones.Length; i++)
        {
            if (indexBones[i] == null)
                continue;

            indexBones[i].localRotation = Quaternion.Slerp(indexBones[i].localRotation, openIndex[i], point);
        }
    }

    void CacheOpenIndex()
    {
        if (openIndex != null)
            return;

        List<Transform> bones = new List<Transform>();
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            string boneName = children[i].name;
            if (boneName.Contains("index_ignore"))
                continue;
            if (boneName.Contains("index1") || boneName.Contains("index2") || boneName.Contains("index3"))
                bones.Add(children[i]);
        }

        indexBones = bones.ToArray();
        openIndex = new Quaternion[indexBones.Length];
        for (int i = 0; i < indexBones.Length; i++)
            openIndex[i] = indexBones[i].localRotation;
    }
}
