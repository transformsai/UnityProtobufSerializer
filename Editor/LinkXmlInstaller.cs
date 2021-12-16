
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.UnityLinker;
using UnityEngine;

namespace TransformsAI.Unity.Protobuf.Editor
{
    internal class LinkXmlInstaller  : ScriptableObject, IUnityLinkerProcessor
    {
        public TextAsset XmlFile;

        int IOrderedCallback.callbackOrder => 0;
        string IUnityLinkerProcessor.GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data)
        {

            // Uses Static reference on the script to get link.xml
            var obj = CreateInstance<LinkXmlInstaller>();
            var assetPath = AssetDatabase.GetAssetPath(XmlFile);
            // assets paths are relative to the unity project root, but they don't correspond to actual folders for
            // Packages that are embedded. I.e. it won't work if a package is installed as a git submodule
            // So resolve it to an absolute path:
            return Path.GetFullPath(assetPath);
        }
        void IUnityLinkerProcessor.OnBeforeRun(BuildReport report, UnityLinkerBuildPipelineData data) { }
        void IUnityLinkerProcessor.OnAfterRun(BuildReport report, UnityLinkerBuildPipelineData data) { }
    }
}
