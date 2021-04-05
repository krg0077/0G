using UnityEngine;

namespace _0G
{
    public static class Input
    {
        // TODO: should this be static or instanced?
        // input should reference a player object
        // as well as InputKey

        public static bool KeyDown(InputKey key)
        {
            switch (key)
            {
                case InputKey.UISubmit:
                    return UnityEngine.Input.GetKeyDown(KeyCode.Return);
            }
            return false;
        }

        public static bool KeyHeld(InputKey key)
        {
            switch (key)
            {
                case InputKey.UISubmit:
                    return UnityEngine.Input.GetKey(KeyCode.Return);
            }
            return false;
        }

        public static bool KeyUp(InputKey key)
        {
            switch (key)
            {
                case InputKey.UISubmit:
                    return UnityEngine.Input.GetKeyUp(KeyCode.Return);
            }
            return false;
        }
    }
}