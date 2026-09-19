using UnityEngine;

namespace MagesDementiaGame
{
    public enum RoomPerspective
    {
        Caregiver,
        Recipient
    }

    public enum TelevisionState
    {
        On,
        Lowered,
        Off
    }

    public sealed class RoomView : MonoBehaviour
    {
        private static readonly Vector2 TelevisionPosition = new Vector2(0f, 1.7f);
        private static readonly Vector2 TablePosition = new Vector2(0f, -0.9f);
        private static readonly Vector2 SofaPosition = new Vector2(0f, -2.65f);

        private static Sprite rectangleSprite;

        private RoomArtSet artSet;
        private Color televisionOnColor;
        private bool built;

        public Camera RoomCamera { get; private set; }
        public GameObject Television { get; private set; }
        public GameObject Table { get; private set; }
        public GameObject Sofa { get; private set; }
        public GameObject Photograph { get; private set; }
        public GameObject PhotographSpot { get; private set; }
        public GameObject Lan { get; private set; }
        public GameObject Minh { get; private set; }
        public TopDownPlayerController PlayerController { get; private set; }
        public InteractionController PlayerInteractionController { get; private set; }
        public Vector3 LanDoorwayPosition => new Vector3(-6f, -2.7f, 0f);
        public Vector3 LanConversationPosition => new Vector3(3f, -2.7f, 0f);

        public void Initialize(RoomArtSet sharedArtSet)
        {
            artSet = sharedArtSet;
        }

        public void Build(RoomPerspective perspective)
        {
            if (built)
            {
                return;
            }

            built = true;
            CreateCamera();
            CreateEnvironment();
            CreateCharacters(perspective);
        }

        public void SetLanVisible(bool visible)
        {
            if (Lan != null)
            {
                Lan.SetActive(visible);
            }
        }

        public void SetPhotoVisible(bool visible)
        {
            if (Photograph != null)
            {
                Photograph.SetActive(visible);
            }
        }

        public void SetTelevisionState(TelevisionState state)
        {
            if (Television == null)
            {
                return;
            }

            var renderer = Television.GetComponent<SpriteRenderer>();
            switch (state)
            {
                case TelevisionState.Lowered:
                    renderer.color = Color.Lerp(televisionOnColor, Color.black, 0.4f);
                    break;
                case TelevisionState.Off:
                    renderer.color = Color.Lerp(televisionOnColor, Color.black, 0.75f);
                    break;
                default:
                    renderer.color = televisionOnColor;
                    break;
            }
        }

        private void CreateEnvironment()
        {
            if (artSet != null && artSet.Background != null)
            {
                CreateVisual("Room Background", Vector2.zero, new Vector2(18.84f, 10.6f), Color.white, -10, false, artSet.Background);
                CreateColliderOnly("Top Boundary", new Vector2(0f, 4.65f), new Vector2(18f, 0.5f));
                CreateColliderOnly("Bottom Boundary", new Vector2(0f, -4.55f), new Vector2(18f, 0.5f));
                CreateColliderOnly("Left Boundary", new Vector2(-8.55f, 0f), new Vector2(0.5f, 10f));
                CreateColliderOnly("Right Boundary", new Vector2(8.55f, 0f), new Vector2(0.5f, 10f));
            }
            else
            {
                CreateVisual("Floor", Vector2.zero, new Vector2(15.5f, 8.4f), new Color(0.25f, 0.23f, 0.21f), -10, false);
                CreateVisual("Rug", new Vector2(0.6f, -0.2f), new Vector2(7.4f, 4.5f), new Color(0.28f, 0.14f, 0.16f), -8, false);
                CreateWall("Top Wall", new Vector2(0f, 4.45f), new Vector2(16.4f, 0.6f));
                CreateWall("Bottom Wall", new Vector2(0f, -4.45f), new Vector2(16.4f, 0.6f));
                CreateWall("Left Wall", new Vector2(-8f, 0f), new Vector2(0.6f, 9.5f));
                CreateWall("Right Wall", new Vector2(8f, 0f), new Vector2(0.6f, 9.5f));
            }

            var televisionSprite = artSet != null ? artSet.Television : null;
            Television = CreateVisual("Television", TelevisionPosition, new Vector2(4.8f, 2.23f),
                televisionSprite != null ? Color.white : new Color(0.18f, 0.72f, 0.95f), 1, true, televisionSprite);
            televisionOnColor = Television.GetComponent<SpriteRenderer>().color;

            var tableSprite = artSet != null ? artSet.Table : null;
            Table = CreateVisual("Table", TablePosition, new Vector2(3.4f, 1f),
                tableSprite != null ? Color.white : new Color(0.48f, 0.31f, 0.18f), 0, true, tableSprite, 0.82f);

            Photograph = CreateVisual("Family Photograph", TablePosition + new Vector2(0f, 0.15f), new Vector2(0.55f, 0.42f),
                new Color(0.96f, 0.78f, 0.24f), 3, false);

            PhotographSpot = new GameObject("Photograph Interaction Point");
            PhotographSpot.transform.SetParent(transform);
            PhotographSpot.transform.position = Photograph.transform.position;
            var photoTrigger = PhotographSpot.AddComponent<CircleCollider2D>();
            photoTrigger.radius = 0.75f;
            photoTrigger.isTrigger = true;

            var sofaSprite = artSet != null ? artSet.Sofa : null;
            Sofa = CreateVisual("Sofa", SofaPosition, new Vector2(5.2f, 1.82f),
                sofaSprite != null ? Color.white : new Color(0.4f, 0.17f, 0.21f), 0, true, sofaSprite, 0.35f);
        }

