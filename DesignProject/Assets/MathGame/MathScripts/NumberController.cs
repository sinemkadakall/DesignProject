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
    private bool isPlaced = false; // Say�n�n bir drop zone'a yerle�tirilip yerle�tirilmedi�ini takip eder

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
        isPlaced = false; // Yeni initialize edildi�inde yerle�tirilmemi� durumda
        SaveOriginalState();
    }

    private void SaveOriginalState()
    {
        originalPosition = rectTransform.localPosition;
        originalParent = transform.parent;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // E�er say� zaten yerle�tirilmi�se ve do�ru yerle�tirildiyse, s�r�klemeyi engelle
        if (isPlaced && transform.parent.CompareTag("DropZone"))
        {
            return;
        }

        SaveOriginalState();
        // Canvas'�n en �st�ne ta��
        transform.SetParent(canvas.transform);
        transform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // E�er say� zaten do�ru yerle�tirildiyse, s�r�klemeyi engelle
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

            // E�er bu drop zone zaten dolu ise, yerle�tirmeyi engelle
            if (IsDropZoneOccupied(dropZone))
            {
                ResetPosition();
                Debug.Log("Drop zone is already occupied!");
                return;
            }

            // GameManager �zerinden cevab� kontrol et
            if (gameManager != null && gameManager.CheckAnswer(numberValue, dropZone))
            {
                // Do�ru cevap
                transform.SetParent(dropZone);
                rectTransform.anchoredPosition = Vector2.zero;

                // Merkeze hizala
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);

                isPlaced = true; // Ba�ar�yla yerle�tirildi

                Debug.Log($"Correct answer: {numberValue} placed in zone");
            }
            else
            {
                // Yanl�� cevap
                ResetPosition();
                Debug.Log($"Wrong answer: {numberValue} returned to original position");
            }
        }
        else
        {
            // Ge�erli bir drop zone'a b�rak�lmad�
            ResetPosition();
            Debug.Log("Not dropped on a valid drop zone");
        }
    }

    private void ResetPosition()
    {
        transform.SetParent(originalParent);
        rectTransform.localPosition = originalPosition;

        // Orijinal anchor ve pivot de�erlerini geri y�kle
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        isPlaced = false; // Yerle�tirme durumunu s�f�rla
    }

    // Drop zone'un dolu olup olmad���n� kontrol et
    private bool IsDropZoneOccupied(Transform dropZone)
    {
        foreach (Transform child in dropZone)
        {
            NumberController numberController = child.GetComponent<NumberController>();
            if (numberController != null && numberController != this)
            {
                return true; // Ba�ka bir say� zaten yerle�tirilmi�
            }
        }
        return false;
    }

    // Say�n�n de�erini d�nd�ren metod (DropZone i�in)
    public int GetNumberValue()
    {
        return numberValue;
    }

    // Say�n�n yerle�tirilip yerle�tirilmedi�ini kontrol eden metod
    public bool IsPlaced()
    {
        return isPlaced;
    }

    // Say�y� s�f�rlama metodu (yeni round i�in)
    public void ResetNumber()
    {
        isPlaced = false;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }
}