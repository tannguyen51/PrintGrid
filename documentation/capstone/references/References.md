# References — PrintGrid

| Ref ID | Type | Title / Description | Link (URL) | Source / Author | Notes |
|---|---|---|---|---|---|
| R1 | Book | Scheduling: Theory, Algorithms, and Systems (5th ed.) | [Springer](https://link.springer.com/book/10.1007/978-1-4614-2361-4) | M. Pinedo — Springer, 2016 | Foundation for job scheduling: parallel machines, dispatching rules (EDD/ATC), weighted tardiness — basis of the assignment engine (step ⑤) |
| R2 | Paper | Optimization and approximation in deterministic sequencing and scheduling: a survey | — | Graham, Lawler, Lenstra, Rinnooy Kan — *Annals of Discrete Mathematics* 5, 1979 | The α\|β\|γ notation; establishes scheduling as an NP-hard problem class |
| R3 | Book | Computers and Intractability: A Guide to the Theory of NP-Completeness | — | Garey & Johnson — W.H. Freeman, 1979 | NP-hardness reference for parallel machine scheduling |
| R4 | Paper | Scheduling with batching: a review | — | Potts & Kovalyov — *EJOR* 120(2), 2000 | Basis for batch consolidation by material/colour and the lot-sizing vs. responsiveness trade-off |
| R5 | Paper | Rescheduling manufacturing systems: a framework of strategies, policies, and methods | — | Vieira, Herrmann & Lin — *Journal of Scheduling* 6(1), 2003 | Predictive-reactive rescheduling framework — basis of the event-driven rescheduler (step ⑨) |
| R6 | Paper | Project scheduling under uncertainty: Survey and research potentials | — | Herroelen & Leus — *EJOR* 165(2), 2005 | Schedule stability / nervousness — grounds the rule "never move a committed customer deadline" |
| R7 | Paper | Quantity and due date quoting available to promise | — | Chen, Zhao & Ball — *Information Systems Frontiers* 3(4), 2001 | Capable-to-Promise theory — basis of speculative quote-time scheduling (step ③) |
| R8 | Paper | Optimization by simulated annealing | [DOI](https://doi.org/10.1126/science.220.4598.671) | Kirkpatrick, Gelatt & Vecchi — *Science* 220, 1983 | Local-search improvement within the 30-second rescheduling budget |
| R9 | Paper | Tabu search — Part I | — | Glover — *ORSA J. on Computing* 1(3), 1989 | Tabu search over swap/transfer neighbourhoods |
| R10 | Paper | Another look at measures of forecast accuracy | [link](https://robjhyndman.com/publications/measures-of-forecast-accuracy/) | Hyndman & Koehler — *Int. J. Forecasting* 22(4), 2006 | Justifies MAPE for monitoring estimation error — drives the calibration loop |
| R11 | Book | Introduction to Linear Regression Analysis (5th ed.) | — | Montgomery, Peck & Vining — Wiley, 2012 | Regression of actual vs. estimated durations per machine model — correction factors |
| R12 | Paper | An optimal algorithm for 3D triangle mesh slicing | — | Minetto, Volpato, Stolfi et al. — *Computer-Aided Design* 92, 2017 | Slicing and print-time/material estimation (step ②) |
| R13 | Paper | Reputation systems | [DOI](https://doi.org/10.1145/355112.355122) | Resnick, Kuwabara, Zeckhauser & Friedman — *CACM* 43(12), 2000 | Theoretical basis of the Lab Performance Ledger |
| R14 | Paper | Reputation and feedback systems in online platform markets | — | Tadelis — *Annual Review of Economics* 8, 2016 | Cold start + exploration–exploitation (the 5% exploration allowance for new labs) |
| R15 | Book | Clean Architecture: A Craftsman's Guide | — | Robert C. Martin — Prentice Hall, 2017 | The four-layer Clean Architecture used across modules |
| R16 | Book | Implementing Domain-Driven Design | — | Vaughn Vernon — Addison-Wesley, 2013 | DDD: aggregates, repositories, domain events between modules |
| R17 | Online | .NET Application Architecture (microservices & modular monolith guidance) | [Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/architecture/) | Microsoft Docs | Modular Monolith vs. microservices trade-offs and module boundaries |
| R18 | Report | Wohlers Report 2024 | [Wohlers](https://wohlersassociates.com/) | Wohlers Associates | 3D printing market data and capacity fragmentation (project Context) |
| R19 | Book | Simulation Modeling and Analysis (5th ed.) | — | A. Law — McGraw-Hill, 2015 | Methodology for building the Simulation Harness (WP5) |
| R20 | Online | CuraEngine / PrusaSlicer, MinIO S3, MediatR, SignalR documentation | [PrusaSlicer](https://help.prusa3d.com/), [MinIO](https://min.io/docs/) | Open-source projects | Engineering implementation of slicing, storage, CQRS and realtime updates |