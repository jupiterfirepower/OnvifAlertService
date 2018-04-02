// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.
open System
open System.IO
open Topshelf
open System.Configuration
open FSharp.Configuration
open System.ServiceModel
open System.ServiceModel.Web
open System.Runtime.Serialization
open Onvif.Contracts.Enums
open Onvif.Contracts.Messages
open Akka.FSharp

[<Literal>]
let ok = "ok"
[<Literal>]
let AppConfig = "app.config"
[<Literal>]
let recoveryFileName = "recovery.bat"


type Settings = AppSettings<AppConfig>    
    
    type IOnvifAlarmService =
        abstract FireAlarm : alarmType : AlarmType * customerId : int * action : string -> string    

    [<ServiceContract>]
    [<ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, IncludeExceptionDetailInFaults = true, ConcurrencyMode = ConcurrencyMode.Multiple)>]
    type OnvifAlarmService(actorSystem : Akka.Actor.ActorSystem) =
            let mutable system : Akka.Actor.ActorSystem = actorSystem
            [<OperationContract>]
            [<WebGet>]
            member this.FireAlarm(secToken: string, alarmSourceType : AlarmSourceType, sourceId : int, serviceType : ServiceType, customerId : int, alarmType : AlarmType, message : string, srvDateTime : string) : string =
                    try 
                       let address = new Akka.Actor.Address("akka.tcp", Settings.RemoteActorSystemName, Settings.Server, new Nullable<int>(Settings.ServerPort))
                       let aktorPath = String.Format("{0}/user/{1}", address, Settings.GateWayCoordinatorActorName)
                       let actorToSend = system.ActorSelection(aktorPath);
                       actorToSend.Tell(new AlertMessage(alarmSourceType,sourceId,serviceType, customerId, alarmType, message, srvDateTime), Akka.Actor.ActorRefs.NoSender)
                    with e -> () 
                    ok
            new () = OnvifAlarmService(System.create Settings.LocalSystemActorSystem (Configuration.load())) 

let startAt(address) =
        let host = new WebServiceHost(typeof<OnvifAlarmService>, new Uri(address))
        host.AddServiceEndpoint(typeof<OnvifAlarmService>, new WebHttpBinding(), String.Empty)
          |> ignore
        host.Open()
        host

type AlertService() =
        let mutable Log = NLog.LogManager.GetCurrentClassLogger()
        let mutable server : WebServiceHost = null
        let mutable _fctrace : bool = false
        interface ServiceControl with
            override x.Start(hostControl: HostControl) =
                server <- startAt(Settings.ServiceUri.ToString())
                true
            override x.Stop(hostControl : HostControl) = 
                if server <> null then server.Close()
                true

[<EntryPoint>]
let main argv = 
    let result = HostFactory.Run(new System.Action<HostConfigurators.HostConfigurator> (fun x -> 
                    x.SetServiceName(Settings.ServiceName)
                    x.SetDisplayName(Settings.ServiceDisplayName)
                    x.SetDescription(Settings.ServiceDescription)

                    x.RunAsLocalSystem() |> ignore
                    x.StartAutomatically() |> ignore
                    x.Service<AlertService>() |> ignore

                    let dir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                    let path = Path.Combine(dir, recoveryFileName)

                    x.EnableServiceRecovery(fun r ->
                        r.OnCrashOnly() // Only run this recovery if it crashed
                        r.RunProgram(0, path) |> ignore // Run a specified program
                        r.RestartService(1) |> ignore //first Restart the service after one min
                        r.RestartService(1) |> ignore //second
                        r.RestartService(1) |> ignore //subsequents
                        r.SetResetPeriod(0) |> ignore // Reset failure count after 0 day
                    )  |> ignore

                    ))
    (int result)
    

