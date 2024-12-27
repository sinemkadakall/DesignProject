using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class NumberController : MonoBehaviour,IBeginDragHandler, IDragHandler, IEndDragHandler
{

    private int numberValue;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector3 originalPosition;
    private Canvas canvas;
    private GameManager gameManager;

    

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        gameManager = FindObjectOfType<GameManager>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void Initialize(int value)
    {
        numberValue = value;
        SaveOriginalState();
    }

    private void SaveOriginalState()
    {
        originalPosition = rectTransform.localPosition;
        originalParent = transform.parent;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        SaveOriginalState();

        // Canvas'ýn en üstüne taþý
        transform.SetParent(canvas.transform);
        transform.SetAsLastSibling();

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;

        Vector2 position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out position);

        rectTransform.position = canvas.transform.TransformPoint(position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
         canvasGroup.blocksRaycasts = true;
         canvasGroup.alpha = 1f;

         GameObject droppedObject = eventData.pointerCurrentRaycast.gameObject;

         if (droppedObject != null && droppedObject.CompareTag("DropZone"))
         {
             Transform dropZone = droppedObject.transform;

             // GameManager üzerinden cevabý kontrol et
             if (gameManager != null && gameManager.CheckAnswer(numberValue, dropZone))
             {
                 // Doðru cevap
                 transform.SetParent(dropZone);
                 rectTransform.anchoredPosition = Vector2.zero;

                 // Merkeze hizala
                 rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                 rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                 rectTransform.pivot = new Vector2(0.5f, 0.5f);
                 Debug.Log($"Correct answer: {numberValue} placed in zone");
             }
             else
             {
                 // Yanlýþ cevap
                 ResetPosition();
                 Debug.Log($"Wrong answer: {numberValue} returned to original position");
             }
         }
         else
         {
             // Geçerli bir drop zone'a býrakýlmadý
             ResetPosition();
             Debug.Log("Not dropped on a valid drop zone");
         }
    }

    private void ResetPosition()
    {
        transform.SetParent(originalParent);
        rectTransform.localPosition = originalPosition;

        // Orijinal anchor ve pivot deðerlerini geri yükle
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }
}