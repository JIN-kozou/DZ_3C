#if UNITY_EDITOR
using DZ_3C.MachineRepair;
using DZ_3C.Reverse;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DZ_3C.UI.WorldInteraction.Editor
{
  public static class WorldInteractionSceneSetup
  {
    [MenuItem("DZ_3C/UI/Add World Interaction Prompts In Open Scene")]
    public static void AddPromptsInOpenScene()
    {
      WorldInteractionPrefabBuilder.BuildPrefabs();

      int battery = WireBatteryZones();
      int parts = WireMachineParts();
      int receivers = WireReceivers();
      int players = WirePlayers();

      EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
      Debug.Log(
        $"[WorldInteraction] Open scene wired: batteries={battery}, parts={parts}, receivers={receivers}, players={players}.");
    }

    private static int WireBatteryZones()
    {
      ReverseBatteryZone[] zones = Object.FindObjectsOfType<ReverseBatteryZone>(true);
      for (int i = 0; i < zones.Length; i++)
      {
        EditorUtility.SetDirty(zones[i]);
      }

      return zones.Length;
    }

    private static int WireMachineParts()
    {
      MachinePart[] parts = Object.FindObjectsOfType<MachinePart>(true);
      for (int i = 0; i < parts.Length; i++)
      {
        EditorUtility.SetDirty(parts[i]);
      }

      return parts.Length;
    }

    private static int WireReceivers()
    {
      MachinePartReceiver[] receivers = Object.FindObjectsOfType<MachinePartReceiver>(true);
      for (int i = 0; i < receivers.Length; i++)
      {
        EditorUtility.SetDirty(receivers[i]);
      }

      return receivers.Length;
    }

    private static int WirePlayers()
    {
      Player[] players = Object.FindObjectsOfType<Player>(true);
      int count = 0;
      for (int i = 0; i < players.Length; i++)
      {
        Player player = players[i];
        if (player.GetComponent<WorldInteractionPromptManager>() == null)
        {
          player.gameObject.AddComponent<WorldInteractionPromptManager>();
          count++;
        }

        if (player.GetComponent<RepairInteractionHub>() == null)
        {
          continue;
        }

        EditorUtility.SetDirty(player.gameObject);
      }

      return count;
    }
  }
}
#endif
