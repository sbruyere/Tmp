using System.Text;

namespace TrainTrain
{
    internal class TrainModel
    {
        public TrainModel()
        {
        }

        public string train_id { get; set; }
        public StringBuilder seats { get; set; }
        public string bookingRef { get; set; }
    }
}