#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HMD.Scripts.Streaming.VLC;
using HMD.Scripts.Util;
using UnityEngine;
using UnityEngine.UI;

public class DashPanels : MonoBehaviour
{
    // [HideInInspector]
    // public VlcController controller;

    public GameObject vlcPlayerTemplate;

    public Dropdown playerDropdown;

    public class Player : Dependent<DashPanels>, IDisposable
    {
        public string ID;

        public GameObject Prefab;

        private VlcController? _controller;

        public VlcController Controller
        {
            get
            {
                return _controller ??= Prefab.GetComponent<VlcController>();
            }
        }

        public void Focus()
        {
            Controller.BindUI();
            var dropdown = Outer.playerDropdown;
            var newIndex = dropdown.options.FindIndex(option => option.text == ID);
            dropdown.value = newIndex;
        }

        // TODO: icon is also used to mark player with no video, need to highlight icon
        // public void Select()
        // {
        //     Controller.screen.cone.SetActive(true);
        // }

        public void Dispose()
        {
            Destroy(Prefab);
            Outer._activePlayers.Remove(this.ID);
        }
    }

    private AtomicInt _incCounter = new AtomicInt();

    private Dictionary<string, Player> _activePlayers = new Dictionary<string, Player> { };

    // private Player? _focusedPlayer;

    private Player _setupPlayer(GameObject prefab, string playerName, bool focus = true)
    {
        prefab.SetActive(true);
        var id = playerName + "(" + _incCounter.Next() + ")";

        var neo = new Player { Outer = this, Prefab = prefab, ID = id };
        _activePlayers.Add(id, neo);

        if (focus)
        {
            neo.Focus();
        }

        return neo;
    }

    public Player SetupVlc()
    {
        var prefab = Instantiate(vlcPlayerTemplate, Vector3.zero, Quaternion.identity);

        var player = _setupPlayer(prefab, "VLC");

        playerDropdown.options.Add(new Dropdown.OptionData(player.ID));
        playerDropdown.RefreshShownValue();

        return player;
    }

    // SetupCaptureDevice()

    public GameObject playerTab;
    public void TogglePlayerTab()
    {
        ExtendDisplayOnce();
        ToggleElement(playerTab);
    }

    private List<Display>? _extendDisplay;
    // TODO: this shouldn't be cached, display may be connected or disconnected during execution
    private List<Display> ExtendDisplayOnce() // In Unity, display cannot be scrapped
    {
        if (_extendDisplay == null)
        {
            Debug.Log("displays connected: " + Display.displays.Length);
            // Display.displays[0] is the primary, default display and is always ON, so start at index 1.
            // Check if additional displays are available and activate each.

            var result = Display.displays.Skip(1).ToList();

            foreach (Display d in result)
            {
                Debug.Log("display" + d.systemWidth + "x" + d.systemHeight + " : " + d.renderingWidth + "x"
                    + d.renderingHeight);
                d.Activate();
            }

            _extendDisplay = result;
            return result;
        }

        return _extendDisplay;
    }

    public FOVController fovController;

    public GameObject consoleTab;
    public void ToggleConsoleTab()
    {
        ToggleElement(consoleTab);
    }

    public GameObject trackTab;
    public void ToggleTrackTab()
    {
        ToggleElement(trackTab);
    }

    public GameObject volumeTab;
    public void ToggleVolumeTab()
    {
        ToggleElement(volumeTab);
    }

    private List<Button>? _allTabs;
    private List<Button> AllTabs
    {
        get
        {
            return _allTabs ??= new List<Button>
            {
                playerTab.GetComponent<Button>(),
                consoleTab.GetComponent<Button>(),
                trackTab.GetComponent<Button>(),
                volumeTab.GetComponent<Button>()
            };
        }
    }

    //Enable a GameObject if it is disabled, or disable it if it is enabled
    private static bool ToggleElement(GameObject element)
    {
        var toggled = !element.activeInHierarchy;
        element.SetActive(toggled);
        return toggled;
    }


