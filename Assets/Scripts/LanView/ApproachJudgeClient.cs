using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace MagesDementiaGame
{
    [Serializable]
    public sealed class ApproachJudgeResult
    {
        public int recognition;
        public int trust;
        public int distress;
        public int clarity;
        public string outcome;
        public string feedback;

        public bool IsValid =>
            InRange(recognition) && InRange(trust) && InRange(distress) && InRange(clarity) &&
            (outcome == "harmful" || outcome == "mixed" || outcome == "supportive") &&
            !string.IsNullOrWhiteSpace(feedback);

        private static bool InRange(int value) => value >= 0 && value <= 2;
    }

    public static class ApproachJudgeClient
    {
        [Serializable]
        private sealed class RequestBody
        {
            public string text;
        }

        [Serializable]
        private sealed class ErrorBody
        {
            public string error;
        }

        public static IEnumerator Evaluate(
            string endpoint,
            string playerText,
            Action<ApproachJudgeResult> succeeded,
            Action<string> failed)
        {
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out _))
            {
                failed?.Invoke("The AI judge URL has not been configured.");
                yield break;
            }

            var json = JsonUtility.ToJson(new RequestBody { text = playerText });
            using var request = new UnityWebRequest(endpoint, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer(),
                // The Worker can make one initial request plus two immediate
                // retries when a free model returns invalid structured output.
                timeout = 65
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                var message = ReadServerError(request.downloadHandler?.text);
                if (string.IsNullOrWhiteSpace(message))
                {
                    message = request.responseCode == 429
                        ? "The free AI judge is temporarily rate-limited."
                        : "The AI judge could not be reached.";
                }
                failed?.Invoke(message);
                yield break;
            }

            ApproachJudgeResult result;
            try
            {
                result = JsonUtility.FromJson<ApproachJudgeResult>(request.downloadHandler.text);
            }
            catch (Exception)
            {
                result = null;
            }

            if (result == null || !result.IsValid)
            {
                failed?.Invoke("The AI judge returned an unexpected response.");
                yield break;
            }

            succeeded?.Invoke(result);
        }

        private static string ReadServerError(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonUtility.FromJson<ErrorBody>(json)?.error;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
