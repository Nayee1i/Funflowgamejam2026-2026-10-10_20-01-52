using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VerifyPhysicalMirrorReflection
{
    [InitializeOnLoadMethod]
    static void Hook() => EditorApplication.update += CheckRequest;

    static void CheckRequest()
    {
        const string request = "Tools/verify-physical-mirrors.request";
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || !File.Exists(request)) return;
        File.Delete(request);
        try
        {
            Verify();
            File.WriteAllText("Tools/physical-mirrors-result.txt", "PASSED: collider contact, angled reflection, no phantom face, repeated mirror hit, projectile/preview agreement and red charge.");
        }
        catch (Exception error)
        {
            File.WriteAllText("Tools/physical-mirrors-result.txt", "FAILED: " + error);
            Debug.LogException(error);
        }
    }

    public static void Verify()
    {
        var original = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        // Keep synthetic geometry away from colliders in other loaded scenes.
        Vector2 center = new Vector2(10000, 10000);
        try
        {
            var mirrorObject = new GameObject("Physical mirror verification");
            mirrorObject.transform.position = center;
            var collider = mirrorObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(4, .1f);
            var mirror = mirrorObject.AddComponent<MirrorRicochetSurface>();
            mirror.chargesShot = true;
            mirror.localReflectionPoint = new Vector2(0, -1);
            mirror.reflectFromCollider = false;
            Physics2D.SyncTransforms();
            Vector2 origin = center + Vector2.down * 2;
            var contact = FirstLevelShotPath.Cast(origin, Vector2.up, .035f, 3);
            Require(contact.mirror == mirror && contact.mirrorCollider == collider, "Physical collider must supply contact");
            Require(Mathf.Abs(contact.distance - 1.915f) < .005f, "Contact must include ball radius and ignore legacy face position");
            Require(Vector2.Distance(contact.Reflect(Vector2.up), Vector2.down) < .001f, "Head-on reflection");
            // Returning to a previously hit mirror must still collide with it.
            Require(FirstLevelShotPath.Cast(origin, Vector2.up, .035f, 3, collider).mirror == mirror, "Previously hit mirror must not be permanently ignored");
            var path = new List<FirstLevelAimPreview.Segment>();
            FirstLevelAimPreview.Trace(origin, Vector2.up, .035f, 3, path);
            var shot = FirstLevelShot.Create(origin, Vector2.up, null, null);
            shot.Step(3);
            Require(path.Count == 2 && shot.Reflections == 1 && shot.Charged, "One real bounce and red charge");
            Require(Vector2.Distance(shot.transform.position, path[1].end) < .005f, "Projectile and preview endpoints must match");
            UnityEngine.Object.DestroyImmediate(shot.gameObject);
            mirrorObject.transform.rotation = Quaternion.Euler(0, 0, 45);
            Physics2D.SyncTransforms();
            origin = center + (Vector2)mirrorObject.transform.up * -2;
            Vector2 incoming = ((Vector2)mirrorObject.transform.up + (Vector2)mirrorObject.transform.right * .25f).normalized;
            contact = FirstLevelShotPath.Cast(origin, incoming, .035f, 4);
            Require(contact.mirror == mirror, "Angled shot must hit rotated collider");
            Require(Vector2.Distance(contact.Reflect(incoming), Vector2.Reflect(incoming, mirrorObject.transform.up)) < .001f, "Rotated collider normal must determine reflection");
            collider.enabled = false;
            Physics2D.SyncTransforms();
            Require(!FirstLevelShotPath.Cast(origin, incoming, .035f, 4).Hit, "Disabled collider must not leave a phantom reflecting face");
        }
        finally
        {
            SceneManager.SetActiveScene(original);
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
