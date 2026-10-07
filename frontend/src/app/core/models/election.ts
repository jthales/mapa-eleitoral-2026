import { partyColor } from './party-colors';

export type DataSource = 'mock' | 'tse';
export type DataStatus = 'fresh' | 'stale';
export type ElectionStatus = 'not_started' | 'in_progress' | 'completed';
export interface CandidateResult {
  photoUrl?: string | null;
  id: string;
  number: string;
  name: string;
  party: string;
  votes: number | null;
  percentage: number | null;
  officialStatus: string | null;
}
export interface ElectionResult {
  electionId: string | null;
  round: number;
  status: ElectionStatus;
  updatedAt: string;
  fetchedAt: string;
  processedPercentage: number | null;
  totalSections: number | null;
  processedSections: number | null;
  candidates: CandidateResult[];
  source: DataSource;
  dataStatus: DataStatus;
  hasData: boolean;
  votesReleased: boolean;
  candidatesOnly?: boolean;
}
export interface StateElectionResult {
  uf: string;
  name: string;
  electionId: string | null;
  round: number;
  status: ElectionStatus;
  updatedAt: string;
  processedPercentage: number | null;
  candidates: CandidateResult[];
  leadingCandidateId: string | null;
  marginPercentagePoints: number | null;
  source: DataSource;
  dataStatus: DataStatus;
  hasData: boolean;
  votesReleased: boolean;
  candidatesOnly?: boolean;
}
export interface StateElectionDetails {
  result: StateElectionResult;
  municipalitiesAvailable: boolean;
}

export function candidateColors(candidates: CandidateResult[]): Record<string, string> {
  return Object.fromEntries(
    candidates.map((candidate) => [candidate.id, partyColor(candidate.party)]),
  );
}
export function statusLabel(status: ElectionStatus): string {
  return {
    not_started: 'Aguardando totalização',
    in_progress: 'Totalização em andamento',
    completed: 'Totalização concluída',
  }[status];
}
