using System.IO;
using System.Xml;
using UnityEditor.Android;

namespace ProjectBootstrap
{
    // Declares the helper activity that opens the gallery (Assets/Plugins/Android/PhotoPickerActivity.java).
    // Unity's own manifest has no place for it, so it's added to the generated unityLibrary's.
    class PhotoPickerManifest : IPostGenerateGradleAndroidProject
    {
        const string k_AndroidNamespace = "http://schemas.android.com/apk/res/android";
        const string k_Activity = "com.brenpoly.cardboardxr.PhotoPickerActivity";

        public int callbackOrder => 1;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
            var document = new XmlDocument();
            document.Load(manifestPath);
            var application = document.DocumentElement["application"];
            if (application == null)
                return;
            foreach (XmlElement activity in application.GetElementsByTagName("activity"))
            {
                if (activity.GetAttribute("name", k_AndroidNamespace) == k_Activity)
                    return;
            }

            var element = document.CreateElement("activity");
            element.SetAttribute("name", k_AndroidNamespace, k_Activity);
            element.SetAttribute("exported", k_AndroidNamespace, "false");
            element.SetAttribute("theme", k_AndroidNamespace, "@android:style/Theme.Translucent.NoTitleBar");
            element.SetAttribute("screenOrientation", k_AndroidNamespace, "landscape");
            element.SetAttribute("configChanges", k_AndroidNamespace, "orientation|screenSize|keyboardHidden");
            application.AppendChild(element);
            document.Save(manifestPath);
        }
    }
}