        private void CreateCharacters(RoomPerspective perspective)
        {
            Lan = CreateVisual("Lan", new Vector2(-5.4f, -1.35f), new Vector2(0.7f, 0.95f),
                new Color(0.2f, 0.82f, 0.68f), 4, false);
            Minh = CreateVisual("Minh", new Vector2(4.4f, -0.65f), new Vector2(0.75f, 1f),
                new Color(0.93f, 0.65f, 0.25f), 4, false);

            var controlledCharacter = perspective == RoomPerspective.Caregiver ? Lan : Minh;
            var npc = perspective == RoomPerspective.Caregiver ? Minh : Lan;
            AddCharacterCollider(npc);
            ConfigurePlayer(controlledCharacter);

            if (perspective == RoomPerspective.Recipient)
            {
                Minh.transform.position = new Vector3(4.4f, -1.35f, 0f);
                Lan.transform.position = LanDoorwayPosition;
                Lan.SetActive(false);
            }
        }

        private void ConfigurePlayer(GameObject character)
        {
            var collider = character.AddComponent<BoxCollider2D>();
            var nativeSize = character.GetComponent<SpriteRenderer>().sprite.bounds.size;
            collider.size = nativeSize;

            var body = character.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            PlayerController = character.AddComponent<TopDownPlayerController>();
            PlayerInteractionController = character.AddComponent<InteractionController>();
        }

        private void AddCharacterCollider(GameObject character)
        {
            var collider = character.AddComponent<BoxCollider2D>();
            collider.size = character.GetComponent<SpriteRenderer>().sprite.bounds.size;
        }

        private void CreateCamera()
        {
            RoomCamera = Camera.main;
            if (RoomCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                RoomCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            RoomCamera.orthographic = true;
            RoomCamera.orthographicSize = 5.3f;
            RoomCamera.transform.position = new Vector3(0f, 0f, -10f);
            RoomCamera.clearFlags = CameraClearFlags.SolidColor;
            RoomCamera.backgroundColor = new Color(0.08f, 0.09f, 0.11f);
        }

        private GameObject CreateWall(string name, Vector2 position, Vector2 size)
        {
            return CreateVisual(name, position, size, new Color(0.68f, 0.62f, 0.53f), 0, true);
        }

        private GameObject CreateVisual(
            string name,
            Vector2 position,
            Vector2 size,
            Color color,
            int order,
            bool addCollider,
            Sprite customSprite = null,
            float colliderHeightScale = 1f)
        {
            var visual = new GameObject(name);
            visual.transform.SetParent(transform);
            visual.transform.position = position;

            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = customSprite != null ? customSprite : GetRectangleSprite();
            renderer.color = color;
            renderer.sortingOrder = order;

            var nativeSize = renderer.sprite.bounds.size;
            visual.transform.localScale = new Vector3(size.x / nativeSize.x, size.y / nativeSize.y, 1f);

            if (addCollider)
            {
                var collider = visual.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(nativeSize.x, nativeSize.y * colliderHeightScale);
            }

            return visual;
        }

        private void CreateColliderOnly(string name, Vector2 position, Vector2 size)
        {
            var boundary = new GameObject(name);
            boundary.transform.SetParent(transform);
            boundary.transform.position = position;
            boundary.AddComponent<BoxCollider2D>().size = size;
        }

        private static Sprite GetRectangleSprite()
        {
            if (rectangleSprite == null)
            {
                rectangleSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
                rectangleSprite.name = "Runtime Rectangle";
            }

            return rectangleSprite;
        }
    }
}
