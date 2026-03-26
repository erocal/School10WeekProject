using UnityEngine;

public class ButtonPushLeft : MonoBehaviour
{

    private Animation animation;

    private void Awake()
    {
        animation = GetComponent<Animation>();
    }

    // Update is called once per frame
    void Update()
    {
        
        if (Input.GetMouseButtonDown(0))
        {
            animation.Stop();
            animation.Play();
        }
            

    }

}
