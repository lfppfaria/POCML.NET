namespace POCMachineLearning.Entity
{
    public class OutputModel
    {
        public float[] ForecastedSells { get; set; }

        public float[] LowerBoundSells { get; set; }

        public float[] UpperBoundSells { get; set; }
    }
}
