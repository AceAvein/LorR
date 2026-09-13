using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;

public class ChangeProfileImage : MonoBehaviour
{
    public Image iconPreview;
    public string SelectedImagePath { get; private set; }

    void Awake()
    {
        Debug.Log("[AWAKE] ChangeProfileImage on GameObject: " + gameObject.name + " | InstanceID: " + GetInstanceID());
    }

    public void OnChangeProfileClicked()
    {
        Debug.Log("[CLICKED] OnChangeProfileClicked called on GameObject: " + gameObject.name);
#if UNITY_ANDROID && !UNITY_EDITOR
        PickImageAndroid();
#else
        StartCoroutine(OpenFileBrowser());
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    void PickImageAndroid()
    {
        NativeGalleryNamespace.NativeGallery.GetImageFromGallery((path) =>
        {
            if (path == null)
                return;
            Texture2D texture = NativeGalleryNamespace.NativeGallery.LoadImageAtPath(path, 1024);
            if (texture == null)
                return;
            SetImage(texture, path);
        }, "Select Profile Picture", "image/*");
    }
#endif

    IEnumerator OpenFileBrowser()
    {
        yield return SimpleFileBrowser.FileBrowser.WaitForLoadDialog(
            SimpleFileBrowser.FileBrowser.PickMode.Files,
            false,
            null,
            null,
            "Select Profile Picture",
            "Select");
        if (!SimpleFileBrowser.FileBrowser.Success)
            yield break;
        string path = SimpleFileBrowser.FileBrowser.Result[0];
        byte[] bytes = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(bytes);
        SetImage(texture, path);
    }

    void SetImage(Texture2D texture, string originalPath)
    {
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f));
        iconPreview.sprite = sprite;
        string fileName = "profile_" + System.Guid.NewGuid() + Path.GetExtension(originalPath);
        string destination = Path.Combine(Application.persistentDataPath, fileName);
        File.Copy(originalPath, destination, true);
        SelectedImagePath = destination;
        Debug.Log("[SETIMAGE] Called on GameObject: " + gameObject.name + " | InstanceID: " + GetInstanceID() + " | Path: " + SelectedImagePath);
    }
}