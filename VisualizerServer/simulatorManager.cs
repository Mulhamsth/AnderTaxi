using CloudNative.CloudEvents;
using Model;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace VisualizerServer
{
    public class simulatorManager : IDisposable
    {
        private RabbitMqManager _rmq;
        // Lock to protect access to shared collections.
        private readonly object _lockgrid = new();

        // Use a dictionary with a string key (GUID) to store taxis and their last update time.
        private Dictionary<string, (TaxiDto taxi, DateTime lastUpdate)> _taxis = new();
        
        // Passenger list remains unchanged.
        private List<PassengerRequest> _passenger = new();

        // Timer to periodically clean up stale taxi entries.
        private System.Threading.Timer? _cleanupTimer;

        public simulatorManager(RabbitMqManager rmq)
        {
            _rmq = rmq;
            // Start consuming messages from the "visualizer" queue.
            _rmq.ConsumeCloudEventMessage("visualizer", HandleCloudEventMessage);
            
            // Create a timer that checks every second for taxis that haven't been updated in the last 3 seconds.
            _cleanupTimer = new System.Threading.Timer(CleanupStaleTaxis, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Returns a 30x30 grid with the latest taxi and passenger positions.
        /// 'd' for taxi, 'p' for passenger location and 'P' for passenger desired destination.
        /// </summary>
        public char[,] GetGrid()
        {
            char[,] grid = new char[30, 30];
            lock (_lockgrid)
            {
                // Mark each taxi on the grid.
                foreach (var entry in _taxis.Values)
                {
                    // Ensure the coordinates are within grid bounds.
                    if (entry.taxi.Location.X < grid.GetLength(0) && entry.taxi.Location.Y < grid.GetLength(1))
                    {
                        grid[entry.taxi.Location.X, entry.taxi.Location.Y] = 'd';
                    }
                }
                // Mark passengers and their desired destinations.
                foreach (var passenger in _passenger)
                {
                    if (passenger.Location.X < grid.GetLength(0) && passenger.Location.Y < grid.GetLength(1))
                    {
                        grid[passenger.Location.X, passenger.Location.Y] = 'p';
                    }
                    if (passenger.DesiredLocation.X < grid.GetLength(0) && passenger.DesiredLocation.Y < grid.GetLength(1))
                    {
                        grid[passenger.DesiredLocation.X, passenger.DesiredLocation.Y] = 'P';
                    }
                }
            }
            return grid;
        }

        /// <summary>
        /// Handles incoming CloudEvent messages.
        /// </summary>
        private async Task HandleCloudEventMessage(CloudEvent cloudEvent)
        {
            if (cloudEvent.Type == "passenger.location")
            {
                var passengerRequest = await _rmq.DeserializeCloudEventMessage<PassengerRequest>(cloudEvent);
                await UpdatePassenger(passengerRequest);
                Console.WriteLine($"Passenger location updated: {passengerRequest.Location}");
            }
            else if (cloudEvent.Type == "taxi.location")
            {
                var taxiDto = await _rmq.DeserializeCloudEventMessage<TaxiDto>(cloudEvent);
                await UpdateTaxi(taxiDto);
                Console.WriteLine($"Taxi location updated: {taxiDto.Location.X}, {taxiDto.Location.Y}");
            }
        }

        /// <summary>
        /// Updates the passenger list with the latest passenger location.
        /// </summary>
        private Task UpdatePassenger(PassengerRequest passengerRequest)
        {
            lock (_lockgrid)
            {
                // Remove any existing entry for this passenger and add the new one.
                _passenger.RemoveAll(p => p.PassengerId == passengerRequest.PassengerId);
                _passenger.Add(passengerRequest);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Updates (or adds) the taxi with the new location and sets the current time as its last update.
        /// Taxi IDs are now stored as strings (GUIDs).
        /// </summary>
        private Task UpdateTaxi(TaxiDto taxiDto)
        {
            lock (_lockgrid)
            {
                // Use taxiDto.Id (a GUID string) as the key.
                _taxis[taxiDto.Id] = (taxiDto, DateTime.UtcNow);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Timer callback to remove taxis that have not been updated for more than 3 seconds.
        /// </summary>
        private void CleanupStaleTaxis(object? state)
        {
            lock (_lockgrid)
            {
                var now = DateTime.UtcNow;
                var staleKeys = new List<string>();

                foreach (var kvp in _taxis)
                {
                    if ((now - kvp.Value.lastUpdate).TotalSeconds > 3)
                    {
                        staleKeys.Add(kvp.Key);
                    }
                }

                foreach (var key in staleKeys)
                {
                    _taxis.Remove(key);
                    Console.WriteLine($"Removed stale taxi with ID: {key}");
                }
            }
        }
        
        public void ResetGrid()
        {
            _taxis.Clear();
            _passenger.Clear();
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
        }
    }
}
