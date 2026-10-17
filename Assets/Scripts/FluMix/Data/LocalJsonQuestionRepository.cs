using System.IO;
using UnityEngine;

namespace FluMix.Data
{
    public class LocalJsonQuestionRepository : MonoBehaviour, IQuestionRepository
    {
        QuestionData[] cache;

        public QuestionData[] LoadAll()
        {
            if (cache != null)
                return cache;

            FluMixPaths.EnsureCopied();
            string json = File.ReadAllText(FluMixPaths.Persistent(FluMixPaths.QuestionsFile));
            QuestionFile file = JsonUtility.FromJson<QuestionFile>(json);
            cache = file != null && file.questions != null ? file.questions : new QuestionData[0];
            return cache;
        }

        public void FetchRemote()
        {
            // Remote questions are not implemented. Gameplay does not call this.
        }

        public void Reload()
        {
            cache = null;
            LoadAll();
        }
    }
}
