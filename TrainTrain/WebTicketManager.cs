using System.Text;

namespace TrainTrain;

public class WebTicketManager
{
    private const string UriBookingReferenceService = "https://localhost:7264/";
    private const string UriTrainDataService = "https://localhost:7177";
    private readonly ITrainDataService _trainDataService;
    private readonly IBookingReferenceService _bookingReferenceService;

    public WebTicketManager() : this(new TrainDataService(UriTrainDataService), new BookingReferenceService(UriBookingReferenceService))
    {
    }

    public WebTicketManager(ITrainDataService trainDataService, IBookingReferenceService bookingReferenceService)
    {
        _trainDataService = trainDataService;
        _bookingReferenceService = bookingReferenceService;
    }

    public async Task<string> Reserve(string trainId, int seatsRequestedCount)
    {
        List<Seat> availableSeats = new List<Seat>();
        int count = 0;
        var result = string.Empty;
        string bookingRef;

        // get the train
        var JsonTrain = await _trainDataService.GetTrain(trainId);

        result = JsonTrain;

        var trainInst = new Train(JsonTrain);


        var numberOfReserv = 0;
        // find seats to reserve
        Seat previousSeat = null;

        var coaches = trainInst.Seats.GroupBy(v => v.CoachName).ToList();

        foreach (var coach in coaches)
        {
            var reservedSeatsInCoach = coach.Count(s => s.BookingRef != "");
            var maxSeatsInCoach = coach.Count();

            Console.WriteLine($"Coach {coach.Key} has {reservedSeatsInCoach} reserved seats out of {maxSeatsInCoach} total seats.");

            if ((reservedSeatsInCoach + seatsRequestedCount) <= Math.Floor(ThreasholdManager.GetMaxRes() * maxSeatsInCoach))
            {
                for (int index = 0, i = 0; index < coach.Count(); index++)
                {
                    var each = coach.ElementAt(index);

                    var seatNum = each.SeatNumber;
                    var coachName = each.CoachName;

                    if (each.BookingRef == "") // disponible
                    {
                        i++;

                        if (i <= seatsRequestedCount)
                        {
                            // Si même coach et siège consécutif, on continue la recherche
                            bool isSameCoach = previousSeat == null || (previousSeat.CoachName == coachName);
                            bool isNextSeat = previousSeat == null || (previousSeat.SeatNumber == seatNum - 1);

                            // Si siège different ou pas consécutif, on recommence la recherche
                            if (!(isSameCoach && isNextSeat))
                            {
                                availableSeats.Clear();
                                i = 0;
                            }

                            // On ajoute le siège à la liste des sièges disponibles car disponible
                            availableSeats.Add(each);
                            previousSeat = each;

                            if (availableSeats.Count == seatsRequestedCount)
                            {
                                break;
                            }

                        }
                    }

                }
            }
        }


                foreach (var a in availableSeats)
                {
                    count++;
                }

                var reservedSets = 0;


                if (count != seatsRequestedCount)
                {
                    return string.Format("{{\"train_id\": \"{0}\", \"booking_reference\": \"\", \"seats\": []}}",
                        trainId);
                }
                else
                {
                    bookingRef = await _bookingReferenceService.GetBookingReference();

                    foreach (var availableSeat in availableSeats)
                    {
                        availableSeat.BookingRef = bookingRef;
                        numberOfReserv++;
                        reservedSets++;
                    }
                }

                if (numberOfReserv == seatsRequestedCount)
                {
                    await _trainDataService.Reserve(trainId, bookingRef, availableSeats);

                    var todod = "[TODOD]";


                    return string.Format(
                        "{{\"train_id\": \"{0}\", \"booking_reference\": \"{1}\", \"seats\": {2}}}",
                        trainId,
                        bookingRef,
                        dumpSeats(availableSeats));

                }
            

            return string.Format("{{\"train_id\": \"{0}\", \"booking_reference\": \"\", \"seats\": []}}", trainId);
        }

    private string dumpSeats(IEnumerable<Seat> seats)
    {
        var sb = new StringBuilder("[");

        var firstTime = true;
        foreach (var seat in seats)
        {
            if (!firstTime)
            {
                sb.Append(", ");
            }
            else
            {
                firstTime = false;
            }

            sb.Append(string.Format("\"{0}{1}\"", seat.SeatNumber, seat.CoachName));
        }

        sb.Append("]");

        return sb.ToString();
    }
}