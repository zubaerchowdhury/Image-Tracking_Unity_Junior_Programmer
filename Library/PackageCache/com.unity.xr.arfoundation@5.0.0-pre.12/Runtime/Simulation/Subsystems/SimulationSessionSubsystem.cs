using System;
using Unity.XR.CoreUtils;
using UnityEngine.XR.ARSubsystems;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.XR.Simulation
{
    /// <summary>
    /// Simulation implementation of
    /// [XRSessionSubsystem](xref:UnityEngine.XR.ARSubsystems.XRSessionSubsystem).
    /// </summary>
    public sealed class SimulationSessionSubsystem : XRSessionSubsystem
    {
        internal const string k_SubsystemId = "XRSimulation-Session";

        static SimulationSceneManager s_SimulationSceneManager;

        internal static SimulationSceneManager simulationSceneManager => s_SimulationSceneManager;

        class SimulationProvider : Provider
        {
            CameraPoseProvider m_CameraPoseProvider;
            SimulationMeshSubsystem m_MeshSubsystem;
            SimulationEnvironmentScanner m_SimulationEnvironmentScanner;

            Camera m_XROriginCamera;
            int m_PreviousCullingMask;

            public override TrackingState trackingState => TrackingState.Tracking;

            public override Promise<SessionAvailability> GetAvailabilityAsync() =>
                Promise<SessionAvailability>.CreateResolvedPromise(SessionAvailability.Installed | SessionAvailability.Supported);

            public override void Start()
            {
#if UNITY_EDITOR
                SimulationSubsystemAnalytics.SubsystemStarted(k_SubsystemId);
#endif

                s_SimulationSceneManager = new SimulationSceneManager();
                if (XRSimulationPreferences.Instance.enableNavigation)
                    m_CameraPoseProvider = CameraPoseProvider.AddPoseProviderToScene();

                if (SimulationMeshSubsystem.GetActiveSubsystemInstance() != null)
                {
                    m_MeshSubsystem = new SimulationMeshSubsystem();
                    m_MeshSubsystem.Start();
                }

                SetupSimulation();

                var xrOrigin = Object.FindObjectOfType<XROrigin>();

                if (xrOrigin == null)
                    throw new NullReferenceException($"An XR Origin is required in the scene, none found.");

                m_XROriginCamera = xrOrigin.Camera;
                m_PreviousCullingMask = m_XROriginCamera.cullingMask;
                m_XROriginCamera.cullingMask &= ~(1 << XRSimulationRuntimeSettings.Instance.environmentLayer);

                m_SimulationEnvironmentScanner = SimulationEnvironmentScanner.instance;
                m_SimulationEnvironmentScanner.Initialize(xrOrigin,
                    s_SimulationSceneManager.environmentScene.GetPhysicsScene(),
                    s_SimulationSceneManager.simulationEnvironment.gameObject);

#if UNITY_EDITOR
                AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
#endif
            }

            public override void Stop()
            {
                if (m_XROriginCamera)
                    m_XROriginCamera.cullingMask = m_PreviousCullingMask;

                ShutdownSimulation();

                if (m_CameraPoseProvider != null)
                {
                    Object.Destroy(m_CameraPoseProvider.gameObject);
                    m_CameraPoseProvider = null;
                }

                s_SimulationSceneManager = null;

                m_SimulationEnvironmentScanner.Dispose();
                m_SimulationEnvironmentScanner = null;

                if (m_MeshSubsystem != null)
                {
                    m_MeshSubsystem.Dispose();
                    m_MeshSubsystem = null;
                }

#if UNITY_EDITOR
                AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
#endif
            }

            public override void Update(XRSessionUpdateParams updateParams)
            {
                m_SimulationEnvironmentScanner.Update();
            }

            void SetupSimulation()
            {
                s_SimulationSceneManager.SetupEnvironment();
                m_CameraPoseProvider.SetSimulationEnvironment(s_SimulationSceneManager.simulationEnvironment);
            }

            void ShutdownSimulation()
            {
                s_SimulationSceneManager.TearDownEnvironment();
            }

#if UNITY_EDITOR
            void OnBeforeAssemblyReload()
            {
                Debug.LogError(
                    "XR Simulation does not support script recompilation while playing. To disable script compilation" +
                    " while playing, in the Preferences window under <b>General > Script Changes While Playing</b>,"+
                    " select either <b>Recompile After Finished Playing</b> or <b>Stop Playing and Recompile</b>.");
            }
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RegisterDescriptor()
        {
            XRSessionSubsystemDescriptor.RegisterDescriptor(new XRSessionSubsystemDescriptor.Cinfo {
                id = k_SubsystemId,
                providerType = typeof(SimulationProvider),
                subsystemTypeOverride = typeof(SimulationSessionSubsystem),
                supportsInstall = false,
                supportsMatchFrameRate = false,
            });
        }
    }
}
