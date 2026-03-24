using System.Collections.Generic;

namespace Twinny.Navigation
{
    public static class LandmarkHub
    {
        private static readonly Dictionary<string, Landmark> landmarksByGuid = new();

        public static IReadOnlyCollection<Landmark> All => landmarksByGuid.Values;

        public static void Register(Landmark landmark)
        {
            if (landmark == null)
            {
                return;
            }

            landmark.EnsureLandmarkGuid();

            if (string.IsNullOrWhiteSpace(landmark.LandmarkGuid))
            {
                return;
            }

            if (landmarksByGuid.TryGetValue(landmark.LandmarkGuid, out Landmark current) && current != null && current != landmark)
            {
                UnityEngine.Debug.LogWarning(
                    $"LandmarkHub already has a Landmark registered with landmarkGuid '{landmark.LandmarkGuid}'. Replacing the previous reference.",
                    landmark);
            }

            landmarksByGuid[landmark.LandmarkGuid] = landmark;
        }

        public static void Unregister(Landmark landmark)
        {
            if (landmark == null || string.IsNullOrWhiteSpace(landmark.LandmarkGuid))
            {
                return;
            }

            if (landmarksByGuid.TryGetValue(landmark.LandmarkGuid, out Landmark current) && current == landmark)
            {
                landmarksByGuid.Remove(landmark.LandmarkGuid);
            }
        }

        public static Landmark GetByLandmarkGuid(string landmarkGuid)
        {
            TryGetByLandmarkGuid(landmarkGuid, out Landmark landmark);
            return landmark;
        }

        public static bool TryGetByLandmarkGuid(string landmarkGuid, out Landmark landmark)
        {
            if (string.IsNullOrWhiteSpace(landmarkGuid))
            {
                landmark = null;
                return false;
            }

            if (landmarksByGuid.TryGetValue(landmarkGuid, out landmark) && landmark != null)
            {
                return true;
            }

            landmark = null;
            return false;
        }
    }
}
