using System;
using FluMix.Data;
using UnityEngine;

namespace FluMix.Api
{
    public static class QuestionMapper
    {
        public static QuestionData[] FromServer(QuestionDto[] source)
        {
            if (source == null || source.Length == 0)
                return Array.Empty<QuestionData>();

            var result = new QuestionData[source.Length];
            for (int i = 0; i < source.Length; i++)
                result[i] = FromServer(source[i]);
            return result;
        }

        public static QuestionData FromServer(QuestionDto dto)
        {
            if (dto == null)
                return null;

            int count = dto.options != null ? dto.options.Length : 0;
            var options = new string[count];
            var keys = new string[count];
            for (int i = 0; i < count; i++)
            {
                OptionDto opt = dto.options[i];
                keys[i] = opt != null ? opt.key : string.Empty;
                options[i] = opt != null ? opt.label : string.Empty;
            }

            return new QuestionData
            {
                id = dto.codeId,
                codeId = dto.codeId,
                prompt = dto.content,
                options = options,
                optionKeys = keys,
                correctIndex = -1,
                explanation = dto.explanation,
                reference = null,
                points = dto.points,
                source = dto
            };
        }

        public static void ShuffleOptions(QuestionData question)
        {
            if (question == null || question.options == null || question.optionKeys == null)
                return;
            if (question.options.Length != question.optionKeys.Length || question.options.Length <= 1)
                return;

            for (int i = question.options.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (question.options[i], question.options[j]) = (question.options[j], question.options[i]);
                (question.optionKeys[i], question.optionKeys[j]) = (question.optionKeys[j], question.optionKeys[i]);
            }
        }

        public static string KeyAt(QuestionData question, int index)
        {
            if (question == null || question.optionKeys == null || index < 0 || index >= question.optionKeys.Length)
                return string.Empty;
            return question.optionKeys[index] ?? string.Empty;
        }

        public static int IndexOfKey(QuestionData question, string key)
        {
            if (question == null || question.optionKeys == null || string.IsNullOrEmpty(key))
                return -1;
            for (int i = 0; i < question.optionKeys.Length; i++)
            {
                if (string.Equals(question.optionKeys[i], key, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }
    }
}
