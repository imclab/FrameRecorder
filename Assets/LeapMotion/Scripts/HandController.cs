﻿/******************************************************************************\
* Copyright (C) Leap Motion, Inc. 2011-2014.                                   *
* Leap Motion proprietary. Licensed under Apache 2.0                           *
* Available at http://www.apache.org/licenses/LICENSE-2.0.html                 *
\******************************************************************************/

using UnityEngine;
using System.Collections.Generic;
using Leap;

// Overall Controller object that will instantiate hands and tools when they appear.
public class HandController : MonoBehaviour {

  // Reference distance from thumb base to pinky base in mm.
  protected const float MODEL_PALM_WIDTH = 85.0f;

  public bool separateLeftRight = false;
  public HandModel leftGraphicsModel;
  public HandModel leftPhysicsModel;
  public HandModel rightGraphicsModel;
  public HandModel rightPhysicsModel;
  public ToolModel toolModel;
  public Vector3 handMovementScale = Vector3.one;
  public string recorderFilePath;
  public TextAsset playerFilePath;
  public KeyCode keyToRecord = KeyCode.None;
  public KeyCode keyToSave = KeyCode.None;
  public KeyCode keyToReset = KeyCode.None;
  public RecorderMode recorderMode = RecorderMode.Off;
  public bool playerLoop = false;
  public int playerStartTime = 0;
  public float playerSpeed = 1.0f;
  public float playerDelay = 0.0f;
  
  private Controller leap_controller_;

  private Dictionary<int, HandModel> hand_graphics_;
  private Dictionary<int, HandModel> hand_physics_;
  private Dictionary<int, ToolModel> tools_;
  
  private LeapRecorder leap_recorder_;

  private int record_state_ = 0;
  private float record_index_ = 0.0f;

  void Start() {
    leap_controller_ = new Controller();
    hand_graphics_ = new Dictionary<int, HandModel>();
    hand_physics_ = new Dictionary<int, HandModel>();
    leap_recorder_ = new LeapRecorder();

    tools_ = new Dictionary<int, ToolModel>();

    if (leap_controller_ == null) {
      Debug.LogWarning(
          "Cannot connect to controller. Make sure you have Leap Motion v2.0+ installed");
    }
  }

  private void IgnoreCollisions(GameObject first, GameObject second, bool ignore = true) {
    if (first == null || second == null)
      return;

    Collider[] first_colliders = first.GetComponentsInChildren<Collider>();
    Collider[] second_colliders = second.GetComponentsInChildren<Collider>();

    for (int i = 0; i < first_colliders.Length; ++i) {
      for (int j = 0; j < second_colliders.Length; ++j)
        Physics.IgnoreCollision(first_colliders[i], second_colliders[j], ignore);
    }
  }

  private void IgnoreCollisionsWithChildren(GameObject to_ignore) {
    IgnoreCollisions(gameObject, to_ignore);
  }

  public void IgnoreCollisionsWithHands(GameObject to_ignore, bool ignore = true) {
    foreach (HandModel hand in hand_physics_.Values)
      IgnoreCollisions(hand.gameObject, to_ignore, ignore);
  }

  private HandModel CreateHand(HandModel model) {
    HandModel hand_model = Instantiate(model, transform.position, transform.rotation)
                           as HandModel;
    hand_model.gameObject.SetActive(true);
    IgnoreCollisionsWithChildren(hand_model.gameObject);
    return hand_model;
  }

  private void UpdateHandModels(Dictionary<int, HandModel> all_hands,
                                HandList leap_hands,
                                HandModel left_model, HandModel right_model) {
    List<int> ids_to_check = new List<int>(all_hands.Keys);

    // Go through all the active hands and update them.
    int num_hands = leap_hands.Count;
    for (int h = 0; h < num_hands; ++h) {
      Hand leap_hand = leap_hands[h];
      
      HandModel model = leap_hand.IsLeft? left_model : right_model;
      // Only create or update if the hand is enabled.
      if (model != null) {
        ids_to_check.Remove(leap_hand.Id);

        // Create the hand and initialized it if it doesn't exist yet.
        if (!all_hands.ContainsKey(leap_hand.Id)) {
          HandModel new_hand = CreateHand(model);
          new_hand.SetLeapHand(leap_hand);
          new_hand.SetController(this);

          // Set scaling based on reference hand.
          float hand_scale = leap_hand.PalmWidth / MODEL_PALM_WIDTH;
          new_hand.transform.localScale = hand_scale * transform.localScale;

          new_hand.InitHand();
          new_hand.UpdateHand();
          all_hands[leap_hand.Id] = new_hand;
        }
        else {
          // Make sure we update the Leap Hand reference.
          HandModel hand_model = all_hands[leap_hand.Id];
          hand_model.SetLeapHand(leap_hand);

          // Set scaling based on reference hand.
          float hand_scale = leap_hand.PalmWidth / MODEL_PALM_WIDTH;
          hand_model.transform.localScale = hand_scale * transform.localScale;
          hand_model.UpdateHand();
        }
      }
    }

    // Destroy all hands with defunct IDs.
    for (int i = 0; i < ids_to_check.Count; ++i) {
      Destroy(all_hands[ids_to_check[i]].gameObject);
      all_hands.Remove(ids_to_check[i]);
    }
  }

