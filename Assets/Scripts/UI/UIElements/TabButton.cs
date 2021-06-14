using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Valve.VR.InteractionSystem;

namespace VRVis.UI.UIElements {

    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(VRUIItem))]
    public class TabButton : UIElement, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler {

        public TabGroup tabGroup;

        public Image backgroundImage;

        // Start is called before the first frame update
        void Start() {
            backgroundImage = GetComponent<Image>();
            tabGroup.Subscribe(this);
        }

        // Update is called once per frame
        void Update() {

        }

        protected override void OnHandHoverBegin(Hand hand) {
            base.OnHandHoverBegin(hand);
            tabGroup.OnTabEnter(this);
        }

        protected override void OnHandHoverEnd(Hand hand) {
            base.OnHandHoverEnd(hand);
            tabGroup.OnTabExit(this);
        }

        protected override void HandHoverUpdate(Hand hand) {
            if (hand.uiInteractAction != null && hand.uiInteractAction.GetStateDown(hand.handType)) {
                InputModule.instance.Submit(gameObject);
                ControllerButtonHints.HideButtonHint(hand, hand.uiInteractAction);

                tabGroup.OnTabSelected(this);
            }
        }

        public void OnPointerEnter(PointerEventData eventData) {
            tabGroup.OnTabEnter(this);
        }

        public void OnPointerClick(PointerEventData eventData) {
            tabGroup.OnTabSelected(this);
        }

        public void OnPointerExit(PointerEventData eventData) {
            tabGroup.OnTabExit(this);
        }
    }
}