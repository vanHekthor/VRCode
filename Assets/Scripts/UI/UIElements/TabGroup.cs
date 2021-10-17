using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VRVis.UI.UIElements {
    public class TabGroup : MonoBehaviour {

        public List<TabButton> tabButtons;
        public Sprite tabIdle;
        public Sprite tabHovered;
        public Sprite tabActive;
        public GameObject tabPanelArea;

        public TabSelected tabSelected = new TabSelected();
        public class TabSelected : UnityEvent<string> { }

        private List<GameObject> tabPanels;
        private TabButton selectedTab;

        private void Awake() {
            tabPanels = new List<GameObject>();
            for (int i = 0; i < tabPanelArea.transform.childCount; i++) {
                tabPanels.Add(tabPanelArea.transform.GetChild(i).gameObject);
            }

            if (tabPanels.Count > 0) {
                tabPanels[0].SetActive(true);
            }
        }

        public void Subscribe(TabButton button) {
            if (tabButtons == null) {
                tabButtons = new List<TabButton>();
            }

            tabButtons.Add(button);

            if (button.transform.GetSiblingIndex() == 0) {
                selectedTab = button;
                button.backgroundImage.sprite = tabActive;
            }
        }

        public void OnTabEnter(TabButton button) {
            ResetTabs();
            if (selectedTab == null || button != selectedTab) {
                button.backgroundImage.sprite = tabHovered;
            }
        }

        public void OnTabExit(TabButton button) {
            ResetTabs();
        }

        public void OnTabSelected(TabButton button) {
            selectedTab = button;
            ResetTabs();
            button.backgroundImage.sprite = tabActive;

            int index = button.transform.GetSiblingIndex();
            string tabTitle = "";
            for (int i = 0; i < tabPanels.Count; i++) {
                if (i == index) {
                    tabPanels[i].SetActive(true);

                    tabTitle = tabPanels[i].transform.Find("Header").GetComponentInChildren<TextMeshProUGUI>().text;
                }
                else {
                    tabPanels[i].SetActive(false);
                }
            }

            
            tabSelected.Invoke(tabTitle);
        }

        private void ResetTabs() {
            foreach (TabButton button in tabButtons) {
                if (selectedTab != null && button == selectedTab) { continue; }
                button.backgroundImage.sprite = tabIdle;
            }
        }
    }
}