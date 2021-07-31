using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if NS_DG_TWEENING
using DG.Tweening;
#endif

namespace _0G.Legacy
{
    public class UIMenu : MonoBehaviour
    {
        // EVENTS

        public event ItemSelectionHandler ItemSelectionChanged;

        // DELEGATES

        public delegate void ItemSelectionHandler(int newItemIndex);

        // SERIALIZED FIELDS

        [Header("Object References")]
        public ScrollRect ScrollView;
        public GameObject MenuItem;
        public GameObject Cursor;
        
        [Header("Menu Options")]
        public Vector3 ItemOffset;
        public Vector3 CursorOffset;
        public bool AllowNoSelectedItem;
        public bool NavigateExplicitHorizontal;
        public bool NavigateExplicitVertical;
        public float TweenDuration = 0.1f;

        // PRIVATE FIELDS

        private CanvasGroup m_CanvasGroup;
        private List<Item> m_Items = new List<Item>();
        private int m_PrevSelectedItemIndex = -1;

        // STRUCTS

        public struct Item
        {
            public bool IsValid;
            public int Index;
            public string Key;
            public string Text;
            public string Description;
            public UnityAction OnClick;
            public GameObject MenuItem;
            public Button Button;
            public ISelectSilently ISelectSilently;
        }

        // PROPERTIES

        public int ItemCount => m_Items.Count;

        public Item SelectedItem
        {
            get
            {
                for (int i = 0; i < m_Items.Count; i++)
                {
                    Item item = m_Items[i];
                    if (item.MenuItem == EventSystem.current.currentSelectedGameObject)
                    {
                        return item;
                    }
                }
                return new Item();
            }
        }

        public int SelectedItemIndex
        {
            get
            {
                for (int i = 0; i < m_Items.Count; i++)
                {
                    Item item = m_Items[i];
                    if (item.MenuItem == EventSystem.current.currentSelectedGameObject)
                    {
                        return i;
                    }
                }
                return -1;
            }
        }

        // MONOBEHAVIOUR METHODS

        private void Awake()
        {
            m_CanvasGroup = GetComponentInChildren<CanvasGroup>(true);
        }

        private void Update()
        {
            if (!AllowNoSelectedItem && EventSystem.current.currentSelectedGameObject == null)
            {
                SelectDefaultItem();
            }
            
            int index = SelectedItemIndex;
            if (index == m_PrevSelectedItemIndex) return;
            
            if (ScrollView != null && ScrollView.vertical)
            {
                ScrollView.verticalNormalizedPosition = Mathf.InverseLerp(m_Items.Count - 1, 0, index);
            }
                
            if (Cursor != null && Cursor.activeSelf)
            {
                Transform menuItemTransform = m_Items[index].MenuItem.transform;
                Transform cursorTransform = Cursor.transform;
#if NS_DG_TWEENING
                cursorTransform.DOMove(menuItemTransform.position + CursorOffset, TweenDuration);
#else
                cursorTransform.position = menuItemTransform.position + CursorOffset;
#endif
            }

            ItemSelectionChanged?.Invoke(index);
            m_PrevSelectedItemIndex = index;
        }

        // PUBLIC METHODS

        public void Clear()
        {
            foreach (Item item in m_Items)
            {
                GameObject menuItem = item.MenuItem;
                if (menuItem == EventSystem.current.currentSelectedGameObject)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }
                if (menuItem == MenuItem)
                {
                    menuItem.name = "Menu Item";
                    menuItem.GetComponentInChildren<TextMeshProUGUI>().text = "Menu Item";

                    Button button = menuItem.GetComponent<Button>();
                    Button.ButtonClickedEvent clickedEvent = button.onClick;
                    clickedEvent.RemoveAllListeners();
                }
                else
                {
                    Destroy(menuItem);
                }
            }
            m_Items.Clear();
            m_PrevSelectedItemIndex = -1;
        }

        public void AddItem(string text, UnityAction onClick, string description = null)
        {
            AddItem(null, text, onClick, description);
        }

        public void AddItem(string key, string text, UnityAction onClick, string description = null)
        {
            int index = m_Items.Count;
            GameObject menuItem;
            if (index == 0)
            {
                menuItem = MenuItem;
                ScrollView.content.sizeDelta = Vector2.zero;
            }
            else
            {
                menuItem = Instantiate(MenuItem, MenuItem.transform.parent);
                RectTransform rt = menuItem.GetComponent<RectTransform>();
                rt.localPosition += ItemOffset * index;

                // we need to set the new content size so the scrolling works properly
                if (ScrollView.vertical)
                {
                    const float referenceHeight = 1080; // TODO: get this dynamically from canvas
                    float y = Mathf.Abs(rt.localPosition.y) + rt.sizeDelta.y - referenceHeight;
                    ScrollView.content.sizeDelta = ScrollView.content.sizeDelta.SetY(y);
                }
                else
                {
                    ScrollView.content.sizeDelta = rt.localPosition.ToVector2().Abs() + rt.sizeDelta;
                }
            }

            menuItem.name = key ?? text;
            menuItem.GetComponentInChildren<TextMeshProUGUI>().text = text;

            var button = menuItem.GetComponent<Button>();
            if (onClick != null)
            {
                Button.ButtonClickedEvent clickedEvent = button.onClick;
                clickedEvent.RemoveAllListeners();
                clickedEvent.AddListener(onClick);
            }
            if (NavigateExplicitHorizontal || NavigateExplicitVertical)
            {
                Navigation nav = button.navigation;
                nav.mode = Navigation.Mode.Explicit;
                if (index > 0)
                {
                    Button buttonPrev = m_Items[index - 1].Button;
                    Navigation navPrev = buttonPrev.navigation;
                    if (NavigateExplicitHorizontal)
                    {
                        navPrev.selectOnRight = button;
                        nav.selectOnLeft = buttonPrev;
                        nav.selectOnRight = null; // clear out duplicated value from Instantiate (3+ items)
                    }
                    if (NavigateExplicitVertical)
                    {
                        navPrev.selectOnDown = button;
                        nav.selectOnUp = buttonPrev;
                        nav.selectOnDown = null; // clear out duplicated value from Instantiate (3+ items)
                    }
                    buttonPrev.navigation = navPrev;
                }
                button.navigation = nav;
            }

            m_Items.Add(new Item
            {
                IsValid = true,
                Index = index,
                Key = key,
                Text = text,
                Description = description,
                OnClick = onClick,
                MenuItem = menuItem,
                Button = button,
                ISelectSilently = menuItem.GetComponent<ISelectSilently>(),
            });
        }

        // this function should be reserved for reading an item's data, such as text or description
        // modifications should be handled through new methods as needed
        public Item GetItem(int itemIndex) => m_Items[itemIndex];

        public void RenameItem(int itemIndex, string text)
        {
            Item item = m_Items[itemIndex];
            item.Text = text;
            item.MenuItem.name = item.Key ?? text;
            item.MenuItem.GetComponentInChildren<TextMeshProUGUI>().text = text;
        }

        public void SelectDefaultItem() => SelectItem(m_Items[0]);

        public void SelectItem(int itemIndex) => SelectItem(m_Items[itemIndex]);

        public void SetVisible(bool value)
        {
            m_CanvasGroup.alpha = value ? 1 : 0;
        }
        
        // PRIVATE METHODS

        private static void SelectItem(Item item)
        {
            if (item.ISelectSilently != null)
            {
                item.ISelectSilently.SelectSilently();
            }
            else
            {
                item.Button.Select();
            }
        }
    }
}