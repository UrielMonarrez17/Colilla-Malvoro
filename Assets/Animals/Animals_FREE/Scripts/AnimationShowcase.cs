using UnityEngine;

public class AnimationShowcase : MonoBehaviour {
    private Animator animator;

    [Header("Blend Tree Thresholds")]
    [SerializeField] private float idleSpeed = 0.0f;
    [SerializeField] private float walkSpeed = 0.5f;
    [SerializeField] private float runSpeed = 1.0f;

    [Header("Damping")]
    [SerializeField] private float dampTime = 0.1f;

    void Start() {
        animator = GetComponent<Animator>();
    }

    void Update() {
        float targetSpeed = idleSpeed;

        if (Input.GetKey(KeyCode.W)) {
            targetSpeed = walkSpeed;
        }
        else if (Input.GetKey(KeyCode.S)) {
            targetSpeed = runSpeed;
        }

        animator.SetFloat("Speed", targetSpeed, dampTime, Time.deltaTime);
    }
}