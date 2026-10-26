import type { MyProjectsData } from '../../models/projects/MyProjectsTypes'

/// <summary>
/// Dočasná data rozcestníku. Nahradí je odpověď API, až bude endpoint hotový.
/// </summary>
const mockData: MyProjectsData = {
  projects: [
    {
      id: 1,
      name: 'Rezervační systém knihovny',
      teamName: 'Tým Sova',
      role: 'leader',
      tasksDone: 3,
      tasksTotal: 10,
      myOpenTasks: 1,
      myOverdueTasks: 0,
      nextMilestone: { name: 'Inception', date: '2026-10-31' },
      members: [
        { id: 1, name: 'Petra Svobodová', initials: 'PS' },
        { id: 2, name: 'Jan Novák', initials: 'JN' },
        { id: 3, name: 'Tomáš Dvořák', initials: 'TD' },
        { id: 4, name: 'Lenka Černá', initials: 'LČ' },
        { id: 5, name: 'Marek Kolář', initials: 'MK' },
      ],
    },
    {
      id: 2,
      name: 'Analýza dat: semestrální práce',
      teamName: 'Tým Delta',
      role: 'member',
      tasksDone: 5,
      tasksTotal: 8,
      myOpenTasks: 2,
      myOverdueTasks: 1,
      nextMilestone: { name: 'Odevzdání', date: '2026-12-12' },
      members: [
        { id: 6, name: 'Karel Beneš', initials: 'KB' },
        { id: 1, name: 'Petra Svobodová', initials: 'PS' },
        { id: 7, name: 'Olga Vránová', initials: 'OV' },
        { id: 8, name: 'Radek Hájek', initials: 'RH' },
      ],
    },
  ],
  tasks: [
    {
      id: 11, title: 'Čištění datasetu', projectId: 2, projectName: 'Analýza dat: semestrální práce',
      deadline: '2026-10-24', priority: 'high', status: 'inProgress',
    },
    {
      id: 12, title: 'Wireframy obrazovek', projectId: 1, projectName: 'Rezervační systém knihovny',
      deadline: '2026-10-30', priority: 'medium', status: 'inProgress',
    },
    {
      id: 13, title: 'Regresní model', projectId: 2, projectName: 'Analýza dat: semestrální práce',
      deadline: '2026-11-06', priority: 'medium', status: 'todo',
    },
    {
      id: 14, title: 'Sběr dat', projectId: 2, projectName: 'Analýza dat: semestrální práce',
      deadline: '2026-10-15', priority: 'low', status: 'done',
    },
  ],
}

/// <summary>
/// Načte projekty a úkoly přihlášeného studenta.
/// TODO: nahradit voláním GET /api/projects/mine.
/// </summary>
export async function getMyProjects(signal?: AbortSignal): Promise<MyProjectsData> {
  await new Promise((resolve) => window.setTimeout(resolve, 300))
  signal?.throwIfAborted()
  return mockData
}
