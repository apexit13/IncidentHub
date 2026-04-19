import { useState } from 'react';
import { Button } from '../ui/Button';

interface NewIncidentModalProps {
  onClose: () => void;
  onSubmit: (f: { title: string; description: string; severity: string }) => void;
  isSubmitting: boolean;
}

export function NewIncidentModal({ onClose, onSubmit, isSubmitting }: NewIncidentModalProps) {
  const [form, setForm] = useState({ title: "", description: "", severity: "Medium" });
  const [errors, setErrors] = useState<Record<string, string>>({});

  function validate() {
    const e: Record<string, string> = {};
    if (!form.title.trim()) e.title = "Title is required";
    else if (form.title.trim().length < 5) e.title = "Title must be at least 5 characters";
    return e;
  }

  function handleSubmit() {
    const e = validate();
    if (Object.keys(e).length > 0) { setErrors(e); return; }
    onSubmit(form);
  }

  const SEV_BTN: Record<string, string> = {
    Critical: "border-red-500 bg-red-500 text-white",
    High:     "border-orange-500 bg-orange-500 text-white",
    Medium:   "border-yellow-400 bg-yellow-400 text-gray-900",
    Low:      "border-green-500 bg-green-500 text-white",
  };

  return (
    <div
      className="fixed inset-0 bg-black/50 flex items-center justify-center z-1000 p-5"
      onClick={e => e.target === e.currentTarget && onClose()}
    >
      <div className="bg-white rounded-xl w-full max-w-lg shadow-2xl">
        <div className="px-6 pt-5 pb-4 border-b border-gray-200 flex justify-between items-center">
          <div>
            <div className="text-base font-extrabold text-gray-800">Raise Incident</div>
            <div className="text-xs text-gray-500 mt-0.5">Saved to database and broadcast via SignalR</div>
          </div>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 text-xl transition-colors cursor-pointer">×</button>
        </div>

        <div className="px-6 py-5 flex flex-col gap-4">
          <div>
            <label className="block text-xs font-bold text-gray-600 mb-1">Incident Title *</label>
            <input
              value={form.title}
              onChange={e => { setForm(f => ({ ...f, title: e.target.value })); setErrors(er => ({ ...er, title: "" })); }}
              placeholder="Brief, actionable description of the incident"
              className={`w-full border rounded px-3 py-2 text-sm outline-none transition-colors focus:border-blue-500 ${
                errors.title ? "border-red-400" : "border-gray-300"
              }`}
            />
            {errors.title && <div className="text-xs text-red-500 mt-1">{errors.title}</div>}
          </div>

          <div>
            <label className="block text-xs font-bold text-gray-600 mb-1">Description</label>
            <textarea
              value={form.description}
              onChange={e => setForm(f => ({ ...f, description: e.target.value }))}
              placeholder="What's happening? What's the impact? Any initial hypotheses?"
              rows={3}
              className="w-full border border-gray-300 rounded px-3 py-2 text-sm outline-none resize-y focus:border-blue-500 transition-colors leading-relaxed"
            />
          </div>

          <div>
            <label className="block text-xs font-bold text-gray-600 mb-2">Severity</label>
            <div className="flex gap-2">
              {["Low", "Medium", "High", "Critical"].map(s => (
                <button
                  key={s}
                  onClick={() => setForm(f => ({ ...f, severity: s }))}
                  className={`flex-1 py-2 rounded border-2 text-xs font-bold transition-colors cursor-pointer ${
                    form.severity === s ? SEV_BTN[s] : "border-gray-200 bg-white text-gray-500 hover:bg-gray-50"
                  }`}
                >
                  {s}
                </button>
              ))}
            </div>
          </div>
        </div>

        <div className="px-6 pb-5 flex gap-2.5 justify-end">
          <Button onClick={onClose} variant="secondary">
            Cancel
          </Button>
          <Button
            onClick={handleSubmit}
            disabled={isSubmitting}
          >
            {isSubmitting ? "Raising…" : "Raise Incident"}
          </Button>
        </div>
      </div>
    </div>
  );
}