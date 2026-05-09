using UnityEngine;

public class NextExerciseButton : MonoBehaviour
{
    public void OnClick()
    {
        ProgressManager.Instance.NextExerciseNoDelay();
    }
}
