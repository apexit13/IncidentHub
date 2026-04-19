import type { Status } from '../../types/incidents';

interface FiltersBarProps {
  filter: string;
  setFilter: (s: string) => void;
  search: string;
  setSearch: (s: string) => void;
}

export function FiltersBar({ filter, setFilter, search, setSearch }: FiltersBarProps) {
  const statuses: (Status | "All")[] = ["All", "New", "Investigating", "Identified", "Monitoring", "Resolved"];

  const handleClearSearch = () => setSearch("");
  
  return (
    <div className="flex gap-2.5 items-center flex-wrap">
      <div className="relative flex-1 min-w-30">
        <input
          value={search}
          onChange={e => setSearch(e.target.value)}
          placeholder="Search incidents…"
          className="w-full border border-gray-300 rounded px-3 py-1.5 pr-8 text-sm outline-none focus:border-blue-500 transition-colors"
        />
        {search && (
          <button
            onClick={handleClearSearch}
            className="absolute right-2 top-1/2 transform -translate-y-1/2 text-gray-400 hover:text-gray-600 text-sm font-bold cursor-pointer"
            aria-label="Clear search"
          >
            ×
          </button>
        )}
      </div>
      <div className="flex gap-1 flex-wrap">
        {statuses.map(s => (
          <button
            key={s}
            onClick={() => setFilter(s)}
            className={`px-3 py-1.5 rounded text-xs font-semibold border transition-colors cursor-pointer ${
              filter === s
                ? "border-blue-700 bg-blue-700 text-white"
                : "border-gray-300 bg-white text-gray-600 hover:bg-gray-50"
            }`}
          >
            {s}
          </button>
        ))}
      </div>
    </div>
  );
}