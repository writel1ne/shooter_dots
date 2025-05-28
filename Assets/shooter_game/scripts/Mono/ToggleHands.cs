using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.HighDefinition;

namespace shooter_game.scripts
{
    public class ToggleHands : MonoBehaviour
    {
        private bool _isClicked;
        public CustomPassVolume pass;
    
        void Update()
        {
            if (_isClicked)
            {
                pass.customPasses.ForEach(p => p.enabled = false);
            }
            else
            {
                pass.customPasses.ForEach(p => p.enabled = true);
            }
        }

        public void OnClick(InputAction.CallbackContext ctx)
        {
            _isClicked = ctx.ReadValueAsButton();
        }
    }
}
