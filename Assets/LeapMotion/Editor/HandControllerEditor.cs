using UnityEditor;
using UnityEngine;
using System.Collections;

enum RECORD_OPTIONS {
  IDLE = 0,
  RECORD = 1,
  PLAYBACK = 2
}

[CustomEditor(typeof(HandController))]
public class HandControllerEditor : Editor {

  private const float BOX_HEIGHT = 0.45f;
  private const float BOX_WIDTH = 0.965f;
  private const float BOX_DEPTH = 0.6671f;
  
  private string recorder_or_player_name_ = "Toggle Recorder/Player";
  private bool recorder_or_player_ = true;

  public void OnSceneGUI() {
    HandController controller = (HandController)target;
    Vector3 origin = controller.transform.TransformPoint(Vector3.zero);
    Vector3 top_left =
        controller.transform.TransformPoint(new Vector3(-BOX_WIDTH, BOX_HEIGHT, BOX_DEPTH));
    Vector3 top_right =
        controller.transform.TransformPoint(new Vector3(BOX_WIDTH, BOX_HEIGHT, BOX_DEPTH));
    Vector3 bottom_left =
        controller.transform.TransformPoint(new Vector3(-BOX_WIDTH, BOX_HEIGHT, -BOX_DEPTH));
    Vector3 bottom_right =
        controller.transform.TransformPoint(new Vector3(BOX_WIDTH, BOX_HEIGHT, -BOX_DEPTH));

    Handles.DrawLine(origin, top_left);
    Handles.DrawLine(origin, top_right);
    Handles.DrawLine(origin, bottom_left);
    Handles.DrawLine(origin, bottom_right);

    Handles.DrawLine(bottom_left, top_left);
    Handles.DrawLine(top_left, top_right);
    Handles.DrawLine(top_right, bottom_right);
    Handles.DrawLine(bottom_right, bottom_left);
  }

  public override void OnInspectorGUI() {
    HandController controller = (HandController)target;

    controller.separateLeftRight = EditorGUILayout.Toggle("Separate Left/Right",
                                                          controller.separateLeftRight);

    if (controller.separateLeftRight) {
      controller.leftGraphicsModel =
          (HandModel)EditorGUILayout.ObjectField("Left Hand Graphics Model",
                                                 controller.leftGraphicsModel,
                                                 typeof(HandModel), true);
      controller.rightGraphicsModel =
          (HandModel)EditorGUILayout.ObjectField("Right Hand Graphics Model",
                                                 controller.rightGraphicsModel,
                                                 typeof(HandModel), true);
      controller.leftPhysicsModel =
          (HandModel)EditorGUILayout.ObjectField("Left Hand Physics Model",
                                                 controller.leftPhysicsModel,
                                                 typeof(HandModel), true);
      controller.rightPhysicsModel =
          (HandModel)EditorGUILayout.ObjectField("Right Hand Physics Model",
                                                 controller.rightPhysicsModel,
                                                 typeof(HandModel), true);
    }
    else {
      controller.leftGraphicsModel = controller.rightGraphicsModel = 
          (HandModel)EditorGUILayout.ObjectField("Hand Graphics Model",
                                                 controller.leftGraphicsModel,
                                                 typeof(HandModel), true);

      controller.leftPhysicsModel = controller.rightPhysicsModel = 
          (HandModel)EditorGUILayout.ObjectField("Hand Physics Model",
                                                 controller.leftPhysicsModel,
                                                 typeof(HandModel), true);
    }

    controller.toolModel = 
        (ToolModel)EditorGUILayout.ObjectField("Tool Model",
                                               controller.toolModel,
                                               typeof(ToolModel), true);

    controller.handMovementScale =
        EditorGUILayout.Vector3Field("Hand Movement Scale", controller.handMovementScale);
                          

    GUIStyle buttonStyle = new GUIStyle("Button");
    buttonStyle.alignment = TextAnchor.MiddleLeft;
    if (GUILayout.Button(recorder_or_player_name_, buttonStyle)) {
      if (recorder_or_player_) {
        recorder_or_player_ = !recorder_or_player_;
        recorder_or_player_name_ = "Toggle Recorder/Player [Player]";
      } else {
        recorder_or_player_ = !recorder_or_player_;
        recorder_or_player_name_ = "Toggle Recorder/Player [Recorder]";
      }
    }
    
    if (recorder_or_player_) {
      controller.recorderFilePath = EditorGUILayout.TextField("Recorder File Path", controller.recorderFilePath);
      controller.keyToRecord = (KeyCode)EditorGUILayout.EnumPopup("Key To Record", controller.keyToRecord);
      controller.keyToSave = (KeyCode)EditorGUILayout.EnumPopup("Key To Save", controller.keyToSave);
      controller.keyToReset = (KeyCode)EditorGUILayout.EnumPopup("Key To Reset", controller.keyToReset);
    } else {
      controller.playerFilePath = (TextAsset)EditorGUILayout.ObjectField("Player File Path", controller.playerFilePath, typeof(TextAsset), true);
    }
    
    if (GUI.changed)
      EditorUtility.SetDirty(controller);

    Undo.RecordObject(controller, "Hand Preferences Changed: " + controller.name);
  }
}
