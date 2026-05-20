using System.Collections.Generic;
using UnityEngine;

namespace DZ_3C.UI.WorldInteraction
{
  public static class WorldInteractionAimUtility
  {
    /// <param name="allowAngleFallback">为 false 时仅准心射线命中才算聚焦（移开准心即降级为圆点）。</param>
    public static bool TryResolveFocusedAnchor(
      Camera camera,
      Transform playerTransform,
      float maxRayDistance,
      LayerMask layerMask,
      float focusMaxViewAngle,
      IReadOnlyList<WorldInteractionPromptAnchor> candidates,
      float activeMaxSqr,
      bool allowAngleFallback,
      out WorldInteractionPromptAnchor anchor)
    {
      anchor = null;
      if (camera == null || candidates == null || candidates.Count == 0)
      {
        return false;
      }

      if (TryRaycastAnchor(camera, maxRayDistance, layerMask, playerTransform, out anchor)
          && anchor != null
          && anchor.IsAvailable
          && IsWithinActiveDistance(anchor, playerTransform.position, activeMaxSqr)
          && ContainsCandidate(candidates, anchor))
      {
        return true;
      }

      if (!allowAngleFallback)
      {
        anchor = null;
        return false;
      }

      return TryAngleFocusAnchor(camera, candidates, playerTransform.position, activeMaxSqr, focusMaxViewAngle, out anchor);
    }

    private static bool TryRaycastAnchor(
      Camera camera,
      float maxRayDistance,
      LayerMask layerMask,
      Transform playerTransform,
      out WorldInteractionPromptAnchor anchor)
    {
      anchor = null;
      Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
      RaycastHit[] hits = Physics.RaycastAll(ray, maxRayDistance, layerMask, QueryTriggerInteraction.Collide);
      if (hits == null || hits.Length == 0)
      {
        return false;
      }

      System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
      for (int i = 0; i < hits.Length; i++)
      {
        Collider col = hits[i].collider;
        if (col == null || ShouldIgnoreCollider(col, playerTransform))
        {
          continue;
        }

        WorldInteractionPromptAnchor hitAnchor = col.GetComponentInParent<WorldInteractionPromptAnchor>();
        if (hitAnchor != null)
        {
          anchor = hitAnchor;
          return true;
        }
      }

      return false;
    }

    private static bool TryAngleFocusAnchor(
      Camera camera,
      IReadOnlyList<WorldInteractionPromptAnchor> candidates,
      Vector3 playerPos,
      float activeMaxSqr,
      float focusMaxViewAngle,
      out WorldInteractionPromptAnchor anchor)
    {
      anchor = null;
      float bestAngle = focusMaxViewAngle;
      Vector3 camPos = camera.transform.position;
      Vector3 camFwd = camera.transform.forward;

      for (int i = 0; i < candidates.Count; i++)
      {
        WorldInteractionPromptAnchor candidate = candidates[i];
        if (candidate == null || !candidate.IsAvailable)
        {
          continue;
        }

        if (!IsWithinActiveDistance(candidate, playerPos, activeMaxSqr))
        {
          continue;
        }

        Vector3 toTarget = candidate.WorldPosition - camPos;
        if (toTarget.sqrMagnitude < 0.0001f)
        {
          continue;
        }

        float angle = Vector3.Angle(camFwd, toTarget.normalized);
        if (angle < bestAngle)
        {
          bestAngle = angle;
          anchor = candidate;
        }
      }

      return anchor != null;
    }

    private static bool IsWithinActiveDistance(
      WorldInteractionPromptAnchor anchor,
      Vector3 playerPos,
      float activeMaxSqr)
    {
      if (anchor == null)
      {
        return false;
      }

      float sqr = (anchor.WorldPosition - playerPos).sqrMagnitude;
      return sqr <= activeMaxSqr;
    }

    private static bool ContainsCandidate(
      IReadOnlyList<WorldInteractionPromptAnchor> candidates,
      WorldInteractionPromptAnchor anchor)
    {
      for (int i = 0; i < candidates.Count; i++)
      {
        if (candidates[i] == anchor)
        {
          return true;
        }
      }

      return false;
    }

    private static bool ShouldIgnoreCollider(Collider col, Transform playerTransform)
    {
      if (col == null || playerTransform == null)
      {
        return false;
      }

      Transform hit = col.transform;
      if (hit == playerTransform || hit.IsChildOf(playerTransform))
      {
        return true;
      }

      if (hit.GetComponentInParent<Player>() != null)
      {
        return true;
      }

      return false;
    }
  }
}
