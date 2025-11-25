using UnityEngine;
using UnityEngine.UI;

public class CDArt : MonoBehaviour
{
    public Image img;    // drag CDImage here

    public void SetSprite(Sprite s)
    {
        img.sprite = s;      // sudah tidak patah karena transform spinner tidak reset
    }
}
