using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Combat.Arena.View
{
    /// <summary>
    /// Thin adapter for the draft screen (G4): binds the authored panel's widgets, relays the
    /// board viewport's clicks as stage UVs, and runs the one presentation-only flourish —
    /// the remote-pick flight from the board toward the opponents' readout. All content and
    /// timing decisions are the presenter's.
    /// </summary>
    public class ArenaDraftView : MonoBehaviour, IArenaDraftView
    {
        private const float FlightSeconds = 0.5f;

        [Tooltip("Root toggled with the draft's visibility")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private RawImage _boardImage;
        [SerializeField] private DraftBoardPointerRelay _boardRelay;
        [SerializeField] private TextMeshProUGUI _turnBanner;
        [SerializeField] private TextMeshProUGUI _orderLine;
        [SerializeField] private TextMeshProUGUI _timerLabel;
        [SerializeField] private TextMeshProUGUI _localReadout;
        [SerializeField] private TextMeshProUGUI _opponentsReadout;
        [SerializeField] private Image _flightIcon;
        [SerializeField] private GameObject _beatOverlay;
        [SerializeField] private TextMeshProUGUI _beatTitle;
        [SerializeField] private TextMeshProUGUI _beatParts;
        [SerializeField] private Button _beatContinueButton;

        private static readonly Color LocalTurnColor = new Color(1f, 0.85f, 0.3f);
        private static readonly Color RemoteTurnColor = new Color(0.85f, 0.85f, 0.9f);

        private Coroutine _flight;

        public event Action<Vector2> BoardClicked;
        public event Action BeatConfirmed;

        private void Awake()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }

            if (_beatOverlay != null)
            {
                _beatOverlay.SetActive(false);
            }

            if (_flightIcon != null)
            {
                _flightIcon.gameObject.SetActive(false);
            }

            if (_boardRelay != null)
            {
                _boardRelay.Clicked += uv => BoardClicked?.Invoke(uv);
            }

            if (_beatContinueButton != null)
            {
                _beatContinueButton.onClick.AddListener(() => BeatConfirmed?.Invoke());
            }
        }

        public void ShowPanel()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(true);
            }
        }

        public void HidePanel()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }
        }

        public void SetTurn(string pickerName, bool isLocalTurn)
        {
            if (_turnBanner == null)
            {
                return;
            }

            _turnBanner.text = isLocalTurn ? "YOUR PICK" : $"{pickerName} is picking…";
            _turnBanner.color = isLocalTurn ? LocalTurnColor : RemoteTurnColor;
        }

        public void SetOrderLine(string orderLine)
        {
            if (_orderLine != null)
            {
                _orderLine.text = orderLine;
            }
        }

        public void SetTimer(float secondsLeft, bool visible)
        {
            if (_timerLabel == null)
            {
                return;
            }

            _timerLabel.gameObject.SetActive(visible);
            if (visible)
            {
                _timerLabel.text = $"{Mathf.CeilToInt(Mathf.Max(0f, secondsLeft))}s";
            }
        }

        public void SetBoardTexture(Texture texture)
        {
            if (_boardImage != null)
            {
                _boardImage.texture = texture;
            }
        }

        public void SetLocalReadout(string text)
        {
            if (_localReadout != null)
            {
                _localReadout.text = text;
            }
        }

        public void SetOpponentsReadout(string text)
        {
            if (_opponentsReadout != null)
            {
                _opponentsReadout.text = text;
            }
        }

        public void PlayRemotePickFlight(Vector2 boardViewportUv, Sprite partIcon)
        {
            if (_flightIcon == null || _boardImage == null || _opponentsReadout == null)
            {
                return;
            }

            if (_flight != null)
            {
                StopCoroutine(_flight);
            }

            var boardRect = (RectTransform)_boardImage.transform;
            var rect = boardRect.rect;
            var localStart = new Vector2(
                rect.xMin + boardViewportUv.x * rect.width,
                rect.yMin + boardViewportUv.y * rect.height);
            var from = boardRect.TransformPoint(localStart);
            var to = _opponentsReadout.transform.position;

            _flightIcon.sprite = partIcon;
            _flightIcon.enabled = partIcon != null;
            _flight = StartCoroutine(Fly(from, to));
        }

        public Vector2 BoardUvToScreen(Vector2 boardViewportUv)
        {
            if (_boardImage == null)
            {
                return Vector2.zero;
            }

            var boardRect = (RectTransform)_boardImage.transform;
            var rect = boardRect.rect;
            var local = new Vector2(
                rect.xMin + boardViewportUv.x * rect.width,
                rect.yMin + boardViewportUv.y * rect.height);
            return boardRect.TransformPoint(local);
        }

        public void ShowBeat(string title, string partsText)
        {
            if (_beatOverlay == null)
            {
                return;
            }

            if (_beatTitle != null)
            {
                _beatTitle.text = title;
            }

            if (_beatParts != null)
            {
                _beatParts.text = partsText;
            }

            _beatOverlay.SetActive(true);
        }

        public void HideBeat()
        {
            if (_beatOverlay != null)
            {
                _beatOverlay.SetActive(false);
            }
        }

        private IEnumerator Fly(Vector3 from, Vector3 to)
        {
            _flightIcon.gameObject.SetActive(true);
            float elapsed = 0f;
            while (elapsed < FlightSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / FlightSeconds);
                _flightIcon.transform.position = Vector3.Lerp(from, to, t);
                yield return null;
            }

            _flightIcon.gameObject.SetActive(false);
            _flight = null;
        }
    }
}
