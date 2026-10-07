// Paleta definida pelo projeto; não representa classificação política.
export const PARTY_MAP_COLORS = {
  PT: '#D7191C',
  PL: '#225EA8',
  MDB: '#1A9850',
  PSD: '#54278F',
  PP: '#00A6CA',
  UNIAO: '#2C7FB8',
  REPUBLICANOS: '#253494',
  PSB: '#F03B20',
  PDT: '#41B6C4',
  PSDB: '#3690C0',
  NOVO: '#F7941D',
  PSOL: '#FDBF11',
  REDE: '#66BD63',
  PODE: '#7A5195',
  PCdoB: '#B2182B',
  PV: '#008837',
  SOLIDARIEDADE: '#F46D43',
  PRD: '#4575B4',
  CIDADANIA: '#FDAE61',
  UP: '#762A83',
  MISSAO: '#E6B800',
} as const;

const normalizeParty = (party: string) =>
  party
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .trim()
    .toUpperCase();

const colorsByParty = new Map(
  Object.entries(PARTY_MAP_COLORS).map(([party, color]) => [normalizeParty(party), color]),
);

export function partyColor(party: string): string {
  return colorsByParty.get(normalizeParty(party)) ?? '#69777D';
}
