using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VRVis.UI.Helper {

    /// <summary>
    /// Helper script that can be attached to a UI element.<para/>
    /// It can then be used, for instance, to easily change the according image
    /// by sending a message at this object using the according method name.<para/>
    /// 
    /// Available methods for SendMessage:<para/>
    /// - "ChangeImageTexture" with Texture2D as value<para/>
    /// - "ChangeImageColor" with Color as value
    /// </summary>
    public class ChangeImageHelper : MonoBehaviour {

        [Tooltip("The image element to change the sprite image of")]
        public Image img;

        /// <summary>Change the text.</summary>
        public void ChangeImageTexture(Texture2D tex) {

            if (img) {
                img.sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 50);
                Color c = img.color;
                c.a = 1; // disable transparency
                img.color = c;
            }
        }

        /// <summary>Change the color.</summary>
        public void ChangeImageColor(Color color) {
            if (img) { img.color = color; }
        }

    }
}
