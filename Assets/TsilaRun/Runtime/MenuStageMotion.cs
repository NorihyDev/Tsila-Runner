using UnityEngine;

namespace TsilaRun
{
    public sealed class MenuStageMotion : MonoBehaviour
    {
        Renderer[] accents;
        MaterialPropertyBlock block;
        UnityEngine.InputSystem.Accelerometer sensor;
        bool enabledSensor;
        void OnEnable()
        {
            sensor = UnityEngine.InputSystem.Accelerometer.current;
            if (sensor != null && !sensor.enabled) { UnityEngine.InputSystem.InputSystem.EnableDevice(sensor); enabledSensor = true; }
        }
        void OnDisable()
        {
            if (enabledSensor && sensor != null && sensor.added) UnityEngine.InputSystem.InputSystem.DisableDevice(sensor);
            enabledSensor = false;
        }
        void Awake()
        {
            var found = new System.Collections.Generic.List<Renderer>();
            foreach (var renderer in GetComponentsInChildren<Renderer>())
                if (renderer.sharedMaterial != null && renderer.sharedMaterial.name == "Stage Neon") found.Add(renderer);
            accents = found.ToArray(); block = new MaterialPropertyBlock();
        }
        void Update()
        {
            block.SetColor("_EmissionColor", new Color(.05f, .85f, .32f) * (1.8f + Mathf.Sin(Time.unscaledTime * 1.8f) * .2f));
            foreach (var accent in accents) accent.SetPropertyBlock(block);
            // Accelerometer works without a permission prompt; cap motion to keep the platform aligned.
            Vector3 tilt = UnityEngine.InputSystem.Accelerometer.current != null ? UnityEngine.InputSystem.Accelerometer.current.acceleration.ReadValue() / 9.81f : Vector3.zero;
            transform.localRotation = Quaternion.Euler(0, Mathf.Clamp(tilt.x, -.3f, .3f) * 2f, 0);
        }
    }
}