  private ToolModel CreateTool(ToolModel model) {
    ToolModel tool_model = Instantiate(model, transform.position, transform.rotation)
                           as ToolModel;
    tool_model.gameObject.SetActive(true);
    IgnoreCollisionsWithChildren(tool_model.gameObject);
    return tool_model;
  }

  private void UpdateToolModels(Dictionary<int, ToolModel> all_tools,
                                ToolList leap_tools, ToolModel model) {
    List<int> ids_to_check = new List<int>(all_tools.Keys);

    // Go through all the active tools and update them.
    int num_tools = leap_tools.Count;
    for (int h = 0; h < num_tools; ++h) {
      Tool leap_tool = leap_tools[h];
      
      // Only create or update if the tool is enabled.
      if (model) {

        ids_to_check.Remove(leap_tool.Id);

        // Create the tool and initialized it if it doesn't exist yet.
        if (!all_tools.ContainsKey(leap_tool.Id)) {
          ToolModel new_tool = CreateTool(model);
          new_tool.SetController(this);
          new_tool.SetLeapTool(leap_tool);
          new_tool.InitTool();
          all_tools[leap_tool.Id] = new_tool;
        }

        // Make sure we update the Leap Tool reference.
        ToolModel tool_model = all_tools[leap_tool.Id];
        tool_model.SetLeapTool(leap_tool);

        // Set scaling.
        tool_model.transform.localScale = transform.localScale;

        tool_model.UpdateTool();
      }
    }

    // Destroy all tools with defunct IDs.
    for (int i = 0; i < ids_to_check.Count; ++i) {
      Destroy(all_tools[ids_to_check[i]].gameObject);
      all_tools.Remove(ids_to_check[i]);
    }
  }

  void Update() {
    if (leap_controller_ == null)
      return;
    
    Frame frame = new Frame();
    
    if (recorderMode == RecorderMode.Record) {
      frame = leap_controller_.Frame();
      if (recorderFilePath == "") {
        recorderFilePath = Application.dataPath + "/LeapRecording.bytes";
      }
      if (Input.GetKeyDown(keyToRecord)) {
        Debug.Log("Record");
        record_state_ = 1;
      } else if (Input.GetKeyDown(keyToSave)) {
        Debug.Log("Save");
        playerLoop = true;
        playerStartTime = 0;
        record_state_ = 2;
        record_index_ = 0;
        leap_recorder_.Save(recorderFilePath);
      } else if (Input.GetKeyDown(keyToReset)) {
        leap_recorder_.Reset();
        record_state_ = 0;
      }
    }
    
    if (recorderMode == RecorderMode.Playback && record_state_ != 2) {
      if (playerFilePath) {
        leap_recorder_.Load(playerFilePath.bytes);
        record_state_ = 2;
      }
    }
    
    if (record_state_ == 1) {
      leap_recorder_.Record(frame);
    } else if (record_state_ == 2) {
      if (Time.frameCount >= playerStartTime) {
        if (record_index_ >= 0) {
          frame = leap_recorder_.GetFrame(Mathf.Min((int)record_index_,leap_recorder_.GetFramesCount() - 1));
        }
        if (record_index_ < leap_recorder_.GetFramesCount() - 1) {
          record_index_ += playerSpeed;
        } else {
          if (playerLoop) {
            record_index_ = -playerDelay;
          }
        }
      }
    }
    
    UpdateHandModels(hand_graphics_, frame.Hands, leftGraphicsModel, rightGraphicsModel);
  }

  void FixedUpdate() {
    if (leap_controller_ == null)
      return;

    Frame frame = leap_controller_.Frame();
    UpdateHandModels(hand_physics_, frame.Hands, leftPhysicsModel, rightPhysicsModel);
    UpdateToolModels(tools_, frame.Tools, toolModel);
  }
}
