namespace PrintGrid.Modules.Scheduling.Domain.Enums;

public enum MachineStatus
{
    Idle,
    Printing,
    Maintenance,
    Offline
}

public enum PrintTechnology
{
    Fdm,
    Sla,
    Sls
}
