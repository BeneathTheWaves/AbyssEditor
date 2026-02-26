using UnityEngine;
using UnityEngine.EventSystems;
namespace AbyssEditor.Scripts.InputMaps
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager main;
        
        public AbyssEditorInput input;

        private void Awake()
        {
            main = this;
            input = new AbyssEditorInput();
        }
    }
}
