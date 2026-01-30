# MPM Exposure

## Service Fabric Application
- MPM.Exposure

## Service Fabric Services
- Exposure.Generator
- Exposure.Api
- Exposure.ReadModel

### JobQueue Service - Singleton
This singleton service serves as a host for three background cron jobs that run on configured schedules.
1. **JobQueueProcessor**

   Responsible for orchestrating the execution of jobs enqueued by the QueuedMessageReciever used by the other services in the application.


2. **ExpiredJobProcessor**

	Jobs can be configured with an optional timeout.  This job is responsible for checking running jobs for any that have not registered any activity within the timeout 
configured for the job type and sets their status to TimedOut which eliminates it's configured blocking behavor from preventing execution of the next queued job. 

	**Note:** This task could also be used to just send an event to AppInsights for alerting when a job runs too long.

3. **JobCleanupProcessor**

	This cron job checks the jobs table for any completed jobs older than the JobRetentionDays configuration  setting and deletes them along with any related job activities.

### ReadModel Service
This service is responsible for keeping the internal readmodel up to date. After the initial load the service responds to ReadModelVersionUpdated events published by DataSync Publisher Service, retrieves the events from the publisher API and projects the changes into the tables in the src schema.  
The service also provides the following API endpoints. 

| Route | Purpose | Notes |
|---|
| /models/reload | Performs a complete reload of one or more models | This is a 'wipe and replace' operation completely replacing any data the exists in the requested models with the current model from DataSync. |
| /models/refresh |Updates one or models to the Current DataSync model. | This can be used a Bug or system failure has caused the internal model to become out of sync with DataSync. <br/>***Note:*** DataSync only retains changeset events for a limited number of versions so if the internal model gets too far out of sync this endpoint will return an error message stating that a full reload will be required.|


#### ReadModelVersionUpdated Processing Sequence
[Sequence Diagram](./docs/ReadModelSequence.svg) This is in svg format which is not supported for display in devops so you will have to download to view.

| Sequence | MessageClass | Processed By | Description |
|----|----|----|
|1| **ReadModelVersionUpdated** | [ReadModelVersionUpdatedHandler](./src/shared/MPM.DataSync.EventProcessing/EventHandlers/ReadModelVersionUpdatedHandler.cs)| This is the notification sent from the DataSync.Publisher service.  This message contains the metadata about the models that were changed and data required to request the change events needed to project the changes into the read model. <br/> A StartReadmodelLoad command is processed synchronously and a ProjectModelUpdate message is published for each model included in the change set.|
|2| **StartReadModelLoad** | StartReadModelLoadProcessor | Creates a LoadHistory record for each model included in the change set.|
|3| **ProjectModelUpdate** | ProjectModelUpdateProcessor | Determines the next EventType to process for the model and Invokes the ProjectReadModelVersionEventsAsync method of the IModelUpdateProjector implementation tp project the update. The ModelProjector creates ModelVersionHistory record for the update; determines a batch size based on the ModelType and the EventType using the <_eventType_>EntitiesPerApiRequest configuration setting; Creates an EventBatchHistory record and publishes a PublishEventBatches command for each batch.;<br/> **Note:** This process repeats for each eventType |
|4| **PublishEventBatches**| PublishEventBatchesProcessor | Makes a request to the Publisher API streams endpoint to retrieve a page of change events; Divides the retrieved events into batches based on the EventsPer\<_eventType_>Projection configuration setting for the ModelType and EventType and creates a ProjectEventBatch command for each and and sent to the internal Bus via MediatR.   |
|5| **ProjectEventBatch**| ProjectEventBatchProcessor | Gets the proper IModelProjector for the ModelType and invokes the Create, Modlfy or Delete method based on the EventType being projected. It then creates a CloseEventBatch command an places it on the internal Bus for processing.| 
|6| **CloseEventBatch** |CloseEventBatchProcessor|Updates the EventBatchHistory record for the batch to Projected then checks to see if ALL batches for the EventType for this Model have been projected and if so then creates a CloseEventVersion command an places it on the internal Bus for processing.|
|7| **CloseEventVersion**|CloseEventVersionProcessor | Updates the ModelUpdateHistory record for this model and eventtype to Completed and publishes a ReadModelEventsProjected event.|
|8| **ReadModelEventsProjected** |ReadModelEventsProjectedHandler | Checks if all EventTypes in the update have been processed. If there are unprocessed event types then the NextEvent is updated on the ReadModelUpdate a new ProjectModelUpdate command is published to process the update again; If not then a CompleteReadModelLoad command is Created and sent to the internal Bus via MediatR.<br/> ***Note:** This process effectively creates a loop in the process. Steps 3 - 8 repeat for each of the EventTypes included in the update in the order Create, Modified and Deleted for each model. |
|9| **CompleteReadModelLoad** |CompleteReadModelLoadProcessor | The LoadHistory record for this model is updated to Status Completed; Checks if all models in the load have a status of Completed and if so a SynchronizeReadModel command is published.
|10| **SynchronizeReadModel** | SynchronizeReadModelProcessor | The SynchronizeReadModelAsync method of the IReadModelSynchronizer is invoked passing all the models in the change set which merges in the new changes, sets the IsCurrent flags and sets the Version for each of the models.; Updates all the LoadHistory records for the load to status of Synchronized and Completes the job. |


## App Configuration Settings


| **Variable Name** | **Scope** | **Description** | **Configured for CI/CD Substitution?** |
|--------|--------|--------|--------|
|ServiceBusClientOptions | ReadModel | These are settings used to configure the message listeners. | Y |
|PublisherApiBaseUri |ReadModel | This is the base url to DataSync publisher API  | Y |
|ProjectionOptions.Defaults.\<_eventType_>EntitiesPerApiRequest |ReadModel | This setting specifies the page size to use when retrieving events from Publisher API.   | N|
|ProjectionOptions.Defaults.EventsPer\<_eventType_>Projection | ReadModel | This setting specifies how many events to include in the ProjectEventBatch command. | N |
|ProjectionOptions.Models | ReadModel | These configurations specify the models that will be projected into the src tables, what order they will be projected and overrides for the Defaults listed above.  | N |
| LogJobActivity | ReadModel | If True, entries are created in JobActivity table upon receipt and updated on completion of every message that has a Job with the same CorrelationId. | Y |
| QueueProcessingSchedule | JobQueue | Cron expression for scheduling how often the JobQueueProcessor dequeues any messages from the job queue. | Y 
| ExpiredJobProcessingSchedule | JobQueue | Cron expression for scheduling how often the ExpiredJobProcessor executes or 'NONE' to disable the processor. This processor checks jobs are checked to see if they have run longer than the TimeOut configured for the message class.  <br/> **Note:** I am currently recommending we do not register timeouts as analysis has not been done on what if any compensating logic would have to be applied to make sure te database is in a consistent state. | Y |
| JobCleanupSchedule | JobQueue | Cron expression for scheduling how often the Job cleanup routine runs or 'NONE' to disable the processor. | Y |
| JobRetentionDays | JobQueue | How many days of Job and JobActivity history to retain in the database. Used by the JobCleanupProcessor when enabled.| Y |