using UnityEngine;
using UnityEngine.InputSystem;

public abstract class GachaResultPopupBase : UIPopup
{
    [SerializeField] private InputActionAsset inputActions;
    private InputAction tapAction;

    protected virtual void Awake()
    {
        tapAction = inputActions.FindActionMap("UI").FindAction("Click");
    }

    protected virtual void OnEnable()
    {
        tapAction.performed += OnTap;
    }

    protected virtual void OnDisable()
    {
        tapAction.performed -= OnTap;
    }

    protected virtual void OnTap(InputAction.CallbackContext ctx) => Hide();

 }