    // the following are set in `UpdateReferences`

    private GameObject _rootMenu;
    private GameObject _appMenu;

    private List<GameObject>? _allMenus;
    private List<GameObject> AllMenus
    {
        get
        {
            return _allMenus ??= new List<GameObject>
            {
                _rootMenu,
                _appMenu
            };
        }
    }

    private GameObject _optionsButton;

    private GameObject _lockScreenNotice;

    private GameObject _aspectRatioPopup;
    private GameObject _screenPopup;
    private GameObject _formatPopup;
    private GameObject _releaseInfoPopup;
    private GameObject _pictureSettingsPopup;

    private List<GameObject>? _allPopups;
    private List<GameObject> AllPopups
    {
        get
        {
            return _allPopups ??= new List<GameObject>
            {
                _aspectRatioPopup,
                _screenPopup,
                _formatPopup,
                _releaseInfoPopup,
                _pictureSettingsPopup
            };
        }
    }

    // private MenuID _visibleMenuID;

    public enum MenuID
    {
        CONTROLLER_MENU,
        APP_MENU
    };

    // Start is called before the first frame update
    private void Start()
    {
        BindUI();

        _lockScreenNotice = GlobalFinder.Find("LockScreenNotice").Only();

        var versionName = Application.version;
        var versionCode = Application.buildGUID;
        GlobalFinder.Find("AppMenu/AppMenuInner/Subtitle").Only().GetComponent<Text>().text =
            $"{versionName} ({versionCode})";

        // center UI things that i had spread out in Editor
        CenterPopupLocations();

        // Center Menus/Objects
        CenterXY(_lockScreenNotice);
        CenterXY(_rootMenu);
        CenterXY(_appMenu);

        _lockScreenNotice.SetActive(false);

        HideAllMenus();
        HideAllPopups();

        UIShowControllerMenu();

        if (PlayerPrefs.GetInt("OnboardingSeen_0_0_5_g") == 1)
        {
            // The user has already seen the onboarding tutorial text
        }
        else
        {
            // The user has not yet seen the onboarding tutorial text
            PlayerPrefs.SetInt("OnboardingSeen_0_0_5_g", 1);
            ShowWhatsNewPopup();
        }
    }

    private void CenterPopupLocations()
    {
        // Get the "Popups" game object, then loop over each of it's top-level children
        // and center them on the screen

        var popups = GlobalFinder.Find("Canvas/Popups").Only();
        for (var i = 0; i < popups.transform.childCount; i++)
        {
            var childGameObject = popups.transform.GetChild(i).gameObject;
            //Debug.Log("centering " + childGameObject.name);
            CenterXY(childGameObject);
        }
    }

    private void CenterXY(GameObject o)
    {
        o.transform.localPosition = new Vector3(
            0.0f,
            0.0f,
            o.transform.localPosition.z
        );
    }

    // private void OnApplicationFocus(bool hasFocus)
    // { TODO: remove, useless
    // }

    private void UpdateReferences()
    {
        // TODO: this is unsafe, should change to static binding
        _rootMenu = gameObject.ByName("RootPanel").Only();
        _appMenu = gameObject.ByName("AppMenu").Only();

        // _unlock_3d_sphere_mode_prompt_popup = FindGameObjectsAllFirst("Unlock3DSphereModePopup");

        _aspectRatioPopup = gameObject.ByName("AspectRatioPopup").Only();
        _optionsButton = gameObject.ByName("OptionsButton").Only();
        _screenPopup = gameObject.ByName("ScreenPopup").Only();
        _formatPopup = gameObject.ByName("FormatPopup").Only();
        _releaseInfoPopup = gameObject.ByName("WhatsNewPopup").Only();
        _pictureSettingsPopup = gameObject.ByName("PictureSettingsPopup").Only();
    }

