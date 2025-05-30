using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.HighDefinition;

namespace shooter_game.scripts
{
    public class ToggleHands : MonoBehaviour
    {
        public CustomPassVolume pass;
        private bool _isClicked;

        private void Update()
        {
            if (_isClicked)
                pass.customPasses.ForEach(p => p.enabled = false);
            else
                pass.customPasses.ForEach(p => p.enabled = true);
        }

        public void OnClick(InputAction.CallbackContext ctx)
        {
            _isClicked = ctx.ReadValueAsButton();
        }
    }
}