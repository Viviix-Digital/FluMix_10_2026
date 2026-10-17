namespace FluMix.Data
{
    public interface IQuestionRepository
    {
        QuestionData[] LoadAll();

        // Reserved for a future HTTP implementation. Gameplay must not call this.
        void FetchRemote();
    }
}