    private const string NEW_VLC = "New VLC ...";

    private void BindUI()
    {
        UpdateReferences();

        // playerDropdown.RefreshShownValue();

        playerDropdown.options.Add(new Dropdown.OptionData(NEW_VLC));
        playerDropdown.RefreshShownValue();

        playerDropdown.onValueChanged.AddListener(
            value =>
            {
                var option = playerDropdown.options[value];

                if (option.text == NEW_VLC)
                {
                    SetupVlc();
                }
                else if (option.text == "")
                {
                    // do nothing
                }
                else
                {
                    var selected = _activePlayers[option.text];
                    selected.Focus();
                }
            }
        );
    }

    public void ClearPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
    }

    private void ShowAppMenu()
    {
        // UpdateReferences();

        _appMenu.SetActive(true);
        CenterXY(_appMenu);
    }

    private void ShowRootMenu()
    {
        _rootMenu.SetActive(true);
        _optionsButton.SetActive(true);
    }

    public void UIShowControllerMenu()
    {
        ShowMenuByID(MenuID.CONTROLLER_MENU);
    }

    public void UIShowAppMenu()
    {
        ShowMenuByID(MenuID.APP_MENU);
    }

    public void ShowMenuByID(MenuID id)
    {
        HideAllMenus();
        // _visibleMenuID = id;
        // _menuToggleButton.SetActive(false);
        _optionsButton.SetActive(false);
        switch (id)
        {
            case MenuID.CONTROLLER_MENU:
                ShowRootMenu();
                break;
            case MenuID.APP_MENU:
                ShowAppMenu();
                break;
        }
    }

    private void HideAllMenus()
    {
        foreach (var p in AllMenus)
        {
            p.SetActive(false);
        }
    }

    public void HideAllPopups()
    {
        foreach (var p in AllPopups)
        {
            p.SetActive(false);
        }
    }

    // TODO: aggregate into a view
    public void ShowAspectRatioPopup()
    {
        _aspectRatioPopup.SetActive(true);
    }

    public void ShowScreenPopup()
    {
        _screenPopup.SetActive(true);
    }

    public void ShowFormatPopup()
    {
        _formatPopup.SetActive(true);
    }

    public void ShowWhatsNewPopup()
    {
        _releaseInfoPopup.SetActive(true);
    }

    public void ShowPictureSettingsPopup()
    {
        _pictureSettingsPopup.SetActive(true);
    }

    public class Lock
    {
        private bool _screenLocked = false;
        private float _brightnessOnLock;

        protected GameObject HideWhenLocked;

        protected GameObject LockScreenNotice;
        protected GameObject MenuToggleButton;
        //
        protected GameObject Logo;

        // TODO: set the following in editor
        // _hideWhenLocked = GameObject.Find("HideWhenScreenLocked");
        // _lockScreenNotice = GameObject.Find("LockScreenNotice");
        // _logo = GameObject.Find("logo");
        // _menuToggleButton = GameObject.Find("MenuToggleButton");

        public void ToggleScreenLock()
        {
            _screenLocked = !_screenLocked;

            if (_screenLocked)
            {
                // Hide All UI except for the lock button
                HideWhenLocked.SetActive(false);
                LockScreenNotice.SetActive(true);
                Logo.SetActive(false);
                MenuToggleButton.SetActive(false);
                // Lower Brightness
                var unityBrightnessOnLock = Screen.brightness;
                Debug.Log($"lockbrightness Unity brightness on lock {unityBrightnessOnLock}");

                _brightnessOnLock = Screen.brightness;
            }
            else
            {
                // Restore Brightness
                Screen.brightness = _brightnessOnLock;

                // Show All UI when screen is unlocked
                HideWhenLocked.SetActive(true);
                LockScreenNotice.SetActive(false);
                Logo.SetActive(true);
                MenuToggleButton.SetActive(true);
            }
        }
    }

}
