using UnityEngine;
using UnityEngine.UI;

public class StartGameButton : MonoBehaviour
{
    Button myButton;

    private void Start()
    {
        myButton = GetComponent<Button>();
        myButton.onClick.AddListener(async () =>
        {

            GameManager.Instance.OpenLevelScence();
        });
    }
}
