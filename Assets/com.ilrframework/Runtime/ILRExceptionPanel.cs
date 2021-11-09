using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace com.ilrframework.Runtime
{
    public class ILRExceptionPanel
    {
        private static bool _canvasCreated = false;
        private static Canvas _canvas;
        
        public static void Show(string content) {
	        CreateCanvas();
	        
        	var panel = new GameObject("ILRExceptionPanel");
        
        	var rootRect = panel.AddComponent<RectTransform>();
        	rootRect.anchoredPosition = new Vector2(Screen.width / 2.0f, Screen.height / 2.0f);
        	rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Screen.width - 300);
            rootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Screen.height - 300);

            var canvasGroup = panel.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0;

        	// 背景
        	var bgImg = panel.AddComponent<Image>();
        	bgImg.color = new Color(0.0f, 0.0f, 0.0f, 0.75f);
        	bgImg.raycastTarget = true;
            
            // Scroll View
            var scrollObj = new GameObject("Scroll View");
            scrollObj.transform.SetParent(panel.transform);
            var scrollViewRect = scrollObj.AddComponent<RectTransform>();
            scrollViewRect.anchorMin = new Vector2(0, 0);
            scrollViewRect.anchorMax = new Vector2(1, 1);
            SetRect(scrollViewRect, 20, 20, 20, 20);
            
            var scrollView = scrollObj.AddComponent<ScrollRect>();
            scrollView.horizontal = false;
            scrollView.vertical = true;
            
            // Viewport
            var viewPort = new GameObject("Viewport");
            viewPort.transform.SetParent(scrollObj.transform);
            var viewPortRect = viewPort.AddComponent<RectTransform>();
            scrollView.viewport = viewPortRect;
            viewPortRect.anchorMin = new Vector2(0, 0);
            viewPortRect.anchorMax = new Vector2(1, 1);
            SetRect(viewPortRect, 0, 0, 0, 0);

            viewPort.AddComponent<Image>();
            viewPort.AddComponent<Mask>().showMaskGraphic = false;
            
            // Container
            var container = new GameObject("Container");
            container.transform.SetParent(viewPort.transform);
            var containerRect = container.AddComponent<RectTransform>();
            scrollView.content = containerRect;
            containerRect.pivot = new Vector2(0.5f, 1f);
            containerRect.anchorMin = new Vector2(0, 0);
            containerRect.anchorMax = new Vector2(1, 1);
            SetRect(containerRect, 0, 0, 0, 0);

            container.AddComponent<VerticalLayoutGroup>();
            container.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        	// 文本子节点
        	var textObj = new GameObject("Text");
        	textObj.transform.SetParent(container.transform);
        	var textRect = textObj.AddComponent<RectTransform>();
        	textRect.anchorMin = new Vector2(0, 0);
        	textRect.anchorMax = new Vector2(1, 1);
            SetRect(textRect, 0, 0, 0, 0);
        
        	// 文本内容
        	var contentText = textObj.AddComponent<Text>();
        	contentText.text = content;
        	contentText.color = Color.white;
        	contentText.font = Font.CreateDynamicFontFromOSFont("Arial", 22);
        	contentText.fontSize = 30;
        	contentText.alignment = TextAnchor.UpperLeft;
        	contentText.raycastTarget = true;
            contentText.horizontalOverflow = HorizontalWrapMode.Wrap;

        	panel.transform.SetParent(_canvas.transform);
            
            // Close Button
            var closeButton = DefaultControls.CreateButton(new DefaultControls.Resources());
            closeButton.name = "CloseButton";
            closeButton.transform.SetParent(panel.transform);
            var closeBtnRect = closeButton.GetComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(1, 0);
            closeBtnRect.anchorMax = new Vector2(1, 0);
            closeBtnRect.pivot = new Vector2(0.5f, 0.5f);
            closeBtnRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200);
            closeBtnRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50);
            closeBtnRect.anchoredPosition = new Vector2(-120, 45);

            var closeBtnText = closeButton.transform.Find("Text").GetComponent<Text>();
            closeBtnText.fontSize = 30;
            closeBtnText.text = "OK";
            
            closeButton.GetComponent<Button>().onClick.AddListener(delegate { OnToggleCloseButton(panel); });

            canvasGroup.alpha = 1;
        }

        private static void CreateCanvas() {
	        if (_canvasCreated) return;
	        
	        // 创建canvas
	        var go = new GameObject("ILRExceptionCanvas", typeof(Canvas));
	        Object.DontDestroyOnLoad(go);

	        _canvas = go.GetComponent<Canvas>();
	        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
	        _canvas.sortingOrder = Int16.MaxValue;
	        
	        var canvasScaler = go.AddComponent<CanvasScaler>();
	        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
	        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
	        canvasScaler.referenceResolution = new Vector2(2436, 1125);

	        go.AddComponent<GraphicRaycaster>();

	        _canvasCreated = true;
        }

        private static void OnToggleCloseButton(GameObject panel) {
	        Object.DestroyImmediate(panel);
        }
        
        private static void SetLeft(RectTransform rt, float left) {
	        rt.offsetMin = new Vector2(left, rt.offsetMin.y);
        }
 
        private static void SetRight(RectTransform rt, float right) {
	        rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
        }
 
        private static void SetTop(RectTransform rt, float top) {
	        rt.offsetMax = new Vector2(rt.offsetMax.x, -top);
        }
 
        private static void SetBottom(RectTransform rt, float bottom) {
	        rt.offsetMin = new Vector2(rt.offsetMin.x, bottom);
        }

        private static void SetRect(RectTransform rt, float left, float right, float top, float bottom) {
	        SetLeft(rt, left);
	        SetRight(rt, right);
	        SetTop(rt, top);
	        SetBottom(rt, bottom);
        }
    }
}