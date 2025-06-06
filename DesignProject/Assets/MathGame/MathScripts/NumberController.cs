using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class NumberController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private int numberValue;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector3 originalPosition;
    private Canvas canvas;
    private GameManager gameManager;
    private bool isPlaced = false; // Sayýnýn bir drop zone'a yerleþtirilip yerleþtirilmediðini takip eder

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
        isPlaced = false; // Yeni initialize edildiðinde yerleþtirilmemiþ durumda
        SaveOriginalState();
    }

    private void SaveOriginalState()
    {
        originalPosition = rectTransform.localPosition;
        originalParent = transform.parent;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Eðer sayý zaten yerleþtirilmiþse ve doðru yerleþtirildiyse, sürüklemeyi engelle
        if (isPlaced && transform.parent.CompareTag("DropZone"))
        {
            return;
        }

        SaveOriginalState();
        // Canvas'ýn en üstüne taþý
        transform.SetParent(canvas.transform);
        transform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Eðer sayý zaten doðru yerleþtirildiyse, sürüklemeyi engelle
        if (isPlaced && originalParent.CompareTag("DropZone"))
        {
            return;
        }

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

            // Eðer bu drop zone zaten dolu ise, yerleþtirmeyi engelle
            if (IsDropZoneOccupied(dropZone))
            {
                ResetPosition();
                Debug.Log("Drop zone is already occupied!");
                return;
            }

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

                isPlaced = true; // Baþarýyla yerleþtirildi

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

        isPlaced = false; // Yerleþtirme durumunu sýfýrla
    }

    // Drop zone'un dolu olup olmadýðýný kontrol et
    private bool IsDropZoneOccupied(Transform dropZone)
    {
        foreach (Transform child in dropZone)
        {
            NumberController numberController = child.GetComponent<NumberController>();
            if (numberController != null && numberController != this)
            {
                return true; // Baþka bir sayý zaten yerleþtirilmiþ
            }
        }
        return false;
    }

    // Sayýnýn deðerini döndüren metod (DropZone için)
    public int GetNumberValue()
    {
        return numberValue;
    }

    // Sayýnýn yerleþtirilip yerleþtirilmediðini kontrol eden metod
    public bool IsPlaced()
    {
        return isPlaced;
    }

    // Sayýyý sýfýrlama metodu (yeni round için)
    public void ResetNumber()
    {
        isPlaced = false;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }
}