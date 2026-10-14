using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 가운데 뜨는 창. 반투명 막 + 한지 판 + 위쪽 붉은 띠 제목. 열릴 때 통 튀어나오고 닫힐 때 줄어든다.
/// 내용은 여는 쪽에서 Panel 아래에 직접 놓는다 (위에서부터 y 좌표로).
/// </summary>
public class Popup : MonoBehaviour
{
    public RectTransform Panel { get; private set; }
    /// <summary>뒤로 가기 / 막 누르기. 비우면 닫기만 한다. null을 돌려주는 대신 CanClose=false로 막을 수 있다.</summary>
    public Action OnBackPressed;
    public bool CanClose = true;
    public event Action Closed;

    CanvasGroup group;
    bool closing;

    public static Popup Open(Vector2 size, string title, bool tapOutsideToClose = false)
    {
        var root = Ui.Rect("Popup", App.I.PopupLayer);
        Ui.Stretch(root);
        var popup = root.gameObject.AddComponent<Popup>();
        popup.group = root.gameObject.AddComponent<CanvasGroup>();
        Ui.Dim(root, 0.55f, tapOutsideToClose ? (Action)popup.Back : () => { });

        var panel = Ui.Panel(root, size);
        Ui.Place(panel, 0.5f, 0.5f, size);
        popup.Panel = panel;

        if (!string.IsNullOrEmpty(title))
        {
            // 위쪽 붉은 띠
            var ribbon = Ui.Image(panel, Art.Round, Art.Red, "Ribbon");
            Ui.Place(ribbon.rectTransform, 0.5f, 1f, new Vector2(Mathf.Min(size.x - 80f, 640f), 120f), new Vector2(0f, 8f));
            var lip = Ui.Image(ribbon.rectTransform, Art.Round, Art.RedDark, "RibbonLip");
            Ui.Stretch(lip.rectTransform);
            lip.rectTransform.offsetMax = new Vector2(0f, -100f);
            lip.transform.SetAsFirstSibling();
            var edge = Ui.Image(ribbon.rectTransform, Art.RoundOutline, Art.Gold, "RibbonEdge");
            Ui.Stretch(edge.rectTransform, 6f);
            var t = Ui.Text(ribbon.rectTransform, title, 64, Color.white, TextAnchor.MiddleCenter, "Title");
            Ui.Stretch(t.rectTransform);
            Ui.Shadow(t, new Color(0f, 0f, 0f, 0.35f), 4f);
        }

        App.I.Register(popup);
        popup.group.alpha = 0f;
        Anim.Run(popup, 0.28f, k =>
        {
            popup.group.alpha = Mathf.Clamp01(k * 3f);
            panel.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, Anim.OutBack(k));
        });
        return popup;
    }

    public void Back()
    {
        if (closing) return;
        if (OnBackPressed != null) OnBackPressed();
        else if (CanClose) Close();
    }

    public void Close(bool immediate = false)
    {
        if (closing) return;
        closing = true;
        App.I.Unregister(this);
        group.blocksRaycasts = false;
        Closed?.Invoke();
        if (immediate)
        {
            Destroy(gameObject);
            return;
        }
        Anim.Run(this, 0.16f, k =>
        {
            group.alpha = 1f - k;
            Panel.localScale = Vector3.one * Mathf.Lerp(1f, 0.85f, k);
        }, () => Destroy(gameObject));
    }

    // ------------------------------------------------------------ 내용 도우미 (y: 판 위쪽에서 아래로 잰 중심 위치)
    public Text Label(string text, int size, Color color, float y, float height = 100f, float width = 0f)
    {
        var t = Ui.Text(Panel, text, size, color);
        float w = width > 0f ? width : Panel.sizeDelta.x - 100f;
        Ui.Place(t.rectTransform, 0.5f, 1f, new Vector2(w, height), new Vector2(0f, -y));
        return t;
    }

    public Button Button(string label, Sprite icon, Color face, Color lip, float y, Action onClick, float width = 0f, float x = 0f, float height = 130f)
    {
        float w = width > 0f ? width : Panel.sizeDelta.x - 140f;
        var b = Ui.Button(Panel, new Vector2(w, height), face, lip, label, icon, onClick);
        Ui.Place((RectTransform)b.transform, 0.5f, 1f, new Vector2(w, height), new Vector2(x, -y));
        return b;
    }

    /// <summary>켜기/끄기 줄: 왼쪽에 이름, 오른쪽에 스위치 버튼.</summary>
    public Button Toggle(string label, float y, Func<bool> get, Action<bool> set)
    {
        var t = Ui.Text(Panel, label, 54, Art.Ink, TextAnchor.MiddleLeft, "ToggleLabel");
        Ui.Place(t.rectTransform, 0.5f, 1f, new Vector2(Panel.sizeDelta.x - 140f, 110f), new Vector2(0f, -y));
        Button b = null;
        void Refresh()
        {
            bool on = get();
            Ui.SetLabel(b, on ? Loc.T("켬", "ON") : Loc.T("끔", "OFF"));
            if (on) Ui.SetColors(b, Art.Green, Art.GreenDark);
            else Ui.SetColors(b, Art.Grey, Art.GreyDark);
        }
        b = Ui.Button(Panel, new Vector2(200f, 100f), Art.Green, Art.GreenDark, " ", null, () =>
        {
            set(!get());
            Refresh();
        }, 46);
        Ui.Place((RectTransform)b.transform, 1f, 1f, new Vector2(200f, 100f), new Vector2(-170f, -y));
        Refresh();
        return b;
    }
}
