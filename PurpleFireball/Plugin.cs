using BepInEx;
using UnityEngine;
using UnityEngine.XR;

namespace PurpleFireball
{
    [BepInPlugin("com.batcat.purplefireball", "Purple Fireball", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        // Hold a grip to summon a fireball in that hand, release to throw it. Works on both hands.
        const float ThrowBoost = 1.4f;   // multiplies your hand speed when throwing
        const float MaxSpeed = 25f;

        // Where the fireball sits relative to your hand (x = right, y = up, z = forward).
        // (0,0,0) is the middle of the hand. Nudge these if it floats off the palm.
        static readonly Vector3 HoldOffset = new Vector3(0f, 0f, 0f);

        class HandState
        {
            public string Name;
            public XRNode Node;
            public FireballProjectile Held;
            public Vector3 LastPos;
            public Vector3 Velocity;
            public bool WasGripping;
            public bool LoggedHand;
        }

        readonly HandState right = new HandState { Name = "Right", Node = XRNode.RightHand };
        readonly HandState left = new HandState { Name = "Left", Node = XRNode.LeftHand };
        float nextStatusLog;

        void Awake()
        {
            Logger.LogInfo("Purple Fireball loaded!");
        }

        void Update()
        {
            // Wait until the player rig exists
            if (GorillaTagger.Instance == null)
            {
                if (Time.time > nextStatusLog)
                {
                    nextStatusLog = Time.time + 5f;
                    Logger.LogInfo("Waiting for GorillaTagger.Instance...");
                }
                return;
            }

            Tick(right, GorillaTagger.Instance.rightHandTransform);
            Tick(left, GorillaTagger.Instance.leftHandTransform);
        }

        bool ReadGrip(HandState h, out string source)
        {
            source = "none";

            var poller = ControllerInputPoller.instance;
            if (poller != null)
            {
                bool pressed = h.Node == XRNode.RightHand
                    ? (poller.rightGrab || poller.rightControllerGripFloat > 0.6f)
                    : (poller.leftGrab || poller.leftControllerGripFloat > 0.6f);
                if (pressed)
                {
                    source = "ControllerInputPoller";
                    return true;
                }
            }

            // Backup: raw XR input
            InputDevice device = InputDevices.GetDeviceAtXRNode(h.Node);
            bool btn;
            float axis;
            if (device.TryGetFeatureValue(CommonUsages.gripButton, out btn) && btn)
            {
                source = "XR gripButton";
                return true;
            }
            if (device.TryGetFeatureValue(CommonUsages.grip, out axis) && axis > 0.6f)
            {
                source = "XR grip axis";
                return true;
            }
            return false;
        }

        void Tick(HandState h, Transform hand)
        {
            if (hand == null) return;

            if (!h.LoggedHand)
            {
                h.LoggedHand = true;
                Logger.LogInfo("Found " + h.Name + " hand transform: " + hand.name);
                h.LastPos = hand.position;
            }

            // Smoothed hand velocity (world space)
            Vector3 raw = (hand.position - h.LastPos) / Mathf.Max(Time.deltaTime, 0.0001f);
            h.Velocity = Vector3.Lerp(h.Velocity, raw, 0.5f);
            h.LastPos = hand.position;

            string source;
            bool gripping = ReadGrip(h, out source);

            // Grip pressed: summon
            if (gripping && !h.WasGripping)
            {
                Logger.LogInfo(h.Name + " grip pressed (source: " + source + ")");
                if (h.Held == null)
                {
                    h.Held = FireballProjectile.Create(hand.TransformPoint(HoldOffset));
                }
            }

            // Grip held: follow the hand
            if (gripping && h.Held != null)
            {
                h.Held.transform.position = hand.TransformPoint(HoldOffset);
            }

            // Grip released: throw
            if (!gripping && h.WasGripping && h.Held != null)
            {
                Vector3 v = Vector3.ClampMagnitude(h.Velocity * ThrowBoost, MaxSpeed);
                h.Held.Launch(v);
                h.Held = null;
                Logger.LogInfo(h.Name + " fireball thrown at speed " + v.magnitude);
            }

            h.WasGripping = gripping;
        }
    }
}