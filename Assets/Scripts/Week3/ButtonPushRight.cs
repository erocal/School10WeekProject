using UnityEngine;

public class ButtonPushRight : MonoBehaviour
{

    private Animation animation;

    private void Awake()
    {
        animation = GetComponent<Animation>();
    }

    // Update is called once per frame
    void Update()
    {
        
        if (Input.GetMouseButtonDown(1))
        {
            animation.Stop();
            animation.Play();
        }
            

    }

}
