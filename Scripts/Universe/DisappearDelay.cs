using UnityEngine;

public class DisappearDelay : MonoBehaviour {

    [SerializeField] private float duration;

    private void OnEnable()
    {
        Invoke(nameof(Disappear), duration);
    }

    /// <summary>
    /// Destroys the gameobject object after the specified delay.
    /// </summary>
    private void Disappear()
    {
        Destroy(gameObject);
    }
}